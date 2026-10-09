# Operación, construcción y controles de entrega

## Alcance y estados comprobables

El destino de ejecución es Windows x64 con Docker Desktop Linux. El repositorio personal privado de GitHub contiene fuentes, decisiones y CI. SharePoint sirve para distribuir la entrega descargable. No hay pasos de alojamiento público ni credenciales de publicación.

El pipeline `Quality and local delivery` es la autoridad operativa para producir el ZIP. Antes de una ejecución exitosa, el código de automatización se considera preparado: no se afirma que Docker, restauración, DAST o reproducción en un equipo limpio hayan pasado. Los controles no disponibles en el equipo del autor se ejecutan en el runner Linux; la evaluación de instalación en Windows debe quedar registrada por separado.

## Perfiles y conexiones

`Api/LocalEvaluation/LocalEvaluation.csproj` produce `LocalEvaluation.dll`, host separado que sirve el build Angular y la API. Mantiene el origen `http://127.0.0.1:5080`, sesión por cookie HttpOnly/SameSite, antiforgery y verificación del Host. El puerto sólo se publica en loopback de Windows. No se permite activar Accounts real en este host.

SQL Server Developer se ejecuta en la red interna del proyecto Compose, sin puerto publicado. La conexión local usa `Encrypt=True;TrustServerCertificate=True` exclusivamente para el certificado autofirmado del contenedor aislado. Esta excepción no es una configuración para servidores remotos. Los dos clientes tienen credenciales separadas:

- Migrador: `Database__AdminConnectionString`, `Database__Name`, `Database__RuntimePassword`. Sólo se ejecuta durante inicialización/mantenimiento.
- API: `ConnectionStrings__AppDbContext` con `facturacion_runtime`, sólo `SELECT`/`INSERT` en las tablas de facturación; sin actualización, borrado ni DDL.

`Local__InstallationId` identifica al propietario local y se genera una sola vez en `local.env`. Las contraseñas contienen 32 bytes aleatorios más caracteres de complejidad y no se muestran. El archivo recibe permisos restringidos. Un usuario con control de Docker o del sistema puede administrar sus contenedores; el aislamiento no pretende proteger la instalación frente a su propio administrador.

Los contenedores de aplicación/migrador usan UID 1654, sistema de archivos de sólo lectura, `tmpfs`, sin capacidades Linux y `no-new-privileges`. El volumen SQL conserva los datos. Las claves de sesión temporales pueden cambiar al reiniciar y la aplicación emite otra sesión para el mismo identificador de instalación.

## Arranque y recuperación

`iniciar.cmd` invoca `scripts/start.ps1`. Primero valida los checksums de la entrega; luego comprueba Docker Linux, prepara los secretos locales si faltan, espera la salud de SQL, construye las imágenes a partir de binarios ya compilados y ejecuta el migrador `init`. No compila Angular ni .NET en el equipo del evaluador.

El migrador no aplica cambios silenciosos a un esquema existente que tenga migraciones pendientes: requiere un mantenimiento explícito. `detener.cmd` ejecuta `docker compose stop`; no elimina volúmenes. No use `down -v` ni borre `local.env` como paso de solución de problemas.

Si el puerto `5080` está ocupado, identifique primero su propietario con las herramientas de Windows. No finalice procesos ajenos ni cambie la exposición a `0.0.0.0`. Si Docker no inicia SQL, compruebe memoria, motor Linux, espacio y virtualización; `diagnosticar.cmd` no imprime variables, cadenas de conexión ni cuerpos de peticiones.

La política de ejecución de PowerShell se establece como `RemoteSigned` sólo para cada proceso lanzado por los `.cmd`. Windows puede bloquear scripts extraídos de un ZIP marcado como descargado. Verifique hash y procedencia, desbloquee el ZIP desde Propiedades y vuelva a extraer. Una política organizacional o WDAC prevalece; no se deshabilitan sus controles. La sintaxis de los scripts es compatible con Windows PowerShell 5.1; la construcción y CI usan PowerShell 7.

## Backup y restauración

`backup.cmd` crea un backup nativo SQL con checksum, ejecuta `RESTORE VERIFYONLY` y copia el archivo a `backups/`, restringiendo sus permisos. Contiene datos propios y debe mantenerse fuera del ZIP de distribución. No exporta credenciales de Novasoft.

`restaurar.cmd` requiere un `.bak`, analiza sus nombres lógicos permitidos y restaura en `FacturacionLocal_Restored_<fecha>`. Se niega a sustituir una base ya existente. La conexión de la aplicación permanece sin cambios. Después se puede cambiar `DATABASE_NAME` de forma explícita y volver a iniciar; `init` vincula el usuario restaurado al login local sin otorgar DDL ni cambiar contraseñas existentes.

Use backups de la misma versión de SQL o de una versión compatible admitida por SQL Server. El paquete fija imagen y digest para reproducibilidad; el respaldo incluido se genera y restaura con ese mismo motor. Un backup de otra instalación conserva su propietario; no se modifica para asignárselo a otro evaluador.

## Recompilar y producir el ZIP

