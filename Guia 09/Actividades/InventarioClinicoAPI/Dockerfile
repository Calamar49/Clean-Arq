# ------------------------------
# ETAPA 1: BUILD
# ------------------------------
# Usamos la imagen SDK de .NET 10 para restaurar, compilar y publicar.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

# Definimos el directorio de trabajo dentro del contenedor.
WORKDIR /src

# Copiamos primero el archivo del proyecto para aprovechar la caché de Docker.
COPY ["InventarioClinicoAPI.csproj", "./"]

# Restauramos las dependencias NuGet del proyecto.
RUN dotnet restore "InventarioClinicoAPI.csproj"

# Copiamos el resto de archivos del proyecto.
COPY . .

# Publicamos la aplicación en modo Release en una carpeta de salida.
RUN dotnet publish "InventarioClinicoAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ------------------------------
# ETAPA 2: RUNTIME
# ------------------------------
# Usamos una imagen más ligera solo para ejecutar la aplicación.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

# Directorio donde vivirá la aplicación publicada.
WORKDIR /app

# Copiamos desde la etapa build únicamente los archivos ya publicados.
COPY --from=build /app/publish .

# Ejecutamos como usuario sin privilegios: si alguien comprometiera la
# aplicación, no tendría root dentro del contenedor.
USER $APP_UID

# Indicamos el puerto que usará la aplicación dentro del contenedor.
EXPOSE 8080

# Variable de entorno para que Kestrel escuche en el puerto 8080.
ENV ASPNETCORE_URLS=http://+:8080

# Comando de inicio del contenedor.
ENTRYPOINT ["dotnet", "InventarioClinicoAPI.dll"]
