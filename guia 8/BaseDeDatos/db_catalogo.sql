CREATE DATABASE IF NOT EXISTS db_catalogo CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci;
USE db_catalogo;

CREATE TABLE IF NOT EXISTS Productos (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Codigo VARCHAR(20) NOT NULL UNIQUE,
    Nombre VARCHAR(100) NOT NULL,
    Descripcion VARCHAR(250) NULL,
    Precio DECIMAL(10,2) NOT NULL,
    Stock INT NOT NULL,
    Categoria VARCHAR(100) NOT NULL,
    FechaRegistro DATETIME NOT NULL,
    Activo TINYINT(1) NOT NULL DEFAULT 1
);

INSERT INTO Productos (Codigo, Nombre, Descripcion, Precio, Stock, Categoria, FechaRegistro, Activo)
VALUES
('PRD-001', 'Laptop Lenovo IdeaPad', 'Laptop de 15 pulgadas, 16 GB de RAM y 512 GB SSD', 3200.00, 10, 'Computo', NOW(), 1),
('PRD-002', 'Mouse Logitech M185', 'Mouse inalambrico de 1000 DPI', 85.50, 25, 'Accesorios', NOW(), 1),
('PRD-003', 'Teclado Redragon Kumara', 'Teclado mecanico retroiluminado', 150.00, 15, 'Accesorios', NOW(), 1);
