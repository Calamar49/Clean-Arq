# Saga de reservas (coreografiada)

La reserva de una habitación afecta a varios servicios, cada uno con su propia base de datos. No existe una transacción distribuida: en su lugar se usa una **Saga coreografiada**, donde cada servicio reacciona a los eventos de los demás y, si algo falla, se ejecuta una **acción de compensación**.

## Camino feliz

| # | Servicio | Acción | Evento publicado |
|---|---|---|---|
| 1 | Reservas | Registra la reserva como `Pendiente` | `reserva.solicitada` |
| 2 | Habitaciones | Bloquea las noches (`UNIQUE` por habitación y fecha) | `habitacion.bloqueada` |
| 3 | Reservas | Pasa la reserva a `Bloqueada` | `reserva.bloqueada` |
| 4 | Pagos | Cobra el monto | `pago.aprobado` |
| 5 | Reservas | Pasa la reserva a `Confirmada` | `reserva.confirmada` |
| 6 | Notificaciones | Registra el aviso al huésped | — |

El paso 3 existe para que el cobro nunca se procese antes de que la reserva haya registrado el bloqueo. Sin él, `pago.aprobado` podría llegar mientras la reserva sigue en `Pendiente` y la transición sería inválida.

## Caminos alternativos y compensación

| Situación | Evento | Reacción |
|---|---|---|
| La habitación ya está ocupada | `habitacion.nodisponible` | Reservas pasa a `Rechazada`. No hay nada que compensar: nunca se bloqueó. |
| El pago falla | `pago.rechazado` | Reservas pasa a `Rechazada` y publica `reserva.rechazada`. **Habitaciones libera las noches.** |
| El huésped cancela | `reserva.cancelada` | **Habitaciones libera las noches** y Notificaciones avisa. |

La liberación de noches es la **compensación**: deshace el efecto del paso 2 cuando la Saga no llega a buen término.

## Topología de RabbitMQ

Exchange `hotel.events` de tipo **topic**. Cada servicio tiene tres colas:

| Cola | Función |
|---|---|
| `hotel.<servicio>` | Cola principal, suscrita a sus claves de enrutamiento |
| `hotel.<servicio>.retry` | Retiene el mensaje con un TTL y lo devuelve a la principal al vencer |
| `hotel.<servicio>.dlq` | Cola de errores: mensajes agotados tras N intentos |

Suscripciones:

| Servicio | Claves que consume |
|---|---|
| Habitaciones | `reserva.solicitada`, `reserva.rechazada`, `reserva.cancelada` |
| Reservas | `habitacion.bloqueada`, `habitacion.nodisponible`, `pago.aprobado`, `pago.rechazado` |
| Pagos | `reserva.bloqueada` |
| Notificaciones | `reserva.confirmada`, `reserva.rechazada`, `reserva.cancelada` |

## Tratamiento de fallos

Si un consumidor lanza una excepción, el mensaje pasa a la cola de reintento con un contador en la cabecera `x-intentos`. Al vencer el TTL vuelve a la cola principal. Tras `MaxIntentos` (3 por defecto) se envía a la cola de errores, donde queda para revisión manual sin bloquear al resto.

Cuando reintentar no tiene sentido (un mensaje reentregado cuya reserva ya cambió de estado), el consumidor lanza `EventoIgnorableException`: el mensaje se confirma y se descarta sin pasar por la cola de errores.

## Idempotencia

Los mensajes pueden reentregarse, así que cada paso debe poder repetirse sin efectos adicionales:

- **Habitaciones** comprueba si la reserva ya tiene noches bloqueadas antes de insertarlas; liberar una reserva sin noches simplemente no borra nada.
- **Reservas** apoya la idempotencia en la máquina de estados: repetir una transición ya aplicada lanza `TransicionInvalidaException`, que el consumidor traduce a "evento ignorable".

## Vencimiento de los bloqueos

La compensación descrita arriba la dispara siempre un evento: si el cobro se rechaza, `reserva.rechazada` hace que Habitaciones libere las noches. Pero eso supone que el evento llega. Si el servicio de Pagos está caído el tiempo suficiente, el mensaje se agota en la cola de errores y **nadie libera nunca esas noches**: la reserva se queda en `Bloqueada` y la habitación deja de venderse sin que nadie la haya comprado.

Para cerrar ese caso, el servicio de Reservas ejecuta en segundo plano `VencimientoDeBloqueos`, que cada minuto busca las reservas que llevan más de 15 bloqueadas y las rechaza:

```
Reserva en Bloqueada
      │
      ├── llega pago.aprobado / pago.rechazado  ──▶  la Saga decide (camino normal)
      │
      └── no llega nada en 15 min               ──▶  rechazo por vencimiento
                                                      │
                                                      └──▶ reserva.rechazada
                                                             └──▶ noches liberadas
```

Lo importante del diseño es que **no abre un camino nuevo**: el vencimiento reutiliza el mismo `RechazarAsync` que usa la Saga, así que publica el mismo `reserva.rechazada` de siempre. Habitaciones no necesita enterarse de que ese rechazo vino de un reloj y no de una tarjeta. La diferencia es solo el disparador: un evento en el caso normal, el paso del tiempo en este.

Los plazos se configuran en `appsettings.json` del servicio de Reservas:

```json
"Vencimiento": { "Activo": true, "MinutosLimite": 15, "IntervaloSegundos": 60 }
```

`Activo: false` lo desactiva, lo que resulta útil si en una demostración conviene mantener una reserva bloqueada el tiempo que haga falta.

### El cobro que llega tarde

Queda un caso derivado: que Pagos vuelva en sí **después** del vencimiento y cobre una reserva que ya se rechazó. El huésped habría pagado sin reserva. Hoy el sistema **detecta la situación y la registra** con un aviso explícito —`Cobro aprobado fuera de plazo … Requiere devolución`—, pero la devolución es manual.

Cerrarlo del todo pide una segunda rama de compensación: que Reservas publique una petición de reembolso y que Pagos la ejecute contra la pasarela. Queda identificado como la continuación natural de esta mejora.

## Limitación conocida

La publicación del evento ocurre **después** de guardar en la base de datos, por lo que ambas operaciones no son atómicas. Si el servicio cayera entre ambas, la reserva quedaría registrada sin que la Saga avanzara. El patrón **Outbox** (guardar el evento en la misma transacción y publicarlo luego desde un proceso aparte) cerraría esa ventana; queda identificado como mejora futura.

## Evidencias obtenidas

| Prueba | Resultado |
|---|---|
| Reserva válida | `Pendiente` → `Bloqueada` en ~2 s, 3 noches bloqueadas |
| Habitación ya ocupada | `Rechazada` en ~1 s con motivo explicativo |
| Cancelación | Noches liberadas en ~1 s (compensación) |
| Servicio caído | Los mensajes esperan en la cola; al reiniciar se procesan |
| Base de datos caída | 3 reintentos y envío a la cola de errores |
| Base de datos restablecida | El mensaje pendiente se procesa solo al volver el servicio |
| Pagos caído de forma prolongada | La reserva vence y las noches se liberan solas al cumplirse el plazo |
