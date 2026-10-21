@echo off
REM Levanta los cuatro microservicios en ventanas separadas.
REM Requisitos previos: MySQL/MariaDB y RabbitMQ en ejecucion.

cd /d "%~dp0.."

start "Habitaciones" cmd /k dotnet run --project src\Habitaciones\Habitaciones.Api --launch-profile https
start "Reservas"     cmd /k dotnet run --project src\Reservas\Reservas.Api --launch-profile https
start "Pagos"        cmd /k dotnet run --project src\Pagos\Pagos.Api --launch-profile https
start "Notificaciones" cmd /k dotnet run --project src\Notificaciones\Notificaciones.Api --launch-profile https
start "Cliente Web"   cmd /k dotnet run --project src\Web\ClienteHotelMVC --launch-profile https

echo.
echo Servicios iniciados:
echo   Habitaciones   https://localhost:7301/swagger
echo   Reservas       https://localhost:7302/swagger
echo   Pagos          https://localhost:7303/swagger
echo   Notificaciones https://localhost:7304/swagger
echo   Cliente web    https://localhost:7300
echo.
pause
