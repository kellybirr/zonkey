-- Identifiers are intentionally unquoted: PostgreSQL folds them to lowercase,
-- which matches the unquoted SQL that Zonkey generates by default.
CREATE TABLE Species (
    SpeciesId SERIAL PRIMARY KEY,
    Name VARCHAR(100) NOT NULL,
    Classification VARCHAR(50),
    IsEndangered BOOLEAN NOT NULL
);

CREATE TABLE Exhibit (
    ExhibitId SERIAL PRIMARY KEY,
    Name VARCHAR(100) NOT NULL,
    Location VARCHAR(200),
    Capacity INT NOT NULL,
    IsOpen BOOLEAN NOT NULL DEFAULT TRUE
);

CREATE TABLE Zookeeper (
    ZookeeperId UUID NOT NULL PRIMARY KEY,
    FirstName VARCHAR(50) NOT NULL,
    LastName VARCHAR(50) NOT NULL,
    Email VARCHAR(200),
    HireDate DATE NOT NULL,
    Specialty VARCHAR(100)
);

CREATE TABLE Animal (
    AnimalId SERIAL PRIMARY KEY,
    Name VARCHAR(100) NOT NULL,
    SpeciesId INT NOT NULL REFERENCES Species(SpeciesId),
    ExhibitId INT REFERENCES Exhibit(ExhibitId),
    ZookeeperId UUID NOT NULL REFERENCES Zookeeper(ZookeeperId),
    DateOfBirth TIMESTAMP,
    Weight NUMERIC(8,2),
    Notes TEXT
);

CREATE TABLE FeedingSchedule (
    AnimalId INT NOT NULL,
    DayOfWeek INT NOT NULL,
    TimeSlot VARCHAR(10) NOT NULL,
    FoodType VARCHAR(100) NOT NULL,
    Quantity NUMERIC(6,2) NOT NULL,
    AssignedKeeperId UUID REFERENCES Zookeeper(ZookeeperId),
    PRIMARY KEY (AnimalId, DayOfWeek, TimeSlot),
    FOREIGN KEY (AnimalId) REFERENCES Animal(AnimalId)
);

INSERT INTO Species (Name, Classification, IsEndangered) VALUES ('Red Panda', 'Mammalia', TRUE);
INSERT INTO Species (Name, Classification, IsEndangered) VALUES ('African Penguin', 'Aves', FALSE);
INSERT INTO Species (Name, Classification, IsEndangered) VALUES ('Axolotl', NULL, TRUE);

INSERT INTO Exhibit (Name, Location, Capacity, IsOpen) VALUES ('Bamboo Grove', 'Building A', 5, TRUE);
INSERT INTO Exhibit (Name, Location, Capacity, IsOpen) VALUES ('Aquatic House', 'Building B', 20, TRUE);

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
