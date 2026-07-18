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
