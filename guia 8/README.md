# Guía 08 — Microservicio CRUD con MySQL y cliente ASP.NET MVC

Patrón **Database per Service**: cada microservicio tiene su propia base de datos y la aplicación MVC solo lo consume por HTTP. Proyectos en **.NET 10**, Entity Framework Core con `Pomelo.EntityFrameworkCore.MySql` (probado con MariaDB 10.4 de XAMPP).

## Contenido

| Carpeta | Descripción |
|---|---|
| `Actividades/` | Actividades 01 y 02: `ServicioProductosAPI` (Web API, base `db_productos`) y `ClienteProductosMVC` (cliente MVC) |
| `Propuestos/` | Ejercicios Propuestos 01 y 02: `ServicioCatalogoAPI` (base `db_catalogo`, con código único, fecha de registro y estado activo) y `ClienteCatalogoMVC` (validaciones y mensajes de error) |
| `BaseDeDatos/` | Scripts SQL para crear las bases de datos y los datos de prueba |

## Cómo ejecutar

1. Iniciar MySQL/MariaDB (por ejemplo, desde el panel de XAMPP).
2. Ejecutar los scripts de `BaseDeDatos/` (`db_productos.sql` y/o `db_catalogo.sql`).
3. Ajustar la cadena de conexión en `appsettings.json` de la API si el usuario o la contraseña son distintos (por defecto `root` sin contraseña).
4. Iniciar la API y el cliente MVC de la solución elegida (en Visual Studio: *Varios proyectos de inicio*, o con `dotnet run` en cada proyecto).

| Solución | API | MVC |
|---|---|---|
| `Actividades/ArquitecturaServiciosDemo.slnx` | `https://localhost:7231/swagger` | `https://localhost:7180` |
| `Propuestos/EjerciciosPropuestos.slnx` | `https://localhost:7241/swagger` | `https://localhost:7021` |

La dirección de la API que usa cada cliente MVC está en su `Program.cs` (`BaseAddress` del `HttpClient`).
