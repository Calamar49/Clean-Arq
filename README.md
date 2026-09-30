# ClientesMicroservice

Microservicio de gestión de clientes construido con **.NET 10** siguiendo **Clean Architecture** (Domain, Application, Infrastructure, API).

## Estructura

- **Domain** — entidad `Cliente` y sus reglas de negocio.
- **Application** — DTOs, interfaces (`IClienteRepository`, `IClienteService`) y el servicio de aplicación.
- **Infrastructure** — repositorio en memoria (`ClienteRepository`).
- **API** — controlador REST expuesto con Swagger UI.

## Ejecutar

```
dotnet run --project API
```

Luego abrir `https://localhost:<puerto>/swagger` para probar los endpoints.

## Endpoints

| Método | Ruta | Descripción |
|---|---|---|
| GET | `/api/clientes` | Lista todos los clientes |
| POST | `/api/clientes` | Crea un cliente |
| GET | `/api/clientes/{id}` | Consulta un cliente por id |
| PUT | `/api/clientes/{id}` | Actualiza un cliente |
| DELETE | `/api/clientes/{id}` | Elimina un cliente |
| PATCH | `/api/clientes/{id}/desactivar` | Desactiva un cliente (baja lógica) |
| PATCH | `/api/clientes/{id}/activar` | Reactiva un cliente |
| GET | `/api/clientes/buscar?nombre=...` | Busca clientes por coincidencia parcial de nombre |
