# Evidencias de evaluación

[Proyecto](../../README.md) · [Pruebas](../TESTING.md) · [Seguridad](../../SECURITY.md)

## Identificación del paquete

- Aplicación: **1.0.2-local.2**.
- Commit de construcción: [`cc1f05e`](https://github.com/Sebastian2759/facturacion-prueba-tecnica/commit/cc1f05e764cb3550395e63ec8020a51fa5e0a004).
- ZIP SHA-256: `3320e99a21c843394a093663d7c525bf071f73ec61bb8f0cb32be9d590889985`.
- Evidencias registradas: **10 de octubre de 2026 UTC**.

La publicación agrega la documentación del evaluador y estos informes al código verificado. El paquete Windows conserva su versión y hash: la publicación del repositorio no se presenta como una recompilación del ZIP. El historial permite consultar el commit exacto de construcción y los ajustes posteriores de documentación.

## Resultados

| Comprobación | Evidencia |
|---|---|
| 155 pruebas backend, incluidas 8 SQL; 0 fallidas/omitidas | [TRX](1.0.2-local.2/reports/tests.trx) |
| 28 unitarias frontend | [Informe](1.0.2-local.2/reports/cleanup-unit.txt) |
| 4 pruebas proxy | [Informe](1.0.2-local.2/reports/cleanup-proxy.txt) |
| 9 recorridos con HTTP controlado | [Informe](1.0.2-local.2/reports/cleanup-browser.txt) |
| API/SQL, cálculo e idempotencia | [Recibo funcional](1.0.2-local.2/reports/cleanup-functional.json) |
| Consulta real Novasoft en navegador | [Informe](1.0.2-local.2/reports/cleanup-accounts-live.txt) y [captura](1.0.2-local.2/accounts-live.png) |
| Extracción final, inicio, backup, restore y reinicio Windows | [Recibo del ZIP final](1.0.2-local.2/windows-final.json) |
| 31 controles automatizados Windows | [Informe](1.0.2-local.2/reports/cleanup-windows.json) |
| Escaneos y decisión SQL | [Informes](1.0.2-local.2/reports) y [decisión](1.0.2-local.2/sql-risk-decision.json) |
| Inventario de componentes | [SBOM](1.0.2-local.2/sbom.cdx.json) |
| 95 criterios con estado y evidencia | [Checklist](1.0.2-local.2/checklist.json) |

El [manifiesto local](1.0.2-local.2/local-verification.json) incluye resultados y hashes originales. Los logs publicados cambian únicamente de extensión `.log` a `.txt`; su contenido y hash se conservan.

## Integración externa y alcance

La consulta de Novasoft en esta versión utiliza un registro ficticio previamente autorizado y no crea nuevos registros. El alta desde formulario y su consulta posterior corresponden a **1.0.1-local.2**: [informe histórico](1.0.2-local.2/historical-1.0.1-local.2/verificacion-adicional.json) y [captura](1.0.2-local.2/historical-1.0.1-local.2/novasoft-formulario.jpg).

Las pruebas con respuestas controladas permiten validar errores, timeouts y contratos; no se atribuyen a una llamada real al proveedor. Las pruebas Windows se ejecutaron en el equipo de preparación; la verificación en un segundo PC figura como no ejecutada en el checklist. La aceptación SQL se conserva en su [análisis de seguridad](../sql-risk-acceptance.md).

## Reproducibilidad

[TESTING](../TESTING.md) describe los comandos y los requisitos para las integraciones. El [workflow](../../.github/workflows/quality.yml) ejecuta los controles remotos por commit; sus resultados se consultan en Actions y se distinguen de esta evidencia local.