El desarrollador requiere Git, SDK .NET fijado por `global.json`, Node/npm fijados por `Frontend/package.json`, PowerShell 7 y Docker Linux para la verificación integral. Los paquetes NuGet/npm se restauran desde sus locks.

```powershell
dotnet restore Example.sln --locked-mode
dotnet build Example.sln --no-restore -c Release
npm ci --prefix Frontend
npm run build --prefix Frontend
pwsh ./scripts/build-release.ps1 -Version 1.0.0 -PrepareOnly
```

`build-release.ps1` exige un árbol Git limpio, publica sólo el host local y el migrador para `linux-x64`, copia Angular a `app/wwwroot`, incorpora fuentes rastreadas y documentación y crea `release.json` con estado `prepared-not-verified`. Excluye secretos de instalación, `.git`, caches, binarios de desarrollo y backups personales. La API independiente no se publica como segundo producto.

La finalización exige el job CI completo. `verify-and-package.ps1` rechaza una finalización aislada sin el contexto y el gate de CI. El ZIP sólo se adjunta cuando todos los pasos del job han terminado correctamente; en un fallo se adjuntan los reportes disponibles, sin presentar el ZIP como válido.

## Pipeline de seguridad y calidad

`.github/workflows/quality.yml` corre en `ubuntu-24.04` para PR, push a `main` o ejecución manual. `permissions: contents: read`, checkout sin credenciales persistentes, sin secretos de repositorio ni ejecución `pull_request_target`. Todos los pasos son bloqueantes salvo la limpieza de los contenedores creados por el mismo job.

1. Instala acciones por SHA y analizadores por checksum publicado; `scripts/toolchain-lock.json` registra versiones y orígenes consultados.
2. Gitleaks revisa historial y árbol con resultados redactados. No se permiten secretos de uso real ni se copian las credenciales del examen a la aplicación.
3. Restaura locks, audita NuGet y npm y falla si no puede consultar feeds. Todas las vulnerabilidades NuGet bloquean. npm bloquea desde severidad moderada.
4. Comprueba formato, compila .NET con sus analizadores de seguridad y Angular estricto. Semgrep CE revisa reglas locales explícitas de ejecución dinámica y bypass/inyección de HTML en TypeScript. Son reglas limitadas; el análisis manual y las pruebas de autorización cubren riesgos que esos patrones no detectan. No se afirma análisis Semgrep completo de C# moderno.
5. Crea un SQL Server efímero con contraseña aleatoria por job, ejecuta tests .NET (incluidos SQL real), tests Angular y Playwright. La variable `FACTURACION_TEST_SQL_CONNECTION` es obligatoria para esta ejecución.
6. Trivy revisa dependencias, secretos, configuración e imágenes oficiales/compiladas; los hallazgos altos/críticos bloquean. No se utiliza `--ignore-unfixed` ni se ignoran fallos del escáner.
7. Genera un backup **vacío migrado**, lo restaura en otra base, verifica esquema/historial, ausencia de datos y `CHECKDB`, prueba los contenedores y realiza guardado/consulta/idempotencia contra esa restauración.
8. ZAP baseline inspecciona únicamente la aplicación local efímera y su simulador. No ataca Novasoft ni servicios externos. La exploración pasiva no sustituye un pentest autenticado. Las únicas reglas no aplicables declaradas son HSTS y flag Secure en HTTP loopback; HttpOnly, SameSite, CSRF y restricciones de origen siguen exigidos.
9. Escanea el paquete, genera SBOM CycloneDX, manifiesto y checksums, comprime y vuelve a extraer para verificar integridad y secretos. Sólo entonces se adjunta el ZIP.

Los reportes se conservan siete días en el repositorio privado de CI. Para la entrega estable, el propietario descarga la evidencia redactada necesaria y la incorpora a la carpeta autorizada de SharePoint. Los reportes de cobertura/pruebas no garantizan cobertura total; una vulnerabilidad bloqueante requiere corrección o revisión explícita documentada, nunca un `continue-on-error` general.

Los límites de GitHub Actions de la cuenta personal aplican a la ejecución; no se habilitan compras, ampliaciones ni servicios pagos mediante estos scripts. El repositorio debe exigir el check de calidad antes de aceptar cambios en `main`, configuración que se verifica en GitHub y no se presume por existir el YAML.

## Actualización y reversión

Antes de cambiar versiones, genere un backup y conserve el ZIP, checksum y `local.env` de la instalación anterior en un lugar privado. Revise las migraciones y dependencias del commit nuevo. Si hay migraciones pendientes, detenga la aplicación, ejecute el migrador `migrate` como mantenimiento explícito y vuelva a verificar con `init`; no reutilice la automatización de primer arranque para un cambio de esquema no revisado.

La reversión de binarios sólo es válida si el esquema sigue siendo compatible. Para cambios incompatibles, restaure el backup en una base nueva y cambie explícitamente la configuración hacia ella usando el paquete anterior. No se ejecuta un rollback destructivo automático.

Ante un secreto expuesto, retire el paquete afectado, revoque/rote el secreto en su sistema de origen, revise los accesos y genere otra entrega desde un commit corregido. Borrar una línea del historial no revoca la credencial. Nunca adjunte `local.env`, logs con datos ni backups personales como evidencia pública.
