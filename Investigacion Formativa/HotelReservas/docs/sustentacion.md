# Guion de sustentación (15 minutos)

## Antes de empezar

Con el proyector ya conectado y **al menos 10 minutos antes**:

1. Iniciar **MySQL** desde el panel de XAMPP y comprobar que **RabbitMQ** está en ejecución.
2. Ejecutar `scripts\iniciar_servicios.bat` y esperar a que las cinco ventanas terminen de arrancar.
3. Abrir estas pestañas en el navegador, en este orden:
   - `https://localhost:7300` — aplicación del hotel
   - `http://localhost:15672` — panel de RabbitMQ (`guest` / `guest`)
   - `http://localhost/phpmyadmin` — bases de datos
4. Dejar la pasarela en **modo simulado**. No depender de internet durante la exposición.
5. Limpiar los datos de prueba para empezar con el hotel vacío.

> Si algo falla en el arranque, la demostración puede hacerse igualmente desde Swagger. Tener `https://localhost:7302/swagger` como alternativa.

---

## Minutos 0 – 2 · El problema

> «Un hotel recibe reservas por su web. Si dos personas intentan reservar la misma habitación para las mismas noches en el mismo instante, un sistema mal diseñado acepta las dos. Eso es *overbooking*, y obliga al hotel a reubicar o indemnizar al huésped.
>
> Hay un segundo problema: si la habitación se bloquea pero el pago falla después, esa habitación queda retenida sin haberse vendido. Y como cada servicio tiene su propia base de datos, no existe una transacción que pueda deshacerlo automáticamente.
>
> Nuestro prototipo resuelve ambos.»

---

## Minutos 2 – 5 · La arquitectura

Mostrar el **diagrama de la figura 1** del informe.

Puntos a cubrir, uno por uno:

- **Cuatro microservicios**, cada uno con **su propia base de datos**. Enseñar phpMyAdmin con las cuatro bases: `db_habitaciones`, `db_reservas`, `db_pagos`, `db_notificaciones`. *«Ningún servicio consulta la base de otro. Si Reservas necesita el precio de una habitación, se lo pregunta a Habitaciones por su API.»*

- **RabbitMQ** conecta los servicios. Enseñar el panel con las colas. *«Cada servicio tiene tres colas: la principal, una de reintento y una de errores.»*

- **Clean Architecture** en cada servicio. *«Las reglas de negocio no dependen de la base de datos ni de la mensajería, y por eso pudimos probarlas con un 79 % de cobertura.»*

---

## Minutos 5 – 12 · Demostración en vivo

### A. Reserva correcta (2 min)

1. Buscar disponibilidad para unas fechas.
2. Elegir una habitación.
3. Completar nombre y correo, y pulsar *Continuar al pago*.
4. En la pantalla de pago, dejar seleccionada la **tarjeta **** 4242** y pulsar *Pagar y confirmar*.
5. **Detenerse en la pantalla de seguimiento.** Dejar que avance sola.

> «El proceso tiene tres pasos, como cualquier compra: habitación, datos y pago. Hasta que no se confirma el pago no se crea nada.
>
> Ahora fíjense en que la página se actualiza sola. La reserva pasa por tres estados: se registra la solicitud, se bloquea la habitación y se cobra. Cada paso lo ejecuta un servicio distinto, comunicándose por mensajes.
>
> Un detalle importante: el cobro ocurre **después** de asegurar la habitación, nunca antes. Si no hubiera disponibilidad, no se cobraría nada.»

### B. Sin overbooking (2 min)

Este es el punto central. En la terminal:

```
node scripts/prueba_concurrencia.js 20 1
```

> «Acabamos de lanzar 20 solicitudes **simultáneas** para la misma habitación y las mismas fechas. Resultado: un solo éxito y 19 rechazos.
>
> Lo importante es **dónde** está la garantía. No está en el código de la aplicación, sino en una restricción `UNIQUE` de la base de datos sobre la combinación habitación y fecha. Aunque dos procesos pasaran a la vez la comprobación previa, la base de datos solo deja insertar una fila. Es infalible.»

### C. Compensación: el pago falla (3 min)

No hace falta tocar nada: basta con **elegir la otra tarjeta** en el paso de pago.

1. Hacer una reserva nueva desde la web.
2. En la pantalla de pago, seleccionar la **tarjeta **** 0002**, la que la entidad rechaza.
3. Pulsar *Pagar y confirmar* y mostrar el seguimiento.

> «Aquí la reserva se rechaza. Pero lo interesante no es eso, sino lo que ocurre detrás: la habitación que se había bloqueado **se liberó sola**.
>
> Eso es el patrón **Saga con compensación**. Como no hay una transacción que abarque las tres bases de datos, cuando un paso falla se ejecuta una acción que deshace el anterior.»

Enseñar en phpMyAdmin que la tabla `NochesBloqueadas` está vacía, y volver a buscar en la web para comprobar que la habitación aparece disponible otra vez.

### D. Opcional: la habitación se libera sola (2 min)

Solo si sobra tiempo o si preguntan qué ocurre cuando un servicio no responde. **Requiere preparación**: arrancar el servicio de Reservas con el plazo rebajado a un minuto, porque el valor real son quince.

```
set Vencimiento__MinutosLimite=1
set Vencimiento__IntervaloSegundos=10
```

1. Cerrar la ventana del servicio de **Pagos**.
2. Hacer una reserva normal desde la web.
3. Quedarse en el seguimiento: se detiene en *Bloqueada*, porque nadie va a cobrar.
4. Esperar. Al cumplirse el plazo, la reserva pasa a *Rechazada* sola y la habitación vuelve al buscador.

