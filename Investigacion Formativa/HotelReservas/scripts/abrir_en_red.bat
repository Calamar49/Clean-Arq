@echo off
REM Publica el hotel en la red local para que otras maquinas puedan usarlo.
REM
REM Solo se expone el cliente web (puerto 5300). Las cuatro APIs siguen
REM escuchando unicamente en localhost, porque es el servidor quien las llama:
REM el navegador del visitante nunca se comunica con ellas.
REM
REM Requisitos previos: MySQL/MariaDB y RabbitMQ en ejecucion.
REM La regla de cortafuegos exige permisos de administrador; si no los hay,
REM este script lo avisa y el resto sigue funcionando igual.

cd /d "%~dp0.."

echo.
echo Abriendo el puerto 5300 en el cortafuegos de Windows...
REM La regla cubre los perfiles privado y publico porque la wifi de un aula
REM suele clasificarse como publica, y se limita a LocalSubnet para que solo
REM alcancen el sistema los equipos de la misma red.
powershell -NoProfile -Command ^
  "$ya = Get-NetFirewallRule -DisplayName 'HotelReservas 5300' -ErrorAction SilentlyContinue;" ^
  "if ($ya) { '  la regla ya existia'; exit 0 };" ^
  "try {" ^
  "  New-NetFirewallRule -DisplayName 'HotelReservas 5300' -Direction Inbound -Protocol TCP" ^
  "    -LocalPort 5300 -Action Allow -Profile Private,Public -RemoteAddress LocalSubnet | Out-Null;" ^
  "  '  regla creada (solo para equipos de la misma red)'" ^
  "} catch {" ^
  "  '';" ^
  "  '  AVISO: no se pudo crear la regla del cortafuegos.';" ^
  "  '  Cierre esta ventana y vuelva a abrir este archivo con el boton derecho,';" ^
  "  '  usando \"Ejecutar como administrador\". Sin esa regla, Windows bloqueara';" ^
  "  '  las conexiones de sus companeros aunque el sistema este funcionando.';" ^
  "  ''" ^
  "}"

echo.
echo Iniciando los servicios...
start "Habitaciones"   cmd /k dotnet run --project src\Habitaciones\Habitaciones.Api --launch-profile https
start "Reservas"       cmd /k dotnet run --project src\Reservas\Reservas.Api --launch-profile https
start "Pagos"          cmd /k dotnet run --project src\Pagos\Pagos.Api --launch-profile https
start "Notificaciones" cmd /k dotnet run --project src\Notificaciones\Notificaciones.Api --launch-profile https
start "Cliente Web"    cmd /k dotnet run --project src\Web\ClienteHotelMVC --launch-profile red

echo.
echo Esperando a que el cliente web responda...
powershell -NoProfile -Command ^
  "$fin = (Get-Date).AddSeconds(90);" ^
  "while ((Get-Date) -lt $fin) {" ^
  "  if (Get-NetTCPConnection -LocalPort 5300 -State Listen -ErrorAction SilentlyContinue) { break }" ^
  "  Start-Sleep -Milliseconds 700 }" ^
  "$ip = (Get-NetIPConfiguration | Where-Object { $_.IPv4DefaultGateway -ne $null -and $_.NetAdapter.Status -eq 'Up' } | Select-Object -First 1).IPv4Address.IPAddress;" ^
  "if (-not $ip) { $ip = '<ip-no-detectada>' };" ^
  "'';" ^
  "'  Comparta esta direccion con la clase:';" ^
  "'';" ^
  "'      http://' + $ip + ':5300';" ^
  "'';" ^
  "'  Todos deben estar en la misma red wifi. Si alguien no logra entrar,';" ^
  "'  lo mas probable es que esa wifi aisle a los equipos entre si; en ese';" ^
  "'  caso use un punto de acceso del telefono.';" ^
  "'';" ^
  "'  La direccion cambia al cambiar de red: vuelva a ejecutar este archivo.'"

echo.
pause
