# Contexto del proyecto

Nexo implementa el examen de facturación C#/HTML5 y consumo Accounts sobre Template, base `3393c2e7b02f7d0ea439b461b8fba5d561107681`.

## Alcance vigente

ZIP Windows x64 con internet y Docker Desktop, código GitHub personal privado y carpeta SharePoint que cargará el usuario. Azure/Cloudflare y demo pública fueron cancelados. Propietario: cuenta Sebastian2759, correo principal requerido verificado forero.sebastian157@gmail.com. Repositorio privado confirmado: https://github.com/Sebastian2759/facturacion-prueba-tecnica ; código sincronizado en commit `21a36f9a246727db3f602d58cb1055dc4528c515`.

Facturas persisten en SQL Server local; Angular/API comparten origen localhost. Accounts del ZIP está forzado a simulación. El cliente real existe, pero no tiene prueba de conexión real. El host JWT opcional no es dependencia del ZIP.

## Referencias autoritativas

- [Requisitos](requirements.md): enunciado frente a mejoras adoptadas.
- [Contrato](implementation-contract.md): rutas, DTO y errores.
- [Arquitectura](ARCHITECTURE.md): capas y flujo.
- [Decisiones](decisions.md): IVA, dinero, identity, seguridad local y backup.
- [Seguridad](../SECURITY.md) y [pruebas](TESTING.md): controles y evidencia.

Se mantienen convenciones .llmops de la plantilla que sean compatibles con el alcance acordado. No interpretar referencias heredadas a Azure Pipelines ni gráficos DemoArchitecture como recursos activos o análisis actualizado de las funciones nuevas.

Estado a 9 de octubre de 2026: [CI 37986378549](https://github.com/Sebastian2759/facturacion-prueba-tecnica/actions/runs/37986378549) aprobó 149 pruebas .NET sin omisiones, incluyendo cinco SQL/modelo EF; frontend 14 unitarias, 4 proxy y 7 UI controladas. Gitleaks/Semgrep/NuGet/npm/Trivy filesystem/ASP.NET aprobados. Trivy de SQL bloquea release; se investiga sin excepción. Backup/restore, ZIP y E2E live pendientes. Node 24.21.0 LTS/npm 11.13.0 portable verificado. WDAC y ausencia Docker/WSL siguen limitando el equipo local; los resultados backend son de CI Linux. Datos ficticios; no incluir PDF con credenciales ni secretos en Git/artifacts.
