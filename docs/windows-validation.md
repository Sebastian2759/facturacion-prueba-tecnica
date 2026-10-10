# Validación final del paquete en Windows

Fecha UTC: 2026-10-10T04:03:46.5494951Z.

Paquete: `Aplicacion-1.0.0-ci.15.zip`.
SHA-256: `fd333c43e0ad90a9ef1055481761c211557b1fa95eb6975373ecc15f74799652`.
Commit: `89be6447e9f69fb7b6b24be2dd90b9fd0fbff6c6`.
CI: [run15](https://github.com/Sebastian2759/facturacion-prueba-tecnica/actions/runs/38022085155).

La prueba usó una extracción nueva, sin editar archivos del ZIP ni regenerar sus checksums. Los cinco CMD ejecutaron Windows PowerShell 5.1. No se deshabilitaron políticas de seguridad de Windows. Docker Desktop ya estaba instalado y SVM fue habilitado por el propietario.

El proceso de pruebas se ejecutó desde PowerShell 7. Para reproducir la apertura del CMD desde el Explorador, se retiró únicamente PSModulePath heredado en los procesos hijos, permitiendo que Windows PowerShell cargara sus módulos estándar. La herencia de módulos binarios de PowerShell 7 produjo falsos fallos de carga de Get-Acl/Get-FileHash durante los intentos anteriores. No se modificaron las variables persistentes del equipo ni se omitieron controles de integridad o ACL.

## Resultados

- Unmodified CMD first start under Windows PowerShell 5.1.
- Fresh installation contains no invoices.
- Live API invoice, calculation, idempotency, session, CSRF, no-store and simulated Accounts.
- Edge browser: create, reload, persisted invoice, styles and CSP nonce.
- Windows CMD backup with checksum and RESTORE VERIFYONLY.
- Windows CMD restore to separate database; CHECKDB and row checksums match.
- Windows CMD stop/start preserves all invoice and detail rows.
- Only web 127.0.0.1:5080 published; SQL has no published host port.
- Original package files still pass SHA-256 checks after operation.

El respaldo de prueba contiene datos ficticios y no forma parte de la distribución. El ZIP conserva su backup inicial vacío. La restauración creó otra base, sin sobrescribir la activa. Los datos creados para probar la instalación local tampoco se redistribuyen.

## Correcciones que motivaron esta prueba

- Verificación SHA-256 mediante la biblioteca criptográfica de .NET, sin depender de la carga de funciones Utility entre scripts de PowerShell 5.1.
- CMD con código de salida conservado después de pause; iniciar transmite -NoBrowser.
- SQL y migrador exclusivamente en red privada; API con una red web adicional para publicar sólo 127.0.0.1:5080. La API puede tener salida; no se declara aislamiento total de salida.
- Delimitador sqlcmd protegido con comillas compatibles con el paso de argumentos nativos de PowerShell 5.1. Backup y restore reales verificados.
- Job obligatorio de Windows con 27 fixtures antes del empaquetado Linux.

## Alcance de la evidencia

Probado en un equipo Windows 11 x64; no garantiza compatibilidad con todas las políticas empresariales o configuraciones. Novasoft real no se invocó. No es un pentest. Los hallazgos SQL permanecen abiertos bajo SQL-LOCAL-2026-10-09 y su plazo original; esta prueba no renueva la aceptación.

El campo windowsDockerValidation del manifiesto inmutable registra lo conocido al empaquetar en CI; este informe posterior, vinculado al SHA-256 exacto, acredita la prueba en Windows sin alterar dicho manifiesto.
