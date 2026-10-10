# Seguridad de la evaluación local

La aplicación usa datos ficticios y sólo publica HTTP en 127.0.0.1:5080. SQL no publica puerto al host; migrador y SQL pertenecen a una red Docker interna. La API tiene salida HTTPS para Novasoft. No se publica en Azure/Cloudflare ni en red local.

## Controles

- Cookie HttpOnly/SameSite Strict por instalación, Host y Origin exactos, token CSRF para escrituras y autorización por permisos.
- La instalación representa una identidad local; no pretende aislar procesos maliciosos ni administradores del host. HTTP loopback usa cookie sin Secure; no trasladar ese ajuste a internet.
- Validación frontend/backend, límites de cuerpo/profundidad/detalles/dinero, rate limiting, CSP con nonce, errores depurados y traceId, API no-store.
- Identidad SQL runtime con SELECT/INSERT; migrador administrativo separado. Encrypt=True con certificado autofirmado confiado sólo en la red local, no validación pública de identidad.
- Idempotencia por propietario, transacción de cabecera/detalles y consultas que filtran propietario.
- Novasoft real deshabilitado por defecto. Requiere configuración privada completa; la UI sólo anuncia la capacidad del servidor. Login/token permanecen en C#, host HTTPS fijo, sin redirects ni reintentos automáticos de POST.

## Secretos

`local.env` y `novasoft.local.json` se generan/configuran en el equipo y tienen permisos del propietario. El segundo se monta como secreto Docker de sólo lectura. No son un almacén cifrado contra un administrador local. No incluirlos en Git, ZIP, capturas o reportes. No distribuir credenciales/tokens del examen ni el PDF original. Los backups de uso son privados; el backup distribuido tiene esquema vacío.

## DevSecOps y riesgo aceptado

Build/analizadores, pruebas, NuGet/npm, Gitleaks, Semgrep, Trivy, SBOM y ZAP son controles distintos. Un reporte faltante o herramienta fallida no equivale a aprobación. El estado de ejecución vigente está en [TESTING](docs/TESTING.md); CI15 sólo acredita su commit anterior. Los ajustes se verifican localmente por decisión del propietario.

SQL-LOCAL-2026-10-09 conserva la aceptación autorizada de 71 ocurrencias HIGH, 24 CVE únicos y 0 CRITICAL del inventario fijado. Motivo: evaluación local con datos ficticios, SQL exigido y alternativas oficiales revisadas sin eliminar hallazgos. Aislamiento y mínimo privilegio reducen exposición; no corrigen el software del proveedor. Caduca el 24/10/2026 02:32 UTC y limita nuevas entregas, no apaga instalaciones. [Decisión e inventario](docs/sql-risk-acceptance.md).

Detectar y gestionar hallazgos es habitual en DevSecOps. No es evidencia de que sean inocuos ni autorización para ignorarlos. Nuevos hallazgos, CRITICAL, digest diferente o excepción vencida bloquean. ZAP baseline y escáneres no constituyen un pentest exhaustivo.

## Respuesta y pendientes externos

Reportar al propietario por canal privado con versión, pasos y traceId depurado. Ante filtración: detener distribución, revocar/rotar secretos y preservar evidencia depurada. Borrar un secreto de Git no lo revoca.

GitHub main se observó sin protección. Su refuerzo queda preparado/documentado y aplazado al reanudar trabajo remoto; no se afirma que la CI impida pushes directos. SharePoint, invitaciones y un segundo PC del evaluador están fuera de la ejecución local actual.
