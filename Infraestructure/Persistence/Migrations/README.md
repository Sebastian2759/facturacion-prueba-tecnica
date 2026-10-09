# Migración inicial de facturación

`20261009000100_InitialInvoices` crea el ejemplo original, `dbo.Facturas` y `dbo.DetalleFactura`.
El Guid es la clave interna y FK; `Number` es un INT IDENTITY con índice único. Se conservan VARCHAR(15), DATETIME y MONEY.

La migración y el modelo histórico fueron escritos y revisados manualmente porque Windows Application Control bloquea cargar los ensamblados propios (0x800711C7). La compilación no demuestra ejecución de SQL. Antes de entregar, ejecutar los tests de modelo y los tests contra SQL Server real en CI: `Database.HasPendingModelChanges()` debe ser falso y `MigrateAsync` debe crear el esquema correctamente. No omitir la comprobación ni suprimir PendingModelChangesWarning.

`InitialInvoicesModel` es un modelo histórico explícito que no consulta las entidades/configuraciones actuales. No modificarlo al agregar capacidades; las siguientes migraciones deben conservar ese historial y actualizar el snapshot normalmente.

Generación de migraciones desde un entorno autorizado:
```text
dotnet ef migrations add Nombre --project Infraestructure/Persistence --startup-project Infraestructure/Persistence --output-dir Migrations
```
La fábrica de diseño usa una cadena ficticia de autenticación integrada para obtener el modelo sin conectarse. Aplicar migraciones requiere indicar/configurar una conexión real únicamente en el proceso de mantenimiento.

Decisiones de facturación:
- Documento obligatorio del enunciado; nombres, apellidos, dirección y teléfono también obligatorios como mejora adoptada.
- Precio positivo con máximo dos decimales para mantener coherencia línea/bruto; MONEY se conserva como tipo SQL.
- Bruto redondeado a dos decimales, 5% de descuento desde 500000, IVA 19% después del descuento, redondeo AwayFromZero.
- `OwnerSubject` es el identificador técnico del usuario, limitado a 64 caracteres con comparación binaria. Nunca se acepta en el JSON del cliente.
- La inserción del agregado usa una sola operación SaveChanges y su transacción relacional; el índice único (propietario, IdempotencyKey) resuelve reenvíos/concurrencia.
- `FACTURACION_TEST_SQL_CONNECTION` habilita los tests SQL. Debe apuntar a un servidor de pruebas con permiso para crear una base temporal; los tests crean y eliminan únicamente su propia base con nombre aleatorio.
