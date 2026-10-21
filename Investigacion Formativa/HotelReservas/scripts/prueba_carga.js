// Prueba de carga del sistema completo.
// Lanza N reservas con una concurrencia dada y mide:
//   - latencia de la petición de creación (respuesta HTTP)
//   - latencia extremo a extremo de la Saga (hasta Confirmada o Rechazada)
//   - rendimiento en reservas por segundo
//
// Requiere los cuatro microservicios, MySQL y RabbitMQ en ejecución.
//
// Uso: node scripts/prueba_carga.js [total] [concurrencia] [urlBase]
const TOTAL = parseInt(process.argv[2] || "100", 10);
const CONCURRENCIA = parseInt(process.argv[3] || "10", 10);
const BASE = process.argv[4] || "http://localhost:5302";

const ESTADOS_FINALES = ["Confirmada", "Rechazada", "Cancelada"];

const percentil = (valores, p) => {
  if (valores.length === 0) return 0;
  const orden = [...valores].sort((a, b) => a - b);
  return orden[Math.min(orden.length - 1, Math.floor((p / 100) * orden.length))];
};

const resumen = (nombre, valores) => {
  if (valores.length === 0) {
    console.log(`  ${nombre.padEnd(32)} sin datos`);
    return;
  }
  const media = valores.reduce((a, b) => a + b, 0) / valores.length;
  console.log(
    `  ${nombre.padEnd(32)} media=${media.toFixed(0)} ms   ` +
    `p50=${percentil(valores, 50).toFixed(0)} ms   ` +
    `p95=${percentil(valores, 95).toFixed(0)} ms   ` +
    `max=${Math.max(...valores).toFixed(0)} ms`
  );
};

// Reparte las reservas entre habitaciones y fechas distintas para que
// la carga no se concentre en una sola fila de la base de datos.
function datosReserva(indice) {
  const habitacion = (indice % 8) + 1;
  const dia = (indice % 25) + 1;
  const mes = ((Math.floor(indice / 25)) % 6) + 4;
  const f = (d) => `2027-${String(mes).padStart(2, "0")}-${String(d).padStart(2, "0")}`;

  return {
    habitacionId: habitacion,
    huespedNombre: `Huesped de carga ${indice}`,
    huespedEmail: `carga${indice}@ejemplo.com`,
    fechaEntrada: f(dia),
    fechaSalida: f(dia + 1),
  };
}

async function ejecutarReserva(indice, metricas) {
  const inicio = performance.now();

  let respuesta;
  try {
    respuesta = await fetch(`${BASE}/api/reservas`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(datosReserva(indice)),
    });
  } catch {
    metricas.errores++;
    return;
  }

  const latenciaCreacion = performance.now() - inicio;

  if (!respuesta.ok) {
    metricas.rechazadasAlCrear++;
    return;
  }

  metricas.creacion.push(latenciaCreacion);
  const reserva = await respuesta.json();

  // Espera a que la Saga alcance un estado final.
  for (let intento = 0; intento < 300; intento++) {
    const consulta = await fetch(`${BASE}/api/reservas/${reserva.id}`);
    if (consulta.ok) {
      const actual = await consulta.json();
      if (ESTADOS_FINALES.includes(actual.estado)) {
        metricas.saga.push(performance.now() - inicio);
        metricas.estados[actual.estado] = (metricas.estados[actual.estado] || 0) + 1;
        return;
      }
    }
    await new Promise((r) => setTimeout(r, 50));
  }

  metricas.sinCompletar++;
}

(async () => {
  console.log(`Prueba de carga: ${TOTAL} reservas, concurrencia ${CONCURRENCIA}\n`);

  const metricas = {
    creacion: [],
    saga: [],
    estados: {},
    errores: 0,
    rechazadasAlCrear: 0,
    sinCompletar: 0,
  };

  const inicio = performance.now();
  let siguiente = 0;

  // Mantiene constante el número de peticiones en vuelo.
  const trabajadores = Array.from({ length: CONCURRENCIA }, async () => {
    while (siguiente < TOTAL) {
      const indice = siguiente++;
      await ejecutarReserva(indice, metricas);
    }
  });

  await Promise.all(trabajadores);
  const duracion = (performance.now() - inicio) / 1000;

  console.log("Latencias:");
  resumen("creación (respuesta HTTP)", metricas.creacion);
  resumen("Saga completa (extremo a extremo)", metricas.saga);

  console.log(`\nRendimiento:`);
  console.log(`  duración total                   ${duracion.toFixed(1)} s`);
  console.log(`  reservas completadas             ${metricas.saga.length} de ${TOTAL}`);
  console.log(`  rendimiento                      ${(metricas.saga.length / duracion).toFixed(1)} reservas/s`);

  console.log(`\nDesenlaces:`);
  for (const [estado, cantidad] of Object.entries(metricas.estados)) {
    console.log(`  ${estado.padEnd(32)} ${cantidad}`);
  }

  if (metricas.rechazadasAlCrear || metricas.errores || metricas.sinCompletar) {
    console.log(`\nIncidencias:`);
    if (metricas.rechazadasAlCrear)
      console.log(`  rechazadas al crear (HTTP)       ${metricas.rechazadasAlCrear}`);
    if (metricas.errores)
      console.log(`  errores de conexión              ${metricas.errores}`);
    if (metricas.sinCompletar)
      console.log(`  sin alcanzar estado final        ${metricas.sinCompletar}`);
  }
})();
