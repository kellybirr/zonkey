CREATE TABLE species (
    species_id      serial PRIMARY KEY,
    name            varchar(100) NOT NULL UNIQUE,
    classification  varchar(50),
    is_endangered   boolean NOT NULL DEFAULT false
);

CREATE TABLE zookeeper (
    zookeeper_id    uuid PRIMARY KEY,
    first_name      varchar(50) NOT NULL,
    hire_date       date NOT NULL,
    created_utc     timestamptz NOT NULL,     -- DbType.DateTime + DateTimeKind.Utc
    local_noted_at  timestamp                  -- DbType.DateTime2
);

CREATE TABLE animal (
    animal_id       bigserial PRIMARY KEY,
    name            varchar(100) NOT NULL,
    species_id      integer NOT NULL REFERENCES species(species_id),
    zookeeper_id    uuid NOT NULL REFERENCES zookeeper(zookeeper_id),
    weight_kg       numeric(8,2),
    head_count      numeric(9,0),              -- narrows to int
    big_count       numeric(18,0),             -- narrows to long
    notes           text,
    photo           bytea
);

CREATE TABLE feeding_schedule (
    animal_id       bigint NOT NULL REFERENCES animal(animal_id),
    day_of_week     smallint NOT NULL,
    time_slot       varchar(20) NOT NULL,
    quantity        numeric(6,2) NOT NULL,
    PRIMARY KEY (animal_id, day_of_week, time_slot)
);

CREATE VIEW animal_names AS SELECT animal_id, name FROM animal;

CREATE SCHEMA archive;
CREATE TABLE archive.animal (
    animal_id       bigint PRIMARY KEY,
    name            varchar(100) NOT NULL
);
