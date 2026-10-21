@echo off
REM Publica el hotel en internet mediante un tunel de Cloudflare.
REM
REM A diferencia de abrir_en_red.bat, esto NO depende de estar en la misma
REM wifi: genera una direccion https publica que funciona desde cualquier
REM lugar. La direccion es aleatoria y deja de existir al cerrar la ventana.
REM
REM IMPORTANTE: el sistema no tiene autenticacion. Cualquiera que reciba la
REM direccion puede reservar y cancelar reservas. Comparta el enlace solo con
REM quien corresponda y cierre esta ventana cuando termine la demostracion.
REM
REM Requisito previo: los servicios en ejecucion (scripts\abrir_en_red.bat
REM o iniciar_servicios.bat), con el cliente web escuchando en el puerto 5300.

setlocal
set CF="C:\Program Files (x86)\cloudflared\cloudflared.exe"

if not exist %CF% (
  echo.
  echo No se encuentra cloudflared. Instalelo con:
  echo    winget install --id Cloudflare.cloudflared
  echo.
  pause
  exit /b 1
)

powershell -NoProfile -Command ^
  "if (Get-NetTCPConnection -LocalPort 5300 -State Listen -ErrorAction SilentlyContinue) { exit 0 } else { exit 1 }"
if errorlevel 1 (
  echo.
  echo El cliente web no esta escuchando en el puerto 5300.
  echo Ejecute primero scripts\abrir_en_red.bat y espere a que arranque.
  echo.
  pause
  exit /b 1
)

echo.
echo Abriendo el tunel. La direccion publica aparecera en unos segundos,
echo dentro de un recuadro, con el aspecto:
echo.
echo     https://algo-algo-algo.trycloudflare.com
echo.
echo Esa es la direccion que debe compartir. Para cerrarla, pulse Ctrl+C
echo o cierre esta ventana: el enlace dejara de funcionar al instante.
echo.

%CF% tunnel --url http://localhost:5300 --no-autoupdate

endlocal
