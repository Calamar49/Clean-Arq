// Prueba de concurrencia del servicio de Habitaciones.
// Lanza N solicitudes simultáneas para bloquear la MISMA habitación y las MISMAS noches.
// Resultado esperado: exactamente 1 éxito (200) y N-1 rechazos (409).
//
// Uso: node scripts/prueba_concurrencia.js [N] [habitacionId] [entrada] [salida] [urlBase]
const crypto = require("crypto");

const N = parseInt(process.argv[2] || "20", 10);
const habitacionId = parseInt(process.argv[3] || "1", 10);
const entrada = process.argv[4] || "2026-12-20";
const salida = process.argv[5] || "2026-12-23";
const urlBase = process.argv[6] || "http://localhost:5301";

async function bloquear() {
  const inicio = performance.now();
  const respuesta = await fetch(`${urlBase}/api/bloqueos`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({
      reservaId: crypto.randomUUID(),
      habitacionId,
      fechaEntrada: entrada,
      fechaSalida: salida,
    }),
  });
  return { status: respuesta.status, ms: performance.now() - inicio };
}

(async () => {
  console.log(`Lanzando ${N} solicitudes simultáneas: habitación ${habitacionId}, ${entrada} -> ${salida}`);
  const inicio = performance.now();
  const resultados = await Promise.all(Array.from({ length: N }, bloquear));
  const total = performance.now() - inicio;

  const conteo = {};
  resultados.forEach((r) => (conteo[r.status] = (conteo[r.status] || 0) + 1));
  const tiempos = resultados.map((r) => r.ms).sort((a, b) => a - b);

  console.log("Códigos HTTP obtenidos:", conteo);
  console.log(`Éxitos (200): ${conteo[200] || 0} | Rechazos (409): ${conteo[409] || 0}`);
  console.log(
    `Tiempo total: ${total.toFixed(0)} ms | mediana: ${tiempos[Math.floor(N / 2)].toFixed(0)} ms | máximo: ${tiempos[N - 1].toFixed(0)} ms`
  );
  console.log((conteo[200] || 0) === 1 ? "RESULTADO: OK - sin overbooking" : "RESULTADO: FALLO - revisar");
})();
