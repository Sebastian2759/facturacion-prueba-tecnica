# Pruebas y evidencia local

El corte de ajustes de octubre de 2026 usa Docker Desktop Windows con contenedores Linux. La release ci.15 es histórica; sus aprobaciones CI no se atribuyen al código nuevo. Consulte release.json y docs/local-verification.json del ZIP para los resultados finales ligados al payload.

## Comprobaciones del código ajustado

| Comprobación | Resultado observado |
|---|---|
| Backend .NET con SQL Server aislado | 155 aprobadas, 0 fallidas, 0 omitidas. |
| Migración VARCHAR UTF-8 | Preserva datos antiguos y nuevos caracteres Unicode; sys.columns confirma los tipos exigidos; snapshot sin diferencias. |
| Frontend unitarias / proxy | 28 / 4 aprobadas. |
| Navegador con HTTP controlado | 9 aprobadas. El caso live se ejecuta por separado. |
| Novasoft C# real | Login/Bearer y GET general; POST único autorizado, HTTP 201; consulta del código NEXOTEST261010A, HTTP 200 y registro confirmado. |
| Seguridad del paquete | Los escaneos de esta versión se registran en docs/local-verification.json; no se heredan los resultados del paquete anterior. |
| SQL en Trivy | 71 HIGH aceptados, 0 CRITICAL; sin hallazgos nuevos fuera de la política. No es un resultado limpio. |

Los resultados de instalación desde ZIP, backup/restauración, navegador live, ZAP y escaneos del paquete se registran después de construirlo en docs/local-verification.json y docs/evidence. No se deducen de compilar. La solución anterior abrió en Visual Studio con 9/9 proyectos. No se atribuye esa apertura a una nueva compilación del IDE. No se verifican controles remotos de GitHub ni se publica.

## Reproducir

Backend: dotnet restore Example.sln --locked-mode; dotnet test Example.sln -c Release. Defina FACTURACION_TEST_SQL_CONNECTION hacia un SQL de pruebas dedicado: los tests crean bases InvoiceTests_<guid>. Sin conexión, las pruebas SQL se omiten y no acreditan persistencia. Nunca apunte a datos del evaluador.

Frontend, desde Frontend: npm ci; npm run build; npm test; npm run test:proxy; npm run test:e2e. Para Edge instalado, PLAYWRIGHT_CHANNEL=msedge. E2E_LIVE=1 y E2E_BASE_URL=http://127.0.0.1:5080 habilitan e2e/local-live.spec.ts con API/SQL reales y datos ficticios.

Windows: scripts/tests/windows-delivery.tests.ps1 comprueba configuración privada, integridad y lanzadores. Esos fixtures no sustituyen iniciar.cmd desde una extracción nueva. WDAC impidió cargar algunos ensamblados nativos: la suite completa se ejecutó en Linux Docker sin alterar la política del equipo.

La prueba SQL adicional detectó inicialmente pérdida Unicode en parámetros ANSI. Se corrigió usando VARCHAR explícito, collation UTF-8 e IsUnicode para parámetros, según https://learn.microsoft.com/en-us/ef/core/providers/sql-server/columns . La regresión comprueba escritura y lectura reales, además de migración.

Los informes no contienen credenciales, tokens ni datos del directorio externo. Las pruebas y escáneres no son un pentest exhaustivo ni certificación de seguridad.

## Retirada de simuladores del runtime

El corte 1.0.2-local.1 elimina los endpoints de demostración, las facturas de portada y el almacén de Accounts en memoria. Accounts sólo utiliza Novasoft. La suite disminuye al retirar pruebas del ejemplo y del almacén eliminados; se conservan las pruebas de facturación, contratos, seguridad y proveedor. Las nuevas regresiones comprueban la eliminación de la tabla vacía, rollback y rechazo de borrado si contiene datos, 404 de endpoints retirados y rechazo HTTP de escrituras antiguas con mode=simulated.

Las respuestas HTTP controladas existen únicamente en Test y Frontend/e2e: permiten verificar timeouts, errores y validaciones sin generar registros externos. No se compilan en la aplicación entregada. La factura live acredita SQL; la consulta live de Novasoft sólo lee el código ya autorizado, sin repetir el alta:

```powershell
$env:E2E_BASE_URL='http://127.0.0.1:5080'
$env:E2E_NOVASOFT='1'
$env:E2E_ACCOUNT_CODE='NEXOUI261010B'
npm run test:e2e -- e2e/accounts-live.spec.ts
```

El POST real de la versión anterior queda registrado como evidencia histórica, no como una nueva creación de esta versión.
