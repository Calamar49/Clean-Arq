-- Base de datos propia del servicio de Pagos (Database per Service).
DROP DATABASE IF EXISTS db_pagos;
CREATE DATABASE db_pagos CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci;
USE db_pagos;

-- Intentos de cobro asociados a una reserva.
CREATE TABLE Pagos (
    Id CHAR(36) NOT NULL PRIMARY KEY,
    ReservaId CHAR(36) NOT NULL,
    Monto DECIMAL(10,2) NOT NULL,
    Moneda VARCHAR(3) NOT NULL,
    Estado VARCHAR(20) NOT NULL,
    Pasarela VARCHAR(30) NOT NULL,
    Referencia VARCHAR(120) NULL,
    Motivo VARCHAR(250) NULL,
    FechaCreacion DATETIME NOT NULL,
    -- Un mismo pago no debe cobrarse dos veces aunque el evento se reentregue.
    CONSTRAINT uq_pagos_reserva UNIQUE (ReservaId),
    INDEX ix_pagos_estado (Estado)
);

-- Registro de los mensajes ya procesados.
-- Es la salvaguarda de idempotencia: si RabbitMQ reentrega un evento,
-- el consumidor lo reconoce y no vuelve a cobrar.
CREATE TABLE MensajesProcesados (
    MessageId CHAR(36) NOT NULL PRIMARY KEY,
    RoutingKey VARCHAR(60) NOT NULL,
    FechaProceso DATETIME NOT NULL
);
