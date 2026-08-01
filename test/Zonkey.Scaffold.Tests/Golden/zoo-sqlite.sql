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
