CREATE TABLE dbo.Species (
    SpeciesId     INT IDENTITY(1,1) PRIMARY KEY,
    Name          NVARCHAR(100) NOT NULL UNIQUE,
    IsEndangered  BIT NOT NULL DEFAULT 0
);

CREATE TABLE dbo.Animal (
    AnimalId      BIGINT IDENTITY(1,1) PRIMARY KEY,
    Name          NVARCHAR(100) NOT NULL,
    SpeciesId     INT NOT NULL,
    WeightKg      DECIMAL(8,2),
    HeadCount     DECIMAL(9,0),
    Notes         NVARCHAR(MAX),
    Photo         VARBINARY(MAX),
    CreatedUtc    DATETIME2 NOT NULL,
    Version       ROWVERSION,
    CONSTRAINT FK_Animal_Species FOREIGN KEY (SpeciesId) REFERENCES dbo.Species(SpeciesId)
);

CREATE TABLE dbo.FeedingSchedule (
    AnimalId      BIGINT NOT NULL,
    DayOfWeek     SMALLINT NOT NULL,
    TimeSlot      NVARCHAR(20) NOT NULL,
    Quantity      DECIMAL(6,2) NOT NULL,
    CONSTRAINT PK_FeedingSchedule PRIMARY KEY (AnimalId, DayOfWeek, TimeSlot)
);

GO
CREATE VIEW dbo.AnimalNames AS SELECT AnimalId, Name FROM dbo.Animal;
GO
CREATE SCHEMA archive;
GO
CREATE TABLE archive.Animal (AnimalId BIGINT PRIMARY KEY, Name NVARCHAR(100) NOT NULL);
