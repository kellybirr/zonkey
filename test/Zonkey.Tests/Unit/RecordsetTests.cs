#if !NETFRAMEWORK
using System;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Xunit;
using Zonkey.Ado;

namespace Zonkey.Tests.Unit
{
    /// <summary>
    /// Cursor mechanics of the classic-ADO-style Recordset against an in-memory
    /// SQLite database. UpdateBatch requires a provider with DbDataAdapter support
    /// (which Microsoft.Data.Sqlite lacks), so the write path is covered by the
    /// MSSQL/PostgreSQL integration tests; everything up to that point lives here.
    /// </summary>
    public class RecordsetTests : IDisposable
    {
        private readonly SqliteConnection _conn;

        public RecordsetTests()
        {
            _conn = new SqliteConnection("Data Source=:memory:");
            _conn.Open();

            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE Critters (Id INTEGER PRIMARY KEY, Name TEXT NOT NULL, Score INTEGER NOT NULL);
                INSERT INTO Critters VALUES (1, 'Ziggy', 10), (2, 'Bongo', 20), (3, 'Pip', 30);";
            cmd.ExecuteNonQuery();
        }

        public void Dispose() => _conn.Dispose();

        private async Task<Recordset> OpenAll()
        {
            var rs = new Recordset(_conn);
            await rs.Open("SELECT * FROM Critters ORDER BY Id");
            return rs;
        }

        // ---- opening ----

        [Fact]
        public async Task Open_ReturnsRecordCount_AndLandsOnFirstRecord()
        {
            var rs = await OpenAll();

            Assert.Equal(3, rs.RecordCount);
            Assert.Equal(0, rs.Position);
            Assert.False(rs.BOF);
            Assert.False(rs.EOF);
        }

        [Fact]
        public async Task Open_WithParameters_BindsPlaceholders()
        {
            var rs = new Recordset(_conn);
            int count = await rs.Open("SELECT * FROM Critters WHERE Score > $0", 15);

            Assert.Equal(2, count);
            Assert.Equal("Bongo", rs["Name"]);
        }

        [Fact]
        public async Task Open_EmptyResult_IsBofAndEof()
        {
            var rs = new Recordset(_conn);
            int count = await rs.Open("SELECT * FROM Critters WHERE Id > 999");

            Assert.Equal(0, count);
            Assert.True(rs.BOF);
            Assert.True(rs.EOF);
        }

        [Fact]
        public async Task Open_WithoutConnection_Throws()
        {
            var rs = new Recordset((System.Data.Common.DbConnection)null);
            await Assert.ThrowsAsync<InvalidOperationException>(() => rs.Open("SELECT 1"));
        }

        [Fact]
        public async Task BeforeOpen_StateIsInert()
        {
            var rs = new Recordset(_conn);

            Assert.Equal(-1, rs.RecordCount);
            Assert.True(rs.BOF);
            Assert.True(rs.EOF);
            Assert.Throws<InvalidOperationException>(() => rs.Fields);
            Assert.Throws<InvalidOperationException>(() => rs.MoveFirst());
            Assert.Throws<InvalidOperationException>(() => rs.FindNext("Id = 1"));
            Assert.Throws<InvalidOperationException>(() => rs.NewRow());
            await Assert.ThrowsAsync<InvalidOperationException>(() => rs.Requery());
            Assert.Throws<InvalidOperationException>(() => rs.InitUpdate("Critters", "Id"));
        }

        // ---- field access ----

        [Fact]
        public async Task Indexers_ReadByNameAndOrdinal_WriteByName()
        {
            var rs = await OpenAll();

            Assert.Equal("Ziggy", rs["Name"]);
            Assert.Equal(1L, rs[0]);

            rs["Score"] = 99;
            Assert.Equal(99L, Convert.ToInt64(rs["Score"]));
        }

        [Fact]
        public async Task Indexers_AtEof_Throw()
        {
            var rs = await OpenAll();
            rs.Move(3);

            Assert.True(rs.EOF);
            Assert.Throws<InvalidOperationException>(() => rs["Name"]);
            Assert.Throws<InvalidOperationException>(() => rs["Name"] = "x");
            Assert.Throws<InvalidOperationException>(() => rs[0]);
        }

        [Fact]
        public async Task Fields_ExposesColumnSchema()
        {
            var rs = await OpenAll();

            Assert.Equal(3, rs.Fields.Count);
            Assert.True(rs.Fields.Contains("Name"));
            Assert.Equal(typeof(long), rs.Fields["Id"].DataType);
        }

        // ---- navigation ----

        [Fact]
        public async Task MoveNext_WalksToEof_ReturningValidity()
        {
            var rs = await OpenAll();

            Assert.True(rs.MoveNext());    // -> Bongo
            Assert.True(rs.MoveNext());    // -> Pip
            Assert.False(rs.MoveNext());   // -> EOF
            Assert.True(rs.EOF);
        }

        [Fact]
        public async Task MovePrevious_BeforeFirst_IsBof()
        {
            var rs = await OpenAll();

            Assert.False(rs.MovePrevious());
            Assert.True(rs.BOF);
        }

