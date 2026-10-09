# Pruebas y evidencia

Actualizado: 9 de octubre de 2026. No confundir compilación con ejecución ni respuestas controladas con persistencia real.

## Resultado observado

Evidencia: [CI 37994136672](https://github.com/Sebastian2759/facturacion-prueba-tecnica/actions/runs/37994136672), commit de código `b0b7e25eb93f93b2870281bc2bc716a38ff2b463`, 9 de octubre de 2026 (run10). Se revisaron `tests.trx`, `functional-evidence.json`, `zap-gate.json` y los JSON Trivy descargados de esa ejecución. El recibo funcional indica `functional-verified-not-authorized-for-release`.

| Comprobación | Resultado |
|---|---|
| Build integrado / formato .NET | Aprobados; build local 0 errores y 0 advertencias. |
| Tests .NET en CI Linux | **159 aprobados, 0 omitidos**. Incluyen HTTP/WAF, modelo/snapshot y 5 integraciones SQL. |
| SQL Server real | 5 aprobadas: round-trip/propiedad, concurrencia/idempotencia, rollback, paginación y permisos SELECT/INSERT. |
| Angular build / formato | Aprobados. Node 24.21.0 LTS/npm 11.13.0; toolchain portable verificado. |
| Frontend unitarias / proxy | 28 unitarias y 4 pruebas HTTP del proxy aprobadas en CI. |
| Playwright con respuestas controladas | 7 aprobadas. |
| Playwright live navegador/API/SQL | 1 aprobada en run10, con estilos efectivos y nonce/CSP comprobados; defecto CSS del run7 corregido. |
| Axe escritorio/móvil | Sin violaciones detectadas en los casos revisados; no certificación completa. |
| Gitleaks / NuGet / npm | Gates aprobados en la ejecución indicada. |
| Semgrep | 1.179.0, 31 archivos examinados, 0 hallazgos y 0 errores. PyPI oficial, Python 3.12.3, venv aislado, hashes y wheels obligatorios. |
| Trivy filesystem e imagen ASP.NET | Aprobados. |
| Trivy SQL 2022 / candidato SQL 2025 | SQL 2022 **bloquea la entrega**; SQL 2025 falla el diagnóstico. 71 ocurrencias HIGH, 24 CVE únicos y 0 CRITICAL en cada imagen. Sin excepción SQL. |
| Backup/restore / CHECKDB / migradores / smoke API | Aprobados en CI sobre la aplicación preparada y SQL real. |
| ZAP baseline local | Gate aprobado: sólo informativos risk 0 aceptados, API no-store comprobada y ninguna alerta rechazada. Exit 2 del escáner revisado mediante JSON. |
| OpenAPI | Dos comprobaciones aprobadas: export real del backend y export HTTP del contenedor, con puente consumido por Angular. |
| ZIP final | Pendiente; no se etiqueta como release verificada. |
| Instalación Windows con Docker | No ejecutada; Docker/WSL ausentes en el equipo local. |
| Novasoft real | No ejecutado; pruebas HTTP controladas y simulación local. |

WDAC 0x800711C7 continúa afectando la ejecución .NET del equipo Windows. Las 159 pruebas aprobadas pertenecen al entorno CI Linux, no a una evasión de esa política.

El run10 valida la integración OpenAPI y las 28 unitarias frontend. La corrección CSP/nonce y `inlineCritical: false` resolvió el CSS bloqueado hallado en la captura del run7. El navegador real del run10 comprueba estilos efectivos, estilos Material/CDK, renovación del nonce y ausencia de violaciones CSP; el éxito funcional ya no es la única evidencia visual.

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

El run10 ejecutó este recorrido con API/SQL real y las aserciones de estilos/CSP añadidas tras el defecto visual del run7. Conservar capturas y reportes depurados; no distribuir trazas con cookies. La revisión automática de accesibilidad se complementa con teclado y revisión visual; no se atribuye certificación WCAG a axe.

## Release y CI

El workflow vigente es [.github/workflows/quality.yml](../.github/workflows/quality.yml). Build/formato, tests .NET/SQL/UI, auditorías NuGet/npm, Gitleaks, Semgrep y Trivy filesystem/ASP.NET pasaron. La fase funcional completó backup/restore, CHECKDB, migradores, siete respuestas HTTP con no-store, ambos contratos OpenAPI, navegador real y gate ZAP. Trivy SQL es el único gate previo a empaquetado que falla. La fase final del ZIP no se ejecuta mientras ese bloqueo persista.

La indisponibilidad del contenedor Semgrep quedó resuelta mediante PyPI oficial y un venv aislado. El run10 verificó instalación con hashes, pip check y versión efectiva; `typescript-sast-toolchain.json` registra Python 3.12.3 y SHA-256 del lock `8e93118487a04e96287ba455c0db092f90076f31d9d066ca06e47c4125a959d6`. El análisis utiliza reglas TypeScript locales acotadas; no acredita revisión exhaustiva de seguridad.

El empaquetado exige backup real vacío, RESTORE VERIFYONLY y restore a otra base, CHECKDB, verificación de esquema/permisos y smoke creando/consultando una factura; esos pasos SQL pasaron en el run10. La generación y verificación final del ZIP, SBOM/manifiesto/checksums y escaneo del paquete siguen condicionados a los gates. El script PrepareOnly no acredita estas verificaciones.

Registrar por comprobación: comando, commit, herramienta/versión, fecha, resultado y artifact. Las categorías son aprobado, fallido, pendiente u omitido con motivo. No transformar omitido/bloqueado en aprobado. Antes de entrega se debe probar Windows con Docker a partir del ZIP descargado, además de CI Linux.
# Comprobaciones adicionales en Windows — 9 de octubre de 2026

Se ejecutaron 13 comprobaciones con PowerShell 7 sobre Windows: generación de credenciales independientes, identificador de instalación, ACL NTFS restringida al usuario, conservación de la configuración al repetir el inicio y rechazo de archivos modificados, ausentes, rutas inseguras y estado de paquete no aprobado. Los paquetes utilizados son fixtures sintéticos, no una release de la aplicación. El script reproducible es `scripts/tests/windows-delivery.tests.ps1`; admite `-ReportPath` para registrar resultados sin secretos.

El parser real de Windows PowerShell 5.1 también aceptó los siete scripts del evaluador. Esto acredita sintaxis, no su ejecución: la ejecución directa de scripts con ese motor está deshabilitada en el equipo y no se modificó su política. PowerShell 7 tiene una política local RemoteSigned ya establecida.

Docker y WSL siguen ausentes. No se acreditan instalación, arranque de contenedores, persistencia ni backup/restore en Windows. Esas operaciones conservan únicamente la evidencia CI Linux del run10. La consulta adicional del registro Microsoft devolvió el mismo digest SQL 2022 ya analizado; el bloqueo de seguridad continúa sin excepción autorizada.
