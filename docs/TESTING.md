# Pruebas y evidencia

Actualizado: 9 de octubre de 2026. No confundir compilación con ejecución ni respuestas controladas con persistencia real.

## Resultado observado

| Comprobación | Resultado |
|---|---|
| Build integrado Example.sln | 0 errores y 0 advertencias. |
| Formato .NET | Aplicado; gate CI pendiente. |
| Angular build / format:check | Aprobados. Bundle inicial 344,22 kB, transferencia estimada 92,62 kB. |
| Angular unitarias | 14 aprobadas; además 4 pruebas HTTP del proxy. |
| Playwright Edge | 7 pruebas UI aprobadas; 1 live omitida sin E2E_LIVE. |
| Axe en vistas escritorio/móvil | Sin violaciones detectadas en los casos ejecutados; no certifica accesibilidad completa. |
| Gitleaks del árbol actual | Sin secretos detectados; historial/paquete final se revisan en CI. |
| Tests .NET | Bloqueados por WDAC 0x800711C7 al cargar assemblies. |
| SQL/migración/backup/restore/containers | Pendientes; Docker/WSL no disponible en este equipo. |
| GitHub Actions | Workflow preparado; primera ejecución pendiente. |
| Novasoft real | No ejecutado; sólo fixtures/control HTTP y simulación local. |

## Backend

Desde la raíz, con SDK fijado en global.json:

```powershell
dotnet restore Example.sln --locked-mode
dotnet build Example.sln --no-restore
dotnet test Example.sln --no-build --logger trx
```

Los tests SQL utilizan `FACTURACION_TEST_SQL_CONNECTION` y crean/eliminan bases dedicadas `InvoiceTests_<guid>`. Usar servidor aislado de pruebas con permisos apropiados, nunca una conexión a datos del evaluador. Sin variable se omiten explícitamente; GitHub CI exige que exista. No hay sustituto InMemory/SQLite para acreditar SQL Server.

| Suite | Riesgo cubierto |
|---|---|
| Test/Invoices/InvoiceCalculatorTests | Umbral de descuento, IVA/redondeo, detalle y overflow. |
| Test/Invoices/*ValidatorTests y *QueryTests | Límites, permisos y contratos de casos de uso. |
| Test/Invoices/InvoiceMappingTests | Mapper por convención y DTO de líneas. |
| Test/Invoices/InvoicePersistenceModelTests | Tipos/identity/FK e igualdad snapshot-modelo. |
| Test/Invoices/InvoiceSqlIntegrationTests | Aggregate real, aislamiento A/B, paginación, concurrencia y rollback. |
| Test/Security | Sesión real de LocalEvaluation mediante WebApplicationFactory, Host/Origin/CSRF, cookies, límites, errores y fixtures sin SQL. |
| Test/Accounts | Cliente HTTP con respuestas controladas, cache de token, simulación y permisos. |

La migración inicial manual necesita el test HasPendingModelChanges y aplicación a SQL. No se silencia el resultado. Las pruebas HTTP locales no reemplazan el smoke de contenedores ni las de persistencia.

## Frontend

Desde Frontend:

```powershell
npm ci
npm run build
npm test
npm run format:check
npx playwright install chromium
npm run test:e2e
```

Para Edge instalado: `$env:PLAYWRIGHT_CHANNEL='msedge'`. En este sandbox Windows se usó TEMP/TMP en .angular/test-temp para resolver un fallo de directorio temporal; procedimiento en el [README frontend](../Frontend/README.md).

Los tests comunes interceptan HTTP y cubren cancelación de preview, CSRF/idempotencia en reintento, errores de campo, fallo de sesión, navegación móvil/teclado, Accounts aceptación/consulta y axe. No acreditan SQL ni proveedor real.

La prueba live usa la aplicación preparada, sin mocks, y crea datos ficticios:

```powershell
$env:E2E_LIVE='1'
$env:E2E_BASE_URL='http://127.0.0.1:5080'
npm run test:e2e -- e2e/local-live.spec.ts
```

Conservar reporte/trazas depuradas. La revisión automática de accesibilidad se complementa con teclado y revisión visual; no se atribuye certificación WCAG a axe.

## Release y CI

El workflow vigente es [.github/workflows/quality.yml](../.github/workflows/quality.yml). Debe ejecutar restore locked, auditorías, análisis, build/formato, tests .NET/SQL/UI, scans y empaquetado.

El empaquetado exige backup real vacío, RESTORE VERIFYONLY y restore a otra base, CHECKDB, verificación de esquema/permisos y smoke creando/consultando una factura. Luego analiza imágenes/paquete, genera SBOM/manifiesto/checksums, extrae ZIP y lo comprueba. El script PrepareOnly no acredita estas verificaciones.

Registrar por comprobación: comando, commit, herramienta/versión, fecha, resultado y artifact. Las categorías son aprobado, fallido, pendiente u omitido con motivo. No transformar omitido/bloqueado en aprobado. Antes de entrega se debe probar Windows con Docker a partir del ZIP descargado, además de CI Linux.
