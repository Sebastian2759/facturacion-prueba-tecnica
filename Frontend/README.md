# Nexo · Frontend de evaluación local

Aplicación Angular en español para facturas y Accounts. La entrega vigente es el **ZIP local**: no requiere cuenta cloud y no publica un sitio en Azure/Cloudflare. Los ejemplos de la portada son ficticios y de sólo lectura; las facturas del recorrido se guardan mediante la API local. Accounts usa exclusivamente simulación identificada en esta entrega.

## Desarrollo reproducible

Toolchain fijado: Node **24.21.0 LTS**, npm **11.13.0**, Angular/CLI/Material/CDK **22.2.2**, TypeScript **6.0.2**. Esta versión LTS de Node está dentro del rango compatible de Angular 22 (`^24.15.0`). `package-lock.json` fija las dependencias transitivas. El evaluador del ZIP no necesita estas herramientas: recibe los archivos ya compilados. Fuentes: [versiones de Node](https://nodejs.org/en/about/previous-releases) y [compatibilidad de Angular](https://angular.dev/reference/versions).

```powershell
npm ci
npm start
npm run build
npm test
```

`npm start` escucha en `http://127.0.0.1:4200` y reenvía `/api` a `http://127.0.0.1:5080` mediante `proxy.conf.cjs`. Iniciar la API y su base antes de probar persistencia. Este proxy de desarrollo exige Host `127.0.0.1:4200` y, para escrituras, Origin exacto `http://127.0.0.1:4200`; sólo entonces adapta Host/Origin al destino. Los orígenes distintos o ausentes en escrituras se rechazan antes de reenviar y el token CSRF sigue siendo obligatorio. Usar esas direcciones exactas, sin sustituirlas por `localhost`. El servidor del paquete sirve `dist/facturacion/browser` junto a la API en un mismo origen y no utiliza este proxy.

`public/config.json` es configuración pública, **sin secretos**. `apiBaseUrl` queda vacío para usar el mismo origen local. Un error de configuración o sesión se muestra en pantalla; nunca se convierte en un éxito simulado.

## Estructura y decisiones

- `core`: contrato tipado, configuración, sesión local, cliente HTTP y normalización de errores.
- `facturas`: formulario, cliente, listado paginado, detalle y resumen monetario.
- `cuentas`: consulta y creación del contrato Accounts normalizado.
- `demo`: ejemplos fijos obtenidos de la ruta pública del backend.
- `shared`: campos accesibles, feedback, iconos vectoriales y controles realmente compartidos.

Se usan componentes standalone, carga diferida por rutas, signals para estado, `inject`, control de flujo nativo y comprobación TypeScript/templates estricta. Angular 22 utiliza OnPush por defecto. Se conservan formularios reactivos tipados por el contrato acordado y su integración con arrays y errores de servidor. Material aporta tema y progreso; CDK aporta utilidades de accesibilidad. No hay recursos de CDN ni fuentes externas.

### Facturación

Los datos de cliente adoptados son obligatorios; se respetan las longitudes del contrato. Código/cantidad son enteros positivos y el precio admite hasta dos decimales. El límite monetario de aplicación es 999.999.999.999,99 COP para precio, importe de línea, bruto y total, inferior al rango de almacenamiento SQL MONEY para conservar los centavos al presentar números JSON en el navegador. Angular valida el precio; el servidor valida también los acumulados. El editor permite 1–100 artículos.

La previsualización espera 350 ms y cancela inmediatamente solicitudes obsoletas al editar. **Angular no replica las fórmulas**: presenta `gross`, `discount`, `tax` y `total` calculados en C#. El resumen definitivo muestra la fecha y número devueltos al guardar.

Cada cuerpo de creación conserva una `Idempotency-Key` UUID durante los reintentos del mismo formulario. Si cambia el cuerpo, cambia la clave. No se persisten datos del cliente en almacenamiento del navegador. Al salir/reabrir el formulario después de un resultado ambiguo, consultar primero el listado antes de repetir la creación.

### Sesión y seguridad local

El host local establece su cookie y devuelve sesión/antiforgery en `/api/v1/session`. Los POST llevan `X-CSRF-TOKEN`. El token se conserva en memoria; no se usa localStorage ni se distribuyen secretos/API Keys. Los guards sólo organizan la navegación: el backend sigue imponiendo permisos, propietario, Host/Origin y CSRF.

Esta entrega no integra login OIDC ni habilita llamadas reales a Novasoft. La UI no mostrará acceso cloud ni una URL publicada inexistente. El perfil local y el rechazo de operaciones reales se prueban también en backend.

### Accounts

Se muestran el modo devuelto y el resultado real del contrato: una creación devuelve **aceptación**, no una cuenta completa inventada; el usuario consulta después por código. Los registros simulados se reinician con el servidor. Los códigos de clasificación no tienen etiquetas de negocio inventadas cuando Swagger sólo define números.

### Errores y contrato

Se normalizan `ResponseBase<T>` y ProblemDetails, incluidos `fieldErrors` con rutas como `details[0].quantity`, códigos HTTP y `traceId`. Los errores no vacían el formulario. Un error de red no cambia silenciosamente el proveedor a simulación.

`core/api-contract.ts` refleja `../docs/implementation-contract.md`. El generador fijado y sus comandos `api:setup`, `api:generate` y `api:check` están preparados en `tools/openapi`; la referencia inicial y su conexión a los modelos consumidos esperan el archivo OpenAPI real exportado por CI. **Todavía no hay un cliente generado ni una comprobación de drift completada.** El comando de comprobación falla mientras falten esos archivos. Consulte [procedimiento OpenAPI](docs/openapi.md).

## Pruebas

```powershell
npm test
npm run test:proxy
npx playwright install chromium
npm run test:e2e
npm run format:check
```

Para usar Edge ya instalado, asignar `$env:PLAYWRIGHT_CHANNEL='msedge'` antes de ejecutar Playwright. Las pruebas comunes interceptan respuestas controladas: verifican UI, CSRF, idempotencia, errores, cancelación de previews, viewport y accesibilidad con axe. **No acreditan persistencia SQL ni conectividad Novasoft real.**

La prueba de extremo a extremo real está separada y no usa mocks. Ejecutarla sólo contra una instancia local de evaluación preparada; crea una factura ficticia:

```powershell
$env:E2E_LIVE='1'
$env:E2E_BASE_URL='http://127.0.0.1:5080'
npm run test:e2e -- e2e/local-live.spec.ts
```

Sin `E2E_LIVE=1` se informa como omitida. Playwright conserva trazas/capturas de fallos y un informe HTML; esos archivos están excluidos de Git. La inspección visual abarca escritorio y móvil. Los controles automáticos de accesibilidad no sustituyen una revisión manual con teclado y lector de pantalla.

Si Vitest se ejecuta en un sandbox Windows cuyo directorio temporal cambia entre procesos, definir `TEMP` y `TMP` en una carpeta de trabajo accesible (por ejemplo `.angular/test-temp`) para evitar errores `ENOENT` de su caché. Esto no afecta al paquete del evaluador.

Referencias: [patrones oficiales Angular](https://angular.dev/style-guide), [formularios tipados](https://angular.dev/guide/forms/typed-forms). Se leyó también la guía de buenas prácticas incluida en Angular CLI 22.2.2 antes de implementar.
