import { createHash } from 'node:crypto';
import { readFile, writeFile, mkdir, access } from 'node:fs/promises';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { spawnSync } from 'node:child_process';
import openapiTS, { astToString } from 'openapi-typescript';
import * as prettier from 'prettier';

const frontend = resolve(dirname(fileURLToPath(import.meta.url)), '../..');
const baseline = resolve(frontend, 'openapi/backend.schema.json');
const generated = resolve(frontend, 'src/app/core/openapi.generated.ts');
const compatibility = resolve(frontend, 'src/app/core/api-contract.compatibility.ts');

function canonical(value) {
  if (Array.isArray(value)) return value.map(canonical);
  if (value !== null && typeof value === 'object') {
    return Object.fromEntries(
      Object.keys(value)
        .sort()
        .map((key) => [key, canonical(value[key])]),
    );
  }
  return value;
}

function assertInternalReferences(value) {
  if (value === null || typeof value !== 'object') return;
  for (const [key, child] of Object.entries(value)) {
    if (
      (key === '$ref' || key === '$dynamicRef') &&
      (typeof child !== 'string' || !child.startsWith('#/'))
    ) {
      throw new Error(
        'El esquema debe estar completo: no se admiten referencias a URLs ni otros archivos.',
      );
    }
    assertInternalReferences(child);
  }
}

async function assertMatches(path, expected) {
  let actual;
  try {
    actual = await readFile(path, 'utf8');
  } catch {
    throw new Error(
      `Falta la referencia verificada ${path}. No se crea automáticamente durante check.`,
    );
  }
  if (actual.replaceAll('\r\n', '\n') !== expected) {
    throw new Error(
      `Drift detectado en ${path}. Revise el cambio del backend y ejecute api:generate con su export real.`,
    );
  }
}

async function main() {
  const [action, sourcePath, ...unexpected] = process.argv.slice(2);
  if (!['generate', 'check'].includes(action) || !sourcePath || unexpected.length) {
    throw new Error('Uso: npm run api:generate|api:check -- ../artifacts/openapi.json');
  }
  if (/^[a-z][a-z\d+.-]*:\/\//i.test(sourcePath)) {
    throw new Error(
      'Use el archivo local exportado por CI. Este comando no descarga esquemas ni autentica servicios.',
    );
  }
  let schema;
  try {
    schema = JSON.parse(await readFile(resolve(sourcePath), 'utf8'));
  } catch {
    throw new Error(
      'No se pudo leer un JSON OpenAPI local. Exporte el contrato desde el backend real antes de generar.',
    );
  }
  if (
    !/^3\.(0|1)\.\d+$/.test(schema?.openapi ?? '') ||
    !schema.paths ||
    !Object.keys(schema.paths).length ||
    !schema.components?.schemas
  ) {
    throw new Error(
      'Se requiere un documento OpenAPI 3.0/3.1 con rutas y esquemas exportados por el backend.',
    );
  }
  assertInternalReferences(schema);
  const normalized = canonical(schema);
  const schemaText = JSON.stringify(normalized, null, 2) + '\n';
  const digest = createHash('sha256').update(schemaText).digest('hex');
  const ast = await openapiTS(normalized, { alphabetize: true });
  const header =
    '/** Generated from the verified local backend OpenAPI export. Do not edit.\n' +
    ` * Canonical schema SHA-256: ${digest}\n */\n`;
  const options = (await prettier.resolveConfig(generated)) ?? {};
  const output = await prettier.format(header + astToString(ast), {
    ...options,
    filepath: generated,
  });
  if (action === 'generate') {
    await mkdir(dirname(baseline), { recursive: true });
    await writeFile(baseline, schemaText, 'utf8');
    await writeFile(generated, output, 'utf8');
    console.log(
      'Generados backend.schema.json y openapi.generated.ts desde el archivo suministrado. Revise el diff y conecte los modelos de la app antes de api:check.',
    );
    return;
  }
  await assertMatches(baseline, schemaText);
  await assertMatches(generated, output);
  try {
    await access(compatibility);
  } catch {
    throw new Error(
      'Falta api-contract.compatibility.ts: el contrato real todavía no está conectado a los modelos consumidos por Angular.',
    );
  }
  const compile = spawnSync(
    process.execPath,
    [
      resolve(frontend, 'node_modules/typescript/lib/tsc.js'),
      '--project',
      resolve(frontend, 'tsconfig.api-contract.json'),
      '--pretty',
      'false',
    ],
    { cwd: frontend, stdio: 'inherit' },
  );
  if (compile.error || compile.status !== 0) {
    throw new Error('Los modelos consumidos por Angular no cumplen el contrato OpenAPI.');
  }
  console.log(
    'OpenAPI coincide con la referencia y con los modelos de Angular (TypeScript de la aplicación).',
  );
}

main().catch((error) => {
  console.error(`Contrato API: ${error.message}`);
  process.exitCode = 1;
});
