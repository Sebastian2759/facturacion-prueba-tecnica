# Contexto del proyecto

Nexo implementa el examen de facturación C#/HTML5 y consumo Accounts sobre Template, base `3393c2e7b02f7d0ea439b461b8fba5d561107681`.

## Alcance vigente

ZIP Windows x64 con internet y Docker Desktop, código GitHub personal privado y carpeta SharePoint que cargará el usuario. Azure/Cloudflare y demo pública fueron cancelados. Propietario: cuenta Sebastian2759, correo principal requerido verificado forero.sebastian157@gmail.com. Repositorio privado confirmado: https://github.com/Sebastian2759/facturacion-prueba-tecnica ; corte de evidencia del código sincronizado en commit `c6b44269ac0944289cb1d848a48510efcd1e5523`.

Facturas persisten en SQL Server local; Angular/API comparten origen localhost. Accounts del ZIP está forzado a simulación. El cliente real existe, pero no tiene prueba de conexión real. El host JWT opcional no es dependencia del ZIP.

## Referencias autoritativas

- [Requisitos](requirements.md): enunciado frente a mejoras adoptadas.
- [Contrato](implementation-contract.md): rutas, DTO y errores.
- [Arquitectura](ARCHITECTURE.md): capas y flujo.
- [Decisiones](decisions.md): IVA, dinero, identity, seguridad local y backup.
- [Seguridad](../SECURITY.md) y [pruebas](TESTING.md): controles y evidencia.

Se mantienen convenciones .llmops de la plantilla que sean compatibles con el alcance acordado. No interpretar referencias heredadas a Azure Pipelines ni gráficos DemoArchitecture como recursos activos o análisis actualizado de las funciones nuevas.

Estado a 9 de octubre de 2026: [CI 37989162596](https://github.com/Sebastian2759/facturacion-prueba-tecnica/actions/runs/37989162596) aprobó 150 pruebas .NET sin omisiones, incluyendo cinco SQL/modelo EF; frontend 16 unitarias, 4 proxy, 7 UI controladas/axe y 1 live API/SQL. Backup/restore, CHECKDB, migradores y smoke API aprobados. Gitleaks/Semgrep/NuGet/npm/Trivy filesystem/ASP.NET aprobados. Trivy SQL mantiene el bloqueo Go/OpenSSL, con símbolos de 9 CVE Go HIGH sin restricción Windows confirmados y sin excepción. ZAP terminó con 4 advertencias pendientes de corrección/verificación.

La captura live del run7 reveló CSS global bloqueado pese al éxito funcional. Después se corrigieron nonce/CSP e inlineCritical y el smoke local de producción pasó sin violaciones CSP; las aserciones live reforzadas esperan nueva CI. OpenAPI real integrado y 28 unitarias frontend pasaron localmente, no en el corte CI anterior. ZIP final, instalación Windows y acceso del evaluador/SharePoint siguen pendientes. Node 24.21.0 LTS/npm 11.13.0 portable verificado. WDAC y ausencia Docker/WSL siguen limitando el equipo local; los resultados backend son de CI Linux. Datos ficticios; no incluir PDF con credenciales ni secretos en Git/artifacts.
