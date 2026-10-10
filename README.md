# Nexo · Facturación y Accounts

Aplicación Angular 22.2.2 + C# .NET 10 + SQL Server basada en Template. El alcance vigente es **evaluación local en Windows x64 con Docker e internet**. No se publican estos ajustes en Azure, Cloudflare, GitHub ni SharePoint por indicación del propietario.

## Evaluación

En el ZIP completo, abra `LEEME.html` y haga doble clic en `iniciar.cmd`. Abra http://127.0.0.1:5080. Docker Desktop debe estar iniciado en modo Linux. No necesita SDK .NET, Node ni Visual Studio para ejecutar el paquete. Si Docker ya funciona, no necesita cambiar la BIOS.

| Comando | Función |
|---|---|
| iniciar.cmd | Comprueba integridad y prepara aplicación/base. |
| detener.cmd | Detiene sin borrar facturas. |
| diagnosticar.cmd | Diagnóstico sin mostrar secretos. |
| backup.cmd / restaurar.cmd | Backup privado y restauración en una base nueva. |
| configurar-novasoft.cmd | Configura en privado las credenciales de prueba del proveedor. |
| configurar-novasoft.cmd -Disable | Deshabilita la conexión real y retira sus credenciales del archivo. |

Accounts usa exclusivamente Novasoft. Configure sus credenciales privadas y reinicie para habilitar consultas y altas. Sin configuración se muestra un estado explícito, sin datos ni altas simuladas. Las altas reales escriben en el servicio de pruebas: use sólo datos ficticios. No distribuya `novasoft.local.json`, `local.env`, backups privados ni el PDF original que contiene credenciales.

## Requerimiento y decisiones

- Formulario HTML5, backend C#, cabecera y detalle persistidos en Facturas/DetalleFactura.
- Número SQL INT IDENTITY y fecha DATETIME del servidor, no editables.
- Documento VARCHAR(15); nombres/apellidos/dirección/teléfono VARCHAR con UTF-8. Capacidad SQL en bytes 400/400/800/120, límites de aplicación 100/100/200/30 caracteres. La migración preserva textos Unicode existentes.
- Código/cantidad INT y precio/importes MONEY. C# decimal; descuento 5 % desde bruto 500.000; IVA 19 % sobre bruto menos descuento. Cálculo y preview comparten implementación.
- Validación FluentValidation, idempotencia, transacción, errores por campo y traceId.
- Accounts: login/Bearer/GET/POST mediante C#, con secreto sólo servidor y origen explícito en pantalla.

## Arquitectura y documentación

[Arquitectura](docs/ARCHITECTURE.md) · [Decisiones](docs/decisions.md) · [Requisitos](docs/requirements.md) · [Contrato](docs/implementation-contract.md) · [Pruebas](docs/TESTING.md) · [Seguridad](SECURITY.md) · [Operación](docs/operations.md) · [Novasoft](docs/novasoft-real.md)

Se conservan Domain/Application/Api/Infraestructure, CQRS/MediatR, validadores, mapper y repositorios de Template. Angular usa componentes standalone reutilizables, rutas con lazy loading, Reactive Forms tipados y contrato generado desde OpenAPI.

## Desarrollo y verificación

`Example.sln` es la solución para Visual Studio con SDK .NET 10.0.401. Node 24.21.0 y npm 11.13.0 están fijados para reproducibilidad.

```powershell
dotnet restore Example.sln --locked-mode
dotnet build Example.sln --no-restore
dotnet test Example.sln --no-build
npm ci --prefix Frontend
npm run build --prefix Frontend
npm test --prefix Frontend
```

Las integraciones SQL requieren `FACTURACION_TEST_SQL_CONNECTION` hacia un servidor exclusivo de pruebas; crean/eliminan sólo bases InvoiceTests_<guid>. No use una base de negocio. En este Windows, WDAC puede impedir cargar DLL de API: la verificación completa se realiza en el SDK oficial dentro de Docker sin cambiar esa política.

El resultado de una versión se acredita con su manifiesto y evidencias; un checkout o payload preparado no equivale a un ZIP verificado. CI15 es la referencia anterior y no prueba los cambios posteriores. Consulte [pruebas](docs/TESTING.md) para el corte local vigente.

## Riesgo residual y propiedad

SQL-LOCAL-2026-10-09 está aceptado explícitamente para datos ficticios y uso local: 71 ocurrencias HIGH, 24 CVE únicos, 0 CRITICAL en el inventario aprobado. [Motivo y vigencia](docs/sql-risk-acceptance.md). La aceptación no corrige las vulnerabilidades y no autoriza publicar el servicio.

Propietario personal: Sebastian2759. [Repositorio existente](https://github.com/Sebastian2759/facturacion-prueba-tecnica), privado. Los ajustes locales no se atribuyen a un commit remoto ni a un nuevo run de Actions. Protección de main, invitaciones y SharePoint quedan aplazados mientras se mantenga el alcance sólo local.
