# Contrato de implementación v1

Contrato acordado para coordinar backend y Angular. Las respuestas mantienen `ResponseBase<T>`: `isSuccess`, `statusCode`, `message`, `data`, `errors`; se añaden `code`, `fieldErrors` y `traceId` sin modificar los campos originales. Los fallos HTTP conservan su estado y usan ProblemDetails cuando ocurren antes del caso de uso.

## Facturas

- `POST /api/v1/invoices/preview`: `{ details: [{ code, quantity, unitPrice }] }`. `data: { gross, discount, tax, total }`.
- `POST /api/v1/invoices`: `{ document, firstName, lastName, address, phone, details: [{ code, quantity, unitPrice }] }`; cabecera `Idempotency-Key` UUID obligatoria. `data: { invoice: InvoiceDetailDto }`.
- `GET /api/v1/invoices?page=1&pageSize=10`: `data: { items: InvoiceListDto[], page, pageSize, totalCount }`.
- `GET /api/v1/invoices/{number}`: `data: { invoice: InvoiceDetailDto }`.
- `GET /api/v1/demo/invoices`: ejemplos fijos públicos, misma envoltura de listado. Ninguna consulta a datos privados.

`InvoiceDetailDto`: `id`, `number`, `date`, `document`, `firstName`, `lastName`, `address`, `phone`, `details`, `gross`, `discount`, `tax`, `total`. Cada detalle contiene `code`, `quantity`, `unitPrice`, `lineTotal`. `InvoiceListDto`: `number`, `date`, `document`, `firstName`, `lastName`, `total`.

Precios y totales son números JSON calculados en C# decimal; Angular sólo presenta el resultado. Código y cantidad son enteros positivos. Máximo 100 detalles, 256 KiB JSON y 100 registros por página. Documento máximo 15, nombres y apellidos 100 cada uno, dirección 200, teléfono 30. Identificador interno Guid distinto del número INT IDENTITY. Guardado transaccional e idempotencia por propietario y huella del cuerpo.

Precio unitario, importe de cada línea, bruto y total no pueden superar `999999999999.99`. Este límite de aplicación conserva los centavos al representar los resultados como números JSON/JavaScript y evita desbordamientos de almacenamiento; no se usa el máximo teórico de SQL MONEY como límite del formulario.

## Identidad y perfil

`ICurrentUser` (Domain/Contracts/Security) ofrece `Subject`, `CanEvaluate`, `CanUseRealAccounts`. Subject procede de identidad validada, no del Request. El perfil local emplea un propietario fijo de instalación y sesión local protegida; no usa identidad cloud. Los casos de uso verifican permiso y los repositorios filtran por propietario.

`GET /api/v1/session` ofrece `{ mode: 'local', authenticated, canEvaluate, canUseRealAccounts: false, csrfToken }` dentro de `data` en la entrega local. El host local crea una cookie HttpOnly por instalación y entrega el token antiforgery; Angular lo envía en `X-CSRF-TOKEN` para POST. El navegador debe enviar el origen exacto de la instalación. Angular y API se sirven desde el mismo origen. El host web y sus contratos de identidad son extensiones de código fuera del alcance de esta entrega: no se configura OIDC, Azure ni Cloudflare.

## Accounts

- `GET /api/v1/accounts?code=...&mode=simulated`: `data: { mode, accounts: AccountDto[] }`. `code` es opcional.
- `POST /api/v1/accounts`: `{ mode, code, identificationType, document, name, firstName?, lastName?, email, personType, cityCode, departmentCode, countryCode, address?, additionalAddress?, phone?, customerType, website?, registeredAt, externalCode? }`.
- La creación devuelve `data: { mode, code, accepted: true }`: la respuesta genérica del proveedor indica aceptación, no un objeto creado. La UI ofrece una consulta posterior explícita.

La respuesta siempre indica `mode: 'simulated' | 'real'`; no cambia a simulación tras fallar una llamada real. El perfil local sólo permite simulación y rechaza `real` incluso si se manipula la petición. El adaptador real se implementó contra el Swagger consultado de Novasoft; no se han ejecutado login ni escrituras contra ese servicio. No se distribuyen sus credenciales.

## Errores

`fieldErrors` es un objeto cuyas claves son rutas JSON camelCase (`details[0].quantity`) y valores arrays de mensajes. ProblemDetails conserva `type`, `title`, `status`, `detail`, `instance`, más `code`, `fieldErrors` y `traceId`. No expone stack trace, SQL ni credenciales.

Fechas, número y totales definitivos se generan en servidor. Base del IVA: bruto menos descuento. Descuento 5% desde bruto 500000; IVA 19%; redondeo de cada acumulado a dos decimales AwayFromZero.
