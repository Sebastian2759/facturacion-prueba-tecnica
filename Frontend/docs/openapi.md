# Contrato OpenAPI y comprobación de cambios

## Procedencia de la referencia

La referencia se generó desde el **OpenAPI 3.1.1 real** exportado por el backend en CI, sin modificar sus campos requeridos, tipos ni nulabilidades:

- Commit del backend: `d17f159`.
- Ejecución CI: `37987889714`; artefacto: `11643334457`.
- Archivo original: `artifacts/openapi.json`.
- SHA-256 del original: `1569517455f485723d4308786e22969f245d3a2b09126981c3ecf9dde441d602`.
- SHA-256 del JSON canónico: `8134e73acd7a265d099964f8d84367d0f5320db4c177ca076f859e8e3cb6b8fb`.

Los hashes difieren por el orden de claves y formato estable del JSON canónico. El hash acredita correspondencia e integridad; la procedencia se vincula al artefacto y commit indicados. `openapi/backend.schema.json` conserva el documento completo y `src/app/core/openapi.generated.ts` sus tipos generados. Ambos se versionan y se comparan en `api:check`.

## Herramientas reproducibles

`tools/openapi/package.json` y su `package-lock.json` fijan **openapi-typescript 7.13.0** y **TypeScript 5.9.3**, con **Node 24.21.0 LTS y npm 11.13.0** como la aplicación. El generador declara compatibilidad con TypeScript 5.x; por eso se instala en un paquete privado separado. Angular mantiene TypeScript 6.0.2 sin forzar dependencias ni relajar sus requisitos. La salida se formatea con Prettier 3.8.1, fijado por el lock principal.

Desde `Frontend`:

```powershell
npm ci
npm run api:setup
npm run api:check -- ../artifacts/openapi.json
```

Después de revisar un cambio real del backend, ejecutar `npm run api:generate -- ../artifacts/openapi.json`, revisar el diff, y volver a ejecutar check, pruebas y build. El generador escribe la referencia y los tipos desde un archivo local OpenAPI 3.0/3.1 completo; rechaza URLs y referencias a otros archivos. No descarga contratos ni utiliza credenciales.

La referencia JSON ordena claves, conserva arrays y usa saltos de línea estables. El archivo TypeScript se genera directamente con la herramienta y contiene el SHA-256 del JSON canónico. Ese hash acredita correspondencia e integridad del archivo; su origen se verifica mediante el artefacto y commit de CI.

## Consumo real en Angular

`api-contract.ts` define aliases **Wire** directamente desde los tipos generados. La envoltura genérica deriva de los metadatos reales de `ResponseBase`; cada endpoint y envoltura concreta se verifica en `api-contract.compatibility.ts`.

El export declara propiedades opcionales en clases de facturas/envolturas y `number | string` en campos numéricos. Los records de Accounts declaran campos requeridos, incluidos algunos anulables. Los modos son `string`. Estas condiciones se conservan en el transporte.

Los decodificadores comprueban la estructura consumida: campos y colecciones presentes, tipos primitivos, números finitos, formato entero cuando corresponde y modos conocidos. Admiten las cadenas numéricas declaradas por el export y las convierten a números; no recalculan bruto, descuento, IVA ni total. Una respuesta incompleta o modo desconocido produce `response.invalid` conservando `traceId`; no se presenta como simulación ni conexión real.

Los formularios pueden ser más estrictos que el esquema: envían los campos obligatorios por negocio. Las asignaciones a `CreateInvoiceWire`, `PreviewInvoiceWire` y `CreateAccountWire` se comprueban al compilar. No se aplica `Required<T>` ni `NonNullable<T>` globalmente al documento. Véase [revisión de nulabilidad y modos](openapi-contract-review.md).

`api:check` regenera en memoria, compara íntegramente esquema/tipos y compila el puente con **TypeScript 6 de la aplicación** mediante `tsconfig.api-contract.json`. No reescribe ni acepta automáticamente una referencia nueva. Si faltan archivos o existe drift, falla.

## CI

Instalar la herramienta con `npm run api:setup` y ejecutar `npm run api:check -- ../artifacts/openapi.json` después de la exportación autenticada. Repetir contra el export HTTP del contenedor que se empaqueta. La referencia real supera check; las unitarias cubren respuestas incompletas, cadenas numéricas, nulabilidad, modos desconocidos y trazas. Estos comandos no sustituyen las pruebas HTTP, SQL ni las validaciones de ejecución.

Referencia de implementación: [API Node de openapi-typescript](https://openapi-ts.dev/node).
