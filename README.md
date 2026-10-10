# Nexo · Facturación y evaluación local

**Estado vigente — Windows verificado:** `Aplicacion-1.0.0-ci.15.zip`, commit `89be6447e9f69fb7b6b24be2dd90b9fd0fbff6c6`, generado por [CI run15](https://github.com/Sebastian2759/facturacion-prueba-tecnica/actions/runs/38022085155), fue descargado y probado sin modificar en Windows 11 x64 con Docker Desktop y los lanzadores de PowerShell 5.1. Pasaron arranque desde extracción nueva, factura por API y navegador, persistencia después de detener/iniciar, backup, restauración en una base nueva y CHECKDB. SHA-256: `fd333c43e0ad90a9ef1055481761c211557b1fa95eb6975373ecc15f74799652`. SQL mantiene riesgo aceptado, no corregido. Las referencias a SVM desactivado o pruebas Windows pendientes que se conservan más abajo pertenecen a cortes históricos anteriores a esta validación.

[Detalle de la prueba Windows](docs/windows-validation.md).

**Entrega anterior — run12, sustituida:** [CI 38018319032 / run12](https://github.com/Sebastian2759/facturacion-prueba-tecnica/actions/runs/38018319032) terminó correctamente sobre el commit `5b1efa2c75fba7ed7fa839d8addc78693b78424f` y produjo `Aplicacion-1.0.0-ci.12.zip`. Estado: `release-verified-with-accepted-risk`, con SQL-LOCAL-2026-10-09 visible en el manifiesto. SHA-256 del ZIP: `ad415b466dfcedd427494632ad5ed0fe086e6862fe35a503a6980b0f6076c358`. Se verificaron descarga, extracción e integridad en Windows; el arranque completo con Docker Desktop sigue pendiente de SVM. Los apartados sobre runs anteriores conservan el historial; no describen el estado actual del empaquetado.

**Decisión vigente:** el propietario aprobó el riesgo residual SQL para evaluación local con datos ficticios. La [aceptación SQL-LOCAL-2026-10-09](docs/sql-risk-acceptance.md) y su inventario exacto permiten tratar sólo los hallazgos conocidos; los demás controles siguen siendo obligatorios. Los resultados que siguen conservan el historial anterior a esta decisión. La CI run12 generó el ZIP; el arranque completo en Windows sigue pendiente.

Aplicación Angular + C# basada en Template para registrar facturas en SQL Server y explorar Accounts. La entrega vigente es **local en Windows x64**, acompañada por este repositorio privado y documentación en SharePoint. No requiere cuenta cloud ni publica una demo en Azure/Cloudflare.

## Evaluar la release

Con Windows x64, internet y Docker Desktop funcionando con contenedores Linux/Compose v2, descargue y verifique el ZIP de la release, extráigalo y ejecute:

```powershell
.\iniciar.cmd
```

Abra **http://127.0.0.1:5080**. La guía incluida es `LEEME.html`. El primer inicio descarga imágenes oficiales fijadas y ensambla imágenes copiando publicaciones .NET/Angular precompiladas; el evaluador no necesita SDK ni Node. SQL queda en una red privada Docker sin puerto al host.

Este comando corresponde al **ZIP verificado**. Un checkout del código todavía no contiene app/migrator publicados ni el manifiesto final. CI ya comprobó backup/restore, CHECKDB y el recorrido API/SQL de la aplicación preparada. El ZIP run12 está generado con aceptación SQL acotada y los demás controles aprobados. La instalación completa en Windows sigue pendiente.

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

Toolchain fijado: .NET SDK **10.0.401**, EF Core **10.0.10**, Node **24.21.0 LTS**, npm **11.13.0**, Angular **22.2.2**, TypeScript **6.0.2**. Ver `global.json`, `Directory.Build.props`, locks y `Frontend/package.json`.

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

Evidencia del 9 de octubre de 2026: [CI 37994136672](https://github.com/Sebastian2759/facturacion-prueba-tecnica/actions/runs/37994136672), commit `b0b7e25eb93f93b2870281bc2bc716a38ff2b463` (run10).

- Build .NET integrado: **0 errores y 0 advertencias**; formato aprobado.
- **159 pruebas .NET aprobadas, 0 omitidas**, incluidas las **5 de SQL Server real**, modelo/snapshot EF, autorización HTTP y cliente Accounts con respuestas controladas.
- Angular en CI: build/formato, **28 unitarias**, **4 pruebas de proxy**, **7 de navegador con respuestas controladas/axe** y **1 live con API/SQL** aprobadas. Axe no acredita accesibilidad completa.
- Backup vacío, restore a otra base, CHECKDB, migradores y smoke de API con persistencia: aprobados en CI Linux.
- Gitleaks, NuGet/npm Audit, Trivy del filesystem y de la imagen ASP.NET: gates aprobados. **Semgrep 1.179.0 aprobado**: 31 archivos TypeScript examinados, 0 hallazgos y 0 errores con las reglas locales; instalación desde PyPI con hashes en un entorno aislado Python 3.12.3.
- **Bloqueo de seguridad:** la imagen oficial SQL conserva hallazgos Go y OpenSSL; el análisis binario identifica símbolos asociados a **9 CVE Go HIGH** sin restricción Windows. No demuestra explotabilidad remota ni justifica una excepción. Véase el [análisis de runtime](docs/sql-runtime-risk-assessment.md).
- El gate ZAP **pasó** con informativos risk 0 revisados y API `no-store` comprobada. El escáner devolvió 2; el JSON no contiene alertas rechazadas. No equivale a cero alertas ni a un pentest.
- El CSS bloqueado detectado en el run7 está **corregido**, con comprobación repetida en el run10 de estilos efectivos y nonce/CSP. Pasaron ambas comprobaciones OpenAPI, incluida la del contenedor. **Trivy SQL es el único gate previo a empaquetado que bloquea**; no hay ZIP final aprobado ni instalación Windows con Docker acreditada.
- En el equipo local, WDAC `0x800711C7` sigue limitando el runtime .NET y Docker Desktop/WSL ya están instalados, pero SVM sigue desactivado en BIOS; las pruebas backend/SQL anteriores se ejecutaron en CI Linux.
- Novasoft real: no ejecutado; el ZIP fuerza simulación identificada. La guía HTML se revisó offline en escritorio/móvil; no sustituye el arranque de la aplicación.

Node **24.21.0 LTS** con npm **11.13.0** fue verificado también mediante toolchain portable. La preparación no instala ni cambia políticas del equipo.

## Origen y propiedad

Basado en [ServiciosAndesTool/Template](https://dev.azure.com/ServiciosAndesTool/_git/Template), commit `3393c2e7b02f7d0ea439b461b8fba5d561107681`. Se conserva Clean Architecture, CQRS, validación, ResponseBase y convenciones de repositorio/mapper. Los archivos DemoArchitecture son material heredado y no un grafo actualizado de la aplicación.

Destino: [GitHub privado personal](https://github.com/Sebastian2759/facturacion-prueba-tecnica), cuenta `Sebastian2759`, correo principal requerido verificado `forero.sebastian157@gmail.com`. Repositorio sincronizado; el corte de evidencia CI corresponde al commit `b0b7e25eb93f93b2870281bc2bc716a38ff2b463`. El ZIP run12 está generado; falta el arranque completo en Windows. El workflow vigente es `.github/workflows/quality.yml`; `azure-pipelines.yml` fue retirado y reemplazado por GitHub Actions.

El paquete excluye datos reales, credenciales del examen, secretos locales y PDF original. Los componentes de terceros conservan sus condiciones de licencia; la configuración SQL Developer se destina a evaluación.
