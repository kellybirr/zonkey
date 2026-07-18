# Test Reengineering Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the broken AdventureWorks-based MSTest suite with a zoo-themed xUnit test suite that runs against SQLite (always), MSSQL, and PostgreSQL (via Docker/CI).

**Architecture:** Single test project with generic base classes for integration tests. Fixtures manage database lifecycle. SQLite runs everywhere; MSSQL/PostgreSQL skip gracefully when unavailable.

**Tech Stack:** xUnit v3, Microsoft.Data.Sqlite, Npgsql, Microsoft.Data.SqlClient, Docker Compose, GitHub Actions

**Spec:** `docs/superpowers/specs/2026-03-11-test-reengineering-design.md`

---

## Chunk 1: Project Scaffolding, Infrastructure, Models, Seed SQL

### Task 1: Create test project

**Files:**
- Create: `test/Zonkey.Tests/Zonkey.Tests.csproj`

- [ ] **Step 1: Create directory structure**

```bash
mkdir -p test/Zonkey.Tests/{Infrastructure,Models,Seed,Unit,Integration/{Sqlite,Mssql,Pgsql}}
```

- [ ] **Step 2: Create the csproj file**

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFrameworks>net10.0;net48</TargetFrameworks>
    <IsPackable>false</IsPackable>
    <RootNamespace>Zonkey.Tests</RootNamespace>
    <AssemblyName>Zonkey.Tests</AssemblyName>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="xunit.v3" Version="1.*" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.*">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
    </PackageReference>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="18.*" />
    <PackageReference Include="Microsoft.Data.Sqlite" Version="9.*" />
    <PackageReference Include="Npgsql" Version="9.*" />
    <PackageReference Include="Microsoft.Data.SqlClient" Version="6.*" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\Zonkey.Data\Zonkey.Data.csproj" />
    <ProjectReference Include="..\..\src\Zonkey.Data.MsSql\Zonkey.Data.MsSql.csproj" />
  </ItemGroup>

  <ItemGroup>
    <None Include="Seed\*.sql" CopyToOutputDirectory="PreserveNewest" />
  </ItemGroup>

</Project>
```

- [ ] **Step 3: Verify it builds**

Run: `dotnet build test/Zonkey.Tests/Zonkey.Tests.csproj`
Expected: Build succeeded (no tests yet, but project compiles)

- [ ] **Step 4: Commit**

```bash
git add test/Zonkey.Tests/Zonkey.Tests.csproj
git commit -m "feat: add Zonkey.Tests xUnit project scaffolding"
```

---

### Task 2: Create infrastructure (IDatabaseFixture, TestConfiguration)

**Files:**
- Create: `test/Zonkey.Tests/Infrastructure/IDatabaseFixture.cs`
- Create: `test/Zonkey.Tests/Infrastructure/TestConfiguration.cs`

- [ ] **Step 1: Create IDatabaseFixture interface**

```csharp
using System;
using System.Data.Common;
using System.Threading.Tasks;
using Zonkey.Dialects;

namespace Zonkey.Tests.Infrastructure
{
    public interface IDatabaseFixture : IAsyncLifetime
    {
        bool IsAvailable { get; }
        string SkipReason { get; }
        SqlDialect Dialect { get; }
        bool SupportsRowVersion { get; }
        DbConnection CreateConnection();
    }
}
```

Note: xUnit v3 `IAsyncLifetime` provides `ValueTask InitializeAsync()` and `ValueTask DisposeAsync()`. If xUnit v3 uses different signatures, adjust accordingly. The key contract is: initialize creates/seeds the DB, dispose drops it.

- [ ] **Step 2: Create TestConfiguration**

```csharp
using System;

namespace Zonkey.Tests.Infrastructure
{
    public static class TestConfiguration
    {
        public static string MssqlConnectionString =>
            Environment.GetEnvironmentVariable("ZONKEY_TEST_MSSQL")
            ?? "Server=localhost,1433;User=sa;Password=Zonkey#Test123;TrustServerCertificate=true";

        public static string PgsqlConnectionString =>
            Environment.GetEnvironmentVariable("ZONKEY_TEST_PGSQL")
            ?? "Host=localhost;Port=5432;Username=zonkey;Password=zonkey";
    }
}
```

- [ ] **Step 3: Verify build**

Run: `dotnet build test/Zonkey.Tests/Zonkey.Tests.csproj`
Expected: Build succeeded

- [ ] **Step 4: Commit**

```bash
git add test/Zonkey.Tests/Infrastructure/
git commit -m "feat: add IDatabaseFixture interface and TestConfiguration"
```

---

### Task 3: Create zoo data models

**Files:**
- Create: `test/Zonkey.Tests/Models/Species.cs`
- Create: `test/Zonkey.Tests/Models/Exhibit.cs`
- Create: `test/Zonkey.Tests/Models/Zookeeper.cs`
- Create: `test/Zonkey.Tests/Models/Animal.cs`
- Create: `test/Zonkey.Tests/Models/FeedingSchedule.cs`

**Important:** Check how existing AdventureWorks model classes derive from `DataClass`. They may use `DataClass` (non-generic) or `DataClass<T>` (generic). Match the existing pattern. The examples below assume non-generic `DataClass`. The constructor `DataClass(bool addingNew)` sets initial `DataRowState` — `true` means `Added`, `false` means `Unchanged`. Provide both a parameterless constructor (for ORM instantiation, defaults to `addingNew: true`) and one with the bool parameter.

- [ ] **Step 1: Create Species model**

```csharp
using System.Data;
using Zonkey.ObjectModel;

namespace Zonkey.Tests.Models
{
    [DataItem("Species")]
    public class Species : DataClass
    {
        private int _speciesId;
        private string _name;
        private string _classification;
        private bool _isEndangered;

        public Species() : base(true) { }
        public Species(bool addingNew) : base(addingNew) { }

        [DataField("SpeciesId", DbType.Int32, IsKeyField = true, IsAutoIncrement = true)]
        public int SpeciesId
        {
            get => _speciesId;
            set => SetFieldValue(ref _speciesId, value);
        }

        [DataField("Name", DbType.String)]
        public string Name
        {
            get => _name;
            set => SetFieldValue(ref _name, value);
        }

        [DataField("Classification", DbType.String, true)]
        public string Classification
        {
            get => _classification;
            set => SetFieldValue(ref _classification, value);
        }

        [DataField("IsEndangered", DbType.Boolean)]
        public bool IsEndangered
        {
            get => _isEndangered;
            set => SetFieldValue(ref _isEndangered, value);
        }
    }
}
```

- [ ] **Step 2: Create Exhibit model**

```csharp
using System.Data;
using Zonkey.ObjectModel;

namespace Zonkey.Tests.Models
{
    [DataItem("Exhibit")]
    public class Exhibit : DataClass
    {
        private int _exhibitId;
        private string _name;
        private string _location;
        private int _capacity;
        private bool _isOpen;
        private byte[] _rowVersion;

        public Exhibit() : base(true) { }
        public Exhibit(bool addingNew) : base(addingNew) { }

        [DataField("ExhibitId", DbType.Int32, IsKeyField = true, IsAutoIncrement = true)]
        public int ExhibitId
        {
            get => _exhibitId;
            set => SetFieldValue(ref _exhibitId, value);
        }

        [DataField("Name", DbType.String)]
        public string Name
        {
            get => _name;
            set => SetFieldValue(ref _name, value);
        }

        [DataField("Location", DbType.String, true)]
        public string Location
        {
            get => _location;
            set => SetFieldValue(ref _location, value);
        }

        [DataField("Capacity", DbType.Int32)]
        public int Capacity
        {
            get => _capacity;
            set => SetFieldValue(ref _capacity, value);
        }

        [DataField("IsOpen", DbType.Boolean)]
        public bool IsOpen
        {
            get => _isOpen;
            set => SetFieldValue(ref _isOpen, value);
        }

        [DataField("RowVersion", DbType.Binary, IsRowVersion = true)]
        public byte[] RowVersion
        {
            get => _rowVersion;
            set => SetFieldValue(ref _rowVersion, value);
        }
    }
}
```

- [ ] **Step 3: Create Zookeeper model**

```csharp
using System;
using System.Data;
using Zonkey.ObjectModel;

namespace Zonkey.Tests.Models
{
    [DataItem("Zookeeper")]
    public class Zookeeper : DataClass
    {
        private Guid _zookeeperId;
        private string _firstName;
        private string _lastName;
        private string _email;
        private DateTime _hireDate;
        private string _specialty;

        public Zookeeper() : base(true) { }
        public Zookeeper(bool addingNew) : base(addingNew) { }

        [DataField("ZookeeperId", DbType.Guid, IsKeyField = true)]
        public Guid ZookeeperId
        {
            get => _zookeeperId;
            set => SetFieldValue(ref _zookeeperId, value);
        }

        [DataField("FirstName", DbType.String)]
        public string FirstName
        {
            get => _firstName;
            set => SetFieldValue(ref _firstName, value);
        }

        [DataField("LastName", DbType.String)]
        public string LastName
        {
            get => _lastName;
            set => SetFieldValue(ref _lastName, value);
        }

        [DataField("Email", DbType.String, true)]
        public string Email
        {
            get => _email;
            set => SetFieldValue(ref _email, value);
        }

        [DataField("HireDate", DbType.Date)]
        public DateTime HireDate
        {
            get => _hireDate;
            set => SetFieldValue(ref _hireDate, value);
        }

        [DataField("Specialty", DbType.String, true)]
        public string Specialty
        {
            get => _specialty;
            set => SetFieldValue(ref _specialty, value);
        }
    }
}
```

- [ ] **Step 4: Create Animal model**

```csharp
using System;
using System.Data;
using Zonkey.ObjectModel;

namespace Zonkey.Tests.Models
{
    [DataItem("Animal")]
    public class Animal : DataClass
    {
        private int _animalId;
        private string _name;
        private int _speciesId;
        private int? _exhibitId;
        private Guid _zookeeperId;
        private DateTime? _dateOfBirth;
        private decimal? _weight;
        private string _notes;

        public Animal() : base(true) { }
        public Animal(bool addingNew) : base(addingNew) { }

        [DataField("AnimalId", DbType.Int32, IsKeyField = true, IsAutoIncrement = true)]
        public int AnimalId
        {
            get => _animalId;
            set => SetFieldValue(ref _animalId, value);
        }

        [DataField("Name", DbType.String)]
        public string Name
        {
            get => _name;
            set => SetFieldValue(ref _name, value);
        }

        [DataField("SpeciesId", DbType.Int32)]
        public int SpeciesId
        {
            get => _speciesId;
            set => SetFieldValue(ref _speciesId, value);
        }

        [DataField("ExhibitId", DbType.Int32, true)]
        public int? ExhibitId
        {
            get => _exhibitId;
            set => SetFieldValue(ref _exhibitId, value);
        }

        [DataField("ZookeeperId", DbType.Guid)]
        public Guid ZookeeperId
        {
            get => _zookeeperId;
            set => SetFieldValue(ref _zookeeperId, value);
        }

        [DataField("DateOfBirth", DbType.DateTime, true)]
        public DateTime? DateOfBirth
        {
            get => _dateOfBirth;
            set => SetFieldValue(ref _dateOfBirth, value);
        }

        [DataField("Weight", DbType.Decimal, true)]
        public decimal? Weight
        {
            get => _weight;
            set => SetFieldValue(ref _weight, value);
        }

        [DataField("Notes", DbType.String, true, IsComparable = false)]
        public string Notes
        {
            get => _notes;
            set => SetFieldValue(ref _notes, value);
        }
    }
}
```

- [ ] **Step 5: Create FeedingSchedule model (composite key)**

```csharp
using System;
using System.Data;
using Zonkey.ObjectModel;

namespace Zonkey.Tests.Models
{
    [DataItem("FeedingSchedule")]
    public class FeedingSchedule : DataClass
    {
        private int _animalId;
        private int _dayOfWeek;
        private string _timeSlot;
        private string _foodType;
        private decimal _quantity;
        private Guid? _assignedKeeperId;

        public FeedingSchedule() : base(true) { }
        public FeedingSchedule(bool addingNew) : base(addingNew) { }

        [DataField("AnimalId", DbType.Int32, IsKeyField = true)]
        public int AnimalId
        {
            get => _animalId;
            set => SetFieldValue(ref _animalId, value);
        }

        [DataField("DayOfWeek", DbType.Int32, IsKeyField = true)]
        public int DayOfWeek
        {
            get => _dayOfWeek;
            set => SetFieldValue(ref _dayOfWeek, value);
        }

        [DataField("TimeSlot", DbType.String, IsKeyField = true)]
        public string TimeSlot
        {
            get => _timeSlot;
            set => SetFieldValue(ref _timeSlot, value);
        }

