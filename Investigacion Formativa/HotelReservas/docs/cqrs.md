# CQRS en el servicio de Reservas

El servicio separa las operaciones que **cambian** el estado de las que solo lo **consultan**. Cada lado tiene su propio modelo, su propio controlador y su propia ruta.

| Lado | Ruta | Modelo | Fuente |
|---|---|---|---|
| Comandos | `/api/reservas` | Entidad `Reserva` con sus reglas de negocio | Tabla `Reservas` |
| Consultas | `/api/consultas/reservas` | `ReservaVistaDTO`, plano y sin lógica | Tabla `ReservasVista` |

## Cómo se mantiene el modelo de lectura

La tabla `ReservasVista` no se escribe desde el servicio de comandos. La actualiza una **proyección** que escucha los eventos de la Saga:

| Evento | Efecto en la vista |
|---|---|
| `reserva.solicitada` | Inserta la fila con estado `Pendiente` |
| `reserva.bloqueada` | Estado `Bloqueada` |
| `reserva.confirmada` | Estado `Confirmada` |
| `reserva.rechazada` | Estado `Rechazada` y motivo |
| `reserva.cancelada` | Estado `Cancelada` y motivo |

La proyección usa **su propia cola** (`hotel.reservas.proyeccion`), separada de la del consumidor de la Saga. Así, un fallo en la proyección no detiene el avance de las reservas, y al revés.

## Consistencia eventual

El modelo de lectura se actualiza mediante mensajes, de modo que va unos milisegundos por detrás del de escritura. Es una decisión consciente, no un defecto: a cambio, ambos lados pueden evolucionar y escalar por separado.

Medición sobre 20 reservas:

| Medida | Resultado |
|---|---|
| Desfase medio (creación hasta visible en la lectura) | 23 ms |
| Percentil 50 | 16 ms |
| Percentil 95 | 67 ms |

### Eventos fuera de orden

Un cambio de estado podría llegar antes que el evento de creación. En ese caso la actualización no encuentra la fila, la proyección falla a propósito y la **cola de reintento** devuelve el mensaje unos segundos después, cuando la fila ya existe. El mecanismo de reintentos resuelve el desorden sin código adicional.

## Rendimiento de las consultas

| Modelo | Media | p50 | p95 |
|---|---|---|---|
| Escritura (command) | 18,5 ms | 16,6 ms | 29,6 ms |
| Lectura (query) | 17,7 ms | 16,0 ms | 28,9 ms |

La diferencia es de apenas un **4,7 %**. Conviene ser honestos: con este volumen de datos y un modelo de escritura tan simple, CQRS **no aporta una ganancia de velocidad significativa**. Su valor aquí es arquitectónico:

- La vista está denormalizada y no necesita conversiones ni uniones, así que su coste no crece aunque el modelo de dominio se complique.
- El lado de lectura podría trasladarse a otra base de datos, a una réplica o a una caché sin tocar el de escritura.
- Ambos lados pueden escalarse de forma independiente según su carga real.

La ventaja se notaría con un modelo de dominio más rico (varias tablas relacionadas) o con una proporción alta de lecturas frente a escrituras.
