// Prueba de concurrencia optimista del servicio de Reservas.
// Lanza N cancelaciones simultáneas sobre LA MISMA reserva.
// Resultado esperado: 1 éxito (200) y N-1 conflictos (409), sin estados inconsistentes.
//
// Uso: node scripts/prueba_concurrencia_reserva.js <reservaId> [N] [urlBase]
const reservaId = process.argv[2];
const N = parseInt(process.argv[3] || "10", 10);
const urlBase = process.argv[4] || "http://localhost:5302";

if (!reservaId) {
  console.error("Falta el id de la reserva.");
  process.exit(1);
}

async function cancelar() {
  const respuesta = await fetch(`${urlBase}/api/reservas/${reservaId}/cancelar`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ motivo: "Prueba de concurrencia" }),
  });
  return respuesta.status;
}

(async () => {
  console.log(`Lanzando ${N} cancelaciones simultáneas sobre la reserva ${reservaId}`);
  const resultados = await Promise.all(Array.from({ length: N }, cancelar));

  const conteo = {};
  resultados.forEach((s) => (conteo[s] = (conteo[s] || 0) + 1));
  console.log("Códigos HTTP obtenidos:", conteo);
  console.log(`Éxitos (200): ${conteo[200] || 0} | Conflictos (409): ${conteo[409] || 0}`);
  console.log((conteo[200] || 0) === 1 ? "RESULTADO: OK - concurrencia controlada" : "RESULTADO: FALLO - revisar");
})();
