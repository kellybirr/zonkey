#if !NETFRAMEWORK
using System;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Zonkey.Ado;
using Zonkey.Tests.Infrastructure;

namespace Zonkey.Tests.Integration
{
    /// <summary>
    /// DataTableAdapter fill and SaveChanges behavior against a live database.
    /// Runs on MSSQL and PostgreSQL only: Microsoft.Data.Sqlite does not implement
    /// DbDataAdapter, which DataTableAdapter builds on.
    /// Lowercase identifiers throughout so the same SQL works on SQL Server
    /// (case-insensitive) and PostgreSQL (folds unquoted to lowercase).
    /// </summary>
    public abstract class DataTableAdapterTests<TFixture> : IClassFixture<TFixture>
        where TFixture : class, IDatabaseFixture
    {
        protected readonly TFixture Db;

        protected DataTableAdapterTests(TFixture db) => Db = db;

        /// <summary>SQL Server rejects an empty SELECT list; PostgreSQL accepts it.</summary>
        protected virtual bool EmptyProjectionThrows => true;

        /// <summary>
        /// Provider schema metadata differs after a SELECT *: SqlClient flags the
        /// identity column (AutoIncrement + ReadOnly) and leaves data columns
        /// writable, while Npgsql marks every column read-only and does not flag
        /// serial columns at all. Normalize so the same flow runs on both.
        /// </summary>
        private static void PrepareForSave(DataTable dt)
        {
            dt.TableName = "species";
            dt.PrimaryKey = new[] { dt.Columns["speciesid"] };
            dt.Columns["speciesid"].AutoIncrement = true;
            foreach (DataColumn c in dt.Columns)
                if (c.ColumnName != "speciesid")
                    c.ReadOnly = false;
        }

        [Fact]
        public void Fill_ColumnsDefineTheProjection()
        {
            if (!Db.IsAvailable) Assert.Skip(Db.SkipReason);

            using var conn = Db.CreateConnection();
            var dt = new DataTable("species");
            dt.Columns.Add("speciesid", typeof(int));
            dt.Columns.Add("name", typeof(string));

            var adapter = new DataTableAdapter(conn);
            adapter.Fill(dt, "name = $0", "Red Panda");

            var row = Assert.Single(dt.Rows.Cast<DataRow>());
            Assert.Equal("Red Panda", row["name"]);
            Assert.Equal(2, dt.Columns.Count); // only the requested columns came back
        }

        [Fact]
        public void FillAll_ReturnsAllRows()
        {
            if (!Db.IsAvailable) Assert.Skip(Db.SkipReason);

            using var conn = Db.CreateConnection();
            var dt = new DataTable("species");
            dt.Columns.Add("speciesid", typeof(int));
            dt.Columns.Add("name", typeof(string));
            dt.Columns.Add("isendangered", typeof(bool));

            new DataTableAdapter(conn).FillAll(dt);

            Assert.True(dt.Rows.Count >= 3, $"expected at least the 3 seeded species, got {dt.Rows.Count}");
        }

        [Fact]
        public void FillAll_WithoutColumns_YieldsNoUsableData()
        {
            if (!Db.IsAvailable) Assert.Skip(Db.SkipReason);

            // The SELECT list is built from the DataTable's columns, so a fresh
            // column-less table produces an empty projection: SQL Server rejects
            // the statement outright, PostgreSQL accepts it but returns no columns.
            // Documented in docs/data-table-adapter.md.
            using var conn = Db.CreateConnection();
            var adapter = new DataTableAdapter(conn);
            var dt = new DataTable("species");

            if (EmptyProjectionThrows)
            {
                Assert.ThrowsAny<DbException>(() => adapter.FillAll(dt));
            }
            else
            {
                adapter.FillAll(dt);
                Assert.Empty(dt.Columns);
            }
        }

        [Fact]
        public void Fill_WithSqlFilters_AppliesAllConditions()
        {
            if (!Db.IsAvailable) Assert.Skip(Db.SkipReason);

            using var conn = Db.CreateConnection();
            var dt = new DataTable("species");
            dt.Columns.Add("name", typeof(string));

            new DataTableAdapter(conn).Fill(dt,
                SqlFilter.EQ("isendangered", true),
                SqlFilter.NEQ("name", "Axolotl"));

            var row = Assert.Single(dt.Rows.Cast<DataRow>());
            Assert.Equal("Red Panda", row["name"]);
        }

        [Fact]
        public async Task RuntimeSchema_SelectStar_SupportsFullCrud()
        {
            if (!Db.IsAvailable) Assert.Skip(Db.SkipReason);

            using var conn = Db.CreateConnection();

            // Discover the schema from the query itself -- no columns declared anywhere.
            var dt = new DataTable();
            await new DataManager(conn).FillDataTable(dt, "SELECT * FROM species", CommandType.Text);
            int seededCount = dt.Rows.Count;

            // Declare what the result set cannot reveal (table, key) and normalize
            // provider-specific schema flags -- see PrepareForSave.
            PrepareForSave(dt);

            // UPDATE an existing row and INSERT a new one in a single batch
            var axolotl = dt.Select("name = 'Axolotl'")[0];
            axolotl["classification"] = "Amphibia";

            var added = dt.NewRow();
            added["name"] = "Fennec Fox";
            added["isendangered"] = false;
            dt.Rows.Add(added);

            var adapter = new DataTableAdapter(conn);
            Assert.Equal(2, adapter.SaveChanges(dt));

            int newId = Convert.ToInt32(added["speciesid"]);
            Assert.True(newId > 0, "identity value should be selected back into the new row");

            // Verify persistence with a fresh load, then DELETE the inserted row
            var check = new DataTable();
            await new DataManager(conn).FillDataTable(check, "SELECT * FROM species", CommandType.Text);
            Assert.Equal(seededCount + 1, check.Rows.Count);
            Assert.Equal("Amphibia", check.Select("name = 'Axolotl'")[0]["classification"]);

            PrepareForSave(check);
            check.Select($"speciesid = {newId}")[0].Delete();
            Assert.Equal(1, adapter.SaveChanges(check));
        }

        [Fact]
        public void SaveChanges_BeforeSaveChangesCancel_ThrowsAndSavesNothing()
        {
            if (!Db.IsAvailable) Assert.Skip(Db.SkipReason);

            using var conn = Db.CreateConnection();
            var dt = new DataTable("species");
            dt.Columns.Add("speciesid", typeof(int));
            dt.Columns.Add("name", typeof(string));

            var adapter = new DataTableAdapter(conn);
            adapter.Fill(dt, "name = $0", "Red Panda");
            dt.PrimaryKey = new[] { dt.Columns["speciesid"] };
            dt.Rows[0]["name"] = "Renamed Panda";

            adapter.BeforeSaveChanges += (_, args) => args.Cancel = true;

            Assert.Throws<OperationCanceledException>(() => adapter.SaveChanges(dt));

            // the row is still dirty and the database untouched
            Assert.Equal(DataRowState.Modified, dt.Rows[0].RowState);
            var check = new DataTable("species");
            check.Columns.Add("name", typeof(string));
            new DataTableAdapter(conn).Fill(check, "name = $0", "Red Panda");
            Assert.Single(check.Rows.Cast<DataRow>());
        }
    }
}
#endif
