# Contrato OpenAPI y comprobación de cambios

## Estado

Herramienta preparada; referencia inicial pendiente del `artifacts/openapi.json` exportado por el backend real en Linux. No se ha inventado un esquema ni generado tipos desde la documentación manual. `src/app/core/api-contract.ts` continúa siendo el contrato manual vigente hasta revisar ese export.

`api:check` falla mientras falten el export, la referencia generada o la conexión tipada con los modelos de Angular. La preparación del mecanismo no acredita todavía que frontend y OpenAPI coincidan.

## Herramientas reproducibles

`tools/openapi/package.json` y su `package-lock.json` fijan **openapi-typescript 7.13.0** y **TypeScript 5.9.3**, con **Node 24.21.0 LTS y npm 11.13.0** como la aplicación. El generador declara compatibilidad con TypeScript 5.x; por eso se instala en un paquete privado separado. Angular mantiene TypeScript 6.0.2 sin forzar dependencias ni relajar sus requisitos. La salida se formatea con Prettier 3.8.1, fijado por el lock principal.

Desde `Frontend`:

```powershell
npm ci
npm run api:setup
npm run api:generate -- ../artifacts/openapi.json
npm run api:check -- ../artifacts/openapi.json
```

`api:generate` sólo debe ejecutarse con el export real y después de revisar su procedencia. Escribe `openapi/backend.schema.json` y `src/app/core/openapi.generated.ts`; ambos se versionarán al establecer la primera referencia. El comando acepta un archivo local OpenAPI 3.0/3.1 completo y rechaza referencias a URLs u otros archivos. No descarga contratos ni utiliza credenciales.

La referencia JSON ordena claves, conserva arrays y usa saltos de línea estables. El archivo TypeScript se genera directamente con la herramienta y contiene el SHA-256 del JSON canónico. Ese hash acredita correspondencia e integridad del archivo; su origen se verifica mediante el artefacto y commit de CI.

## Integración pendiente del esquema real

1. Descargar `artifacts/openapi.json` del job Linux del commit evaluado y revisar nombres, campos, nulabilidad y envolturas reales.
2. Ejecutar `api:generate` sin modificar el documento para hacer coincidir contratos a mano.
3. Conectar los modelos usados por la aplicación mediante aliases a los tipos generados o comprobaciones estructurales acordes al sentido del transporte: petición de la UI asignable a la del servidor y respuesta del servidor asignable al tipo de transporte que consume la UI. Reservar la igualdad bidireccional para aliases que deban ser idénticos. El archivo `src/app/core/api-contract.compatibility.ts` contendrá las comprobaciones de DTO, peticiones y respuestas realmente consumidos. Véase la [revisión previa de nulabilidad y modos](openapi-contract-review.md).
4. Resolver diferencias reales de contrato de forma coordinada; verificar frontend y backend. No convertir campos opcionales en obligatorios mediante aserciones para ocultar un desacuerdo.
5. Versionar la referencia y ejecutar `api:check` con un nuevo export del mismo backend. La comprobación regenera en memoria, compara íntegramente esquema y tipos y compila la conexión con **TypeScript 6 de la aplicación**, mediante `tsconfig.api-contract.json`. No reescribe archivos en modo check.

Comparar un archivo generado que la aplicación no consume sería insuficiente. Por eso la comprobación exige también el archivo de compatibilidad; éste se escribirá sólo después de inspeccionar el esquema real.

## CI

Instalar el paquete de herramienta con `npm run api:setup` y ejecutar `npm run api:check -- ../artifacts/openapi.json` después de la exportación autenticada. Un cambio exige revisar y regenerar la referencia fuera del paso de comprobación: CI no aceptará automáticamente una referencia nueva. Estos comandos no sustituyen las pruebas HTTP, los escenarios de SQL ni las validaciones de ejecución.

Referencia de implementación: [API Node de openapi-typescript](https://openapi-ts.dev/node).