        [DataField("FoodType", DbType.String)]
        public string FoodType
        {
            get => _foodType;
            set => SetFieldValue(ref _foodType, value);
        }

        [DataField("Quantity", DbType.Decimal)]
        public decimal Quantity
        {
            get => _quantity;
            set => SetFieldValue(ref _quantity, value);
        }

        [DataField("AssignedKeeperId", DbType.Guid, true)]
        public Guid? AssignedKeeperId
        {
            get => _assignedKeeperId;
            set => SetFieldValue(ref _assignedKeeperId, value);
        }
    }
}
```

- [ ] **Step 6: Verify build**

Run: `dotnet build test/Zonkey.Tests/Zonkey.Tests.csproj`
Expected: Build succeeded

- [ ] **Step 7: Commit**

```bash
git add test/Zonkey.Tests/Models/
git commit -m "feat: add zoo-themed data model classes"
```

---

### Task 4: Create seed SQL scripts and docker-compose

**Files:**
- Create: `test/Zonkey.Tests/Seed/sqlite-seed.sql`
- Create: `test/Zonkey.Tests/Seed/mssql-seed.sql`
- Create: `test/Zonkey.Tests/Seed/pgsql-seed.sql`
- Create: `docker-compose.yml`

**Known test GUIDs:**
- Zookeeper 1: `A1B2C3D4-E5F6-7890-ABCD-EF1234567890`
- Zookeeper 2: `E5F6A7B8-C9D0-1234-5678-9ABCDEF01234`

- [ ] **Step 1: Create sqlite-seed.sql**

```sql
CREATE TABLE Species (
    SpeciesId INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,
    Classification TEXT,
    IsEndangered INTEGER NOT NULL
);

CREATE TABLE Exhibit (
    ExhibitId INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,
    Location TEXT,
    Capacity INTEGER NOT NULL,
    IsOpen INTEGER NOT NULL DEFAULT 1
);

CREATE TABLE Zookeeper (
    ZookeeperId TEXT NOT NULL PRIMARY KEY,
    FirstName TEXT NOT NULL,
    LastName TEXT NOT NULL,
    Email TEXT,
    HireDate TEXT NOT NULL,
    Specialty TEXT
);

CREATE TABLE Animal (
    AnimalId INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,
    SpeciesId INTEGER NOT NULL,
    ExhibitId INTEGER,
    ZookeeperId TEXT NOT NULL,
    DateOfBirth TEXT,
    Weight REAL,
    Notes TEXT,
    FOREIGN KEY (SpeciesId) REFERENCES Species(SpeciesId),
    FOREIGN KEY (ExhibitId) REFERENCES Exhibit(ExhibitId),
    FOREIGN KEY (ZookeeperId) REFERENCES Zookeeper(ZookeeperId)
);

CREATE TABLE FeedingSchedule (
    AnimalId INTEGER NOT NULL,
    DayOfWeek INTEGER NOT NULL,
    TimeSlot TEXT NOT NULL,
    FoodType TEXT NOT NULL,
    Quantity REAL NOT NULL,
    AssignedKeeperId TEXT,
    PRIMARY KEY (AnimalId, DayOfWeek, TimeSlot),
    FOREIGN KEY (AnimalId) REFERENCES Animal(AnimalId),
    FOREIGN KEY (AssignedKeeperId) REFERENCES Zookeeper(ZookeeperId)
);

-- Seed data
INSERT INTO Species (Name, Classification, IsEndangered) VALUES ('Red Panda', 'Mammalia', 1);
INSERT INTO Species (Name, Classification, IsEndangered) VALUES ('African Penguin', 'Aves', 0);
INSERT INTO Species (Name, Classification, IsEndangered) VALUES ('Axolotl', NULL, 1);

INSERT INTO Exhibit (Name, Location, Capacity, IsOpen) VALUES ('Bamboo Grove', 'Building A', 5, 1);
INSERT INTO Exhibit (Name, Location, Capacity, IsOpen) VALUES ('Aquatic House', 'Building B', 20, 1);

INSERT INTO Zookeeper (ZookeeperId, FirstName, LastName, Email, HireDate, Specialty)
VALUES ('A1B2C3D4-E5F6-7890-ABCD-EF1234567890', 'Jane', 'Goodall', 'jane@zoo.org', '2020-01-15', 'Mammals');
INSERT INTO Zookeeper (ZookeeperId, FirstName, LastName, Email, HireDate, Specialty)
VALUES ('E5F6A7B8-C9D0-1234-5678-9ABCDEF01234', 'Steve', 'Irwin', NULL, '2019-06-01', 'Reptiles');

INSERT INTO Animal (Name, SpeciesId, ExhibitId, ZookeeperId, DateOfBirth, Weight, Notes)
VALUES ('Mei Mei', 1, 1, 'A1B2C3D4-E5F6-7890-ABCD-EF1234567890', '2021-03-15', 5.50, 'Loves bamboo shoots');
INSERT INTO Animal (Name, SpeciesId, ExhibitId, ZookeeperId, DateOfBirth, Weight, Notes)
VALUES ('Waddles', 2, 2, 'E5F6A7B8-C9D0-1234-5678-9ABCDEF01234', '2020-11-20', 3.20, NULL);
INSERT INTO Animal (Name, SpeciesId, ExhibitId, ZookeeperId, DateOfBirth, Weight, Notes)
VALUES ('Bubbles', 3, 2, 'E5F6A7B8-C9D0-1234-5678-9ABCDEF01234', NULL, NULL, 'Very rare pink coloring');
INSERT INTO Animal (Name, SpeciesId, ExhibitId, ZookeeperId, DateOfBirth, Weight, Notes)
VALUES ('Bao Bao', 1, NULL, 'A1B2C3D4-E5F6-7890-ABCD-EF1234567890', '2023-07-01', 4.80, NULL);

INSERT INTO FeedingSchedule (AnimalId, DayOfWeek, TimeSlot, FoodType, Quantity, AssignedKeeperId)
VALUES (1, 1, 'morning', 'Bamboo', 2.50, 'A1B2C3D4-E5F6-7890-ABCD-EF1234567890');
INSERT INTO FeedingSchedule (AnimalId, DayOfWeek, TimeSlot, FoodType, Quantity, AssignedKeeperId)
VALUES (1, 1, 'evening', 'Fruit Mix', 1.00, NULL);
INSERT INTO FeedingSchedule (AnimalId, DayOfWeek, TimeSlot, FoodType, Quantity, AssignedKeeperId)
VALUES (2, 1, 'morning', 'Fish', 3.00, 'E5F6A7B8-C9D0-1234-5678-9ABCDEF01234');
INSERT INTO FeedingSchedule (AnimalId, DayOfWeek, TimeSlot, FoodType, Quantity, AssignedKeeperId)
VALUES (2, 3, 'morning', 'Fish', 3.50, 'E5F6A7B8-C9D0-1234-5678-9ABCDEF01234');
INSERT INTO FeedingSchedule (AnimalId, DayOfWeek, TimeSlot, FoodType, Quantity, AssignedKeeperId)
VALUES (3, 2, 'morning', 'Bloodworms', 0.50, 'E5F6A7B8-C9D0-1234-5678-9ABCDEF01234');
INSERT INTO FeedingSchedule (AnimalId, DayOfWeek, TimeSlot, FoodType, Quantity, AssignedKeeperId)
VALUES (4, 1, 'morning', 'Bamboo', 2.00, 'A1B2C3D4-E5F6-7890-ABCD-EF1234567890');
```

- [ ] **Step 2: Create mssql-seed.sql**

Uses `GO` batch separators so it can be processed by `SqlScriptProcessor` or split manually. Note: `SqlScriptProcessor` splits on `\r\nGO\r\n` (case-insensitive). Ensure this file uses CRLF line endings, or adapt the fixture to use a more flexible split pattern.

```sql
CREATE TABLE Species (
    SpeciesId INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    Classification NVARCHAR(50) NULL,
    IsEndangered BIT NOT NULL
);
GO

CREATE TABLE Exhibit (
    ExhibitId INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    Location NVARCHAR(200) NULL,
    Capacity INT NOT NULL,
    IsOpen BIT NOT NULL DEFAULT 1,
    RowVersion ROWVERSION
);
GO

CREATE TABLE Zookeeper (
    ZookeeperId UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    FirstName NVARCHAR(50) NOT NULL,
    LastName NVARCHAR(50) NOT NULL,
    Email NVARCHAR(200) NULL,
    HireDate DATE NOT NULL,
    Specialty NVARCHAR(100) NULL
);
GO

CREATE TABLE Animal (
    AnimalId INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    SpeciesId INT NOT NULL,
    ExhibitId INT NULL,
    ZookeeperId UNIQUEIDENTIFIER NOT NULL,
    DateOfBirth DATETIME2 NULL,
    Weight DECIMAL(8,2) NULL,
    Notes NVARCHAR(MAX) NULL,
    FOREIGN KEY (SpeciesId) REFERENCES Species(SpeciesId),
    FOREIGN KEY (ExhibitId) REFERENCES Exhibit(ExhibitId),
    FOREIGN KEY (ZookeeperId) REFERENCES Zookeeper(ZookeeperId)
);
GO

CREATE TABLE FeedingSchedule (
    AnimalId INT NOT NULL,
    DayOfWeek INT NOT NULL,
    TimeSlot NVARCHAR(10) NOT NULL,
    FoodType NVARCHAR(100) NOT NULL,
    Quantity DECIMAL(6,2) NOT NULL,
    AssignedKeeperId UNIQUEIDENTIFIER NULL,
    PRIMARY KEY (AnimalId, DayOfWeek, TimeSlot),
    FOREIGN KEY (AnimalId) REFERENCES Animal(AnimalId),
    FOREIGN KEY (AssignedKeeperId) REFERENCES Zookeeper(ZookeeperId)
);
GO

SET IDENTITY_INSERT Species ON;
INSERT INTO Species (SpeciesId, Name, Classification, IsEndangered) VALUES (1, 'Red Panda', 'Mammalia', 1);
INSERT INTO Species (SpeciesId, Name, Classification, IsEndangered) VALUES (2, 'African Penguin', 'Aves', 0);
INSERT INTO Species (SpeciesId, Name, Classification, IsEndangered) VALUES (3, 'Axolotl', NULL, 1);
SET IDENTITY_INSERT Species OFF;
GO

SET IDENTITY_INSERT Exhibit ON;
INSERT INTO Exhibit (ExhibitId, Name, Location, Capacity, IsOpen) VALUES (1, 'Bamboo Grove', 'Building A', 5, 1);
INSERT INTO Exhibit (ExhibitId, Name, Location, Capacity, IsOpen) VALUES (2, 'Aquatic House', 'Building B', 20, 1);
SET IDENTITY_INSERT Exhibit OFF;
GO

INSERT INTO Zookeeper (ZookeeperId, FirstName, LastName, Email, HireDate, Specialty)
VALUES ('A1B2C3D4-E5F6-7890-ABCD-EF1234567890', 'Jane', 'Goodall', 'jane@zoo.org', '2020-01-15', 'Mammals');
INSERT INTO Zookeeper (ZookeeperId, FirstName, LastName, Email, HireDate, Specialty)
VALUES ('E5F6A7B8-C9D0-1234-5678-9ABCDEF01234', 'Steve', 'Irwin', NULL, '2019-06-01', 'Reptiles');
GO

SET IDENTITY_INSERT Animal ON;
INSERT INTO Animal (AnimalId, Name, SpeciesId, ExhibitId, ZookeeperId, DateOfBirth, Weight, Notes)
VALUES (1, 'Mei Mei', 1, 1, 'A1B2C3D4-E5F6-7890-ABCD-EF1234567890', '2021-03-15', 5.50, 'Loves bamboo shoots');
INSERT INTO Animal (AnimalId, Name, SpeciesId, ExhibitId, ZookeeperId, DateOfBirth, Weight, Notes)
VALUES (2, 'Waddles', 2, 2, 'E5F6A7B8-C9D0-1234-5678-9ABCDEF01234', '2020-11-20', 3.20, NULL);
INSERT INTO Animal (AnimalId, Name, SpeciesId, ExhibitId, ZookeeperId, DateOfBirth, Weight, Notes)
VALUES (3, 'Bubbles', 3, 2, 'E5F6A7B8-C9D0-1234-5678-9ABCDEF01234', NULL, NULL, 'Very rare pink coloring');
INSERT INTO Animal (AnimalId, Name, SpeciesId, ExhibitId, ZookeeperId, DateOfBirth, Weight, Notes)
VALUES (4, 'Bao Bao', 1, NULL, 'A1B2C3D4-E5F6-7890-ABCD-EF1234567890', '2023-07-01', 4.80, NULL);
SET IDENTITY_INSERT Animal OFF;
GO

