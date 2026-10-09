# Seguridad

Esta entrega se limita al ZIP local, GitHub privado y distribución controlada. No es una publicación web ni una auditoría de seguridad concluida.

## Perfil de confianza

LocalEvaluation sirve Angular/API en http://127.0.0.1:5080; dentro de Docker escucha 0.0.0.0:8080. SQL no publica puerto al host. La instalación es una sola identidad local; no pretende aislar usuarios del mismo equipo ni procesos maliciosos con acceso al host.

La cookie de sesión es HttpOnly/SameSite=Strict, con nombre y protección de datos por instalación. El HTTP loopback requiere cookie sin Secure; no debe exponerse en LAN/internet. Las escrituras requieren sesión, Host/Origin exactos y X-CSRF-TOKEN. CORS por sí solo no autentica. Los repositorios verifican propietario, las operaciones requieren permiso y Accounts real está bloqueado en local.

Los ejemplos públicos son ficticios y no consultan SQL. La API no acepta propietario, fecha, número ni totales definitivos desde el navegador. Usa límites de cuerpo/detalles, rate limiting, errores estructurados y traceId.

## Secretos y datos

No versionar local.env, contraseñas SQL, cookies, credenciales/tokens Novasoft, PDF con credenciales, backups de usuarios ni logs con datos personales. El script genera claves de instalación y restringe su archivo. No adjuntar salidas completas de docker inspect o compose config.

Migrador y API usan identidades SQL diferentes. Runtime sólo tiene SELECT e INSERT sobre Facturas/DetalleFactura; init revoca UPDATE/DELETE anteriores y verifica que no haya privilegios administrativos/de esquema indebidos. En la red Docker aislada, Encrypt=True con TrustServerCertificate=True cifra la conexión pero no valida la identidad del certificado autofirmado. Esa excepción no se traslada a otros entornos.

El backup de release será esquema vacío, restaurado y comprobado. Backups posteriores del evaluador son privados. Restore usa una base nueva y no sustituye la activa automáticamente.

## Verificación y gates

El workflow GitHub prepara NuGet/npm Audit, analizadores .NET, reglas TypeScript Semgrep, Gitleaks, Trivy, pruebas, SBOM, restore y ZAP baseline local. NuGet NU1900–NU1905 bloquean; npm moderate o superior y Trivy high/critical bloquean. Feeds/herramientas fallidos no son aprobaciones. ZAP baseline no equivale a pentest exhaustivo.

Las pruebas de seguridad están en Test/Security y las de propiedad/persistencia en Test/Invoices. [CI 37989162596](https://github.com/Sebastian2759/facturacion-prueba-tecnica/actions/runs/37989162596), commit `c6b44269ac0944289cb1d848a48510efcd1e5523`, aprobó 150 pruebas .NET sin omisiones, incluidas cinco SQL y modelo EF; también Gitleaks, Semgrep, NuGet/npm Audit y Trivy filesystem/ASP.NET. Backup/restore, CHECKDB, migradores, smoke API y 1 navegador live con API/SQL pasaron. Trivy SQL mantiene hallazgos Go/OpenSSL sin excepción. ZAP terminó con 4 advertencias; las correcciones de cabeceras y el gate de informativos esperan nueva CI. ZIP final y Windows con Docker siguen pendientes. WDAC limita el equipo Windows y no hay Docker/WSL local; la evidencia backend corresponde a CI Linux. No hay certificado ni pentest realizado.

La captura del run7 reveló CSS global bloqueado pese al éxito funcional. Después se corrigieron CSP/nonce y la carga de estilos; el smoke local de producción confirmó estilos efectivos sin violaciones CSP. Las aserciones live reforzadas necesitan nueva CI antes de acreditar esa corrección en el paquete.

El host web/JWT opcional no tiene IdP ni despliegue verificado y queda fuera de la entrega. El cliente real Novasoft tiene pruebas controladas aprobadas, sin prueba de conexión real; local fuerza simulación.

La [evaluación de riesgo del runtime SQL](docs/sql-runtime-risk-assessment.md) documenta digests, fuentes primarias y diagnóstico binario ejecutado. Confirma símbolos asociados a 9 CVE Go HIGH sin restricción Windows en SQL 2022; no demuestra explotabilidad remota. SQL 2025 también falla el escaneo. No se aplicaron exclusiones.

## Reportar y responder

Informar al propietario por el canal privado acordado para la evaluación, con versión/commit, pasos mínimos y traceId depurado. No publicar secretos ni datos personales en issues, capturas o reportes públicos. La identidad propietaria está documentada en el README.

Ante filtración, detener distribución, revocar/rotar la credencial afectada, preservar evidencia depurada y publicar otra versión corregida. Borrar un secreto del historial no lo revoca. Para volver a una versión anterior, comprobar compatibilidad de esquema y conservar backup; no hacer restore destructivo implícito.

Las políticas reales de rama, permisos del evaluador, cuota de CI y descarga desde SharePoint se verifican antes de cerrar la entrega. Un YAML y un hash no bastan para demostrar que esas condiciones ya se cumplieron.
