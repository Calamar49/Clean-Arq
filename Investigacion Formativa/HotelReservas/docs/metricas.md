# Pruebas y métricas

Entorno de medición: Windows 11, .NET 10, MariaDB 10.4 y RabbitMQ 4.3 en la misma máquina. Los servicios se ejecutan en configuración *Debug*, de modo que los valores absolutos serían mejores en producción; lo relevante son las magnitudes y las comparaciones.

## 1. Pruebas automatizadas

| Tipo | Proyecto | Pruebas | Resultado |
|---|---|---|---|
| Unitarias | `Hotel.UnitTests` | 102 | Todas correctas |
| Integración | `Hotel.IntegrationTests` | 13 | Todas correctas |
| **Total** | | **115** | **Todas correctas** |

Las unitarias cubren las reglas de negocio con dobles de prueba (NSubstitute): la máquina de estados de la Saga, las validaciones de cada entidad, la orquestación de los servicios de aplicación, la idempotencia del cobro y el comportamiento de la pasarela simulada.

Las de integración recorren el camino completo (controlador → servicio → repositorio → MySQL) sobre una base de datos separada, `db_habitaciones_test`, que se recrea antes de cada prueba.

```
dotnet test
```

### Cobertura de código

Medida con Coverlet sobre ambas suites combinadas:

| Proyecto | Líneas | Cubiertas | Cobertura |
|---|---:|---:|---:|
| Reservas.Domain | 94 | 90 | **95,7 %** |
| Habitaciones.Domain | 49 | 45 | **91,8 %** |
| Habitaciones.Infrastructure | 98 | 79 | 80,6 % |
| Habitaciones.Application | 95 | 76 | 80,0 % |
| Notificaciones.Application | 57 | 57 | 100,0 % |
| Pagos.Application | 86 | 62 | 72,1 % |
| Pagos.Domain | 51 | 36 | 70,6 % |
| Reservas.Application | 193 | 135 | 69,9 % |
| Notificaciones.Domain | 37 | 22 | 59,5 % |
| Hotel.Contracts | 72 | 37 | 51,4 % |
| Habitaciones.Api | 123 | 58 | 47,2 % |
| Pagos.Infrastructure | 127 | 23 | 18,1 % |
| Hotel.Messaging | 258 | 7 | 2,7 % |
| **Total** | **1340** | **727** | **54,3 %** |

**Capas Domain + Application: 79,0 %** (523 de 662 líneas).

El esfuerzo se concentró deliberadamente en las capas que contienen las reglas de negocio, donde un error tendría consecuencias reales. Las cifras bajas corresponden a código de infraestructura:

- `Hotel.Messaging` (2,7 %) es el cliente de RabbitMQ. Probarlo exigiría un broker dedicado por prueba; su funcionamiento se verificó mediante los ensayos de resiliencia del apartado 4.
- `Pagos.Infrastructure` (18,1 %) incluye la integración con Stripe, que requiere conexión a internet. La pasarela simulada, que sí es verificable sin red, está cubierta.

```
dotnet test --collect:"XPlat Code Coverage" --results-directory TestResults
node scripts/resumen_cobertura.js
```

## 2. Control de concurrencia (sin overbooking)

Se lanzan N solicitudes **simultáneas** para bloquear la misma habitación en las mismas fechas. El resultado correcto es exactamente un éxito.

| Solicitudes simultáneas | Éxitos (200) | Rechazos (409) | Tiempo total |
|---:|---:|---:|---:|
| 20 | **1** | 19 | 306 ms |
| 50 | **1** | 49 | 391 ms |

La garantía no depende del código de la aplicación sino de la restricción `UNIQUE (HabitacionId, Fecha)` de la base de datos: aunque dos procesos pasaran a la vez la comprobación previa, solo uno puede insertar la fila.

Esta misma situación está automatizada en la prueba de integración `Bloquear_ConSolicitudesSimultaneas_SoloUnaTieneExito`.

```
node scripts/prueba_concurrencia.js 20 1
```

## 3. Latencia y rendimiento

Prueba de carga de 100 reservas con 10 peticiones simultáneas, sobre el sistema ya en marcha (sin incluir el arranque en frío).

### Latencia de la petición de creación