> «La compensación que acabamos de ver la dispara un evento. Pero ¿y si ese evento nunca llega porque el servicio de Pagos está caído? Esa habitación quedaría retenida para siempre sin haberse vendido.
>
> Por eso añadimos un vencimiento: si una reserva lleva quince minutos bloqueada sin resolverse, se rechaza sola. Y fíjense en que **no inventamos un camino nuevo**: publica exactamente el mismo evento de rechazo de antes. Habitaciones no se entera de si el rechazo vino de una tarjeta o de un reloj.»

---

## Minuto 12 – 15 · Resultados y cierre

Mostrar la tabla de métricas del informe:

| Indicador | Resultado |
|---|---|
| Pruebas automatizadas | 115, todas correctas |
| Cobertura (Domain + Application) | 79 % |
| Overbooking con 50 solicitudes simultáneas | 0 casos |
| Latencia de creación (p95) | 57 ms |
| Mensajes perdidos ante caídas | 0 |
| Cobros duplicados ante reentregas | 0 |

Dos cosas que conviene decir, porque demuestran criterio:

> «Al medir el rendimiento obtuvimos 2,3 reservas por segundo, que nos pareció bajo. Investigando, descubrimos que el cuello de botella era el retraso artificial de 400 milisegundos que habíamos puesto en la pasarela simulada, no la arquitectura. Al quitarlo subió a 14 por segundo. Y para escalar de verdad bastaría con levantar varias instancias del servicio de Pagos sobre la misma cola, sin tocar el diseño.»

> «También queremos ser honestos con CQRS: en nuestras mediciones solo mejoró las consultas un 4,7 %. Con este volumen de datos su valor es arquitectónico, no de velocidad. La ventaja se notaría con un dominio más complejo o con muchas más lecturas que escrituras.»

Cierre con los ODS:

> «El proyecto se vincula con el **ODS 9**, porque demuestra que una infraestructura digital fiable puede construirse con herramientas libres y estar al alcance de un hospedaje pequeño, y con el **ODS 11**, porque en una ciudad turística como Cusco evitar el *overbooking* protege la experiencia del visitante y la reputación del destino.»

---

## Preguntas probables

**¿Por qué microservicios y no una aplicación monolítica?**
> Por el patrón *Database per Service* que pedía el enunciado, y porque permite escalar solo la parte que lo necesita. Lo comprobamos: el cuello de botella estaba en Pagos, y la solución es levantar más instancias solo de ese servicio.

**¿Qué pasa si RabbitMQ se cae?**
> Los mensajes publicados son persistentes y sobreviven a un reinicio del broker. Mientras esté caído, las reservas nuevas no avanzarían; al volver, se procesan. Lo probamos deteniendo servicios y la base de datos.

**¿Por qué Saga coreografiada y no orquestada?**
> Porque el flujo es lineal y corto. Una Saga orquestada añadiría un coordinador central sin beneficio claro a esta escala. Con más pasos o más ramificaciones, la orquestada sería preferible.

**¿Y si el mismo mensaje llega dos veces?**
> Está contemplado. En el cobro hay tres barreras: una tabla de mensajes procesados, una comprobación de pago previo y una restricción `UNIQUE` sobre la reserva. Lo probamos reenviando el mismo evento tres veces y solo se registró un cobro.

**¿Usaron una pasarela de pago real?**
> Sí. El servicio depende de una interfaz con dos implementaciones: una simulada y otra conectada al entorno de prueba de Stripe, con cobros reales contra su sandbox. Presentamos con la simulada para no depender de internet, pero podemos mostrar Stripe si lo desean.

**¿Dónde se guardan los datos de la tarjeta?**
> En ningún sitio. El sistema nunca los recibe. El formulario envía solo un identificador del medio de pago elegido, igual que hacen las pasarelas reales, donde el servidor maneja un token y jamás el número de la tarjeta.

**¿Por qué se cobra después de bloquear la habitación y no antes?**
> Porque cobrar sin tener la habitación asegurada obligaría a devolver el dinero. Así, si no hay disponibilidad, la Saga termina en rechazo sin haber intentado ningún cobro. Y si el cobro falla después, la compensación libera la habitación.

**¿Y si el pago nunca llega porque el servicio está caído?**
> La reserva vence. Un proceso en segundo plano del servicio de Reservas busca cada minuto las que llevan más de quince bloqueadas y las rechaza, con lo que sus noches se liberan. Sin eso, esa habitación quedaría retenida indefinidamente: es el único caso en que ningún evento llega a dispararla compensación.

**¿Y si el pago se aprueba justo después de que la reserva venció?**
> Ahí el huésped habría pagado sin reserva. El sistema lo detecta y lo deja registrado con un aviso explícito de que requiere devolución, pero el reembolso todavía es manual. Automatizarlo pide una segunda rama de compensación —que Reservas pida el reembolso y Pagos lo ejecute— y la tenemos identificada como la siguiente mejora.

**¿Qué mejorarían?**
> El patrón *Outbox*. Ahora el evento se publica después de guardar en la base de datos, así que no son atómicos. Si el servicio cayera justo entre ambas operaciones, la reserva quedaría registrada sin que la Saga avanzara. Está identificado en el informe como limitación conocida.

---

## Reparto sugerido

| Parte | Minutos | Contenido |
|---|---|---|
| Integrante 1 | 0 – 5 | Problema y arquitectura |
| Integrante 2 | 5 – 12 | Demostración en vivo |
| Integrante 3 | 12 – 15 | Métricas, ODS y cierre |

Las preguntas las responde quien domine mejor el tema consultado.