        [Fact]
        public async Task Move_OffsetsRelative_InBothDirections()
        {
            var rs = await OpenAll();

            Assert.True(rs.Move(2));
            Assert.Equal("Pip", rs["Name"]);
            Assert.True(rs.Move(-1));
            Assert.Equal("Bongo", rs["Name"]);
        }

        [Fact]
        public async Task MoveFirst_ResetsFromAnywhere()
        {
            var rs = await OpenAll();
            rs.Move(2);

            Assert.True(rs.MoveFirst());
            Assert.Equal(0, rs.Position);
            Assert.Equal("Ziggy", rs["Name"]);
        }

        [Fact]
        public async Task MoveLast_LandsOnLastRecord()
        {
            var rs = await OpenAll();

            Assert.True(rs.MoveLast());
            Assert.Equal(rs.RecordCount - 1, rs.Position);
            Assert.False(rs.EOF);
            Assert.Equal("Pip", rs["Name"]);
        }

        [Fact]
        public async Task MoveLast_OnEmptyResult_ReturnsFalse()
        {
            var rs = new Recordset(_conn);
            await rs.Open("SELECT * FROM Critters WHERE Id > 999");

            Assert.False(rs.MoveLast());
            Assert.True(rs.BOF);
        }

        [Fact]
        public async Task FindNext_SearchesForwardFromCurrentPosition()
        {
            var rs = await OpenAll();

            Assert.True(rs.FindNext("Score >= 20")); // matches current-or-later
            Assert.Equal("Bongo", rs["Name"]);

            rs.MoveNext();                            // move past Bongo -> Pip
            Assert.True(rs.FindNext("Score >= 20")); // still matches at Pip
            Assert.Equal("Pip", rs["Name"]);

            rs.MoveNext();
            Assert.False(rs.FindNext("Score >= 20")); // nothing at/after EOF
        }

        [Fact]
        public async Task FindNext_NoMatch_LeavesPositionUnchanged()
        {
            var rs = await OpenAll();

            Assert.False(rs.FindNext("Name = 'Nessie'"));
            Assert.Equal(0, rs.Position);
        }

        // ---- local edits ----

        [Fact]
        public async Task Delete_MakesCurrentRecordInaccessible_UntilMove()
        {
            var rs = await OpenAll();
            rs.Delete();

            Assert.Throws<DeletedRowInaccessibleException>(() => rs["Name"]);

            rs.MoveNext();
            Assert.Equal("Bongo", rs["Name"]);
        }

        [Fact]
        public async Task AddRow_AppendsAndMovesCursorToNewRow()
        {
            var rs = await OpenAll();

            var row = rs.NewRow();
            row["Id"] = 4;
            row["Name"] = "Newt";
            row["Score"] = 40;
            int position = rs.AddRow(row);

            Assert.Equal(3, position);
            Assert.Equal(position, rs.Position);
            Assert.Equal(4, rs.RecordCount);
            Assert.Equal("Newt", rs["Name"]);
        }

        [Fact]
        public async Task AddRow_Null_Throws()
        {
            var rs = await OpenAll();
            Assert.Throws<ArgumentNullException>(() => rs.AddRow(null));
        }

        // ---- requery ----

        [Fact]
        public async Task Requery_RefreshesDataAndResetsCursor()
        {
            var rs = await OpenAll();
            rs.Move(2);

            using (var cmd = _conn.CreateCommand())
            {
                cmd.CommandText = "INSERT INTO Critters VALUES (4, 'Newt', 40)";
                cmd.ExecuteNonQuery();
            }

            int count = await rs.Requery();

            Assert.Equal(4, count);
            Assert.Equal(0, rs.Position);
        }

        // ---- update guards (everything before the DataTableAdapter hand-off) ----

        [Fact]
        public async Task UpdateBatch_WithoutInitUpdate_Throws()
        {
            var rs = await OpenAll();
            await Assert.ThrowsAsync<InvalidOperationException>(() => rs.UpdateBatch());
        }

        [Fact]
        public async Task InitUpdate_SetsTableNameAndPrimaryKey()
        {
            var rs = await OpenAll();
            rs.InitUpdate("Critters", "Id");

            Assert.Equal("Id", Assert.Single(rs.Fields["Id"].Table!.PrimaryKey).ColumnName);
        }

        // ---- lifecycle ----

        [Fact]
        public async Task Dispose_ClosesTheConnection()
        {
            var conn = new SqliteConnection("Data Source=:memory:");
            conn.Open();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "CREATE TABLE T (Id INTEGER)";
                cmd.ExecuteNonQuery();
            }

            var rs = new Recordset(conn);
            await rs.Open("SELECT * FROM T");
            rs.Dispose();

            Assert.Equal(ConnectionState.Closed, conn.State);
            Assert.True(rs.BOF); // recordset is fully reset
        }

        [Fact]
        public async Task Close_IsSameAsDispose()
        {
            var conn = new SqliteConnection("Data Source=:memory:");
            conn.Open();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "CREATE TABLE T (Id INTEGER)";
                cmd.ExecuteNonQuery();
            }

            var rs = new Recordset(conn);
            await rs.Open("SELECT * FROM T");
            rs.Close();

            Assert.Equal(ConnectionState.Closed, conn.State);
        }
    }
}
#endif
