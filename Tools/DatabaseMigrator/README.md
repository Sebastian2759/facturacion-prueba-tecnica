# Migrador de evaluación local y CI

Ejecutable .NET 10 compilado; el ZIP lo ejecuta mediante el runtime oficial. No necesita SDK en el equipo del evaluador. No se ejecuta dentro de una solicitud de la API.

```text
DatabaseMigrator init
DatabaseMigrator migrate
```

- `Database__AdminConnectionString`: conexión administrativa sólo para este proceso; no se imprime.
- `Database__Name`: `FacturacionLocal`, `FacturacionCI` o esos nombres con sufijo `_<1..48 caracteres alfanuméricos o guion bajo>`. Se rechaza cualquier otro destino.
- `Database__RuntimePassword`: contraseña generada localmente de 32 a 128 caracteres, necesaria para `init`. Login fijo: `facturacion_runtime`.

`init` crea la base si no existe, aplica las migraciones iniciales y configura login/usuario/rol runtime. Una base vacía dejada por un primer inicio interrumpido puede completar esa inicialización. Si ya hay tablas de aplicación conserva sus datos, no rota contraseñas y exige ejecutar `migrate` explícitamente si hay migraciones pendientes. También restablece la asociación usuario/login para una base restaurada.

El runtime sólo recibe **SELECT e INSERT** sobre Facturas y DetalleFactura: las facturas son inmutables y sus consultas/reintentos no necesitan UPDATE ni DELETE. `init` revoca esos dos permisos del rol existente antes de conceder lectura/inserción, por lo que también reduce los permisos de instalaciones previas. Después abre una conexión como runtime y comprueba que no tenga permisos administrativos, ALTER/CREATE TABLE ni UPDATE/DELETE efectivos sobre ambas tablas. Si otro rol o un permiso directo concede esos privilegios, falla la inicialización; no modifica permisos ajenos silenciosamente. No concede DDL ni acceso al historial de migraciones.

`migrate` exige base existente y aplica migraciones mediante la identidad de mantenimiento; no gestiona usuarios ni carga datos. Códigos de salida: 0 éxito, 1 error, 2 uso incorrecto. Los errores no imprimen conexiones ni claves.

No hay seed persistido: la base inicial y el backup de entrega contienen el esquema migrado sin facturas. La UI proporciona ejemplos públicos ficticios fuera de SQL. La prueba funcional de restauración debe crear y recuperar una factura en la base restaurada; inspeccionar únicamente cabeceras del backup no demuestra ese recorrido.

Los tests SQL usan la variable independiente `FACTURACION_TEST_SQL_CONNECTION`, crean bases `InvoiceTests_<guid>` y no llaman a este migrador ni utilizan la base del evaluador. El proceso de empaquetado debe ejecutar ambas validaciones: permisos/arranque del migrador y tests de persistencia.

La prueba `ReadInsertOnly_CreatesReadsAndReplaysWithoutUpdateOrDelete` verifica el repositorio con una identidad SQL aislada que sólo puede leer/insertar, incluidos el reintento idempotente y los rechazos efectivos de UPDATE/DELETE. No sustituye la prueba de `init` y sus permisos sobre una instalación real. Su ejecución permanece pendiente hasta disponer de SQL Server en CI; no se presenta como aprobada por compilación.
