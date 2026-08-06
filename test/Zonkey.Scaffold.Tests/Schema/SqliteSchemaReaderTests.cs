using Microsoft.Data.Sqlite;
using Xunit;
using Zonkey.Scaffold.Schema;

public class SqliteSchemaReaderTests : IAsyncLifetime
{
    private string _dbPath = "";
    private string _connectionString = "";
    private DatabaseSchema _schema = null!;

    public async ValueTask InitializeAsync()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"zscaffold-{Guid.NewGuid():N}.db");
        _connectionString = $"Data Source={_dbPath}";

        await using var cnxn = new SqliteConnection(_connectionString);
        await cnxn.OpenAsync();

        await using var cmd = cnxn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE Species (
                SpeciesId INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                Classification TEXT,
                IsEndangered INTEGER NOT NULL
            );
            CREATE TABLE Zookeeper (
                ZookeeperId TEXT NOT NULL PRIMARY KEY,
                FirstName TEXT NOT NULL,
                Email TEXT
            );
            CREATE TABLE Animal (
                AnimalId INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                SpeciesId INTEGER NOT NULL,
                ZookeeperId TEXT NOT NULL,
                Weight REAL,
                FOREIGN KEY (SpeciesId) REFERENCES Species(SpeciesId),
                FOREIGN KEY (ZookeeperId) REFERENCES Zookeeper(ZookeeperId)
            );
            CREATE TABLE FeedingSchedule (
                AnimalId INTEGER NOT NULL,
                DayOfWeek INTEGER NOT NULL,
                TimeSlot TEXT NOT NULL,
                Quantity REAL NOT NULL,
                PRIMARY KEY (AnimalId, DayOfWeek, TimeSlot)
            );
            CREATE TABLE Cage (
                CageId INTEGER PRIMARY KEY,
                Label TEXT NOT NULL
            ) WITHOUT ROWID;
            CREATE TABLE Enclosure (
                EnclosureId INTEGER PRIMARY KEY AUTOINCREMENT,
                SpeciesId INTEGER NOT NULL,
                FOREIGN KEY (SpeciesId) REFERENCES Species
            );
            CREATE TABLE FeedingLog (
                LogId INTEGER PRIMARY KEY AUTOINCREMENT,
                AnimalId INTEGER NOT NULL,
                DayOfWeek INTEGER NOT NULL,
                TimeSlot TEXT NOT NULL,
                LoggedAt TEXT NOT NULL,
                FOREIGN KEY (AnimalId, DayOfWeek, TimeSlot) REFERENCES FeedingSchedule (AnimalId, DayOfWeek, TimeSlot)
            );
            CREATE UNIQUE INDEX UX_Species_Name ON Species(Name);
            CREATE VIEW AnimalNames AS SELECT AnimalId, Name FROM Animal;
            """;
        await cmd.ExecuteNonQueryAsync();

        var reader = new SqliteSchemaReader(_connectionString);
        _schema = await reader.Read(["main"], CancellationToken.None);
    }

    public ValueTask DisposeAsync()
    {
        SqliteConnection.ClearAllPools();
        if (File.Exists(_dbPath)) File.Delete(_dbPath);
        return ValueTask.CompletedTask;
    }

    private TableInfo Table(string name) => _schema.Tables.Single(t => t.Name == name);

    [Fact]
    public void Reports_sqlite_as_provider() => Assert.Equal("sqlite", _schema.Provider);

    [Fact]
    public void Single_schema_named_main()
    {
        var reader = new SqliteSchemaReader(_connectionString);
        Assert.Equal(["main"], reader.GetNonSystemSchemas(CancellationToken.None).Result);
    }

    [Fact]
    public void Reads_all_user_tables_and_views()
    {
        Assert.Contains(_schema.Tables, t => t.Name == "Species" && t.Kind == TableKind.Table);
        Assert.Contains(_schema.Tables, t => t.Name == "AnimalNames" && t.Kind == TableKind.View);
    }

    [Fact]
    public void Skips_sqlite_internal_tables()
        => Assert.DoesNotContain(_schema.Tables, t => t.Name.StartsWith("sqlite_"));

    [Fact]
    public void Reads_columns_in_ordinal_order()
    {
        var cols = Table("Species").Columns;
        Assert.Equal(["SpeciesId", "Name", "Classification", "IsEndangered"], cols.Select(c => c.Name));
        Assert.Equal([0, 1, 2, 3], cols.Select(c => c.Ordinal));
    }

    [Fact]
    public void Reads_nullability()
    {
        var cols = Table("Species").Columns;
        Assert.False(cols.Single(c => c.Name == "Name").IsNullable);
        Assert.True(cols.Single(c => c.Name == "Classification").IsNullable);
    }

    [Fact]
    public void Reads_native_types()
    {
        Assert.Equal("INTEGER", Table("Species").Columns.Single(c => c.Name == "SpeciesId").NativeType);
        Assert.Equal("REAL", Table("Animal").Columns.Single(c => c.Name == "Weight").NativeType);
    }

    [Fact]
    public void Reads_single_column_primary_key()
        => Assert.Equal(["SpeciesId"], Table("Species").PrimaryKey);

    [Fact]
    public void Reads_composite_primary_key_in_declared_order()
        => Assert.Equal(["AnimalId", "DayOfWeek", "TimeSlot"], Table("FeedingSchedule").PrimaryKey);

    [Fact]
    public void Detects_autoincrement_identity()
    {
        Assert.True(Table("Species").Columns.Single(c => c.Name == "SpeciesId").IsIdentity);
        Assert.False(Table("Zookeeper").Columns.Single(c => c.Name == "ZookeeperId").IsIdentity);
    }

    [Fact]
    public void Without_rowid_integer_primary_key_is_not_identity()
        => Assert.False(Table("Cage").Columns.Single(c => c.Name == "CageId").IsIdentity);

    [Fact]
    public void Reads_foreign_keys()
    {
        var fks = Table("Animal").ForeignKeys;
        Assert.Equal(2, fks.Count);

        var speciesFk = fks.Single(f => f.ReferencedTable == "Species");
        Assert.Equal(["SpeciesId"], speciesFk.Columns);
        Assert.Equal(["SpeciesId"], speciesFk.ReferencedColumns);
    }

    [Fact]
    public void Resolves_implicit_foreign_key_target_to_referenced_primary_key()
    {
        var fk = Table("Enclosure").ForeignKeys.Single();

        Assert.Equal(fk.Columns.Count, fk.ReferencedColumns.Count);
        Assert.Equal(["SpeciesId"], fk.Columns);
        Assert.Equal(["SpeciesId"], fk.ReferencedColumns);
    }

    [Fact]
    public void Reads_composite_foreign_key_in_declared_order()
    {
        var fk = Table("FeedingLog").ForeignKeys.Single();

        Assert.Equal(["AnimalId", "DayOfWeek", "TimeSlot"], fk.Columns);
        Assert.Equal(["AnimalId", "DayOfWeek", "TimeSlot"], fk.ReferencedColumns);
    }

    [Fact]
    public void Reads_unique_constraints()
    {
        var uq = Table("Species").UniqueConstraints;
        Assert.Contains(uq, u => u.Columns.SequenceEqual(new[] { "Name" }));
    }

    [Fact]
    public void Primary_key_enforcing_autoindex_is_not_reported_as_a_unique_constraint()
    {
        Assert.Empty(Table("Zookeeper").UniqueConstraints);
        Assert.Empty(Table("FeedingSchedule").UniqueConstraints);
    }

    [Fact]
    public void Tables_are_ordered_deterministically()
        => Assert.Equal(
            _schema.Tables.Select(t => t.Name).OrderBy(n => n, StringComparer.Ordinal),
            _schema.Tables.Select(t => t.Name));
}
