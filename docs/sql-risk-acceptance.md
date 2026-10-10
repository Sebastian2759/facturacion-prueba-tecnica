# Aceptación del riesgo SQL para la evaluación local

**Estado vigente — Windows verificado:** `Aplicacion-1.0.0-ci.15.zip`, commit `89be6447e9f69fb7b6b24be2dd90b9fd0fbff6c6`, generado por [CI run15](https://github.com/Sebastian2759/facturacion-prueba-tecnica/actions/runs/38022085155), fue descargado y probado sin modificar en Windows 11 x64 con Docker Desktop y los lanzadores de PowerShell 5.1. Pasaron arranque desde extracción nueva, factura por API y navegador, persistencia después de detener/iniciar, backup, restauración en una base nueva y CHECKDB. SHA-256: `fd333c43e0ad90a9ef1055481761c211557b1fa95eb6975373ecc15f74799652`. SQL mantiene riesgo aceptado, no corregido. Las referencias a SVM desactivado o pruebas Windows pendientes que se conservan más abajo pertenecen a cortes históricos anteriores a esta validación.

[Detalle de la prueba Windows](windows-validation.md).

**Decisión aprobada por el propietario:** Sebastian Forero / cuenta personal Sebastian2759. El usuario confirmó en el chat: «ya te dije que la aceptemos dejando claro el por que». Se registra el 9 de octubre de 2026, 21:32 de Colombia (10 de octubre, 02:32 UTC), con identificador **SQL-LOCAL-2026-10-09**.

## Motivo y límites

Se acepta el riesgo residual para entregar esta prueba técnica local con datos ficticios y SQL Server, motor requerido por el ejercicio. Las alternativas oficiales SQL 2025 y RHEL examinadas también presentan hallazgos; no resuelven el problema. Actualizar OpenSSL por separado no corrige Go incorporado en binarios propietarios. No se eliminarán esos binarios para ocultar el diagnóstico.

La decisión considera SQL sin puerto publicado, red Docker interna, aplicación accesible sólo por loopback, runtime SQL con SELECT/INSERT y migrador separado, scripts externos deshabilitados, sesión/CSRF y validación. Estos controles reducen exposición; **no reparan los componentes ni demuestran que sean inexplotables**. El riesgo incluye posibles filtraciones o denegación de servicio si se alcanzan las condiciones de los fallos. No hay un exploit demostrado mediante esta aplicación ni una declaración colectiva de falsos positivos.

Se autoriza exclusivamente el perfil `local-evaluation` con la imagen:

```text
mcr.microsoft.com/mssql/server:2022-latest@sha256:4402d880dd4c34bfa7d8705e56a86cd6c88da80a1f6bbbe741f999e76264a090
```

El inventario exacto aprobado está en [sql-risk-acceptance.json](sql-risk-acceptance.json): **71 ocurrencias HIGH de 24 CVE**, con ruta del componente, paquete y versión. El [análisis técnico](sql-runtime-risk-assessment.md) conserva evidencias, alternativas y límites. Los hallazgos quedan **abiertos con riesgo aceptado**, no corregidos ni ocultos.

## Vigencia y comprobación

La ventana para generar nuevas entregas vence el **23 de octubre de 2026 a las 21:32 de Colombia / 24 de octubre a las 02:32 UTC**. Es un límite de 14 días para esta aceptación, no una garantía del proveedor ni una obligación de NIST/OWASP. No se renueva automáticamente. El vencimiento **no apaga ni borra** una instalación ya entregada; el evaluador debe detenerla al terminar la evaluación.

Cada ejecución conserva el reporte Trivy completo y comprueba un escaneo reciente, digest/plataforma, perfil, vigencia y cada ocurrencia. Nuevos CVE, severidad CRITICAL, componentes/versiones adicionales, errores de escáner o informes incompletos bloquean. Los demás controles de seguridad y pruebas siguen siendo obligatorios. No se usa una exclusión global de SQL ni se reduce el umbral HIGH/CRITICAL.

La decisión efectiva de cada paquete figura en `release.json` y `docs/sql-risk-decision.json`, con hashes del inventario y reporte, imagen, responsable y vencimiento. Un paquete con esta decisión se identifica como `release-verified-with-accepted-risk`, nunca como escaneo SQL limpio.

## Cierre y condiciones que exigen revisión

El propietario debe revisar la decisión antes de vencer o antes de cambiar imagen, datos, exposición o finalidad. No cubre producción, internet/LAN, información real, otras imágenes SQL ni nuevas vulnerabilidades. Corregir exige una imagen utilizable y repetir el análisis, las pruebas SQL, migración, backup/restore y aplicación. No se programa vigilancia automática con esta decisión.

Esta autorización permite superar únicamente el bloqueo conocido de SQL. La generación del ZIP depende del resto de CI; el arranque completo en Docker Desktop Windows sigue pendiente de validación. No constituye pentest ni certificación de seguridad.
