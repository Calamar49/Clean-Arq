# Pasarela de pago

El servicio de Pagos no conoce ninguna pasarela concreta: depende de la interfaz `IPasarelaPago` y la implementación se elige por configuración. Esta inversión de dependencias permite demostrar el sistema sin conexión y, a la vez, integrar una pasarela real sin tocar la lógica de negocio.

## El paso de pago en la interfaz

La compra tiene tres pasos: **elegir habitación → datos del huésped → pago**. La reserva **solo se crea al confirmar el pago**: hasta ese momento no se ha registrado nada ni se ha bloqueado ninguna noche.

El sistema **nunca recibe ni almacena datos de tarjeta**. El formulario ofrece dos tarjetas de prueba identificadas y envía únicamente un identificador del medio elegido (`tarjeta-aprobada` o `tarjeta-rechazada`), que viaja en los eventos de la Saga hasta el servicio de Pagos. Es el mismo principio de las pasarelas reales, donde el servidor solo maneja un token y jamás el número de la tarjeta.

| Tarjeta ofrecida | Identificador | Resultado |
|---|---|---|
| Visa de prueba **** 4242 | `tarjeta-aprobada` | El cobro se aprueba y la reserva se confirma |
| Visa de prueba **** 0002 | `tarjeta-rechazada` | El cobro se rechaza y **las noches se liberan** |

La tarjeta elegida **manda sobre la configuración** de la pasarela simulada, de modo que la compensación puede demostrarse en vivo desde la propia interfaz, sin reiniciar servicios ni editar archivos.

### Momento del cobro

El cargo se realiza **después** de asegurar la habitación, nunca antes. Si no hubiera disponibilidad, la Saga termina en rechazo sin haber intentado ningún cobro.

```
PagoService  ──▶  IPasarelaPago  ──┬──▶  PasarelaSimulada
 (Application)      (Application)  └──▶  PasarelaStripe   (Infrastructure)
```

## Pasarela simulada (por defecto)

Se configura en `src/Pagos/Pagos.Api/appsettings.json`:

```json
"Pasarela": {
  "Proveedor": "Simulada",
  "Moneda": "mxn",
  "Simulada": { "Modo": "AprobarSiempre", "TasaAprobacion": 0.8, "RetrasoMs": 400 }
}
```

| Modo | Comportamiento |
|---|---|
| `AprobarSiempre` | Todos los cobros se aprueban. Camino feliz de la Saga. |
| `RechazarSiempre` | Todos se rechazan. Sirve para demostrar la compensación. |
| `Aleatorio` | Aprueba según `TasaAprobacion`. |

Para cambiar el modo sin editar el archivo, basta una variable de entorno:

```
set Pasarela__Simulada__Modo=RechazarSiempre
dotnet run --project src\Pagos\Pagos.Api
```

## Stripe (entorno de pruebas)

Se usa el **entorno de prueba** de Stripe: claves `sk_test_...` y métodos de pago de prueba. No interviene dinero real.

La clave secreta **no se guarda en el repositorio**. Se registra con el gestor de secretos de .NET, que la almacena en el perfil del usuario:

```
dotnet user-secrets init --project src\Pagos\Pagos.Api
dotnet user-secrets set "Pasarela:Stripe:ClaveSecreta" "sk_test_..." --project src\Pagos\Pagos.Api
```

Después se cambia el proveedor en `appsettings.json`:

```json
"Pasarela": { "Proveedor": "Stripe", "Moneda": "mxn" }
```

El cobro se crea y confirma en una sola llamada (`PaymentIntent` con `confirm = true`), sin formulario de tarjeta, usando un método de pago de prueba:

| Método de prueba | Resultado |
|---|---|
| `pm_card_visa` | El cobro se aprueba |
| `pm_card_visa_chargeDeclined` | El cobro se rechaza |

Para cambiar de tarjeta sin editar el archivo:

```
set Pasarela__Proveedor=Stripe
set Pasarela__Stripe__MetodoPagoPrueba=pm_card_visa_chargeDeclined
dotnet run --project src\Pagos\Pagos.Api
```

> El proyecto de Pagos debe recompilarse después de ejecutar `dotnet user-secrets init`: el identificador de secretos se incrusta en el ensamblado durante la compilación, así que con `--no-build` la clave no se cargaría.

### Pruebas realizadas contra el entorno de Stripe

| Caso | Resultado |
|---|---|
| Cobro con `pm_card_visa` | Aprobado. Referencia real de Stripe (`pi_3USOko...`), reserva `Confirmada` en 3 s |
| Cobro con `pm_card_visa_chargeDeclined` | Rechazado (*Your card was declined*), reserva `Rechazada`, **noches liberadas** y aviso enviado |

Ambos cobros quedan registrados en el panel de Stripe, en el apartado de transacciones del entorno de prueba.

## Recomendación para la sustentación

Conviene presentar con la **pasarela simulada**: no depende de internet, responde de inmediato y permite provocar el rechazo en el momento justo para mostrar la compensación. La integración con Stripe se enseña como evidencia de que la arquitectura admite una pasarela real, y puede demostrarse si hay conexión.

## Idempotencia del cobro

Un mensaje puede reentregarse, así que el cobro nunca debe repetirse. Hay tres barreras:

1. La tabla `MensajesProcesados` registra cada `MessageId` ya atendido.
2. Antes de cobrar se comprueba si la reserva ya tiene un pago.
3. La restricción `UNIQUE (ReservaId)` de la tabla `Pagos` lo impide a nivel de base de datos.

Comprobado: tras reenviar tres veces el mismo evento, sigue existiendo **un solo pago**.
