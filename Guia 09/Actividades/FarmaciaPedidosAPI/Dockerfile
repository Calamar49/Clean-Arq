# Etapa de compilación
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copia inicial del archivo de proyecto para restaurar dependencias
COPY ["FarmaciaPedidosAPI.csproj", "./"]
RUN dotnet restore "FarmaciaPedidosAPI.csproj"

# Copia del resto del proyecto
COPY . .

# Publicación en modo Release
RUN dotnet publish "FarmaciaPedidosAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Etapa final de ejecución
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Copia de los archivos publicados
COPY --from=build /app/publish .

# Ejecución sin privilegios de root dentro del contenedor
USER $APP_UID

# Puerto expuesto por la aplicación
EXPOSE 8080

# Configuración del servidor web interno
ENV ASPNETCORE_URLS=http://+:8080

# Punto de entrada
ENTRYPOINT ["dotnet", "FarmaciaPedidosAPI.dll"]
