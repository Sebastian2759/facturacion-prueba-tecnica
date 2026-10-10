# Pruebas y evidencia local

El corte de ajustes de octubre de 2026 usa Docker Desktop Windows con contenedores Linux. La release ci.15 es histórica; sus aprobaciones CI no se atribuyen al código nuevo. Consulte release.json y docs/local-verification.json del ZIP para los resultados finales ligados al payload.

## Comprobaciones del código ajustado

| Comprobación | Resultado observado |
|---|---|
| Backend .NET con SQL Server aislado | 162 aprobadas, 0 fallidas, 0 omitidas. |
| Migración VARCHAR UTF-8 | Preserva datos antiguos y nuevos caracteres Unicode; sys.columns confirma los tipos exigidos; snapshot sin diferencias. |
| Frontend unitarias / proxy | 28 / 4 aprobadas. |
| Navegador con HTTP controlado | 8 aprobadas. El caso live se ejecuta por separado. |
| Novasoft C# real | Login/Bearer y GET general; POST único autorizado, HTTP 201; consulta del código NEXOTEST261010A, HTTP 200 y registro confirmado. |
| Semgrep | 31 archivos, 3 reglas locales, 0 hallazgos; alcance acotado. |
| SQL en Trivy | 71 HIGH aceptados, 0 CRITICAL; sin hallazgos nuevos fuera de la política. No es un resultado limpio. |

Los resultados de instalación desde ZIP, backup/restauración, navegador live, ZAP y escaneos del paquete se registran después de construirlo en docs/local-verification.json y docs/evidence. No se deducen de compilar. No se verificó apertura interactiva del IDE Visual Studio ni controles remotos de GitHub; no hubo publicaciones.

## Reproducir

Backend: dotnet restore Example.sln --locked-mode; dotnet test Example.sln -c Release. Defina FACTURACION_TEST_SQL_CONNECTION hacia un SQL de pruebas dedicado: los tests crean bases InvoiceTests_<guid>. Sin conexión, las pruebas SQL se omiten y no acreditan persistencia. Nunca apunte a datos del evaluador.

Frontend, desde Frontend: npm ci; npm run build; npm test; npm run test:proxy; npm run test:e2e. Para Edge instalado, PLAYWRIGHT_CHANNEL=msedge. E2E_LIVE=1 y E2E_BASE_URL=http://127.0.0.1:5080 habilitan e2e/local-live.spec.ts con API/SQL reales y datos ficticios.

Windows: scripts/tests/windows-delivery.tests.ps1 comprueba configuración privada, integridad y lanzadores. Esos fixtures no sustituyen iniciar.cmd desde una extracción nueva. WDAC impidió cargar algunos ensamblados nativos: la suite completa se ejecutó en Linux Docker sin alterar la política del equipo.

La prueba SQL adicional detectó inicialmente pérdida Unicode en parámetros ANSI. Se corrigió usando VARCHAR explícito, collation UTF-8 e IsUnicode para parámetros, según https://learn.microsoft.com/en-us/ef/core/providers/sql-server/columns . La regresión comprueba escritura y lectura reales, además de migración.

Los informes no contienen credenciales, tokens ni datos del directorio externo. Las pruebas y escáneres no son un pentest exhaustivo ni certificación de seguridad.