INSERT INTO FeedingSchedule (AnimalId, DayOfWeek, TimeSlot, FoodType, Quantity, AssignedKeeperId)
VALUES (1, 1, 'morning', 'Bamboo', 2.50, 'A1B2C3D4-E5F6-7890-ABCD-EF1234567890');
INSERT INTO FeedingSchedule (AnimalId, DayOfWeek, TimeSlot, FoodType, Quantity, AssignedKeeperId)
VALUES (1, 1, 'evening', 'Fruit Mix', 1.00, NULL);
INSERT INTO FeedingSchedule (AnimalId, DayOfWeek, TimeSlot, FoodType, Quantity, AssignedKeeperId)
VALUES (2, 1, 'morning', 'Fish', 3.00, 'E5F6A7B8-C9D0-1234-5678-9ABCDEF01234');
INSERT INTO FeedingSchedule (AnimalId, DayOfWeek, TimeSlot, FoodType, Quantity, AssignedKeeperId)
VALUES (2, 3, 'morning', 'Fish', 3.50, 'E5F6A7B8-C9D0-1234-5678-9ABCDEF01234');
INSERT INTO FeedingSchedule (AnimalId, DayOfWeek, TimeSlot, FoodType, Quantity, AssignedKeeperId)
VALUES (3, 2, 'morning', 'Bloodworms', 0.50, 'E5F6A7B8-C9D0-1234-5678-9ABCDEF01234');
INSERT INTO FeedingSchedule (AnimalId, DayOfWeek, TimeSlot, FoodType, Quantity, AssignedKeeperId)
VALUES (4, 1, 'morning', 'Bamboo', 2.00, 'A1B2C3D4-E5F6-7890-ABCD-EF1234567890');
GO
```

- [ ] **Step 3: Create pgsql-seed.sql**

```sql
CREATE TABLE "Species" (
    "SpeciesId" SERIAL PRIMARY KEY,
    "Name" VARCHAR(100) NOT NULL,
    "Classification" VARCHAR(50),
    "IsEndangered" BOOLEAN NOT NULL
);

CREATE TABLE "Exhibit" (
    "ExhibitId" SERIAL PRIMARY KEY,
    "Name" VARCHAR(100) NOT NULL,
    "Location" VARCHAR(200),
    "Capacity" INT NOT NULL,
    "IsOpen" BOOLEAN NOT NULL DEFAULT TRUE
);

CREATE TABLE "Zookeeper" (
    "ZookeeperId" UUID NOT NULL PRIMARY KEY,
    "FirstName" VARCHAR(50) NOT NULL,
    "LastName" VARCHAR(50) NOT NULL,
    "Email" VARCHAR(200),
    "HireDate" DATE NOT NULL,
    "Specialty" VARCHAR(100)
);

CREATE TABLE "Animal" (
    "AnimalId" SERIAL PRIMARY KEY,
    "Name" VARCHAR(100) NOT NULL,
    "SpeciesId" INT NOT NULL REFERENCES "Species"("SpeciesId"),
    "ExhibitId" INT REFERENCES "Exhibit"("ExhibitId"),
    "ZookeeperId" UUID NOT NULL REFERENCES "Zookeeper"("ZookeeperId"),
    "DateOfBirth" TIMESTAMP,
    "Weight" NUMERIC(8,2),
    "Notes" TEXT
);

CREATE TABLE "FeedingSchedule" (
    "AnimalId" INT NOT NULL,
    "DayOfWeek" INT NOT NULL,
    "TimeSlot" VARCHAR(10) NOT NULL,
    "FoodType" VARCHAR(100) NOT NULL,
    "Quantity" NUMERIC(6,2) NOT NULL,
    "AssignedKeeperId" UUID REFERENCES "Zookeeper"("ZookeeperId"),
    PRIMARY KEY ("AnimalId", "DayOfWeek", "TimeSlot"),
    FOREIGN KEY ("AnimalId") REFERENCES "Animal"("AnimalId")
);

INSERT INTO "Species" ("Name", "Classification", "IsEndangered") VALUES ('Red Panda', 'Mammalia', TRUE);
INSERT INTO "Species" ("Name", "Classification", "IsEndangered") VALUES ('African Penguin', 'Aves', FALSE);
INSERT INTO "Species" ("Name", "Classification", "IsEndangered") VALUES ('Axolotl', NULL, TRUE);

INSERT INTO "Exhibit" ("Name", "Location", "Capacity", "IsOpen") VALUES ('Bamboo Grove', 'Building A', 5, TRUE);
INSERT INTO "Exhibit" ("Name", "Location", "Capacity", "IsOpen") VALUES ('Aquatic House', 'Building B', 20, TRUE);

INSERT INTO "Zookeeper" ("ZookeeperId", "FirstName", "LastName", "Email", "HireDate", "Specialty")
VALUES ('A1B2C3D4-E5F6-7890-ABCD-EF1234567890', 'Jane', 'Goodall', 'jane@zoo.org', '2020-01-15', 'Mammals');
INSERT INTO "Zookeeper" ("ZookeeperId", "FirstName", "LastName", "Email", "HireDate", "Specialty")
VALUES ('E5F6A7B8-C9D0-1234-5678-9ABCDEF01234', 'Steve', 'Irwin', NULL, '2019-06-01', 'Reptiles');

INSERT INTO "Animal" ("Name", "SpeciesId", "ExhibitId", "ZookeeperId", "DateOfBirth", "Weight", "Notes")
VALUES ('Mei Mei', 1, 1, 'A1B2C3D4-E5F6-7890-ABCD-EF1234567890', '2021-03-15', 5.50, 'Loves bamboo shoots');
INSERT INTO "Animal" ("Name", "SpeciesId", "ExhibitId", "ZookeeperId", "DateOfBirth", "Weight", "Notes")
VALUES ('Waddles', 2, 2, 'E5F6A7B8-C9D0-1234-5678-9ABCDEF01234', '2020-11-20', 3.20, NULL);
INSERT INTO "Animal" ("Name", "SpeciesId", "ExhibitId", "ZookeeperId", "DateOfBirth", "Weight", "Notes")
VALUES ('Bubbles', 3, 2, 'E5F6A7B8-C9D0-1234-5678-9ABCDEF01234', NULL, NULL, 'Very rare pink coloring');
INSERT INTO "Animal" ("Name", "SpeciesId", "ExhibitId", "ZookeeperId", "DateOfBirth", "Weight", "Notes")
VALUES ('Bao Bao', 1, NULL, 'A1B2C3D4-E5F6-7890-ABCD-EF1234567890', '2023-07-01', 4.80, NULL);

INSERT INTO "FeedingSchedule" ("AnimalId", "DayOfWeek", "TimeSlot", "FoodType", "Quantity", "AssignedKeeperId")
VALUES (1, 1, 'morning', 'Bamboo', 2.50, 'A1B2C3D4-E5F6-7890-ABCD-EF1234567890');
INSERT INTO "FeedingSchedule" ("AnimalId", "DayOfWeek", "TimeSlot", "FoodType", "Quantity", "AssignedKeeperId")
VALUES (1, 1, 'evening', 'Fruit Mix', 1.00, NULL);
INSERT INTO "FeedingSchedule" ("AnimalId", "DayOfWeek", "TimeSlot", "FoodType", "Quantity", "AssignedKeeperId")
VALUES (2, 1, 'morning', 'Fish', 3.00, 'E5F6A7B8-C9D0-1234-5678-9ABCDEF01234');
INSERT INTO "FeedingSchedule" ("AnimalId", "DayOfWeek", "TimeSlot", "FoodType", "Quantity", "AssignedKeeperId")
VALUES (2, 3, 'morning', 'Fish', 3.50, 'E5F6A7B8-C9D0-1234-5678-9ABCDEF01234');
INSERT INTO "FeedingSchedule" ("AnimalId", "DayOfWeek", "TimeSlot", "FoodType", "Quantity", "AssignedKeeperId")
VALUES (3, 2, 'morning', 'Bloodworms', 0.50, 'E5F6A7B8-C9D0-1234-5678-9ABCDEF01234');
INSERT INTO "FeedingSchedule" ("AnimalId", "DayOfWeek", "TimeSlot", "FoodType", "Quantity", "AssignedKeeperId")
VALUES (4, 1, 'morning', 'Bamboo', 2.00, 'A1B2C3D4-E5F6-7890-ABCD-EF1234567890');
```

**PostgreSQL column naming:** Uses quoted `"PascalCase"` identifiers to match the `[DataField]` `FieldName` values. PostgreSQL preserves case with double quotes. The model classes should set `UseQuotedIdentifier = true` on their `[DataItem]` attribute (e.g., `[DataItem("Species", UseQuotedIdentifier = true)]`) so the ORM generates quoted identifiers in SQL. This makes PostgreSQL work with PascalCase column names without changing anything else. Alternatively, set `UseQuotedIdentifier` on the adapter or command builder at runtime in the PostgreSQL fixture.

- [ ] **Step 4: Create docker-compose.yml**

```yaml
services:
  mssql:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      ACCEPT_EULA: "Y"
      MSSQL_SA_PASSWORD: "Zonkey#Test123"
    ports:
      - "1433:1433"
    healthcheck:
      test: /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "Zonkey#Test123" -C -Q "SELECT 1" || exit 1
      interval: 10s
      timeout: 5s
      retries: 10

  postgres:
    image: postgres:17
    environment:
      POSTGRES_USER: zonkey
      POSTGRES_PASSWORD: zonkey
      POSTGRES_DB: zonkey_test
    ports:
      - "5432:5432"
    healthcheck:
      test: pg_isready -U zonkey
      interval: 10s
      timeout: 5s
      retries: 5
```

- [ ] **Step 5: Commit**

```bash
git add test/Zonkey.Tests/Seed/ docker-compose.yml
git commit -m "feat: add seed SQL scripts and docker-compose"
```

---

## Chunk 2: Database Fixtures

### Task 5: Create SqliteFixture

**Files:**
- Create: `test/Zonkey.Tests/Infrastructure/SqliteFixture.cs`

- [ ] **Step 1: Write SqliteFixture**

```csharp
using System;
using System.Data.Common;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Zonkey.Dialects;

namespace Zonkey.Tests.Infrastructure
{
    public class SqliteFixture : IDatabaseFixture
    {
        private readonly string _dbPath;

        public bool IsAvailable => true;
        public string SkipReason => string.Empty;
        public SqlDialect Dialect { get; } = new SqliteDialect();
        public bool SupportsRowVersion => false;

        public SqliteFixture()
        {
            _dbPath = Path.Combine(Path.GetTempPath(), $"zonkey_test_{Guid.NewGuid():N}.db");
        }

        public DbConnection CreateConnection()
        {
            var conn = new SqliteConnection($"Data Source={_dbPath}");
            conn.Open();
            return conn;
        }

        public async ValueTask InitializeAsync()
        {
            var seedPath = Path.Combine(AppContext.BaseDirectory, "Seed", "sqlite-seed.sql");
            var sql = File.ReadAllText(seedPath);

            using var conn = CreateConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            await cmd.ExecuteNonQueryAsync();
        }

        public ValueTask DisposeAsync()
        {
            try
            {
                if (File.Exists(_dbPath))
                    File.Delete(_dbPath);
            }
            catch
            {
                // Best effort cleanup
            }
            return ValueTask.CompletedTask;
        }
    }
}
```

- [ ] **Step 2: Verify build**

Run: `dotnet build test/Zonkey.Tests/Zonkey.Tests.csproj`

- [ ] **Step 3: Commit**

```bash
git add test/Zonkey.Tests/Infrastructure/SqliteFixture.cs
git commit -m "feat: add SqliteFixture"
```

---

### Task 6: Create MssqlFixture

**Files:**
- Create: `test/Zonkey.Tests/Infrastructure/MssqlFixture.cs`

- [ ] **Step 1: Write MssqlFixture**

The MSSQL fixture creates a unique database, seeds it, and drops it on dispose. Uses `SqlScriptProcessor` from `Zonkey.Utility` to execute the GO-separated seed script. Falls back to manual batch splitting if `SqlScriptProcessor` closes the connection (it does — see `src/Zonkey.Data/Utility/SqlScriptProcessor.cs:96`). Alternative: split batches manually with `Regex.Split` mirroring `SqlScriptProcessor`'s logic but without closing the connection.

```csharp
using System;
using System.Data.Common;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Zonkey.Dialects;

namespace Zonkey.Tests.Infrastructure
{
    public class MssqlFixture : IDatabaseFixture
    {
        private readonly string _baseConnectionString;
        private readonly string _databaseName;

        public bool IsAvailable { get; private set; }
        public string SkipReason { get; private set; } = string.Empty;
        public SqlDialect Dialect { get; } = new SqlServerDialect();
        public bool SupportsRowVersion => true;

        public MssqlFixture()
        {
            _baseConnectionString = TestConfiguration.MssqlConnectionString;
            _databaseName = $"zonkey_test_{Guid.NewGuid():N}";
        }

