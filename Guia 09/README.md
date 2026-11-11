# Guía 09 — Docker y Construcción de Imágenes Seguras en ASP.NET Core

Arquitectura Orientada a Servicios · Universidad Andina del Cusco · 2026-2

Resolución de las dos actividades y los dos ejercicios propuestos.

## Repositorio

```
https://github.com/Calamar49/Clean-Arq/tree/main/Guia%2009
```

## Contenido

```
Guía 09/
  Actividades/
    InventarioClinicoAPI/      Actividad 01 — inventario de insumos médicos
    FarmaciaPedidosAPI/        Actividad 02 — pedidos de farmacia
  Propuestos/
    TriajePacientesAPI/        Propuesto 01 — triaje con prioridad automática
    StockCriticoAPI/           Propuesto 02 — alerta de stock crítico
  capturas/                    evidencias de ejecución
  README.md
  Guía 09 - ... .pdf           enunciado original
```

Cada proyecto es autónomo: su propio `.csproj`, su `Dockerfile` y su `.dockerignore`.

## Puertos

| Proyecto | Imagen | Puerto del host | Endpoint de prueba |
|---|---|---|---|
| InventarioClinicoAPI | `inventario-clinico-api:v1` | 8080 | `/api/inventario` |
| FarmaciaPedidosAPI | `farmacia-pedidos-api:v1` | 8081 | `/api/pedidos` |
| StockCriticoAPI | `stock-critico-api:v1` | 8082 | `/api/stock` |
| TriajePacientesAPI | `triaje-pacientes-api:v1` | 8083 | `/api/triaje` |

Dentro del contenedor **todas** escuchan en el 8080; lo que cambia es la
correspondencia con el host. Swagger queda disponible en `/swagger` de cada una.

## Ejecución con Docker

Desde la carpeta de cada proyecto:

```
docker build -t inventario-clinico-api:v1 .
docker run -d -p 8080:8080 --name inventario-api-contenedor inventario-clinico-api:v1

docker build -t farmacia-pedidos-api:v1 .
docker run -d -p 8081:8080 --name farmacia-api-contenedor farmacia-pedidos-api:v1

docker build -t stock-critico-api:v1 .
docker run -d -p 8082:8080 --name stock-api-contenedor stock-critico-api:v1

docker build -t triaje-pacientes-api:v1 .
docker run -d -p 8083:8080 --name triaje-api-contenedor triaje-pacientes-api:v1
```

Comprobación y mantenimiento:

```
docker images                       # imágenes creadas
docker ps                           # contenedores en ejecución
docker logs inventario-api-contenedor
docker stop inventario-api-contenedor
docker rm inventario-api-contenedor
docker rmi inventario-clinico-api:v1
```

## Ejecución sin Docker

Útil para probar la lógica antes de contenerizar:

```
dotnet run --project "Actividades/InventarioClinicoAPI"   # http://localhost:5080/swagger
dotnet run --project "Actividades/FarmaciaPedidosAPI"     # http://localhost:5081/swagger
dotnet run --project "Propuestos/TriajePacientesAPI"      # http://localhost:5082/swagger
dotnet run --project "Propuestos/StockCriticoAPI"         # http://localhost:5083/swagger
```

## Reglas de negocio de los ejercicios propuestos

### Propuesto 01 — Triaje

`NivelPrioridad` lo calcula el servidor; se ignora lo que envíe el cliente.

| Nivel | Condición |
|---|---|
| Crítico | Temperatura > 39 **o** frecuencia cardíaca > 120 |
| Urgente | Temperatura entre 37 y 39 **o** frecuencia entre 90 y 120 |
| Normal | Cualquier otro caso |

> **Interpretación del enunciado.** La guía lista los dos signos bajo cada
> nivel sin decir si deben cumplirse ambos. Se resolvió con **o** lógico: basta
> un signo alterado para elevar la prioridad. Exigir ambos dejaría en «Normal»
> a un paciente con 40 °C y pulso tranquilo, que es justamente a quien hay que
> atender primero.

El listado devuelve los pacientes ordenados por gravedad, no por Id.

### Propuesto 02 — Stock crítico

`Estado` es un valor derivado, nunca un dato de entrada.

| Estado | Condición |
|---|---|
| Crítico | Stock < StockMinimo |
| Alerta | Stock = StockMinimo |
| Normal | Stock > StockMinimo |

## Decisiones tomadas

