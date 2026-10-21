-- Base de datos propia del servicio de Habitaciones (Database per Service)
DROP DATABASE IF EXISTS db_habitaciones;
CREATE DATABASE db_habitaciones CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci;
USE db_habitaciones;

-- Catálogo de habitaciones del hotel
CREATE TABLE Habitaciones (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Numero VARCHAR(10) NOT NULL,
    Tipo VARCHAR(20) NOT NULL,
    Capacidad INT NOT NULL,
    PrecioPorNoche DECIMAL(10,2) NOT NULL,
    Activa TINYINT(1) NOT NULL DEFAULT 1,
    CONSTRAINT uq_habitaciones_numero UNIQUE (Numero)
);

-- Una fila por cada noche bloqueada de una habitación.
-- La restricción UNIQUE (HabitacionId, Fecha) es la garantía final contra el
-- overbooking: aunque dos solicitudes lleguen al mismo tiempo, la base de datos
-- solo permite que una de ellas bloquee una noche determinada.
CREATE TABLE NochesBloqueadas (
    Id BIGINT AUTO_INCREMENT PRIMARY KEY,
    HabitacionId INT NOT NULL,
    Fecha DATE NOT NULL,
    ReservaId CHAR(36) NOT NULL,
    CONSTRAINT uq_habitacion_fecha UNIQUE (HabitacionId, Fecha),
    CONSTRAINT fk_noches_habitacion FOREIGN KEY (HabitacionId) REFERENCES Habitaciones (Id),
    INDEX ix_noches_reserva (ReservaId)
);

INSERT INTO Habitaciones (Numero, Tipo, Capacidad, PrecioPorNoche, Activa) VALUES
('101', 'Simple', 1, 120.00, 1),
('102', 'Simple', 1, 120.00, 1),
('103', 'Doble', 2, 180.00, 1),
('104', 'Doble', 2, 180.00, 1),
('201', 'Doble', 3, 210.00, 1),
('202', 'Suite', 4, 350.00, 1),
('203', 'Suite', 4, 350.00, 1),
('301', 'Suite', 6, 520.00, 1);
