# Arquitectura de Nexo

La aplicación conserva Clean Architecture y las convenciones de [Template](https://dev.azure.com/ServiciosAndesTool/_git/Template), commit `3393c2e7b02f7d0ea439b461b8fba5d561107681`. Se agregan facturación, Accounts y un host de evaluación local sin reorganizar las capas.

## Composición y límites

```text
Navegador Angular
  → LocalEvaluation: estáticos + sesión local + transporte HTTP
  → Application: Request / Validator / Command o Query / Response
  → Domain: entidades, calculador, puertos, reglas
  → Infraestructure: repositorio EF / mapper / gateway Accounts
  → SQL Server privado; Accounts simulado en la entrega local

DatabaseMigrator → SQL Server (identidad administrativa separada)
```

Domain no referencia otras capas. Application depende de Domain y contiene DTO/casos de uso; no recibe HttpContext ni conexiones SQL. Api compone dependencias, autentica y traduce HTTP. Infraestructure implementa puertos de Domain. Se conserva esa grafía heredada para no introducir un cambio de estructura ajeno al ejercicio.

| Ubicación | Responsabilidad |
|---|---|
| Core/Domain/Invoices | Calculador decimal puro y límites del agregado. |
| Core/Domain/Entities | InvoiceEntity/InvoiceLineEntity sobre las bases Guid. |
| Core/Domain/Contracts | IInvoiceRepository, ICurrentUser e IAccountsGateway. |
| Core/Application/UseCases/Invoices y Accounts | Validación y orquestación; Command/Query implementa IRequestHandler. |
| Core/Application/Dtos | Contratos de lectura de facturas. |
| Infraestructure/Persistence | Configuraciones EF, repositorio específico y migraciones. |
| Infraestructure/Adapters | IMapperAdapter, gateway simulado y cliente Novasoft. |
| Api/Api | Controladores, contratos de error y servicios HTTP compartidos. |
| Api/LocalEvaluation | Ejecutable de la entrega; cookie, Host/Origin y antiforgery. |
| Tools/DatabaseMigrator | Inicialización y migración explícitas. |
| Frontend/src/app | core, facturas, cuentas, demo y shared. |

El repositorio específico hereda RepositoryGeneric y añade consultas que necesitan propiedad, paginación e idempotencia. Se registra con el helper por convención. El mapper usa IMapperAdapter y coincidencia de nombre de entidad más específica para evitar confundir Invoice con InvoiceLine.

## Flujo de una factura

El formulario solicita preview del calculador backend y cancela resultados obsoletos. Angular sólo formatea importes, no mantiene otra implementación de la fórmula.

Al guardar, el controlador exige Idempotency-Key UUID. MediatR ejecuta ValidationBehavior; el comando valida permiso, obtiene propietario de ICurrentUser y normaliza datos. El dominio calcula fecha/importes; EF guarda encabezado y detalles en una operación atómica. La respuesta definitiva contiene número SQL, fecha y totales. Las consultas filtran el propietario antes de devolver datos.

La clave única owner/idempotencyKey protege concurrencia; la huella del cuerpo distingue reintento idéntico de conflicto. Una colisión SQL se resuelve consultando únicamente la clave de ese propietario. No se confía en owner, número, fecha ni total enviados por el navegador.

## Persistencia

| Dato | SQL | Motivo |
|---|---|---|
| Id y FK del agregado | uniqueidentifier | Compatibilidad con EntityBase/RepositoryGeneric. |
| Número visible | int IDENTITY, único | Cumple el consecutivo requerido sin reemplazar Guid globalmente. |
| Documento | varchar(15) | Tipo exigido por el ejercicio. |
| Fecha de negocio | datetime | Fecha del servidor America/Bogota. |
| Código / cantidad | int | Enteros positivos validados. |
| Precio, líneas e importes | money | Tipo exigido; cálculos en C# decimal y presentación a dos decimales. |
| Propietario | identificador opaco de 64 caracteres | Hash SHA-256 de issuer/sub separados por longitud. |

dbo.Facturas y dbo.DetalleFactura tienen relación y FK; la numeración no pretende ser contigua ante rollback. El modelo también conserva la tabla pedagógica DemoArchitectures.

El migrador aplica esquema con credencial administrativa separada. Runtime sólo tiene SELECT e INSERT en Facturas/DetalleFactura; init revoca UPDATE/DELETE previos del rol y comprueba privilegios efectivos. La API no aplica migraciones en requests. El snapshot manual, el modelo y las cinco integraciones SQL superaron CI. El run7 comprobó además init, backup vacío, restore a otra base, CHECKDB y smoke de API con persistencia. Los ejemplos de portada están fuera de SQL.

## HTTP y frontend

ResponseBase conserva isSuccess, statusCode, message, data y errors, añadiendo code, fieldErrors y traceId. El controlador usa el estado real de la respuesta. GlobalExceptionHandler/model binding/seguridad generan ProblemDetails con code, fieldErrors y traceId. Ejemplo de ruta de campo: details[0].quantity.

| Resultado | Estado |
|---|---|
| Lectura / preview / reintento idéntico | 200 |
| Alta persistida | 201 |
| Validación / JSON / CSRF | 400 |
| Sin sesión | 401 |
| Sin permiso / Origin inválido | 403 |
| Factura inexistente o de otro propietario | 404 |
| Idempotencia con contenido diferente | 409 |
| Cuerpo demasiado grande | 413 |
| Regla de dominio | 422 |
| Límite temporal | 429 |
| Error inesperado | 500, mensaje depurado |

El [contrato](implementation-contract.md) describe rutas/DTO. Angular consume tipos de transporte generados desde el OpenAPI real exportado por CI; los decodificadores comprueban las respuestas antes de formar el modelo de UI. El check compara la referencia completa y compila el puente utilizado por la aplicación. Esta integración posterior al run7 pasó localmente y espera la siguiente CI; véase [procedencia OpenAPI](../Frontend/docs/openapi.md).

Frontend usa componentes standalone, lazy loading por rutas, formularios reactivos tipados y signals. core normaliza errores/sesión; shared contiene campos, feedback e iconos realmente reutilizados. La idempotencia se conserva para reintentar el mismo cuerpo y cambia al editarlo.

La revisión visual del run7 detectó CSS global bloqueado aunque el recorrido funcional pasó. La corrección posterior utiliza nonce por respuesta para los estilos Angular/CDK y desactiva el inline de CSS crítico; el archivo global se sirve desde el mismo origen. El smoke local de producción confirmó estilos efectivos sin violaciones CSP. Las aserciones live reforzadas y esa corrección esperan nueva CI.

## Sesión y proveedor

LocalEvaluation sirve Angular/API en el mismo origen, publica sólo loopback y protege escrituras con cookie/Origin/CSRF. Su propietario es la instalación; no un usuario cloud. Las pruebas A/B comprueban aislamiento de las consultas y cookies de instalaciones diferentes. Véase [seguridad](../SECURITY.md).

Accounts local usa almacenamiento simulado por propietario que se reinicia con el proceso. La creación devuelve aceptación, no una cuenta del proveedor inventada. El cliente real implementa login y expiración de token, destino HTTPS fijo, timeout y errores normalizados; no hubo prueba real. Se prohíbe cambiar silenciosamente de real a simulado.

Api/Api conserva un host JWT opcional, fuera del alcance de entrega y sin IdP configurado. Su rechazo de modo local es defensa adicional de diseño, no evidencia de un despliegue web validado.

Los HTML y reportes DemoArchitecture en docs/architecture son documentación histórica de Template, no un grafo actualizado de este sistema. Build y evidencia actual están en [TESTING](TESTING.md).

Evidencia de ejecución: [CI 37989162596](https://github.com/Sebastian2759/facturacion-prueba-tecnica/actions/runs/37989162596), commit `c6b44269ac0944289cb1d848a48510efcd1e5523`: 150 tests .NET aprobados, 0 omitidos; frontend 16 unitarias, 4 proxy, 7 navegador/axe controladas y 1 live API/SQL. Trivy SQL y 4 advertencias ZAP impiden cerrar la release. El ZIP final y su instalación Windows siguen pendientes.