**.NET 10 en lugar de .NET 8.** El enunciado pide .NET 8, pero el equipo solo
tiene instalado el SDK de .NET 10 y su plantilla arrastra paquetes de OpenAPI
incompatibles con `net8.0`. Se usó `net10.0` de forma coherente en los cuatro
proyectos y en las imágenes base (`sdk:10.0` y `aspnet:10.0`). El Dockerfile
mantiene exactamente la misma estructura de dos etapas que enseña la guía;
volver a .NET 8 es cambiar la etiqueta de las dos imágenes y el
`TargetFramework`.

**`ControllerBase` en lugar de `Controller`.** La guía usa `Controller` en la
Actividad 01 y `ControllerBase` en la Actividad 02. Se unificó en
`ControllerBase`, que es el adecuado para una Web API: `Controller` añade
soporte de vistas que aquí no se usa.

**`USER $APP_UID` en los cuatro Dockerfiles.** No lo pide el enunciado, pero el
título de la guía habla de *imágenes seguras*: la aplicación corre con un
usuario sin privilegios en vez de root. Si alguien comprometiera el proceso, no
tendría control del contenedor.

**`.dockerignore` en los cuatro proyectos.** Evita copiar `bin/` y `obj/` al
contexto de construcción. Es la buena práctica «no incluir archivos
innecesarios» del punto 5.11.

**Validaciones de entrada añadidas.** Los ejercicios propuestos comprueban
rangos razonables —edad, temperatura, frecuencia cardíaca, stock no negativo—
y responden 400 con un mensaje claro. El enunciado no las pedía explícitamente,
pero una API que acepta una temperatura de 300 °C no es defendible.

## Verificación realizada

Entorno: Docker Engine 29.9.3 sobre WSL 2, contenedores Linux.

Las cuatro imágenes se construyeron y los cuatro contenedores quedaron en
ejecución simultánea:

```
NAMES                       IMAGE                       PORTS                   STATUS
inventario-api-contenedor   inventario-clinico-api:v1   0.0.0.0:8080->8080/tcp  Up
farmacia-api-contenedor     farmacia-pedidos-api:v1     0.0.0.0:8081->8080/tcp  Up
stock-api-contenedor        stock-critico-api:v1        0.0.0.0:8082->8080/tcp  Up
triaje-api-contenedor       triaje-pacientes-api:v1     0.0.0.0:8083->8080/tcp  Up
```

Las cuatro imágenes pesan **344 MB**, el tamaño de la imagen `aspnet:10.0` más
la aplicación publicada. Es el resultado de la compilación en dos etapas: el
SDK, que ronda el gigabyte, se queda en la etapa de construcción y no viaja a
la imagen final.

### Endpoints, probados contra los contenedores

| Comprobación | Puerto | Resultado |
|---|---|---|
| `GET /api/inventario` | 8080 | 4 productos |
| `GET /api/inventario/3` | 8080 | Alcohol medicinal, stock 180 |
| `GET /api/inventario/99` | 8080 | 404 con mensaje |
| `GET /api/pedidos` | 8081 | 2 pedidos |
| `POST /api/pedidos` | 8081 | Id 3 y estado «Pendiente» asignados solos |
| `GET /api/pedidos/3` | 8081 | Devuelve el pedido recién creado |
| `POST` sin medicamento, o cantidad 0 | 8081 | 400 en ambos |
| `GET /api/triaje` | 8083 | Ordenado Crítico > Urgente > Normal |
| `POST` triaje: 40,1 °C / FC 80 | 8083 | Crítico |
| `POST` triaje: 36,5 °C / FC 150 | 8083 | Crítico |
| `POST` triaje: 37,8 °C / FC 75 | 8083 | Urgente |
| `POST` triaje: 36,4 °C / FC 100 | 8083 | Urgente |
| `POST` triaje: 36,5 °C / FC 70 | 8083 | Normal |
| `POST` triaje sin nombre, o 300 °C | 8083 | 400 en ambos |
| `GET /api/stock` | 8082 | Los tres estados representados |
| `GET /api/stock/criticos` | 8082 | Solo los que están bajo el mínimo |
| `POST` stock 5/20, 30/30, 90/10 | 8082 | Crítico, Alerta, Normal |
| `POST` stock negativo | 8082 | 400 |
| `/swagger` | todas | HTTP 200 |

### Usuario sin privilegios

La comprobación de que `USER $APP_UID` surte efecto:

```
docker exec inventario-api-contenedor id
uid=1654(app) gid=1654(app) groups=1654(app)
```

No es root. Si alguien comprometiera la aplicación, no tendría control del
contenedor.

### Arranque limpio

```
docker logs inventario-api-contenedor
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://[::]:8080
info: Microsoft.Hosting.Lifetime[0]
      Application started.
      Hosting environment: Production
```
