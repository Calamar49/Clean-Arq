// Resume la cobertura de código a partir de los informes Cobertura que genera
// Coverlet, agrupando por proyecto y combinando las líneas cubiertas por las
// pruebas unitarias y las de integración.
//
// Uso: node scripts/resumen_cobertura.js [carpetaResultados]
const fs = require("fs");
const path = require("path");

const RAIZ = process.argv[2] || "TestResults";

function buscarInformes(dir) {
  const encontrados = [];
  for (const entrada of fs.readdirSync(dir, { withFileTypes: true })) {
    const ruta = path.join(dir, entrada.name);
    if (entrada.isDirectory()) encontrados.push(...buscarInformes(ruta));
    else if (entrada.name === "coverage.cobertura.xml") encontrados.push(ruta);
  }
  return encontrados;
}

// Nombre del proyecto a partir de la ruta del archivo fuente.
// Coverlet escribe rutas relativas a src/, por ejemplo:
//   Habitaciones/Habitaciones.Application/Services/X.cs
//   BuildingBlocks/Hotel.Contracts/Eventos.cs
// En ambos casos el proyecto es el segundo segmento de la ruta.
function proyectoDe(nombreArchivo) {
  const partes = nombreArchivo.replace(/\\/g, "/").split("/");
  return partes.length >= 2 ? partes[1] : null;
}

const informes = buscarInformes(RAIZ);
if (informes.length === 0) {
  console.error(`No se encontraron informes de cobertura en ${RAIZ}`);
  process.exit(1);
}

// Para cada proyecto: conjunto de líneas cubiertas y conjunto de líneas totales.
const proyectos = new Map();

for (const informe of informes) {
  const xml = fs.readFileSync(informe, "utf8");

  // Cada <class> trae su filename y sus <line number= hits=>
  const clases = xml.split("<class ").slice(1);

  for (const clase of clases) {
    const archivo = (clase.match(/filename="([^"]+)"/) || [])[1];
    if (!archivo) continue;

    const proyecto = proyectoDe(archivo);
    if (!proyecto) continue;

    if (!proyectos.has(proyecto)) {
      proyectos.set(proyecto, { cubiertas: new Set(), totales: new Set() });
    }
    const datos = proyectos.get(proyecto);

    for (const linea of clase.matchAll(/<line number="(\d+)" hits="(\d+)"/g)) {
      const clave = `${archivo}:${linea[1]}`;
      datos.totales.add(clave);
      if (Number(linea[2]) > 0) datos.cubiertas.add(clave);
    }
  }
}

const orden = [...proyectos.entries()].sort((a, b) => a[0].localeCompare(b[0]));

console.log(`Informes combinados: ${informes.length}\n`);
console.log("Proyecto".padEnd(34) + "Líneas".padStart(9) + "Cubiertas".padStart(11) + "Cobertura".padStart(11));
console.log("-".repeat(65));

let totalLineas = 0;
let totalCubiertas = 0;

for (const [proyecto, datos] of orden) {
  const lineas = datos.totales.size;
  const cubiertas = datos.cubiertas.size;
  if (lineas === 0) continue;

  totalLineas += lineas;
  totalCubiertas += cubiertas;

  const porcentaje = ((cubiertas / lineas) * 100).toFixed(1);
  console.log(
    proyecto.padEnd(34) +
    String(lineas).padStart(9) +
    String(cubiertas).padStart(11) +
    `${porcentaje} %`.padStart(11)
  );
}

console.log("-".repeat(65));
const global = ((totalCubiertas / totalLineas) * 100).toFixed(1);
console.log(
  "TOTAL".padEnd(34) +
  String(totalLineas).padStart(9) +
  String(totalCubiertas).padStart(11) +
  `${global} %`.padStart(11)
);

// Cobertura de las capas que concentran las reglas de negocio.
const nucleo = orden.filter(([p]) => p.endsWith(".Domain") || p.endsWith(".Application"));
const lineasNucleo = nucleo.reduce((a, [, d]) => a + d.totales.size, 0);
const cubiertasNucleo = nucleo.reduce((a, [, d]) => a + d.cubiertas.size, 0);

if (lineasNucleo > 0) {
  console.log(
    `\nCapas Domain + Application: ${((cubiertasNucleo / lineasNucleo) * 100).toFixed(1)} % ` +
    `(${cubiertasNucleo}/${lineasNucleo} líneas)`
  );
}
