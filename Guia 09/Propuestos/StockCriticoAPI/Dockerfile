# Etapa de compilación
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["StockCriticoAPI.csproj", "./"]
RUN dotnet restore "StockCriticoAPI.csproj"

COPY . .
RUN dotnet publish "StockCriticoAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Etapa final de ejecución
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

# Ejecución sin privilegios de root dentro del contenedor
USER $APP_UID

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "StockCriticoAPI.dll"]
