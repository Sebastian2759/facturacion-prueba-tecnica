# Decisiones de implementación

Fecha: 9 de octubre de 2026. Decisiones aceptadas para la entrega local; las evidencias de ejecución se registran por separado en [TESTING](TESTING.md).

## D01. Alcance local y propiedad personal

La aplicación se ejecuta localmente en Windows x64 con internet. Por autorización del propietario, el código y la documentación se publican en GitHub personal y se prepara una carpeta de distribución para SharePoint. No requiere suscripción ni IdP cloud. El host JWT existente queda opcional, fuera de entrega y sin verificación de despliegue.

## D02. Extender Template sin alterar sus capas

Se mantienen Domain/Application/Api/Infraestructure, CQRS/MediatR, FluentValidation, IMapperAdapter y registro por convención. Command/Query implementa IRequestHandler; no se agregan capas Handler redundantes. El repositorio especializado hereda RepositoryGeneric para consultas por propietario y operaciones que el genérico no representa.

La coincidencia de mapper utiliza el nombre más específico para distinguir InvoiceEntity e InvoiceLineEntity. El ejemplo y sus diagramas fueron retirados; las abstracciones de la arquitectura empleadas por facturación se conservan.

## D03. Guid interno y número SQL separados

Cambiar EntityBase a int rompería convenciones generales. Se conserva Guid como clave técnica y se añade Number int IDENTITY único para el consecutivo visible. La FK de detalle usa Guid. Identity puede dejar huecos por rollback; no se promete numeración fiscal continua.

El mapeo explicita VARCHAR(15), DATETIME, INT y MONEY. La segunda migración corrige nombres/apellidos/dirección/teléfono a VARCHAR UTF-8 (400/400/800/120 bytes) para conservar Unicode. Los límites de aplicación siguen en 100/100/200/30 caracteres. C# utiliza decimal para dinero.

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

La release lleva un backup SQL Server de esquema vacío, en lugar del seed persistido inicialmente propuesto. La UI muestra sólo facturas persistidas en SQL. El run7 comprobó backup, restore real, CHECKDB, migradores y creación/consulta posterior de factura mediante smoke. Restore del evaluador crea otra base y no sobrescribe ni cambia destino automáticamente; la instalación ci15 en Windows fue verificada; cada versión posterior necesita su propia evidencia.

## D09. Binarios precompilados, internet y licencias

El ZIP no requiere SDK/Node, pero sí Docker Desktop Linux/Compose y Windows x64. Descarga imágenes oficiales fijadas y construye imágenes copiando binarios: no compila fuentes. No se promete instalación offline ni Windows sin dependencias.

SQL Developer se configura para evaluación y se obtienen componentes de sus fuentes oficiales. Se conservan licencias/avisos y se verifican condiciones aplicables; no se presupone una licencia comercial ni se redistribuye Docker.

## D10. Integración exclusiva con Novasoft

Por decisión del propietario se retiran simuladores y ejemplos del runtime. Las respuestas controladas permanecen sólo en tests. Sin credenciales privadas, Cuentas informa configuración pendiente; no devuelve datos inventados. Se eliminan los campos mode de Accounts y sus tipos generados se actualizan.

## D11. Contrato de errores compatible

Se conservan campos de ResponseBase y se añaden code/fieldErrors/traceId. ProblemDetails cubre seguridad, binding y excepciones. Angular normaliza ambos y conserva el formulario. Los tipos de transporte proceden del OpenAPI real exportado por CI y se consumen mediante un puente tipado y decodificadores. La comparación íntegra del esquema y ese puente pasaron contra el contenedor en el run8. Véase [procedencia OpenAPI](../Frontend/docs/openapi.md).

## D12. Gates antes de etiquetar la release

Build no equivale a tests; mocks no equivalen a SQL; YAML no equivale a CI ejecutado; VERIFYONLY no sustituye restore. Un escáner/feed fallido bloquea. NuGet Audit transitivo y NU1900–NU1905, npm, analizadores, Gitleaks, Semgrep, Trivy, SBOM y ZAP local tienen evidencia separada.

Las evidencias deben corresponder al código/payload verificado. CI15 es el corte anterior. Los ajustes locales requieren build, pruebas, migración/restore, contrato y controles de seguridad locales; no se atribuyen a un run remoto inexistente. La aceptación SQL vigente es SQL-LOCAL-2026-10-09 y no exime hallazgos nuevos o errores de escaneo.

## D13. Node LTS fijado

Se fija Node 24.21.0 LTS con npm 11.13.0, verificados también en distribución portable. Angular/CLI sigue en 22.2.2. La toolchain está bloqueada para reproducibilidad; el evaluador del ZIP no necesita Node.

## D14. Verificar estilos y CSP además del recorrido funcional

La captura del run7 reveló CSS global bloqueado pese a aprobar guardado/consulta. Se corrigió el HTML con nonce por respuesta para Angular/CDK y se desactivó inlineCritical para cargar el CSS global desde el mismo origen. El navegador real del run8 verificó estilos efectivos y nonce/CSP. Las 28 unitarias frontend también pasaron en esa CI.

## D15. Evaluar informativos ZAP con condiciones verificables

Exit 2 del baseline no se interpreta automáticamente como éxito. El gate examina JSON válido del origen loopback, severidad/subtipo y evidencia API no-store; sólo permite informativos conocidos y caché de assets públicos publicados por ruta exacta. Las excepciones HTTP-loopback 10035-1/10011 están limitadas a sus referencias y riesgo bajo. El run8 sólo aceptó risk 0, sin alertas rechazadas. Una alerta desconocida, severidad distinta o reporte incompleto falla. Esta revisión no concede excepciones Trivy SQL ni equivale a pentest.

## D16. Semgrep reproducible desde PyPI en entorno aislado

La indisponibilidad de la imagen del escáner se resolvió sin relajar el análisis. Semgrep 1.179.0 se instala desde PyPI oficial en un venv exclusivo del runner con Python 3.12, versiones/hashes de todas las dependencias y sólo wheels. No instala globalmente ni actualiza pip. CI exige pip check, versión efectiva, reporte completo, archivos examinados y cero hallazgos/errores. Métricas desactivadas y reglas locales TypeScript acotadas. El run10 confirmó Python 3.12.3 y 31 archivos examinados, sin hallazgos ni errores; no equivale a un análisis exhaustivo.

## D14. Retirar el ejemplo sin perder datos

La migración 20261010000200 retira DemoArchitectures únicamente cuando está vacía. Si un usuario hubiera cargado datos en la tabla del ejemplo, se detiene con un error explícito para revisarlos; no borra ni migra esos datos a facturación. Las dos migraciones anteriores permanecen inmutables. El rollback reconstruye el esquema vacío anterior. Se verifica con SQL real en bases desechables.

El inicio redirige al listado de facturas propias. No se prellenan facturas ni cuentas; los estados vacíos reflejan las consultas reales. Los tests y sus dobles se conservan separados del runtime. No se incluye ninguna credencial en el paquete; ejecutar configurar-novasoft.cmd es un requisito para operar Cuentas en una instalación nueva.