| Medida | Valor |
|---|---:|
| Media | 25 ms |
| Percentil 50 | 21 ms |
| Percentil 95 | 57 ms |
| Máximo | 76 ms |

La respuesta al usuario es inmediata porque la creación solo registra la solicitud y publica un evento: el resto de la Saga ocurre después, de forma asíncrona.

> En una primera ejecución *en frío* el percentil 95 subió a 1751 ms por la compilación JIT de las primeras peticiones. Se repitió la medición con el sistema caliente, que es la situación representativa.

### Latencia de la Saga completa

Tiempo desde la solicitud hasta que la reserva alcanza un estado final.

| Configuración del cobro | Mediana | p95 | Rendimiento |
|---|---:|---:|---:|
| Con retraso simulado de 400 ms | 4297 ms | 4500 ms | 2,3 reservas/s |
| Sin retraso (`RetrasoMs = 0`) | **413 ms** | 3454 ms | **14,0 reservas/s** |

**Análisis.** La diferencia de más de seis veces identifica con claridad el cuello de botella: **el retraso artificial de la pasarela simulada**, no la arquitectura. El servicio de Pagos procesa los mensajes de su cola de forma secuencial, de modo que 100 cobros × 400 ms ≈ 40 s, que es prácticamente la duración total medida (43,5 s).

Retirado ese retraso, la Saga completa baja a 413 ms de mediana para cinco saltos de mensajería entre cuatro servicios, lo que resulta razonable.

Para aumentar el rendimiento sin tocar el diseño bastaría con **ejecutar varias instancias del servicio de Pagos**: al compartir la misma cola, RabbitMQ repartiría los mensajes entre ellas. Esa es precisamente la ventaja de desacoplar mediante mensajería.

```
node scripts/prueba_carga.js 100 10
```

## 4. Resiliencia

| Escenario | Comportamiento observado |
|---|---|
| Servicio de Habitaciones detenido | Los mensajes se acumulan en su cola sin perderse. Al reiniciarlo, se procesan en 2 s. |
| Base de datos caída durante el consumo | El mensaje se reintenta 3 veces (5 s entre intentos) y acaba en la cola de errores, sin bloquear al resto. |
| Base de datos restablecida antes de agotar los intentos | El mensaje pendiente **se procesa solo**, sin intervención manual. |
| API de Habitaciones caída al crear una reserva | Respuesta 503 con un mensaje comprensible; las consultas de reservas existentes siguen funcionando. |
| Pago rechazado | La reserva pasa a `Rechazada` y **las noches se liberan automáticamente** (compensación de la Saga). |
| Evento reentregado tres veces | Se registra **un solo cobro**: la idempotencia funciona. |

Ningún escenario provocó pérdida de datos ni estados inconsistentes.

## 5. Consistencia eventual (CQRS)

Desfase entre el modelo de escritura y el de lectura, medido sobre 20 reservas:

| Medida | Valor |
|---|---:|
| Media | 23 ms |
| Percentil 50 | 16 ms |
| Percentil 95 | 67 ms |

Comparación de latencia de consulta:

| Modelo | Media | p50 | p95 |
|---|---:|---:|---:|
| Escritura | 18,5 ms | 16,6 ms | 29,6 ms |
| Lectura | 17,7 ms | 16,0 ms | 28,9 ms |

La ganancia es de apenas un 4,7 %. Con este volumen de datos y un modelo de escritura tan simple, **CQRS no aporta una mejora de rendimiento significativa**; su valor aquí es arquitectónico (separación de responsabilidades y posibilidad de escalar cada lado por separado). La ventaja se notaría con un dominio más complejo o con una proporción mucho mayor de lecturas.

```
node scripts/medir_cqrs.js 20
```

## 6. Resumen

| Indicador | Resultado |
|---|---|
| Pruebas automatizadas | 115, todas correctas |
| Cobertura (Domain + Application) | 79,0 % |
| Overbooking con 50 solicitudes simultáneas | 0 casos |
| Latencia de creación (p95) | 57 ms |
| Latencia de la Saga (mediana, sin retraso artificial) | 413 ms |
| Rendimiento | 14 reservas/s |
| Pérdida de mensajes ante caídas | 0 |
| Cobros duplicados ante reentregas | 0 |
