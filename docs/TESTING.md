# Pruebas y evidencia

Actualizado: 9 de octubre de 2026. No confundir compilación con ejecución ni respuestas controladas con persistencia real.

## Resultado observado

Evidencia: [CI 37989162596](https://github.com/Sebastian2759/facturacion-prueba-tecnica/actions/runs/37989162596), commit `c6b44269ac0944289cb1d848a48510efcd1e5523`, 9 de octubre de 2026 (run7). Artifact `11644926583`, SHA-256 `7722e78ad3d2ffd96e478407e6bbe4cde52ef7f0f697748a6fb16c24dcb3ac2e`; descarga e integridad verificadas.

| Comprobación | Resultado |
|---|---|
| Build integrado / formato .NET | Aprobados; build local 0 errores y 0 advertencias. |
| Tests .NET en CI Linux | **150 aprobados, 0 omitidos**. Incluyen HTTP/WAF, modelo/snapshot y 5 integraciones SQL. |
| SQL Server real | 5 aprobadas: round-trip/propiedad, concurrencia/idempotencia, rollback, paginación y permisos SELECT/INSERT. |
| Angular build / formato | Aprobados. Node 24.21.0 LTS/npm 11.13.0; toolchain portable verificado. |
| Frontend unitarias / proxy | 16 unitarias y 4 pruebas HTTP del proxy aprobadas en CI. |
| Playwright con respuestas controladas | 7 aprobadas. |
| Playwright live navegador/API/SQL | 1 aprobada; la captura reveló CSS bloqueado que las aserciones funcionales no detectaban. |
| Axe escritorio/móvil | Sin violaciones detectadas en los casos revisados; no certificación completa. |
| Gitleaks / Semgrep / NuGet / npm | Gates aprobados en la ejecución indicada. |
| Trivy filesystem e imagen ASP.NET | Aprobados. |
| Trivy imagen oficial SQL Server | **Bloquea la entrega**. Go/OpenSSL; análisis de símbolos confirma 9 CVE Go HIGH sin restricción Windows. Sin excepción. |
| Backup/restore / CHECKDB / migradores / smoke API | Aprobados en CI sobre la aplicación preparada y SQL real. |
| ZAP baseline local | 4 advertencias; correcciones y gate de informativos pendientes de nueva CI. |
| ZIP final | Pendiente; no se etiqueta como release verificada. |
| Instalación Windows con Docker | No ejecutada; Docker/WSL ausentes en el equipo local. |
| Novasoft real | No ejecutado; pruebas HTTP controladas y simulación local. |

WDAC 0x800711C7 continúa afectando la ejecución .NET del equipo Windows. Las 150 pruebas aprobadas pertenecen al entorno CI Linux, no a una evasión de esa política.

Después del run7, la integración del OpenAPI real y 28 unitarias frontend pasaron localmente. Se corrigieron CSP/nonce y la carga de estilos (`inlineCritical: false`); el smoke local de producción confirmó CSS y estilos Material/CDK sin violaciones CSP. Build/formato de los cambios backend también pasó. Estas mejoras y las aserciones live reforzadas aún requieren la siguiente CI; no se atribuyen al run7.

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

El run7 ejecutó este recorrido con API/SQL real. El defecto visual de CSS hallado en su captura demuestra que las aserciones funcionales eran insuficientes: la siguiente ejecución comprobará estilos efectivos y CSP además del guardado. Conservar reporte/trazas depuradas. La revisión automática de accesibilidad se complementa con teclado y revisión visual; no se atribuye certificación WCAG a axe.

## Release y CI

El workflow vigente es [.github/workflows/quality.yml](../.github/workflows/quality.yml). Restore, auditorías, análisis, build/formato y tests .NET/SQL/UI pasaron en la ejecución indicada. El job funcional completó backup/restore, CHECKDB, migradores, smoke y live; terminó fallido por 4 advertencias ZAP. El gate separado de Trivy SQL también bloquea la release; no se omite ni silencia.

El empaquetado exige backup real vacío, RESTORE VERIFYONLY y restore a otra base, CHECKDB, verificación de esquema/permisos y smoke creando/consultando una factura; esos pasos SQL pasaron en el run7. La generación y verificación final del ZIP, SBOM/manifiesto/checksums y escaneo del paquete siguen condicionados a los gates. El script PrepareOnly no acredita estas verificaciones.

Registrar por comprobación: comando, commit, herramienta/versión, fecha, resultado y artifact. Las categorías son aprobado, fallido, pendiente u omitido con motivo. No transformar omitido/bloqueado en aprobado. Antes de entrega se debe probar Windows con Docker a partir del ZIP descargado, además de CI Linux.