        public DbConnection CreateConnection()
        {
            var conn = new SqlConnection($"{_baseConnectionString};Database={_databaseName}");
            conn.Open();
            return conn;
        }

        public async ValueTask InitializeAsync()
        {
            try
            {
                // Connect to master to create test database
                using (var masterConn = new SqlConnection($"{_baseConnectionString};Database=master"))
                {
                    await masterConn.OpenAsync();
                    using var cmd = masterConn.CreateCommand();
                    cmd.CommandText = $"CREATE DATABASE [{_databaseName}]";
                    await cmd.ExecuteNonQueryAsync();
                }

                // Seed the test database
                var seedPath = Path.Combine(AppContext.BaseDirectory, "Seed", "mssql-seed.sql");
                var sql = File.ReadAllText(seedPath);
                var batches = Regex.Split(sql, @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase);

                using var conn = CreateConnection();
                foreach (var batch in batches)
                {
                    var trimmed = batch.Trim();
                    if (string.IsNullOrEmpty(trimmed)) continue;

                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = trimmed;
                    cmd.CommandTimeout = 30;
                    await cmd.ExecuteNonQueryAsync();
                }

                IsAvailable = true;
            }
            catch (Exception ex)
            {
                IsAvailable = false;
                SkipReason = $"MSSQL not available: {ex.Message}. Set ZONKEY_TEST_MSSQL or run docker-compose up.";
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (!IsAvailable) return;

            try
            {
                using var conn = new SqlConnection($"{_baseConnectionString};Database=master");
                await conn.OpenAsync();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = $@"
                    IF DB_ID('{_databaseName}') IS NOT NULL
                    BEGIN
                        ALTER DATABASE [{_databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                        DROP DATABASE [{_databaseName}];
                    END";
                await cmd.ExecuteNonQueryAsync();
            }
            catch
            {
                // Best effort cleanup
            }
        }
    }
}
```

- [ ] **Step 2: Verify build**

Run: `dotnet build test/Zonkey.Tests/Zonkey.Tests.csproj`

- [ ] **Step 3: Commit**

```bash
git add test/Zonkey.Tests/Infrastructure/MssqlFixture.cs
git commit -m "feat: add MssqlFixture"
```

---

### Task 7: Create PgsqlFixture

**Files:**
- Create: `test/Zonkey.Tests/Infrastructure/PgsqlFixture.cs`

- [ ] **Step 1: Write PgsqlFixture**

```csharp
using System;
using System.Data.Common;
using System.IO;
using System.Threading.Tasks;
using Npgsql;
using Zonkey.Dialects;

namespace Zonkey.Tests.Infrastructure
{
    public class PgsqlFixture : IDatabaseFixture
    {
        private readonly string _baseConnectionString;
        private readonly string _databaseName;

        public bool IsAvailable { get; private set; }
        public string SkipReason { get; private set; } = string.Empty;
        public SqlDialect Dialect { get; } = new PostgreSqlDialect();
        public bool SupportsRowVersion => false;

        public PgsqlFixture()
        {
            _baseConnectionString = TestConfiguration.PgsqlConnectionString;
            _databaseName = $"zonkey_test_{Guid.NewGuid():N}";
        }

        public DbConnection CreateConnection()
        {
            var conn = new NpgsqlConnection($"{_baseConnectionString};Database={_databaseName}");
            conn.Open();
            return conn;
        }

        public async ValueTask InitializeAsync()
        {
            try
            {
                // Connect to default db to create test database
                using (var adminConn = new NpgsqlConnection($"{_baseConnectionString};Database=zonkey_test"))
                {
                    await adminConn.OpenAsync();
                    using var cmd = adminConn.CreateCommand();
                    cmd.CommandText = $"CREATE DATABASE \"{_databaseName}\"";
                    await cmd.ExecuteNonQueryAsync();
                }

                // Seed the test database
                var seedPath = Path.Combine(AppContext.BaseDirectory, "Seed", "pgsql-seed.sql");
                var sql = File.ReadAllText(seedPath);

                using var conn = CreateConnection();
                using var seedCmd = conn.CreateCommand();
                seedCmd.CommandText = sql;
                await seedCmd.ExecuteNonQueryAsync();

                IsAvailable = true;
            }
            catch (Exception ex)
            {
                IsAvailable = false;
                SkipReason = $"PostgreSQL not available: {ex.Message}. Set ZONKEY_TEST_PGSQL or run docker-compose up.";
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (!IsAvailable) return;

            try
            {
                using var conn = new NpgsqlConnection($"{_baseConnectionString};Database=zonkey_test");
                await conn.OpenAsync();

                // Terminate existing connections
                using var termCmd = conn.CreateCommand();
                termCmd.CommandText = $@"
                    SELECT pg_terminate_backend(pid)
                    FROM pg_stat_activity
                    WHERE datname = '{_databaseName}' AND pid <> pg_backend_pid()";
                await termCmd.ExecuteNonQueryAsync();

                using var dropCmd = conn.CreateCommand();
                dropCmd.CommandText = $"DROP DATABASE IF EXISTS \"{_databaseName}\"";
                await dropCmd.ExecuteNonQueryAsync();
            }
            catch
            {
                // Best effort cleanup
            }
        }
    }
}
```

- [ ] **Step 2: Verify entire project builds**

Run: `dotnet build test/Zonkey.Tests/Zonkey.Tests.csproj`
Expected: Build succeeded

- [ ] **Step 3: Commit**

```bash
git add test/Zonkey.Tests/Infrastructure/PgsqlFixture.cs
git commit -m "feat: add PgsqlFixture"
```

---

## Chunk 3: Unit Tests

### Task 8: DataClassTests

**Files:**
- Create: `test/Zonkey.Tests/Unit/DataClassTests.cs`
- Reference: `src/Zonkey.Data/ObjectModel/DataClass.cs`

- [ ] **Step 1: Write DataClassTests**

```csharp
using System.Data;
using Xunit;
using Zonkey.Tests.Models;

namespace Zonkey.Tests.Unit
{
    public class DataClassTests
    {
        [Fact]
        public void NewObject_StartsAsAdded()
        {
            var animal = new Animal();
            Assert.Equal(DataRowState.Added, animal.DataRowState);
        }

        [Fact]
        public void ObjectCreatedAsNotNew_StartsAsDetached()
        {
            var animal = new Animal(false);
            Assert.Equal(DataRowState.Detached, animal.DataRowState);
        }

        [Fact]
        public void CommitValues_TransitionsAddedToUnchanged()
        {
            var animal = new Animal(); // Added
            animal.Name = "Test";
            animal.CommitValues();
            Assert.Equal(DataRowState.Unchanged, animal.DataRowState);
        }

        [Fact]
        public void SetField_OnUnchanged_TransitionsToModified()
        {
            var animal = new Animal { Name = "Original" };
            animal.CommitValues(); // Now Unchanged

            animal.Name = "Updated";
            Assert.Equal(DataRowState.Modified, animal.DataRowState);
        }

        [Fact]
        public void SetField_TracksOriginalValue()
        {
            var animal = new Animal { Name = "Original" };
            animal.CommitValues();

            animal.Name = "Updated";
            Assert.True(animal.OriginalValues.ContainsKey("Name"));
        }

        [Fact]
        public void CommitValues_ResetsModifiedToUnchanged()
        {
            var animal = new Animal { Name = "Original" };
            animal.CommitValues();

            animal.Name = "Changed";
            Assert.Equal(DataRowState.Modified, animal.DataRowState);

            animal.CommitValues();
            Assert.Equal(DataRowState.Unchanged, animal.DataRowState);
        }

        [Fact]
        public void CommitValues_ClearsOriginalValues()
        {
            var animal = new Animal { Name = "Original" };
            animal.CommitValues();

            animal.Name = "Updated";
            Assert.NotEmpty(animal.OriginalValues);

            animal.CommitValues();
            Assert.Empty(animal.OriginalValues);
        }

        [Fact]
        public void MultipleFieldChanges_TrackedIndependently()
        {
            var animal = new Animal { Name = "Orig", SpeciesId = 1 };
            animal.CommitValues();

            animal.Name = "New Name";
            animal.SpeciesId = 2;

            Assert.True(animal.OriginalValues.ContainsKey("Name"));
            Assert.True(animal.OriginalValues.ContainsKey("SpeciesId"));
        }

        [Fact]
        public void SetField_OnAdded_StaysAdded()
        {
            var animal = new Animal(); // Added
            animal.Name = "Test";
            Assert.Equal(DataRowState.Added, animal.DataRowState);
        }
    }
}
```

- [ ] **Step 2: Run tests**

Run: `dotnet test test/Zonkey.Tests/Zonkey.Tests.csproj --filter "FullyQualifiedName~DataClassTests"`
Expected: All tests pass

- [ ] **Step 3: Commit**

```bash
git add test/Zonkey.Tests/Unit/DataClassTests.cs
git commit -m "test: add DataClass change tracking unit tests"
```

---

### Task 9: DataMapTests

**Files:**
- Create: `test/Zonkey.Tests/Unit/DataMapTests.cs`
- Reference: `src/Zonkey.Data/ObjectModel/DataMap.cs`

- [ ] **Step 1: Write DataMapTests**

```csharp
using System.Linq;
using Xunit;
using Zonkey.ObjectModel;
using Zonkey.Tests.Models;

namespace Zonkey.Tests.Unit
{
    public class DataMapTests
    {
        [Fact]
        public void GenerateNew_CreatesMapFromAttributedClass()
        {
            var map = DataMap.GenerateNew(typeof(Animal));
            Assert.NotNull(map);
            Assert.NotEmpty(map.DataFields);
        }

        [Fact]
        public void GenerateNew_DiscoversAllFields()
        {
            var map = DataMap.GenerateNew(typeof(Animal));
            // Animal has 8 properties: AnimalId, Name, SpeciesId, ExhibitId, ZookeeperId, DateOfBirth, Weight, Notes
            Assert.Equal(8, map.DataFields.Count);
        }

        [Fact]
        public void GenerateNew_IdentifiesSingleKeyField()
        {
            var map = DataMap.GenerateNew(typeof(Animal));
            Assert.Single(map.KeyFields);
            Assert.Equal("AnimalId", map.KeyFields[0].FieldName);
        }

        [Fact]
        public void GenerateNew_IdentifiesCompositeKey()
        {
            var map = DataMap.GenerateNew(typeof(FeedingSchedule));
            Assert.Equal(3, map.KeyFields.Count);
            var keyNames = map.KeyFields.Select(k => k.FieldName).ToList();
            Assert.Contains("AnimalId", keyNames);
            Assert.Contains("DayOfWeek", keyNames);
            Assert.Contains("TimeSlot", keyNames);
        }

        [Fact]
        public void GenerateNew_IdentifiesGuidKey()
        {
            var map = DataMap.GenerateNew(typeof(Zookeeper));
            Assert.Single(map.KeyFields);
            Assert.Equal("ZookeeperId", map.KeyFields[0].FieldName);
            Assert.Equal(System.Data.DbType.Guid, map.KeyFields[0].DataType);
        }

        [Fact]
        public void GenerateNew_DetectsAutoIncrement()
        {
            var map = DataMap.GenerateNew(typeof(Animal));
            var keyField = map.KeyFields[0];
            Assert.True(keyField.IsAutoIncrement);
        }

        [Fact]
        public void GenerateNew_DetectsRowVersion()
        {
            var map = DataMap.GenerateNew(typeof(Exhibit));
            var rvField = map.DataFields.FirstOrDefault(f => f.FieldName == "RowVersion");
            Assert.NotNull(rvField);
            Assert.True(rvField.IsRowVersion);
        }

        [Fact]
        public void GenerateNew_DetectsNullableFields()
        {
            var map = DataMap.GenerateNew(typeof(Animal));
            var exhibitField = map.DataFields.First(f => f.FieldName == "ExhibitId");
            Assert.True(exhibitField.IsNullable);

            var nameField = map.DataFields.First(f => f.FieldName == "Name");
            Assert.False(nameField.IsNullable);
        }

        [Fact]
        public void GetReadableField_FindsByName()
        {
            var map = DataMap.GenerateNew(typeof(Animal));
            var field = map.GetReadableField("Name");
            Assert.NotNull(field);
            Assert.Equal("Name", field.FieldName);
        }

        [Fact]
        public void GetReadableField_ReturnsNull_ForUnknownField()
        {
            var map = DataMap.GenerateNew(typeof(Animal));
            var field = map.GetReadableField("NonExistent");
            Assert.Null(field);
        }

        [Fact]
        public void ContainsField_ReturnsTrueForExistingField()
        {
            var map = DataMap.GenerateNew(typeof(Animal));
            Assert.True(map.ContainsField("Name"));
        }

        [Fact]
        public void ContainsField_ReturnsFalseForMissingField()
        {
            var map = DataMap.GenerateNew(typeof(Animal));
            Assert.False(map.ContainsField("NonExistent"));
        }

        [Fact]
        public void GenerateCached_ReturnsSameInstance()
        {
            var map1 = DataMap.GenerateCached(typeof(Species));
            var map2 = DataMap.GenerateCached(typeof(Species));
            Assert.Same(map1, map2);
        }

        [Fact]
        public void ReadableFields_ExcludesWriteOnlyFields()
        {
            var map = DataMap.GenerateNew(typeof(Animal));
            // All Animal fields are ReadWrite, so ReadableFields should equal DataFields
            Assert.Equal(map.DataFields.Count, map.ReadableFields.Count);
        }

        [Fact]
        public void IsComparable_False_ForNotesField()
        {
            var map = DataMap.GenerateNew(typeof(Animal));
            var notesField = map.DataFields.First(f => f.FieldName == "Notes");
            Assert.False(notesField.IsComparable);
        }
    }
}
```

- [ ] **Step 2: Run tests**

Run: `dotnet test test/Zonkey.Tests/Zonkey.Tests.csproj --filter "FullyQualifiedName~DataMapTests"`
Expected: All tests pass

- [ ] **Step 3: Commit**

```bash
git add test/Zonkey.Tests/Unit/DataMapTests.cs
git commit -m "test: add DataMap unit tests"
```

---

### Task 10: SqlFilterTests

**Files:**
- Create: `test/Zonkey.Tests/Unit/SqlFilterTests.cs`
- Reference: `src/Zonkey.Data/SqlFilter.cs`

- [ ] **Step 1: Write SqlFilterTests**

```csharp
using System.Data.Common;
using Xunit;
using Zonkey.Dialects;

namespace Zonkey.Tests.Unit
{
    public class SqlFilterTests
    {
        private readonly SqlDialect _sqlServer = new SqlServerDialect();
        private readonly SqlDialect _postgres = new PostgreSqlDialect();
        private readonly SqlDialect _generic = new GenericSqlDialect();

        [Fact]
        public void EQ_GeneratesEqualsClause()
        {
            var filter = SqlFilter.EQ("Name", "Test");
            var sql = filter.ToString(_generic, 0);
            Assert.Contains("Name", sql);
            Assert.Contains("=", sql);
        }

        [Fact]
        public void NEQ_GeneratesNotEqualsClause()
        {
            var filter = SqlFilter.NEQ("Name", "Test");
            var sql = filter.ToString(_generic, 0);
            Assert.Contains("!=", sql);
        }

        [Fact]
        public void GT_GeneratesGreaterThan()
        {
            var filter = SqlFilter.GT("Capacity", 10);
            var sql = filter.ToString(_generic, 0);
            Assert.Contains(">", sql);
        }

        [Fact]
        public void GTE_GeneratesGreaterThanOrEqual()
        {
            var filter = SqlFilter.GTE("Capacity", 10);
            var sql = filter.ToString(_generic, 0);
            Assert.Contains(">=", sql);
        }

        [Fact]
        public void LT_GeneratesLessThan()
        {
            var filter = SqlFilter.LT("Weight", 5.0m);
            var sql = filter.ToString(_generic, 0);
            Assert.Contains("<", sql);
        }

        [Fact]
        public void LTE_GeneratesLessThanOrEqual()
        {
            var filter = SqlFilter.LTE("Weight", 5.0m);
            var sql = filter.ToString(_generic, 0);
            Assert.Contains("<=", sql);
        }

        [Fact]
        public void NULL_GeneratesIsNull()
        {
            var filter = SqlFilter.NULL("ExhibitId");
            var sql = filter.ToString(_generic, 0);
            Assert.Contains("IS NULL", sql);
        }

        [Fact]
        public void NOTNULL_GeneratesIsNotNull()
        {
            var filter = SqlFilter.NOTNULL("ExhibitId");
            var sql = filter.ToString(_generic, 0);
            Assert.Contains("IS NOT NULL", sql);
        }

        [Fact]
        public void LIKE_GeneratesLikeClause()
        {
            var filter = SqlFilter.LIKE("Name", "%panda%");
            var sql = filter.ToString(_generic, 0);
            Assert.Contains("LIKE", sql);
        }

        [Fact]
        public void NOTLIKE_GeneratesNotLikeClause()
        {
            var filter = SqlFilter.NOTLIKE("Name", "%test%");
            var sql = filter.ToString(_generic, 0);
            Assert.Contains("NOT LIKE", sql);
        }

        [Fact]
        public void EQ_SqlServer_UsesNamedParameter()
        {
            var filter = SqlFilter.EQ("Name", "Test");
            var sql = filter.ToString(_sqlServer, 0);
            Assert.Contains("@", sql);
        }

        [Fact]
        public void EQ_FieldNamePreserved()
        {
            var filter = SqlFilter.EQ("SpeciesId", 1);
            Assert.Equal("SpeciesId", filter.FieldName);
        }

        [Fact]
        public void EQ_ValuePreserved()
        {
            var filter = SqlFilter.EQ("SpeciesId", 42);
            Assert.Equal(42, filter.Value);
        }

        [Fact]
        public void NULL_HasNullValue()
        {
            var filter = SqlFilter.NULL("ExhibitId");
            Assert.Null(filter.Value);
        }

        [Fact]
        public void ParameterIndex_AffectsParameterName()
        {
            var filter = SqlFilter.EQ("Name", "Test");
            var sql0 = filter.ToString(_sqlServer, 0);
            var sql1 = filter.ToString(_sqlServer, 1);
            Assert.NotEqual(sql0, sql1);
        }

        // FOLLOW-UP: Add tests for NGT, NLT, ILIKE, NOTILIKE, MATCH, NOTMATCH, IMATCH, NOTIMATCH,
        // AddToCommandParams, and multiple filters combined — same patterns as above.
    }
}
```

- [ ] **Step 2: Run tests**

Run: `dotnet test test/Zonkey.Tests/Zonkey.Tests.csproj --filter "FullyQualifiedName~SqlFilterTests"`
Expected: All tests pass

- [ ] **Step 3: Commit**

```bash
git add test/Zonkey.Tests/Unit/SqlFilterTests.cs
git commit -m "test: add SqlFilter unit tests"
```

---

### Task 11: DialectTests

**Files:**
- Create: `test/Zonkey.Tests/Unit/DialectTests.cs`
- Reference: `src/Zonkey.Data/Dialects/SqlServerDialect.cs`, `SqliteDialect.cs`, `PostgreSqlDialect.cs`, `MySqlDialect.cs`

- [ ] **Step 1: Write DialectTests**

```csharp
using System.Data;
using Xunit;
using Zonkey.Dialects;

namespace Zonkey.Tests.Unit
{
    public class DialectTests
    {
        // Feature flags

        [Fact]
        public void SqlServer_SupportsRowVersion() =>
            Assert.True(new SqlServerDialect().SupportsRowVersion);

        [Fact]
        public void SqlServer_SupportsSchema() =>
            Assert.True(new SqlServerDialect().SupportsSchema);

        [Fact]
        public void SqlServer_SupportsNoLock() =>
            Assert.True(new SqlServerDialect().SupportsNoLock);

        [Fact]
        public void SqlServer_SupportsLimit() =>
            Assert.True(new SqlServerDialect().SupportsLimit);

        [Fact]
        public void Sqlite_DoesNotSupportRowVersion() =>
            Assert.False(new SqliteDialect().SupportsRowVersion);

        [Fact]
        public void Sqlite_SupportsLimit() =>
            Assert.True(new SqliteDialect().SupportsLimit);

        [Fact]
        public void Postgres_SupportsLimit() =>
            Assert.True(new PostgreSqlDialect().SupportsLimit);

        [Fact]
        public void Postgres_DoesNotSupportRowVersion() =>
            Assert.False(new PostgreSqlDialect().SupportsRowVersion);

        // Field name formatting

        [Fact]
        public void SqlServer_FormatsFieldName_WithBrackets()
        {
            var dialect = new SqlServerDialect();
            var result = dialect.FormatFieldName("Name", true);
            Assert.Equal("[Name]", result);
        }

        [Fact]
        public void Sqlite_FormatsFieldName_WithBrackets()
        {
            var dialect = new SqliteDialect();
            var result = dialect.FormatFieldName("Name", true);
            Assert.Equal("[Name]", result);
        }

        [Fact]
        public void MySql_FormatsFieldName_WithBackticks()
        {
            var dialect = new MySqlDialect();
            var result = dialect.FormatFieldName("Name", true);
            Assert.Contains("`", result);
        }

        // Auto-increment

        [Fact]
        public void SqlServer_AutoIncrement_UsesScopeIdentity()
        {
            var dialect = new SqlServerDialect();
            var result = dialect.FormatAutoIncrementSelect(null);
            Assert.Contains("SCOPE_IDENTITY", result);
        }

        [Fact]
        public void Sqlite_AutoIncrement_UsesLastInsertRowId()
        {
            var dialect = new SqliteDialect();
            var result = dialect.FormatAutoIncrementSelect(null);
            Assert.Contains("last_insert_rowid", result);
        }

        [Fact]
        public void Postgres_AutoIncrement_UsesLastVal()
        {
            var dialect = new PostgreSqlDialect();
            var result = dialect.FormatAutoIncrementSelect(null);
            Assert.Contains("lastval", result);
        }

        [Fact]
        public void Postgres_AutoIncrement_WithSequence_UsesCurrVal()
        {
            var dialect = new PostgreSqlDialect();
            var result = dialect.FormatAutoIncrementSelect("my_seq");
            Assert.Contains("currval", result);
            Assert.Contains("my_seq", result);
        }

        // Parameter formatting

        [Fact]
        public void SqlServer_FormatsParameter_WithAtSign()
        {
            var dialect = new SqlServerDialect();
            var result = dialect.FormatParameterName(0, CommandType.Text);
            Assert.StartsWith("@", result);
        }

        // Unary boolean

        [Fact]
        public void SqlServer_FormatUnaryBoolean_UsesEqualsOne()
        {
            var dialect = new SqlServerDialect();
            var result = dialect.FormatUnaryBoolean("IsOpen");
            Assert.Contains("= 1", result);
        }

        [Fact]
        public void Postgres_FormatUnaryBoolean_UsesFieldDirectly()
        {
            var dialect = new PostgreSqlDialect();
            var result = dialect.FormatUnaryBoolean("IsOpen");
            Assert.Equal("(IsOpen)", result);
        }

        // Table name formatting

        [Fact]
        public void SqlServer_FormatTableName_WithSchema()
        {
            var dialect = new SqlServerDialect();
            var result = dialect.FormatTableName("Animals", "dbo", true);
            Assert.Contains("dbo", result);
            Assert.Contains("Animals", result);
        }

        // FOLLOW-UP: Add FormatLimitQuery, FormatParameterName indices, ParseWhereFunction,
        // SupportsStoredProcedures, SupportsChangeContext tests — same patterns as above.
    }
}
```

- [ ] **Step 2: Run tests**

Run: `dotnet test test/Zonkey.Tests/Zonkey.Tests.csproj --filter "FullyQualifiedName~DialectTests"`
Expected: All tests pass

- [ ] **Step 3: Commit**

```bash
git add test/Zonkey.Tests/Unit/DialectTests.cs
git commit -m "test: add SQL dialect unit tests"
```

---

### Task 12: WhereExpressionParserTests

**Files:**
- Create: `test/Zonkey.Tests/Unit/WhereExpressionParserTests.cs`
- Reference: `src/Zonkey.Data/ObjectModel/WhereExpressionParser.cs`

- [ ] **Step 1: Write WhereExpressionParserTests**

This is the most complex unit test class. Uses the zoo models for type-safe LINQ expressions.

```csharp
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using Xunit;
using Zonkey.Dialects;
using Zonkey.ObjectModel;
using Zonkey.Tests.Models;

namespace Zonkey.Tests.Unit
{
    public class WhereExpressionParserTests
    {
        private SqlWhereClause Parse(Expression<Func<Animal, bool>> expr, SqlDialect dialect = null)
        {
            dialect ??= new GenericSqlDialect();
            var parser = new WhereExpressionParser(dialect);
            return parser.Parse(expr);
        }

        private SqlWhereClause ParseExhibit(Expression<Func<Exhibit, bool>> expr, SqlDialect dialect = null)
        {
            dialect ??= new GenericSqlDialect();
            var parser = new WhereExpressionParser(dialect);
            return parser.Parse(expr);
        }

        private SqlWhereClause ParseSpecies(Expression<Func<Species, bool>> expr, SqlDialect dialect = null)
        {
            dialect ??= new GenericSqlDialect();
            var parser = new WhereExpressionParser(dialect);
            return parser.Parse(expr);
        }

        // Basic comparisons

        [Fact]
        public void Equals_IntConstant()
        {
            var result = Parse(a => a.SpeciesId == 1);
            Assert.Contains("SpeciesId", result.SqlText);
            Assert.Contains("=", result.SqlText);
        }

        [Fact]
        public void NotEquals_IntConstant()
        {
            var result = Parse(a => a.SpeciesId != 1);
            Assert.Contains("<>", result.SqlText);
        }

        [Fact]
        public void GreaterThan()
        {
            var result = Parse(a => a.SpeciesId > 1);
            Assert.Contains(">", result.SqlText);
        }

        [Fact]
        public void LessThan()
        {
            var result = Parse(a => a.SpeciesId < 5);
            Assert.Contains("<", result.SqlText);
        }

        [Fact]
        public void GreaterThanOrEqual()
        {
            var result = Parse(a => a.SpeciesId >= 2);
            Assert.Contains(">=", result.SqlText);
        }

        [Fact]
        public void LessThanOrEqual()
        {
            var result = Parse(a => a.SpeciesId <= 3);
            Assert.Contains("<=", result.SqlText);
        }

        [Fact]
        public void Equals_StringConstant()
        {
            var result = Parse(a => a.Name == "Mei Mei");
            Assert.Contains("Name", result.SqlText);
        }

        // Null comparisons

        [Fact]
        public void EqualsNull_GeneratesIsNull()
        {
            var result = Parse(a => a.ExhibitId == null);
            Assert.Contains("IS NULL", result.SqlText);
        }

        [Fact]
        public void NotEqualsNull_GeneratesIsNotNull()
        {
            var result = Parse(a => a.ExhibitId != null);
            Assert.Contains("IS NOT NULL", result.SqlText);
        }

        // Boolean fields

        [Fact]
        public void BooleanField_TrueExpression()
        {
            var result = ParseSpecies(s => s.IsEndangered);
            Assert.NotNull(result.SqlText);
            // Should generate something like (IsEndangered = 1) or (IsEndangered)
        }

        [Fact]
        public void BooleanField_NegatedExpression()
        {
            var result = ParseSpecies(s => !s.IsEndangered);
            Assert.NotNull(result.SqlText);
        }

        // Logical operators

        [Fact]
        public void And_CombinesTwoConditions()
        {
            var result = Parse(a => a.SpeciesId == 1 && a.Name == "Mei Mei");
            Assert.Contains("AND", result.SqlText);
        }

        [Fact]
        public void Or_CombinesTwoConditions()
        {
            var result = Parse(a => a.SpeciesId == 1 || a.SpeciesId == 2);
            Assert.Contains("OR", result.SqlText);
        }

        [Fact]
        public void NestedLogical_HasParentheses()
        {
            var result = Parse(a => (a.SpeciesId == 1 || a.SpeciesId == 2) && a.Name == "Test");
            Assert.Contains("(", result.SqlText);
            Assert.Contains(")", result.SqlText);
        }

        // SqlIn

        [Fact]
        public void SqlIn_IntArray()
        {
            var ids = new[] { 1, 2, 3 };
            var result = Parse(a => a.SpeciesId.SqlInInt(ids));
            Assert.Contains("IN", result.SqlText);
        }

        [Fact]
        public void SqlIn_GuidArray()
        {
            var guids = new[] { Guid.NewGuid(), Guid.NewGuid() };
            var result = Parse(a => a.ZookeeperId.SqlInGuid(guids));
            Assert.Contains("IN", result.SqlText);
        }

        [Fact]
        public void SqlIn_EmptyArray_ThrowsArgumentException()
        {
            var empty = Array.Empty<int>();
            Assert.Throws<ArgumentException>(() => Parse(a => a.SpeciesId.SqlInInt(empty)));
        }

        // String methods

        [Fact]
        public void Contains_GeneratesLike()
        {
            var result = Parse(a => a.Name.Contains("Mei"));
            Assert.Contains("LIKE", result.SqlText);
        }

        [Fact]
        public void StartsWith_GeneratesLike()
        {
            var result = Parse(a => a.Name.StartsWith("Mei"));
            Assert.Contains("LIKE", result.SqlText);
        }

        [Fact]
        public void EndsWith_GeneratesLike()
        {
            var result = Parse(a => a.Name.EndsWith("Mei"));
            Assert.Contains("LIKE", result.SqlText);
        }

        // Parameterization

        [Fact]
        public void Parameterization_CreatesParameters()
        {
            var parser = new WhereExpressionParser(new GenericSqlDialect())
            {
                ParameterizeLiterals = true
            };
            var paramList = new ArrayList();
            Expression<Func<Animal, bool>> expr = a => a.SpeciesId == 1;
            var result = parser.Parse(expr, paramList);
            Assert.NotEmpty(paramList);
        }

        // Dialect-specific output

        [Fact]
        public void SqlServer_UsesAtParameters()
        {
            var result = Parse(a => a.SpeciesId == 1, new SqlServerDialect());
            Assert.Contains("@", result.SqlText);
        }

        // Decimal comparisons

        [Fact]
        public void Decimal_GreaterThan()
        {
            var result = Parse(a => a.Weight > 5.0m);
            Assert.Contains(">", result.SqlText);
        }

        // Arithmetic (if supported by parser)

        [Fact]
        public void Arithmetic_InExpression()
        {
            var result = ParseExhibit(e => e.Capacity + 5 > 10);
            Assert.Contains("+", result.SqlText);
        }

        // ANSI null compensation

        [Fact]
        public void AnsiNullCompensation_CanBeDisabled()
        {
            var parser = new WhereExpressionParser(new GenericSqlDialect())
            {
                AnsiNullCompensation = false
            };
            Expression<Func<Animal, bool>> expr = a => a.ExhibitId == null;
            var result = parser.Parse(expr);
            Assert.NotNull(result.SqlText);
        }

        // FOLLOW-UP: Add DateTime comparisons, GUID literals, NoLock flag, UseQuotedIdentifier,
        // UseTableWithFieldNames, ParameterIndexModifier, multi-dialect output tests.
    }
}
```

- [ ] **Step 2: Run tests**

Run: `dotnet test test/Zonkey.Tests/Zonkey.Tests.csproj --filter "FullyQualifiedName~WhereExpressionParserTests"`
Expected: All tests pass (some may need adjustments based on actual parser output format)

- [ ] **Step 3: Commit**

```bash
git add test/Zonkey.Tests/Unit/WhereExpressionParserTests.cs
git commit -m "test: add WhereExpressionParser unit tests"
```

---

### Task 13: CommandBuilderTests

**Files:**
- Create: `test/Zonkey.Tests/Unit/CommandBuilderTests.cs`
- Reference: `src/Zonkey.Data/ObjectModel/DataClassCommandBuilder/` (Common.cs, Select.cs, Insert.cs, Update.cs, Delete.cs)

- [ ] **Step 1: Write CommandBuilderTests**

CommandBuilder requires a `DbConnection` to construct. For unit tests, use a `SqliteConnection` (lightweight, no server needed) or the `MockDbConnection` from Zonkey.Mocks. Since we don't reference Zonkey.Mocks, use a SQLite in-memory connection.

```csharp
using System;
using System.Linq;
using Microsoft.Data.Sqlite;
using Xunit;
using Zonkey.Dialects;
using Zonkey.ObjectModel;
using Zonkey.Tests.Models;

namespace Zonkey.Tests.Unit
{
    public class CommandBuilderTests : IDisposable
    {
        private readonly SqliteConnection _conn;

        public CommandBuilderTests()
        {
            _conn = new SqliteConnection("Data Source=:memory:");
            _conn.Open();
        }

        public void Dispose() => _conn.Dispose();

        private DataClassCommandBuilder CreateBuilder<T>(SqlDialect dialect = null)
        {
            var map = DataMap.GenerateNew(typeof(T));
            dialect ??= new SqliteDialect();
            return new DataClassCommandBuilder(typeof(T), map, _conn, dialect);
        }

        // SELECT tests

        [Fact]
        public void SelectByKeys_ContainsKeyField()
        {
            var builder = CreateBuilder<Animal>();
            var cmd = builder.SelectByKeysCommand;
            Assert.Contains("AnimalId", cmd.CommandText);
            Assert.Contains("WHERE", cmd.CommandText);
        }

        [Fact]
        public void SelectByKeys_CompositeKey_ContainsAllKeys()
        {
            var builder = CreateBuilder<FeedingSchedule>();
            var cmd = builder.SelectByKeysCommand;
            Assert.Contains("AnimalId", cmd.CommandText);
            Assert.Contains("DayOfWeek", cmd.CommandText);
            Assert.Contains("TimeSlot", cmd.CommandText);
        }

        [Fact]
        public void GetSelectCommand_WithFilter_ContainsWhere()
        {
            var builder = CreateBuilder<Animal>();
            var cmd = builder.GetSelectCommand("SpeciesId = 1");
            Assert.Contains("WHERE", cmd.CommandText);
            Assert.Contains("SpeciesId = 1", cmd.CommandText);
        }

        [Fact]
        public void GetSelectCommand_ContainsAllReadableFields()
        {
            var builder = CreateBuilder<Animal>();
            var cmd = builder.GetSelectCommand("");
            Assert.Contains("AnimalId", cmd.CommandText);
            Assert.Contains("Name", cmd.CommandText);
            Assert.Contains("SpeciesId", cmd.CommandText);
            Assert.Contains("Notes", cmd.CommandText);
        }

        // INSERT tests

        [Fact]
        public void GetInsertCommands_ExcludesAutoIncrementField()
        {
            var builder = CreateBuilder<Animal>();
            var commands = builder.GetInsertCommands(new Animal(), SelectBack.None);
            var insertCmd = commands.First();
            // The INSERT should NOT include AnimalId in the column list
            // since it's auto-increment
            Assert.Contains("INSERT", insertCmd.CommandText);
            Assert.Contains("Name", insertCmd.CommandText);
        }

        [Fact]
        public void GetInsertCommands_GuidKey_IncludesKeyField()
        {
            var builder = CreateBuilder<Zookeeper>();
            var commands = builder.GetInsertCommands(new Zookeeper(), SelectBack.None);
            var insertCmd = commands.First();
            Assert.Contains("ZookeeperId", insertCmd.CommandText);
        }

        // DELETE tests

        [Fact]
        public void DeleteItemCommand_ContainsKeyInWhere()
        {
            var builder = CreateBuilder<Animal>();
            var cmd = builder.DeleteItemCommand;
            Assert.Contains("DELETE", cmd.CommandText);
            Assert.Contains("WHERE", cmd.CommandText);
            Assert.Contains("AnimalId", cmd.CommandText);
        }

        [Fact]
        public void GetDeleteCommand_WithFilter()
        {
            var builder = CreateBuilder<Animal>();
            var cmd = builder.GetDeleteCommand("SpeciesId = 1");
            Assert.Contains("DELETE", cmd.CommandText);
            Assert.Contains("SpeciesId = 1", cmd.CommandText);
        }

        // Dialect-specific

        [Fact]
        public void SqlServer_Select_UsesBrackets()
        {
            using var sqlConn = new SqliteConnection("Data Source=:memory:");
            sqlConn.Open();
            var map = DataMap.GenerateNew(typeof(Animal));
            var builder = new DataClassCommandBuilder(typeof(Animal), map, sqlConn, new SqlServerDialect());
            builder.UseQuotedIdentifier = true;
            var cmd = builder.GetSelectCommand("");
            Assert.Contains("[", cmd.CommandText);
        }

        // FOLLOW-UP: Add INSERT+SelectBack variants, UPDATE with different UpdateCriteria,
        // UPDATE with RowVersion, GetSelectCommand with SqlFilter[], pagination, schema prefix tests.
    }
}
```

- [ ] **Step 2: Run tests**

Run: `dotnet test test/Zonkey.Tests/Zonkey.Tests.csproj --filter "FullyQualifiedName~CommandBuilderTests"`
Expected: All tests pass

- [ ] **Step 3: Commit**

```bash
git add test/Zonkey.Tests/Unit/CommandBuilderTests.cs
git commit -m "test: add DataClassCommandBuilder unit tests"
```

---

## Chunk 4: Integration Tests

### Task 14: CrudTests + concrete subclasses

**Files:**
- Create: `test/Zonkey.Tests/Integration/CrudTests.cs`
- Create: `test/Zonkey.Tests/Integration/Sqlite/SqliteCrudTests.cs`
- Create: `test/Zonkey.Tests/Integration/Mssql/MssqlCrudTests.cs`
- Create: `test/Zonkey.Tests/Integration/Pgsql/PgsqlCrudTests.cs`

- [ ] **Step 1: Write CrudTests base class**

```csharp
using System;
using System.Threading.Tasks;
using Xunit;
using Zonkey.ObjectModel;
using Zonkey.Tests.Infrastructure;
using Zonkey.Tests.Models;

namespace Zonkey.Tests.Integration
{
    public abstract class CrudTests<TFixture> : IClassFixture<TFixture>
        where TFixture : class, IDatabaseFixture
    {
        protected readonly TFixture Db;

        protected CrudTests(TFixture db) => Db = db;

        [Fact]
        public async Task InsertAnimal_AssignsAutoIncrementId()
        {
            if (!Db.IsAvailable) Assert.Skip(Db.SkipReason);

            using var conn = Db.CreateConnection();
            var adapter = new DataClassAdapter<Animal>(conn);

            var animal = new Animal
            {
                Name = "Test Insert",
                SpeciesId = 1,
                ExhibitId = 1,
                ZookeeperId = Guid.Parse("A1B2C3D4-E5F6-7890-ABCD-EF1234567890"),
                Weight = 1.5m
            };

            var saved = await adapter.Save(animal);
            Assert.True(saved);
            Assert.True(animal.AnimalId > 0);

            // Cleanup
            await adapter.DeleteItem(animal);
        }

        [Fact]
        public async Task InsertZookeeper_WithExplicitGuid()
        {
            if (!Db.IsAvailable) Assert.Skip(Db.SkipReason);

            using var conn = Db.CreateConnection();
            var adapter = new DataClassAdapter<Zookeeper>(conn);

            var keeper = new Zookeeper
            {
                ZookeeperId = Guid.NewGuid(),
                FirstName = "Test",
                LastName = "Keeper",
                HireDate = DateTime.Today
            };

            var saved = await adapter.Save(keeper);
            Assert.True(saved);

            await adapter.DeleteItem(keeper);
        }

        [Fact]
        public async Task GetSingleItem_ByIntKey()
        {
            if (!Db.IsAvailable) Assert.Skip(Db.SkipReason);

            using var conn = Db.CreateConnection();
            var adapter = new DataClassAdapter<Animal>(conn);

            var animal = await adapter.GetOne(a => a.AnimalId == 1);
            Assert.NotNull(animal);
            Assert.Equal("Mei Mei", animal.Name);
        }

        [Fact]
        public async Task GetSingleItem_ByGuidKey()
        {
            if (!Db.IsAvailable) Assert.Skip(Db.SkipReason);

            using var conn = Db.CreateConnection();
            var adapter = new DataClassAdapter<Zookeeper>(conn);

            var keeper = await adapter.GetOne(k => k.ZookeeperId == Guid.Parse("A1B2C3D4-E5F6-7890-ABCD-EF1234567890"));
            Assert.NotNull(keeper);
            Assert.Equal("Jane", keeper.FirstName);
        }

        [Fact]
        public async Task GetSingleItem_ByCompositeKey()
        {
            if (!Db.IsAvailable) Assert.Skip(Db.SkipReason);

            using var conn = Db.CreateConnection();
            var adapter = new DataClassAdapter<FeedingSchedule>(conn);

            var schedule = await adapter.GetOne(s => s.AnimalId == 1 && s.DayOfWeek == 1 && s.TimeSlot == "morning");
            Assert.NotNull(schedule);
            Assert.Equal("Bamboo", schedule.FoodType);
        }

        [Fact]
        public async Task UpdateSingleField_SavesOnlyChangedField()
        {
            if (!Db.IsAvailable) Assert.Skip(Db.SkipReason);

            using var conn = Db.CreateConnection();
            var adapter = new DataClassAdapter<Species>(conn);

            var species = await adapter.GetOne(s => s.SpeciesId == 1);
            var originalName = species.Name;

            species.Name = "Updated Red Panda";
            await adapter.Save(species, UpdateCriteria.ChangedFields);

            // Verify
            var reloaded = await adapter.GetOne(s => s.SpeciesId == 1);
            Assert.Equal("Updated Red Panda", reloaded.Name);

            // Restore
            reloaded.Name = originalName;
            await adapter.Save(reloaded, UpdateCriteria.ChangedFields);
        }

        [Fact]
        public async Task SaveNew_ThenUpdate()
        {
            if (!Db.IsAvailable) Assert.Skip(Db.SkipReason);

            using var conn = Db.CreateConnection();
            var adapter = new DataClassAdapter<Species>(conn);

            // Insert
            var species = new Species { Name = "Test Species", IsEndangered = false };
            await adapter.Save(species);
            Assert.True(species.SpeciesId > 0);

            // Update
            species.IsEndangered = true;
            await adapter.Save(species, UpdateCriteria.ChangedFields);

            // Verify
            var reloaded = await adapter.GetOne(s => s.SpeciesId == species.SpeciesId);
            Assert.True(reloaded.IsEndangered);

            // Cleanup
            await adapter.DeleteItem(species);
        }

        [Fact]
        public async Task DeleteByKey_RemovesRecord()
        {
            if (!Db.IsAvailable) Assert.Skip(Db.SkipReason);

            using var conn = Db.CreateConnection();
            var adapter = new DataClassAdapter<Species>(conn);

            var species = new Species { Name = "To Delete", IsEndangered = false };
            await adapter.Save(species);
            var id = species.SpeciesId;

            await adapter.DeleteItem(species);

            var exists = await adapter.Exists(s => s.SpeciesId == id);
            Assert.False(exists);
        }

        [Fact]
        public async Task NullField_InsertAndUpdate()
        {
            if (!Db.IsAvailable) Assert.Skip(Db.SkipReason);

            using var conn = Db.CreateConnection();
            var adapter = new DataClassAdapter<Animal>(conn);

            // Insert with null ExhibitId
            var animal = new Animal
            {
                Name = "Null Test",
                SpeciesId = 1,
                ZookeeperId = Guid.Parse("A1B2C3D4-E5F6-7890-ABCD-EF1234567890")
            };
            await adapter.Save(animal);
            Assert.Null(animal.ExhibitId);

            // Update to non-null
            animal.ExhibitId = 1;
            await adapter.Save(animal, UpdateCriteria.ChangedFields);

            var reloaded = await adapter.GetOne(a => a.AnimalId == animal.AnimalId);
            Assert.Equal(1, reloaded.ExhibitId);

            // Update back to null
            reloaded.ExhibitId = null;
            await adapter.Save(reloaded, UpdateCriteria.ChangedFields);

            var reloaded2 = await adapter.GetOne(a => a.AnimalId == animal.AnimalId);
            Assert.Null(reloaded2.ExhibitId);

            // Cleanup
            await adapter.DeleteItem(animal);
        }

        [Fact]
        public async Task RowVersion_ConcurrencyConflict()
        {
            if (!Db.IsAvailable) Assert.Skip(Db.SkipReason);
            if (!Db.SupportsRowVersion) Assert.Skip("Row version not supported by this provider");

            using var conn = Db.CreateConnection();
            var adapter = new DataClassAdapter<Exhibit>(conn);

            // Get two copies of the same exhibit
            var exhibit1 = await adapter.GetOne(e => e.ExhibitId == 1);
            var exhibit2 = await adapter.GetOne(e => e.ExhibitId == 1);

            // Update first copy
            exhibit1.Capacity = 99;
            await adapter.Save(exhibit1, UpdateCriteria.KeyAndVersion);

            // Try to update second copy (stale row version) — should conflict
            exhibit2.Capacity = 50;
            var result = await adapter.TrySave(exhibit2, UpdateCriteria.KeyAndVersion);
            Assert.Equal(SaveResultStatus.Conflict, result.Status);

            // Restore
            exhibit1.Capacity = 5;
            await adapter.Save(exhibit1, UpdateCriteria.ChangedFields);
        }
    }
}
```

- [ ] **Step 2: Create concrete subclasses**

`test/Zonkey.Tests/Integration/Sqlite/SqliteCrudTests.cs`:
```csharp
using Zonkey.Tests.Infrastructure;

namespace Zonkey.Tests.Integration.Sqlite
{
    public class SqliteCrudTests : CrudTests<SqliteFixture>
    {
        public SqliteCrudTests(SqliteFixture db) : base(db) { }
    }
}
```

`test/Zonkey.Tests/Integration/Mssql/MssqlCrudTests.cs`:
```csharp
using Zonkey.Tests.Infrastructure;

namespace Zonkey.Tests.Integration.Mssql
{
    public class MssqlCrudTests : CrudTests<MssqlFixture>
    {
        public MssqlCrudTests(MssqlFixture db) : base(db) { }
    }
}
```

`test/Zonkey.Tests/Integration/Pgsql/PgsqlCrudTests.cs`:
```csharp
using Zonkey.Tests.Infrastructure;

namespace Zonkey.Tests.Integration.Pgsql
{
    public class PgsqlCrudTests : CrudTests<PgsqlFixture>
    {
        public PgsqlCrudTests(PgsqlFixture db) : base(db) { }
    }
}
```

- [ ] **Step 3: Run SQLite tests locally**

Run: `dotnet test test/Zonkey.Tests/Zonkey.Tests.csproj --filter "FullyQualifiedName~SqliteCrudTests"`
Expected: All tests pass. MSSQL/PostgreSQL tests skip.

- [ ] **Step 4: Commit**

```bash
git add test/Zonkey.Tests/Integration/CrudTests.cs test/Zonkey.Tests/Integration/Sqlite/ test/Zonkey.Tests/Integration/Mssql/ test/Zonkey.Tests/Integration/Pgsql/
git commit -m "test: add CRUD integration tests with provider subclasses"
```

---

### Task 15: FillTests + concrete subclasses

**Files:**
- Create: `test/Zonkey.Tests/Integration/FillTests.cs`
- Create: `test/Zonkey.Tests/Integration/Sqlite/SqliteFillTests.cs`
- Create: `test/Zonkey.Tests/Integration/Mssql/MssqlFillTests.cs`
- Create: `test/Zonkey.Tests/Integration/Pgsql/PgsqlFillTests.cs`

- [ ] **Step 1: Write FillTests base class**

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Zonkey.ObjectModel;
using Zonkey.Tests.Infrastructure;
using Zonkey.Tests.Models;

namespace Zonkey.Tests.Integration
{
    public abstract class FillTests<TFixture> : IClassFixture<TFixture>
        where TFixture : class, IDatabaseFixture
    {
        protected readonly TFixture Db;

        protected FillTests(TFixture db) => Db = db;

        [Fact]
        public async Task FillAll_ReturnsAllSeededAnimals()
        {
            if (!Db.IsAvailable) Assert.Skip(Db.SkipReason);

            using var conn = Db.CreateConnection();
            var adapter = new DataClassAdapter<Animal>(conn);
            var animals = new List<Animal>();

            var count = await adapter.FillAll(animals);
            Assert.Equal(4, count);
        }

        [Fact]
        public async Task Fill_WithLinqExpression()
        {
            if (!Db.IsAvailable) Assert.Skip(Db.SkipReason);

            using var conn = Db.CreateConnection();
            var adapter = new DataClassAdapter<Animal>(conn);
            var animals = new List<Animal>();

            await adapter.Fill(animals, a => a.SpeciesId == 1);
            Assert.Equal(2, animals.Count); // Mei Mei and Bao Bao
        }

        [Fact]
        public async Task Fill_WithSqlFilter()
        {
            if (!Db.IsAvailable) Assert.Skip(Db.SkipReason);

            using var conn = Db.CreateConnection();
            var adapter = new DataClassAdapter<Animal>(conn);
            var animals = new List<Animal>();

            await adapter.Fill(animals, SqlFilter.EQ("SpeciesId", 1));
            Assert.Equal(2, animals.Count);
        }

        [Fact]
        public async Task Fill_WithStringFilter()
        {
            if (!Db.IsAvailable) Assert.Skip(Db.SkipReason);

            using var conn = Db.CreateConnection();
            var adapter = new DataClassAdapter<Animal>(conn);
            var animals = new List<Animal>();

            await adapter.Fill(animals, "SpeciesId = $0", 1);
            Assert.Equal(2, animals.Count);
        }

        [Fact]
        public async Task Fill_WithNullFilter()
        {
            if (!Db.IsAvailable) Assert.Skip(Db.SkipReason);

            using var conn = Db.CreateConnection();
            var adapter = new DataClassAdapter<Animal>(conn);
            var animals = new List<Animal>();

            await adapter.Fill(animals, a => a.ExhibitId == null);
            Assert.Single(animals); // Bao Bao
            Assert.Equal("Bao Bao", animals[0].Name);
        }

        [Fact]
        public async Task Fill_WithBooleanFilter()
        {
            if (!Db.IsAvailable) Assert.Skip(Db.SkipReason);

            using var conn = Db.CreateConnection();
            var adapter = new DataClassAdapter<Species>(conn);
            var species = new List<Species>();

            await adapter.Fill(species, s => s.IsEndangered);
            Assert.Equal(2, species.Count); // Red Panda, Axolotl
        }

        [Fact]
        public async Task Fill_WithCompoundFilter()
        {
            if (!Db.IsAvailable) Assert.Skip(Db.SkipReason);

            using var conn = Db.CreateConnection();
            var adapter = new DataClassAdapter<Animal>(conn);
            var animals = new List<Animal>();

            await adapter.Fill(animals, a => a.SpeciesId == 1 && a.Weight > 5.0m);
            Assert.Single(animals); // Mei Mei (5.50)
        }

        [Fact]
        public async Task GetCount_WithFilter()
        {
            if (!Db.IsAvailable) Assert.Skip(Db.SkipReason);

            using var conn = Db.CreateConnection();
            var adapter = new DataClassAdapter<Animal>(conn);

            var count = await adapter.GetCount(a => a.SpeciesId == 1);
            Assert.Equal(2, count);
        }

        [Fact]
        public async Task Exists_Matching_ReturnsTrue()
        {
            if (!Db.IsAvailable) Assert.Skip(Db.SkipReason);

            using var conn = Db.CreateConnection();
            var adapter = new DataClassAdapter<Animal>(conn);

            var exists = await adapter.Exists(a => a.Name == "Mei Mei");
            Assert.True(exists);
        }

        [Fact]
        public async Task Exists_NonMatching_ReturnsFalse()
        {
            if (!Db.IsAvailable) Assert.Skip(Db.SkipReason);

            using var conn = Db.CreateConnection();
            var adapter = new DataClassAdapter<Animal>(conn);

            var exists = await adapter.Exists(a => a.Name == "NonExistent");
            Assert.False(exists);
        }

        // FOLLOW-UP: Add FillRange pagination and Fill with sort order tests.
    }
}
```

- [ ] **Step 2: Create concrete subclasses** (same pattern as CrudTests)

Create `SqliteFillTests.cs`, `MssqlFillTests.cs`, `PgsqlFillTests.cs` — each is 3 lines inheriting from `FillTests<XFixture>`.

- [ ] **Step 3: Run SQLite tests**

Run: `dotnet test test/Zonkey.Tests/Zonkey.Tests.csproj --filter "FullyQualifiedName~SqliteFillTests"`

- [ ] **Step 4: Commit**

```bash
git add test/Zonkey.Tests/Integration/FillTests.cs test/Zonkey.Tests/Integration/*/
git commit -m "test: add Fill/query integration tests"
```

---

### Task 16: TransactionTests + concrete subclasses

**Files:**
- Create: `test/Zonkey.Tests/Integration/TransactionTests.cs`
- Create: concrete subclasses in `Sqlite/`, `Mssql/`, `Pgsql/`

- [ ] **Step 1: Write TransactionTests base class**

```csharp
using System;
using System.Threading.Tasks;
using Xunit;
using Zonkey.ObjectModel;
using Zonkey.Tests.Infrastructure;
using Zonkey.Tests.Models;

namespace Zonkey.Tests.Integration
{
    public abstract class TransactionTests<TFixture> : IClassFixture<TFixture>
        where TFixture : class, IDatabaseFixture
    {
        protected readonly TFixture Db;

        protected TransactionTests(TFixture db) => Db = db;

        [Fact]
        public async Task Transaction_Commit_PersistsData()
        {
            if (!Db.IsAvailable) Assert.Skip(Db.SkipReason);

            using var conn = Db.CreateConnection();
            var adapter = new DataClassAdapter<Species>(conn);

            int insertedId;
            using (var trx = conn.BeginTransaction())
            {
                adapter.Transaction = trx;
                var species = new Species { Name = "Committed Species", IsEndangered = false };
                await adapter.Save(species);
                insertedId = species.SpeciesId;
                trx.Commit();
            }

            adapter.Transaction = null;
            var exists = await adapter.Exists(s => s.SpeciesId == insertedId);
            Assert.True(exists);

            // Cleanup
            await adapter.Delete(s => s.SpeciesId == insertedId);
        }

        [Fact]
        public async Task Transaction_Rollback_DiscardsData()
        {
            if (!Db.IsAvailable) Assert.Skip(Db.SkipReason);

            using var conn = Db.CreateConnection();
            var adapter = new DataClassAdapter<Species>(conn);

            int insertedId;
            using (var trx = conn.BeginTransaction())
            {
                adapter.Transaction = trx;
                var species = new Species { Name = "Rolled Back Species", IsEndangered = false };
                await adapter.Save(species);
                insertedId = species.SpeciesId;
                trx.Rollback();
            }

            adapter.Transaction = null;
            var exists = await adapter.Exists(s => s.SpeciesId == insertedId);
            Assert.False(exists);
        }

        // FOLLOW-UP: Add multi-operation transaction and DatabaseWrapper.WithTransaction() tests.
    }
}
```

- [ ] **Step 2: Create concrete subclasses** (same pattern)

- [ ] **Step 3: Run and commit**

```bash
git add test/Zonkey.Tests/Integration/TransactionTests.cs test/Zonkey.Tests/Integration/*/
git commit -m "test: add transaction integration tests"
```

---

### Task 17: BulkOperationTests + concrete subclasses

**Files:**
- Create: `test/Zonkey.Tests/Integration/BulkOperationTests.cs`
- Create: concrete subclasses in `Sqlite/`, `Mssql/`, `Pgsql/`

- [ ] **Step 1: Write BulkOperationTests base class**

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Zonkey.ObjectModel;
using Zonkey.Tests.Infrastructure;
using Zonkey.Tests.Models;

namespace Zonkey.Tests.Integration
{
    public abstract class BulkOperationTests<TFixture> : IClassFixture<TFixture>
        where TFixture : class, IDatabaseFixture
    {
        protected readonly TFixture Db;

        protected BulkOperationTests(TFixture db) => Db = db;

        [Fact]
        public async Task BulkInsert_InsertsMultipleRecords()
        {
            if (!Db.IsAvailable) Assert.Skip(Db.SkipReason);

            using var conn = Db.CreateConnection();
            var adapter = new DataClassAdapter<Species>(conn);

            var newSpecies = new List<Species>
            {
                new Species { Name = "Bulk Species 1", IsEndangered = false },
                new Species { Name = "Bulk Species 2", IsEndangered = true }
            };

            var inserted = await adapter.BulkInsert(newSpecies);
            Assert.Equal(2, inserted);

            // Cleanup
            foreach (var s in newSpecies)
                await adapter.Delete(x => x.Name == s.Name);
        }

        // FOLLOW-UP: Add BulkUpdate test.
    }
}
```

- [ ] **Step 2: Create concrete subclasses** (same pattern)

- [ ] **Step 3: Run and commit**

```bash
git add test/Zonkey.Tests/Integration/BulkOperationTests.cs test/Zonkey.Tests/Integration/*/
git commit -m "test: add bulk operation integration tests"
```

---

## Chunk 5: CI/CD & Cleanup

### Task 18: Create GitHub Actions workflow

**Files:**
- Create: `.github/workflows/build-and-test.yml`

- [ ] **Step 1: Write the workflow**

```yaml
name: Build and Test

on:
  push:
    branches: [master]
  pull_request:
    branches: [master]

jobs:
  core-tests:
    strategy:
      matrix:
        include:
          - os: windows-latest
            run-args: ""
          - os: ubuntu-latest
            run-args: "-f net10.0"
    runs-on: ${{ matrix.os }}
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: "10.0.x"
      - run: dotnet restore Zonkey.sln
      - run: dotnet build Zonkey.sln -c Release --no-restore
      - run: dotnet test test/Zonkey.Tests/Zonkey.Tests.csproj -c Release --no-build ${{ matrix.run-args }}

  integration-tests:
    runs-on: ubuntu-latest
    services:
      mssql:
        image: mcr.microsoft.com/mssql/server:2022-latest
        env:
          ACCEPT_EULA: "Y"
          MSSQL_SA_PASSWORD: "Zonkey#Test123"
        ports:
          - 1433:1433
        options: >-
          --health-cmd "/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'Zonkey#Test123' -C -Q 'SELECT 1' || exit 1"
          --health-interval 10s
          --health-timeout 5s
          --health-retries 10
      postgres:
        image: postgres:17
        env:
          POSTGRES_USER: zonkey
          POSTGRES_PASSWORD: zonkey
          POSTGRES_DB: zonkey_test
        ports:
          - 5432:5432
        options: >-
          --health-cmd pg_isready
          --health-interval 10s
          --health-timeout 5s
          --health-retries 5
    env:
      ZONKEY_TEST_MSSQL: "Server=localhost,1433;User=sa;Password=Zonkey#Test123;TrustServerCertificate=true"
      ZONKEY_TEST_PGSQL: "Host=localhost;Port=5432;Username=zonkey;Password=zonkey"
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: "10.0.x"
      - run: dotnet restore Zonkey.sln
      - run: dotnet build Zonkey.sln -c Release --no-restore
      - run: dotnet test test/Zonkey.Tests/Zonkey.Tests.csproj -c Release --no-build -f net10.0
```

- [ ] **Step 2: Commit**

```bash
git add .github/workflows/build-and-test.yml
git commit -m "ci: add build-and-test workflow with MSSQL and PostgreSQL service containers"
```

---

### Task 19: Update solution file

**Files:**
- Modify: `Zonkey.sln`

- [ ] **Step 1: Add new test project to solution**

Run: `dotnet sln "Zonkey.sln" add test/Zonkey.Tests/Zonkey.Tests.csproj`

- [ ] **Step 2: Verify solution builds**

Run: `dotnet build Zonkey.sln`

- [ ] **Step 3: Commit**

```bash
git add Zonkey.sln
git commit -m "chore: add Zonkey.Tests to solution"
```

---

### Task 20: Delete old test project and final verification

**Files:**
- Delete: `test/UnitTests.Core/` (entire directory)
- Modify: `Zonkey.sln` (remove old project reference)

- [ ] **Step 1: Remove old test project from solution**

Run: `dotnet sln "Zonkey.sln" remove test/UnitTests.Core/UnitTests.csproj`

- [ ] **Step 2: Delete old test project directory**

```bash
rm -rf test/UnitTests.Core
```

- [ ] **Step 3: Verify solution builds**

Run: `dotnet build Zonkey.sln`

- [ ] **Step 4: Run all tests**

Run: `dotnet test test/Zonkey.Tests/Zonkey.Tests.csproj`
Expected: All unit tests pass. SQLite integration tests pass. MSSQL/PostgreSQL tests skip (unless Docker is running).

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "chore: remove old UnitTests.Core project"
```

---

## Implementation Notes

### WhereExpressionParser visibility
`WhereExpressionParser` and `WhereExpressionParser<T>` are `internal` (no access modifier). The existing project uses `[InternalsVisibleTo("Zonkey.UnitTests")]` in `src/Zonkey.Data/Properties/AssemblyInfo.cs`. Add a new entry for `Zonkey.Tests`:
```csharp
[assembly: InternalsVisibleTo("Zonkey.Tests")]
```
If the assembly is strong-named, you'll need the public key. Check if `Zonkey.Tests` uses strong naming — if not, the simpler fix is to make `WhereExpressionParser` public. **This must be done before Task 12 (WhereExpressionParserTests) can compile.**

### DataClassAdapter construction
`DataClassAdapter<T>` has no constructor taking `(DbConnection, SqlDialect)`. Use `new DataClassAdapter<T>(conn)` — the `Connection` setter auto-detects dialect via `SqlDialect.Create(connection)`. Since we've registered all common providers in the factory dictionary, auto-detection works for SQLite, MSSQL, and PostgreSQL. If needed, override afterward: `adapter.SqlDialect = Db.Dialect;`

### SqlScriptProcessor
`SqlScriptProcessor` (`src/Zonkey.Data/Utility/SqlScriptProcessor.cs`) has been fixed: it no longer closes the connection (caller's responsibility) and splits on `^\s*GO\s*$` with `Multiline` flag (handles both `\r\n` and `\n`). The MSSQL fixture can use `SqlScriptProcessor` directly instead of manual batch splitting. Example: `var processor = new SqlScriptProcessor(conn); await processor.ExecuteScript(seedPath, true);`

### DataClass inheritance
The models assume `DataClass` (non-generic). If the codebase uses `DataClass<T>`, update all model classes to inherit from that instead. Check existing AdventureWorks models in `test/UnitTests.Core/AdventureWorks/DataObjects/` before they're deleted.

### xUnit v3 API
If xUnit v3 uses different method signatures for `IAsyncLifetime` (e.g., `ValueTask` vs `Task`), adjust the fixture implementations accordingly. The interface contract is the same: initialize on first use, dispose after last test in class.

### DataClassAdapter constructor
The integration tests assume `DataClassAdapter<T>(DbConnection, SqlDialect)` constructor exists. Verify this signature against `src/Zonkey.Data/DataClassAdapter/Base.cs`. If the adapter requires additional parameters (like a `DataMap`), adjust accordingly. The adapter may also need `adapter.Transaction = trx` for transaction tests — verify the `Transaction` property exists.
