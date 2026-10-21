-- Base de datos propia del servicio de Reservas (Database per Service).
-- Ningún otro servicio accede a estas tablas: la comunicación es por mensajes.
DROP DATABASE IF EXISTS db_reservas;
CREATE DATABASE db_reservas CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci;
USE db_reservas;

-- Modelo de ESCRITURA (lado "command" de CQRS).
-- Estados posibles de la Saga:
--   Pendiente  -> solicitud recibida, aún no se bloqueó la habitación
--   Bloqueada  -> el servicio de Habitaciones confirmó el bloqueo de las noches
--   Confirmada -> el pago fue aprobado
--   Rechazada  -> no había disponibilidad o el pago falló (con compensación)
--   Cancelada  -> el huésped canceló una reserva ya confirmada
CREATE TABLE Reservas (
    Id CHAR(36) NOT NULL PRIMARY KEY,
    HabitacionId INT NOT NULL,
    HabitacionNumero VARCHAR(10) NOT NULL,
    HuespedNombre VARCHAR(120) NOT NULL,
    HuespedEmail VARCHAR(150) NOT NULL,
    FechaEntrada DATE NOT NULL,
    FechaSalida DATE NOT NULL,
    Noches INT NOT NULL,
    PrecioPorNoche DECIMAL(10,2) NOT NULL,
    MontoTotal DECIMAL(10,2) NOT NULL,
    -- Referencia al medio de pago elegido. Nunca se guardan datos de tarjeta.
    MetodoPago VARCHAR(40) NOT NULL,
    Estado VARCHAR(20) NOT NULL,
    Motivo VARCHAR(250) NULL,
    FechaCreacion DATETIME NOT NULL,
    FechaActualizacion DATETIME NOT NULL,
    -- Token de concurrencia optimista: EF Core lo compara al actualizar.
    -- Si dos procesos modifican la misma reserva a la vez, el segundo falla
    -- en lugar de pisar silenciosamente el cambio del primero.
    Version INT NOT NULL DEFAULT 1,
    INDEX ix_reservas_estado (Estado),
    INDEX ix_reservas_email (HuespedEmail),
    INDEX ix_reservas_habitacion (HabitacionId)
);

-- Modelo de LECTURA (lado "query" de CQRS).
-- Tabla denormalizada que se actualiza a partir de los eventos de la Saga.
-- Permite consultar el listado sin tocar el modelo de escritura.
CREATE TABLE ReservasVista (
    Id CHAR(36) NOT NULL PRIMARY KEY,
    HabitacionNumero VARCHAR(10) NOT NULL,
    HuespedNombre VARCHAR(120) NOT NULL,
    HuespedEmail VARCHAR(150) NOT NULL,
    FechaEntrada DATE NOT NULL,
    FechaSalida DATE NOT NULL,
    Noches INT NOT NULL,
    MontoTotal DECIMAL(10,2) NOT NULL,
    Estado VARCHAR(20) NOT NULL,
    Motivo VARCHAR(250) NULL,
    FechaCreacion DATETIME NOT NULL,
    FechaActualizacion DATETIME NOT NULL,
    INDEX ix_vista_email (HuespedEmail),
    INDEX ix_vista_estado (Estado)
);
