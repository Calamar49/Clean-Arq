// Mide dos aspectos del modelo de lectura (CQRS):
//   1. Desfase de la proyección: cuánto tarda la vista en reflejar un cambio.
//   2. Latencia de consulta: modelo de lectura frente a modelo de escritura.
//
// Uso: node scripts/medir_cqrs.js [muestras] [urlBase]
const MUESTRAS = parseInt(process.argv[2] || "20", 10);
const BASE = process.argv[3] || "http://localhost:5302";

const percentil = (valores, p) => {
  const orden = [...valores].sort((a, b) => a - b);
  return orden[Math.min(orden.length - 1, Math.floor((p / 100) * orden.length))];
};

const resumen = (nombre, valores) => {
  const media = valores.reduce((a, b) => a + b, 0) / valores.length;
  console.log(
    `  ${nombre.padEnd(34)} media=${media.toFixed(1)} ms  ` +
    `p50=${percentil(valores, 50).toFixed(1)} ms  p95=${percentil(valores, 95).toFixed(1)} ms`
  );
};

async function medirLatencia(url, repeticiones) {
  const tiempos = [];
  for (let i = 0; i < repeticiones; i++) {
    const inicio = performance.now();
    await fetch(url);
    tiempos.push(performance.now() - inicio);
  }
  return tiempos;
}

(async () => {
  console.log(`Creando ${MUESTRAS} reservas y midiendo el desfase de la proyección...\n`);

  const desfases = [];

  for (let i = 0; i < MUESTRAS; i++) {
    const habitacion = (i % 8) + 1;
    const dia = String((i % 20) + 1).padStart(2, "0");

    const respuesta = await fetch(`${BASE}/api/reservas`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        habitacionId: habitacion,
        huespedNombre: `Huesped de prueba ${i}`,
        huespedEmail: `medicion${i}@ejemplo.com`,
        fechaEntrada: `2027-03-${dia}`,
        fechaSalida: `2027-03-${String(Number(dia) + 1).padStart(2, "0")}`,
      }),
    });

    if (!respuesta.ok) continue;

    const reserva = await respuesta.json();
    const inicio = performance.now();

    // Espera activa hasta que la vista refleje la reserva.
    for (let intento = 0; intento < 200; intento++) {
      const vista = await fetch(`${BASE}/api/consultas/reservas/${reserva.id}`);
      if (vista.ok) {
        desfases.push(performance.now() - inicio);
        break;
      }
      await new Promise((r) => setTimeout(r, 10));
    }
  }

  console.log("Desfase de la proyección (consistencia eventual):");
  resumen("creación -> visible en lectura", desfases);

  console.log("\nLatencia de consulta (50 peticiones cada una):");
  const escritura = await medirLatencia(`${BASE}/api/reservas`, 50);
  const lectura = await medirLatencia(`${BASE}/api/consultas/reservas`, 50);
  resumen("modelo de escritura (command)", escritura);
  resumen("modelo de lectura (query)", lectura);

  const mejora = ((1 - lectura.reduce((a, b) => a + b, 0) / escritura.reduce((a, b) => a + b, 0)) * 100);
  console.log(`\n  Diferencia a favor del modelo de lectura: ${mejora.toFixed(1)} %`);
})();
