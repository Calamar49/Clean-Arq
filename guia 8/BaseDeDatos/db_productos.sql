CREATE DATABASE db_productos;
USE db_productos;

CREATE TABLE Productos (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Nombre VARCHAR(100) NOT NULL,
    Categoria VARCHAR(100) NOT NULL,
    Precio DECIMAL(10,2) NOT NULL,
    Stock INT NOT NULL
);

INSERT INTO Productos (Nombre, Categoria, Precio, Stock)
VALUES
('Laptop Lenovo', 'Computo', 3200.00, 10),
('Mouse Logitech', 'Accesorios', 85.50, 25),
('Teclado Redragon', 'Accesorios', 150.00, 15);
