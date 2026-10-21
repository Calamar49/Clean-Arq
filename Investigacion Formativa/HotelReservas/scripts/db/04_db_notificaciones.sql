-- Base de datos propia del servicio de Notificaciones (Database per Service).
DROP DATABASE IF EXISTS db_notificaciones;
CREATE DATABASE db_notificaciones CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci;
USE db_notificaciones;

-- Avisos generados para el huésped.
-- El envío de correo se simula: se registra el mensaje que se habría enviado.
CREATE TABLE Notificaciones (
    Id CHAR(36) NOT NULL PRIMARY KEY,
    ReservaId CHAR(36) NOT NULL,
    Destinatario VARCHAR(150) NOT NULL,
    Tipo VARCHAR(30) NOT NULL,
    Asunto VARCHAR(150) NOT NULL,
    Cuerpo TEXT NOT NULL,
    FechaEnvio DATETIME NOT NULL,
    INDEX ix_notif_reserva (ReservaId),
    INDEX ix_notif_destinatario (Destinatario)
);

-- Salvaguarda de idempotencia: evita avisar dos veces por el mismo evento.
CREATE TABLE MensajesProcesados (
    MessageId CHAR(36) NOT NULL PRIMARY KEY,
    RoutingKey VARCHAR(60) NOT NULL,
    FechaProceso DATETIME NOT NULL
);
