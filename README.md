# Nexo · Facturación y evaluación local

Aplicación Angular + C# basada en Template para registrar facturas en SQL Server y explorar Accounts. La entrega vigente es **local en Windows x64**, acompañada por este repositorio privado y documentación en SharePoint. No requiere cuenta cloud ni publica una demo en Azure/Cloudflare.

## Evaluar la release

Con Windows x64, internet y Docker Desktop funcionando con contenedores Linux/Compose v2, descargue y verifique el ZIP de la release, extráigalo y ejecute:

```cmd
iniciar.cmd
```

Abra **http://127.0.0.1:5080**. La guía incluida será `LEEME.html`. El primer inicio descarga imágenes oficiales fijadas y ensambla imágenes copiando publicaciones .NET/Angular precompiladas; el evaluador no necesita SDK ni Node. SQL queda en una red privada Docker sin puerto al host.

Este comando corresponde al **ZIP verificado**. Un checkout del código todavía no contiene app/migrator publicados ni el manifiesto final. La release y su prueba de instalación real siguen pendientes de CI/SQL; no se presenta el código actual como ZIP ya verificado.

`detener.cmd` conserva datos; `diagnosticar.cmd` revisa estado. `backup.cmd` produce una copia privada; `restaurar.cmd` restaura a una base nueva sin sobrescribir ni cambiar la base activa automáticamente. El backup distribuido será **esquema migrado vacío**; los ejemplos de portada son ficticios y no están almacenados como facturas privadas.

## Qué puede evaluarse

- Crear factura con cliente y 1–100 artículos, obtener número SQL IDENTITY y consultar listado/detalle.
- Previsualizar importes calculados por C#: descuento 5 % desde 500.000, IVA 19 % después del descuento y redondeo a dos decimales.
- Reintentar una creación con idempotencia; errores por campo y código de seguimiento.
- Consultar y crear Accounts en **modo simulado visible**. Se reinicia con el servidor; no prueba Novasoft real.
- Revisar componentes reutilizables, rutas diferidas, tests, persistencia, controles locales y pipeline.

Se implementó el cliente HTTP real de Novasoft y sus pruebas controladas, pero no se realizaron llamadas reales. LocalEvaluation lo mantiene bloqueado. La integración real necesita un futuro entorno controlado, credenciales válidas y permiso específico; no es un requisito para abrir el ZIP.

## Documentación

| Documento | Contenido |
|---|---|
| [Requisitos](docs/requirements.md) | Trazabilidad al ejercicio, reglas y criterios de aceptación. |
| [Arquitectura](docs/ARCHITECTURE.md) | Capas, flujos, persistencia y contrato. |
| [Decisiones](docs/decisions.md) | IVA, redondeo, identity, sesión local, backup y límites. |
| [Contrato API](docs/implementation-contract.md) | DTO, rutas, errores y autenticación aplicable. |
| [Pruebas](docs/TESTING.md) | Comandos, alcance y evidencia pendiente. |
| [Seguridad](SECURITY.md) | Modelo local, secretos, gates y reporte de incidentes. |
| [Migrador](Tools/DatabaseMigrator/README.md) | Inicialización/migración y permisos SQL. |
| [Frontend](Frontend/README.md) | Toolchain, componentes y pruebas de navegador. |

## Desarrollo

Toolchain fijado: .NET SDK **10.0.401**, EF Core **10.0.10**, Node **26.1.0**, npm **11.13.0**, Angular **22.2.2**, TypeScript **6.0.2**. Ver `global.json`, `Directory.Build.props`, locks y `Frontend/package.json`.

Desde la raíz:

```powershell
dotnet restore Example.sln --locked-mode
dotnet build Example.sln --no-restore
dotnet test Example.sln --no-build
npm ci --prefix Frontend
npm run build --prefix Frontend
npm test --prefix Frontend
```

Las pruebas SQL requieren `FACTURACION_TEST_SQL_CONNECTION` hacia un SQL Server de pruebas con permiso para crear/eliminar bases efímeras. No apuntar a una base de usuarios. Sin esa variable se omiten explícitamente; CI la exige. No se utiliza SQLite/InMemory como sustituto de SQL Server para validar MONEY, identity, FK o concurrencia.

El host de evaluación es `Api/LocalEvaluation`. Sirve Angular y API en el mismo origen, con Local:InstallationId y conexión runtime configurados; consulte el migrador antes de preparar un entorno de desarrollo. La API no migra durante requests. El host `Api/Api` con JWT es una base opcional fuera de esta entrega, sin IdP configurado.

Desde un commit y árbol limpios, `pwsh scripts/build-release.ps1 -PrepareOnly` prepara publicaciones. No genera una release aprobada: la verificación final exige CI, SQL, restauración, smoke y escáneres.

## Estado de verificación

Actualizado 9 de octubre de 2026:

- Build .NET integrado: **0 errores y 0 advertencias**.
- Angular: build, formato y **14 unitarias aprobados**.
- Playwright en Edge: **7 pruebas UI aprobadas**, **1 live omitida** sin E2E_LIVE; incluye móvil, teclado y axe. Las respuestas controladas no acreditan SQL.
- Gitleaks del árbol actual: sin secretos detectados; revisión de historial/paquete final pendiente.
- Tests .NET bloqueados localmente por WDAC `0x800711C7`; Docker/WSL ausentes. No se modifica esa política.
- Migración/snapshot manuales pendientes de comprobación EF en CI. SQL, backup/restore, ZIP y gates de GitHub aún pendientes.
- Novasoft real: no ejecutado. Simulación identificada en local.

## Origen y propiedad

Basado en [ServiciosAndesTool/Template](https://dev.azure.com/ServiciosAndesTool/_git/Template), commit `3393c2e7b02f7d0ea439b461b8fba5d561107681`. Se conserva Clean Architecture, CQRS, validación, ResponseBase y convenciones de repositorio/mapper. Los archivos DemoArchitecture son material heredado y no un grafo actualizado de la aplicación.

Destino: [GitHub privado personal](https://github.com/Sebastian2759/facturacion-prueba-tecnica), cuenta `Sebastian2759`, correo principal requerido verificado `forero.sebastian157@gmail.com`. Repositorio creado; sincronización del proyecto y primer CI pendientes. El workflow vigente es `.github/workflows/quality.yml`; `azure-pipelines.yml` fue retirado y reemplazado por GitHub Actions.

El paquete excluye datos reales, credenciales del examen, secretos locales y PDF original. Los componentes de terceros conservan sus condiciones de licencia; la configuración SQL Developer se destina a evaluación.
