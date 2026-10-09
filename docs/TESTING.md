# Pruebas y evidencia

Actualizado: 9 de octubre de 2026. No confundir compilación con ejecución ni respuestas controladas con persistencia real.

## Resultado observado

Evidencia: [CI 37986378549](https://github.com/Sebastian2759/facturacion-prueba-tecnica/actions/runs/37986378549), commit `21a36f9a246727db3f602d58cb1055dc4528c515`, 9 de octubre de 2026.

| Comprobación | Resultado |
|---|---|
| Build integrado / formato .NET | Aprobados; build local 0 errores y 0 advertencias. |
| Tests .NET en CI Linux | **149 aprobados, 0 omitidos**. Incluyen HTTP/WAF, modelo/snapshot y 5 integraciones SQL. |
| SQL Server real | 5 aprobadas: round-trip/propiedad, concurrencia/idempotencia, rollback, paginación y permisos SELECT/INSERT. |
| Angular build / formato | Aprobados. Node 24.21.0 LTS/npm 11.13.0; toolchain portable verificado. |
| Frontend unitarias / proxy | 14 unitarias y 4 pruebas HTTP del proxy aprobadas. |
| Playwright con respuestas controladas | 7 aprobadas; E2E live navegador/API/SQL todavía pendiente. |
| Axe escritorio/móvil | Sin violaciones detectadas en los casos revisados; no certificación completa. |
| Gitleaks / Semgrep / NuGet / npm | Gates aprobados en la ejecución indicada. |
| Trivy filesystem e imagen ASP.NET | Aprobados. |
| Trivy imagen oficial SQL Server | **Bloquea la entrega**; hallazgos en investigación, sin excepción. |
| Backup/restore, smoke del ZIP, paquete final | Pendientes; no se etiqueta como release verificada. |
| Instalación Windows con Docker | No ejecutada; Docker/WSL ausentes en el equipo local. |
| Novasoft real | No ejecutado; pruebas HTTP controladas y simulación local. |

WDAC 0x800711C7 continúa afectando la ejecución .NET del equipo Windows. Las 149 pruebas aprobadas pertenecen al entorno CI Linux, no a una evasión de esa política.

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
| Test/Invoices/InvoiceSqlIntegrationTests | Agregado real, aislamiento A/B, paginación, concurrencia, rollback y funcionamiento con SELECT/INSERT sin UPDATE/DELETE. |
| Test/Security | Sesión real de LocalEvaluation mediante WebApplicationFactory, Host/Origin/CSRF, cookies, límites, errores y fixtures sin SQL. |
| Test/Accounts | Cliente HTTP con respuestas controladas, cache de token, simulación y permisos. |

La migración inicial manual superó HasPendingModelChanges/modelo y aplicación a SQL en la ejecución CI indicada. Ese gate debe seguir ejecutándose ante nuevos cambios. Las pruebas HTTP locales no reemplazan el smoke de contenedores ni las de persistencia.

## Frontend

Desde Frontend:

```powershell
npm ci
npm run build
npm test
npm run test:proxy
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

El workflow vigente es [.github/workflows/quality.yml](../.github/workflows/quality.yml). Restore, auditorías, análisis, build/formato y tests .NET/SQL/UI pasaron en la ejecución indicada. Trivy de SQL bloqueó el avance al empaquetado; no se omite ni silencia el gate.

El empaquetado exige backup real vacío, RESTORE VERIFYONLY y restore a otra base, CHECKDB, verificación de esquema/permisos y smoke creando/consultando una factura. Luego analiza imágenes/paquete, genera SBOM/manifiesto/checksums, extrae ZIP y lo comprueba. El script PrepareOnly no acredita estas verificaciones.

Registrar por comprobación: comando, commit, herramienta/versión, fecha, resultado y artifact. Las categorías son aprobado, fallido, pendiente u omitido con motivo. No transformar omitido/bloqueado en aprobado. Antes de entrega se debe probar Windows con Docker a partir del ZIP descargado, además de CI Linux.
