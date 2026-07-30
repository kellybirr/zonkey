-- Guid columns use CHAR(36) (paired with GuidFormat=Char36 on the connection
-- string) since that is the round-trip-safe mapping for MySqlConnector.
-- No RowVersion column on Exhibit: MySqlDialect does not override
-- SupportsRowVersion (defaults to false), matching PostgreSQL.
CREATE TABLE Species (
    SpeciesId INT AUTO_INCREMENT PRIMARY KEY,
    Name VARCHAR(100) NOT NULL,
    Classification VARCHAR(50),
    IsEndangered TINYINT(1) NOT NULL
);

CREATE TABLE Exhibit (
    ExhibitId INT AUTO_INCREMENT PRIMARY KEY,
    Name VARCHAR(100) NOT NULL,
    Location VARCHAR(200),
    Capacity INT NOT NULL,
    IsOpen TINYINT(1) NOT NULL DEFAULT 1
);

CREATE TABLE Zookeeper (
    ZookeeperId CHAR(36) NOT NULL PRIMARY KEY,
    FirstName VARCHAR(50) NOT NULL,
    LastName VARCHAR(50) NOT NULL,
    Email VARCHAR(200),
    HireDate DATE NOT NULL,
    Specialty VARCHAR(100)
);

CREATE TABLE Animal (
    AnimalId INT AUTO_INCREMENT PRIMARY KEY,
    Name VARCHAR(100) NOT NULL,
    SpeciesId INT NOT NULL,
    ExhibitId INT,
    ZookeeperId CHAR(36) NOT NULL,
    DateOfBirth DATETIME(3),
    Weight DECIMAL(8,2),
    Notes TEXT,
    FOREIGN KEY (SpeciesId) REFERENCES Species(SpeciesId),
    FOREIGN KEY (ExhibitId) REFERENCES Exhibit(ExhibitId),
    FOREIGN KEY (ZookeeperId) REFERENCES Zookeeper(ZookeeperId)
);

CREATE TABLE FeedingSchedule (
    AnimalId INT NOT NULL,
    DayOfWeek INT NOT NULL,
    TimeSlot VARCHAR(10) NOT NULL,
    FoodType VARCHAR(100) NOT NULL,
    Quantity DECIMAL(6,2) NOT NULL,
    AssignedKeeperId CHAR(36),
    PRIMARY KEY (AnimalId, DayOfWeek, TimeSlot),
    FOREIGN KEY (AnimalId) REFERENCES Animal(AnimalId),
    FOREIGN KEY (AssignedKeeperId) REFERENCES Zookeeper(ZookeeperId)
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
