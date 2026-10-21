# HotelReservas — Investigación Formativa (Arquitectura Orientada a Servicios)

Sistema de reservas de hotel con **control de concurrencia**, basado en microservicios, mensajería (RabbitMQ) y base de datos por servicio. Evita el *overbooking* (dos reservas para la misma habitación y noche) y compensa el bloqueo de una habitación cuando el pago falla (patrón **Saga**).

## Arquitectura

| Componente | Tipo | Base de datos | Puertos (https / http) |
|---|---|---|---|
| `ClienteHotelMVC` | ASP.NET MVC | — | 7300 / 5300 |
| `Habitaciones.Api` | API | `db_habitaciones` | 7301 / 5301 |
| `Reservas.Api` | API (CQRS) | `db_reservas` | 7302 / 5302 |
| `Pagos.Api` | API + consumidor | `db_pagos` | 7303 / 5303 |
| `Notificaciones.Api` | API + consumidor | `db_notificaciones` | 7304 / 5304 |

Cada servicio sigue Clean Architecture: `Domain` ← `Application` ← `Infrastructure` / `Api`.
`Hotel.Contracts` contiene los eventos compartidos y `Hotel.Messaging` la abstracción de RabbitMQ.

## Estructura

```
src/
  BuildingBlocks/  Hotel.Contracts, Hotel.Messaging
  Habitaciones/    .Domain .Application .Infrastructure .Api
  Reservas/        .Domain .Application .Infrastructure .Api
  Pagos/           .Domain .Application .Infrastructure .Api
  Notificaciones/  .Domain .Application .Infrastructure .Api
  Web/             ClienteHotelMVC
tests/
docs/
scripts/
```

## Requisitos

.NET 10 SDK, MariaDB/MySQL (XAMPP), RabbitMQ (`guest/guest` en `localhost:5672`).

## Estado

- [x] Fase 0 y 1: definición y esqueleto de la solución
- [x] Fase 2: Habitaciones y control de concurrencia
- [x] Fase 3: Reservas (comandos y máquina de estados de la Saga)
- [x] Fase 4: Mensajería y Saga (RabbitMQ, reintentos, cola de errores, compensación)
- [x] Fase 5: Pagos (pasarela simulada + Stripe) y Notificaciones
- [x] Fase 6: CQRS (modelo de lectura alimentado por eventos)
- [x] Fase 7: Cliente MVC (búsqueda, reserva, seguimiento en vivo y consulta)
- [x] Fase 8: Pruebas (106) y métricas (latencia, resiliencia, cobertura)
- [x] Fase 9: Informe, evidencias y guion de sustentación
