CREATE TABLE species (
    species_id     INT AUTO_INCREMENT PRIMARY KEY,
    name           VARCHAR(100) NOT NULL,
    is_endangered  TINYINT(1) NOT NULL DEFAULT 0,
    UNIQUE KEY ux_species_name (name)
);

CREATE TABLE animal (
    animal_id      BIGINT AUTO_INCREMENT PRIMARY KEY,
    name           VARCHAR(100) NOT NULL,
    species_id     INT NOT NULL,
    weight_kg      DECIMAL(8,2),
    head_count     DECIMAL(9,0),
    notes          TEXT,
    photo          BLOB,
    created_utc    DATETIME NOT NULL,
    CONSTRAINT fk_animal_species FOREIGN KEY (species_id) REFERENCES species(species_id)
);

CREATE TABLE feeding_schedule (
    animal_id      BIGINT NOT NULL,
    day_of_week    SMALLINT NOT NULL,
    time_slot      VARCHAR(20) NOT NULL,
    quantity       DECIMAL(6,2) NOT NULL,
    PRIMARY KEY (animal_id, day_of_week, time_slot)
);

CREATE VIEW animal_names AS SELECT animal_id, name FROM animal;
