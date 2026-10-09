# Decisiones de implementación

Fecha: 9 de octubre de 2026. Decisiones aceptadas para la entrega local; las evidencias de ejecución se registran por separado en [TESTING](TESTING.md).

## D01. Alcance local y propiedad personal

Se canceló publicación Azure/Cloudflare. La entrega será ZIP Windows x64 con internet, GitHub personal privado de Sebastian2759 y carga SharePoint por el usuario. No requiere suscripción ni IdP cloud. El host JWT existente queda opcional, fuera de entrega y sin verificación de despliegue.

## D02. Extender Template sin alterar sus capas

Se mantienen Domain/Application/Api/Infraestructure, CQRS/MediatR, FluentValidation, IMapperAdapter y registro por convención. Command/Query implementa IRequestHandler; no se agregan capas Handler redundantes. El repositorio especializado hereda RepositoryGeneric para consultas por propietario y operaciones que el genérico no representa.

La coincidencia de mapper utiliza el nombre más específico para distinguir InvoiceEntity e InvoiceLineEntity. Los diagramas DemoArchitecture siguen siendo material de la plantilla.

## D03. Guid interno y número SQL separados

Cambiar EntityBase a int rompería convenciones generales. Se conserva Guid como clave técnica y se añade Number int IDENTITY único para el consecutivo visible. La FK de detalle usa Guid. Identity puede dejar huecos por rollback; no se promete numeración fiscal continua.

El mapeo explicita VARCHAR(15), DATETIME, INT y MONEY. C# utiliza decimal para dinero.

## D04. IVA, precisión y límites

El PDF indica IVA 19 % sin detallar base. Se adopta bruto menos descuento; descuento 5 % desde bruto 500.000 inclusive. Preview y persistencia usan el mismo calculador de dominio.

Precios aceptan máximo dos decimales, aunque MONEY puede almacenar más precisión. Líneas y acumulados se redondean a dos decimales AwayFromZero. Se valida rango antes de multiplicar/sumar. Esta es una decisión del ejercicio, no una certificación tributaria.

## D05. Datos requeridos y autoridad del servidor

El documento es obligatorio por el PDF. Se exige también nombres, apellidos, dirección y teléfono como mejora aceptada. No se inventan formatos numéricos para el documento. Se acota el detalle a 100 artículos con cantidades/códigos/precios positivos.

Fecha del negocio del servidor en America/Bogota; auditoría UTC. Número, propietario, fecha y totales no se reciben como valores confiables del cliente.

## D06. Idempotencia y aislamiento

Cada creación lleva UUID en Idempotency-Key. Se guarda por propietario con huella del cuerpo normalizado. Mismo cuerpo: reenvío 200 de la factura existente; otro cuerpo: 409; nueva alta: 201. Índice único resuelve carrera; encabezado/detalles se guardan atómicamente.

El propietario deriva de issuer/sub validados mediante SHA-256 y codificación con prefijo de longitud. Se filtra en el repositorio; un número ajeno devuelve 404. Local representa una instalación, no múltiples usuarios del equipo.

## D07. LocalEvaluation explícito y mismo origen

Un ejecutable local comparte capas con Api, sirve Angular compilado y establece sesión automática. Requiere instalación Guid, Host y Origin exactos, cookie HttpOnly/SameSite y token antiforgery. Los archivos públicos no requieren SQL.

Docker publica sólo 127.0.0.1:5080; el proceso interno escucha 0.0.0.0:8080. SQL sólo está en la red privada. CORS no autentica ni sustituye CSRF. El modelo no protege frente a malware/administrador del equipo o un paquete modificado para exponerse en red.

## D08. Migrador separado y backup vacío

La API no migra en requests. Un proceso administrativo inicializa/migra; runtime sólo tiene SELECT e INSERT sobre las tablas de factura. init revoca UPDATE/DELETE antiguos y rechaza privilegios efectivos indebidos. La migración/snapshot se escribieron manualmente porque WDAC bloqueó el tooling; el gate de modelo SQL/EF ya pasó en CI y seguirá siendo obligatorio.

La release lleva un backup SQL Server de esquema vacío, en lugar del seed persistido inicialmente propuesto. La UI tiene ejemplos ficticios fuera de SQL. CI debe restaurar realmente y crear/consultar después una factura de smoke. Restore del evaluador crea otra base y no sobrescribe ni cambia destino automáticamente.

## D09. Binarios precompilados, internet y licencias

El ZIP no requiere SDK/Node, pero sí Docker Desktop Linux/Compose y Windows x64. Descarga imágenes oficiales fijadas y construye imágenes copiando binarios: no compila fuentes. No se promete instalación offline ni Windows sin dependencias.

SQL Developer se configura para evaluación y se obtienen componentes de sus fuentes oficiales. Se conservan licencias/avisos y se verifican condiciones aplicables; no se presupone una licencia comercial ni se redistribuye Docker.

## D10. Accounts simulado y evidencia real separada

Local bloquea llamadas reales y muestra simulación. El cliente real implementa login, token/expiración, GET/POST y errores, con pruebas HTTP controladas. No ha realizado llamadas reales.

fecIng se mapea a fecha/hora; no se añade estCli sin respaldo del Swagger consultado. No hay fallback silencioso a simulación ni reintento automático de POST externo que pueda duplicar cuentas. Una aceptación de POST no se presenta como una cuenta completa devuelta por el proveedor.

## D11. Contrato de errores compatible

Se conservan campos de ResponseBase y se añaden code/fieldErrors/traceId. ProblemDetails cubre seguridad, binding y excepciones. Angular normaliza ambos y conserva el formulario. Los tipos Angular son explícitos; no se declara generación automática desde OpenAPI sin ese proceso.

## D12. Gates antes de etiquetar la release

Build no equivale a tests; mocks no equivalen a SQL; YAML no equivale a CI ejecutado; VERIFYONLY no sustituye restore. Un escáner/feed fallido bloquea. NuGet Audit transitivo y NU1900–NU1905, npm, analizadores, Gitleaks, Semgrep, Trivy, SBOM y ZAP local tienen evidencia separada.

La release final sólo procede desde commit/árbol limpio y gates aprobados. No se modifica WDAC. [CI 37986378549](https://github.com/Sebastian2759/facturacion-prueba-tecnica/actions/runs/37986378549) (`21a36f9a246727db3f602d58cb1055dc4528c515`) aprobó 149 tests .NET, incluidas cinco integraciones SQL, y los gates Gitleaks/Semgrep/NuGet/npm/Trivy filesystem/ASP.NET. La imagen oficial SQL tiene hallazgos de Trivy que bloquean el paquete, sin excepción. Backup/restore, smoke del ZIP y E2E live siguen pendientes.

## D13. Node LTS fijado

Se fija Node 24.21.0 LTS con npm 11.13.0, verificados también en distribución portable. Angular/CLI sigue en 22.2.2. La toolchain está bloqueada para reproducibilidad; el evaluador del ZIP no necesita Node.
