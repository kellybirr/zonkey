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
