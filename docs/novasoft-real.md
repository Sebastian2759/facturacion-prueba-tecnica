# Evaluación de Novasoft real

El servicio externo de pruebas se consume desde C#, nunca directamente desde Angular. No requiere alojamiento cloud: la aplicación local puede comunicarse por HTTPS con test.novasoft.com.co:8091.

## Configuración privada

1. Inicie la aplicación una vez para preparar Docker y configuración.
2. Ejecute `configurar-novasoft.cmd`; introduzca usuario, conexión y contraseña de prueba del examen. La contraseña se solicita oculta.
3. Ejecute `detener.cmd` y `iniciar.cmd`, y recargue el navegador.
4. Abra Cuentas; la conexión Novasoft es el único origen. Consultar usa GET; Crear envía POST al proveedor. Use un código de prueba nuevo y datos ficticios. Consulte ese código después de guardar.
5. Para retirar la conexión: `configurar-novasoft.cmd -Disable` y reinicie.

`novasoft.local.json` tiene ACL del propietario y se monta como secreto Docker de sólo lectura en `/run/secrets/novasoft`. No va en Git, ZIP ni evidencias. No es un almacén cifrado: un administrador del host sigue siendo de confianza. El login/token permanecen en backend. El perfil distribuido está deshabilitado; habilitar sin credenciales completas impide iniciar. Cambiar valores del navegador no concede permiso.

## Contrato vigente

Swagger CXC consultado el 10/10/2026 UTC: https://test.novasoft.com.co:8091/webapi/swagger/CXC/swagger.json

- Login: POST api/Cuenta/Login, devuelve token y expiration.
- GET api/CXC/Senior/Accounts: array AccountsResult; mismo tipo al consultar por código.
- POST al mismo recurso: AccountsParams y respuesta GenericResponse. Un HTTP 200 se verifica también contra status/errors del cuerpo.
- `fecIng` es date-time. `estCli` **no figura en AccountsParams** y additionalProperties=false; sí figura en AccountsUpd, usado para actualización. Por eso no se envía estCli en creación aunque aparezca en el ejemplo antiguo del PDF.
- El adaptador escapa el código, usa host fijo, TLS válido, timeout 20 s, cuerpo limitado a 2 MiB y sin redirecciones/reintentos automáticos de POST.

Ante timeout/resultado incierto, consultar el código antes de volver a crear; no asumir fracaso ni duplicar. No hay fallback silencioso a simulación.

La evidencia real se registra sin cuerpos con datos de terceros, contraseñas o tokens. Un login externo directo sirve como diagnóstico; el cierre funcional exige el recorrido desde la aplicación C# y la pantalla.

## Resultado autorizado

El 10/10/2026 UTC se envió una única cuenta ficticia autorizada, código NEXOTEST261010A, nombre solicitado PRUEBA TECNICA NEXO FICTICIA. La API C# respondió 201 y accepted=true. GET posterior devolvió el mismo código, documento y correo ficticios. Novasoft devuelve name como Prueba Ficticia (firstName + lastName para personType=1); no se interpreta esa normalización como fallo ni se repite el POST. La respuesta fue comprobada también en la pantalla Cuentas. No es necesario recrear este registro para evaluar: puede consultarse por su código.
