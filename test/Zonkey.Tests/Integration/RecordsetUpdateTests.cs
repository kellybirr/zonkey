#if !NETFRAMEWORK
using System;
using System.Data;
using System.Threading.Tasks;
using Xunit;
using Zonkey.Ado;
using Zonkey.Tests.Infrastructure;

namespace Zonkey.Tests.Integration
{
    /// <summary>
    /// Recordset's write path (InitUpdate / UpdateBatch), which persists through
    /// DataTableAdapter and therefore needs a provider with DbDataAdapter support --
    /// MSSQL and PostgreSQL here. Cursor mechanics are covered by the SQLite-backed
    /// Unit/RecordsetTests.
    /// </summary>
    public abstract class RecordsetUpdateTests<TFixture> : IClassFixture<TFixture>
        where TFixture : class, IDatabaseFixture
    {
        protected readonly TFixture Db;

        protected RecordsetUpdateTests(TFixture db) => Db = db;

        /// <summary>
        /// Recordset loads via DataTable.Load, which applies the provider's reader
        /// schema. SqlClient flags the identity column and leaves the rest writable;
        /// Npgsql marks every column read-only and does not flag serial columns.
        /// Normalize so the same flow runs on both providers.
        /// </summary>
        private static void NormalizeSchemaFlags(Recordset rs)
        {
            foreach (DataColumn c in rs.Fields)
                c.ReadOnly = false;
            rs.Fields["speciesid"].AutoIncrement = true;
        }

        [Fact]
        public async Task Update_ModifyCurrentRecord_Persists()
        {
            if (!Db.IsAvailable) Assert.Skip(Db.SkipReason);

            using var conn = Db.CreateConnection();
            var rs = new Recordset(conn);
            await rs.Open("SELECT * FROM species");
            rs.InitUpdate("species", "speciesid");
            NormalizeSchemaFlags(rs);

            Assert.True(rs.FindNext("name = 'African Penguin'"));
            rs["classification"] = "Aves (updated)";
            Assert.Equal(1, await rs.UpdateBatch());

            await rs.Requery();
            Assert.True(rs.FindNext("name = 'African Penguin'"));
            Assert.Equal("Aves (updated)", rs["classification"]);
        }

        [Fact]
        public async Task Insert_NewRow_GetsIdentityBack_ThenDeleteRemovesIt()
        {
            if (!Db.IsAvailable) Assert.Skip(Db.SkipReason);

            using var conn = Db.CreateConnection();
            var rs = new Recordset(conn);
            int before = await rs.Open("SELECT * FROM species");
            rs.InitUpdate("species", "speciesid");
            NormalizeSchemaFlags(rs);

            var row = rs.NewRow();
            row["name"] = "Quokka";
            row["isendangered"] = false;
            rs.AddRow(row);
            Assert.Equal(1, await rs.UpdateBatch());

            int newId = Convert.ToInt32(row["speciesid"]);
            Assert.True(newId > 0, "identity value should be selected back into the new row");

            Assert.Equal(before + 1, await rs.Requery());
            rs.InitUpdate("species", "speciesid");
            NormalizeSchemaFlags(rs);
            Assert.True(rs.FindNext($"speciesid = {newId}"));
            rs.Delete();
            Assert.Equal(1, await rs.UpdateBatch());

            Assert.Equal(before, await rs.Requery());
        }
    }
}
#endif
