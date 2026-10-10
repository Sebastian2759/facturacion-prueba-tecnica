# Contexto del proyecto

**Entrega generada:** [CI 38018319032 / run12](https://github.com/Sebastian2759/facturacion-prueba-tecnica/actions/runs/38018319032) terminó correctamente sobre el commit `5b1efa2c75fba7ed7fa839d8addc78693b78424f` y produjo `Aplicacion-1.0.0-ci.12.zip`. Estado: `release-verified-with-accepted-risk`, con SQL-LOCAL-2026-10-09 visible en el manifiesto. SHA-256 del ZIP: `ad415b466dfcedd427494632ad5ed0fe086e6862fe35a503a6980b0f6076c358`. Se verificaron descarga, extracción e integridad en Windows; el arranque completo con Docker Desktop sigue pendiente de SVM. Los apartados sobre runs anteriores conservan el historial; no describen el estado actual del empaquetado.

**Decisión vigente:** el propietario aprobó el riesgo residual SQL para evaluación local con datos ficticios. La [aceptación SQL-LOCAL-2026-10-09](sql-risk-acceptance.md) y su inventario exacto permiten tratar sólo los hallazgos conocidos; los demás controles siguen siendo obligatorios. Los resultados que siguen conservan el historial anterior a esta decisión. La CI run12 generó el ZIP; el arranque completo en Windows sigue pendiente.

Nexo implementa el examen de facturación C#/HTML5 y consumo Accounts sobre Template, base `3393c2e7b02f7d0ea439b461b8fba5d561107681`.

## Alcance vigente

ZIP Windows x64 con internet y Docker Desktop, código GitHub personal privado y carpeta SharePoint que cargará el usuario. Azure/Cloudflare y demo pública fueron cancelados. Propietario: cuenta Sebastian2759, correo principal requerido verificado forero.sebastian157@gmail.com. Repositorio privado confirmado: https://github.com/Sebastian2759/facturacion-prueba-tecnica ; corte de evidencia del código sincronizado en commit `b0b7e25eb93f93b2870281bc2bc716a38ff2b463`.

Facturas persisten en SQL Server local; Angular/API comparten origen localhost. Accounts del ZIP está forzado a simulación. El cliente real existe, pero no tiene prueba de conexión real. El host JWT opcional no es dependencia del ZIP.

## Referencias autoritativas

- [Requisitos](requirements.md): enunciado frente a mejoras adoptadas.
- [Contrato](implementation-contract.md): rutas, DTO y errores.
- [Arquitectura](ARCHITECTURE.md): capas y flujo.
- [Decisiones](decisions.md): IVA, dinero, identity, seguridad local y backup.
- [Seguridad](../SECURITY.md) y [pruebas](TESTING.md): controles y evidencia.

Se mantienen convenciones .llmops de la plantilla que sean compatibles con el alcance acordado. No interpretar referencias heredadas a Azure Pipelines ni gráficos DemoArchitecture como recursos activos o análisis actualizado de las funciones nuevas.

Estado a 9 de octubre de 2026: [CI 37994136672](https://github.com/Sebastian2759/facturacion-prueba-tecnica/actions/runs/37994136672) aprobó 159 pruebas .NET sin omisiones, incluyendo cinco SQL/modelo EF; frontend 28 unitarias, 4 proxy, 7 UI controladas/axe y 1 live API/SQL. Backup/restore, CHECKDB, migradores, siete respuestas API/no-store, ambos OpenAPI y gate ZAP aprobados. Gitleaks/NuGet/npm/Trivy filesystem/ASP.NET aprobados. Semgrep 1.179.0 aprobado: 31 archivos, 0 hallazgos/errores, Python 3.12.3 aislado con hashes desde PyPI. Trivy SQL 2022 es el único gate previo a empaquetado bloqueante: 71 ocurrencias HIGH y 24 CVE únicos; SQL 2025 presenta el mismo diagnóstico. En aquel run no existía excepción SQL; la aceptación vigente se documenta por separado. El análisis previo confirmó símbolos de 9 CVE Go HIGH sin restricción Windows; no prueba explotabilidad remota.

La captura live del run7 reveló CSS global bloqueado pese al éxito funcional. El defecto está corregido y el navegador real del run8 validó estilos efectivos y nonce/CSP. ZIP final, instalación Windows y acceso del evaluador/SharePoint siguen pendientes. Node 24.21.0 LTS/npm 11.13.0 portable verificado. Docker Desktop 4.94 y WSL 3.0.1 ya están instalados; falta habilitar SVM en BIOS, reiniciar y completar el primer inicio de Docker. WDAC sigue limitando ejecutables locales; los resultados backend son de CI Linux. Datos ficticios; no incluir PDF con credenciales ni secretos en Git/artifacts.
