# Evaluación local — comenzar aquí

**Riesgo SQL aceptado para esta evaluación:** consulte [motivo, alcance y vigencia](sql-risk-acceptance.md). El paquete identifica los hallazgos pendientes; use datos ficticios y acceso local. La aceptación no acredita por sí sola que el ZIP esté generado ni que Windows haya sido probado.

Esta entrega se ejecuta en **Windows x64 con Docker Desktop en modo Linux**. La aplicación abre en `http://127.0.0.1:5080`. El paquete contiene Angular y .NET ya compilados: el evaluador no necesita Node, Visual Studio ni SDK de .NET.

**Estado de entrega:** revise release.json y la evidencia del mismo payload. CI15 acredita la versión anterior; una verificación local posterior se identifica expresamente como local. No se publica la aplicación en servicios de alojamiento.

## 1. Obtener el ZIP local

Use el ZIP y checksum de la carpeta local de entrega. Las publicaciones y permisos de SharePoint/GitHub están aplazados. El paquete incluye fuentes y documentación para revisar sin acceso remoto.

Antes de extraer, compare el SHA-256 del ZIP en PowerShell:

```powershell
Get-FileHash -LiteralPath '.\Aplicacion-<version>.zip' -Algorithm SHA256
```

Compare el valor completo con el publicado en `SHA256SUMS.txt`. La comparación detecta corrupción; la identidad de quien entrega depende del canal SharePoint autorizado y de la referencia Git del manifiesto.

Windows puede marcar los archivos descargados. Después de comprobar su procedencia e integridad, abra **Propiedades del ZIP → Desbloquear → Aplicar** y luego extráigalo. Los lanzadores usan `RemoteSigned` únicamente para su propio proceso; no modifican la política global ni evaden una política organizacional. Si esa política impide ejecutar scripts, el administrador del equipo debe habilitar el mecanismo aprobado; no desactive controles del equipo.

## 2. Preparar el equipo una sola vez

- Windows x64, virtualización habilitada y Docker Desktop con motor Linux y Compose v2 funcionando. WSL 2 o la virtualización pueden requerir instalación previa y reinicio.
- Internet para la descarga inicial de imágenes oficiales fijadas por digest. Después, las operaciones locales usan los contenedores descargados.
- Memoria suficiente para SQL Server (límite de 2 GiB), la API (512 MiB), Docker y Windows. Se recomiendan al menos 6 GiB asignables al entorno Docker y 10 GiB de disco libre para imágenes, base y backups.
- Puerto `5080` libre. SQL Server no publica ningún puerto hacia Windows en la entrega.

**Si Docker ya funciona con WSL 2 y contenedores Linux, no necesita entrar a la BIOS ni repetir la instalación.** Puede pasar directamente a extraer e iniciar. Si falta Docker, instálelo y complete su primer inicio. Sólo si informa virtualización deshabilitada, revise Administrador de tareas → Rendimiento → CPU y siga las instrucciones del fabricante para activar SVM (AMD) o VT-x (Intel) en BIOS/UEFI, guardar y reiniciar. En un equipo administrado puede necesitar ayuda de TI.

Esta preparación es una sola vez por equipo. Los scripts comprueban Docker y el perfil Linux; no instalan software de sistema, cambian la BIOS ni aceptan términos por usted. Después, iniciar.cmd automatiza la aplicación y su base de datos.

## 3. Extraer e iniciar

Extraiga todo el ZIP en una carpeta local, por ejemplo `C:\Evaluacion\Facturacion`. Evite ejecutar dentro del ZIP o de una carpeta sincronizada por OneDrive/SharePoint. Las rutas con espacios se manejan con argumentos de archivo y comillas.

Abra `LEEME.html` para la guía visual sin conexión. Luego ejecute:

```text
iniciar.cmd
```

El comando verifica los archivos, genera credenciales aleatorias de esta instalación, descarga los runtimes, inicia SQL, aplica la inicialización con el migrador separado, inicia la aplicación y abre el navegador. La primera ejecución tarda más por la descarga y el arranque de SQL Server. Una segunda ejecución conserva las facturas y el identificador de instalación.

En `local.env` se guardan únicamente secretos de esta instalación. El archivo recibe permisos restringidos al usuario y nunca debe adjuntarse a SharePoint ni al repositorio. Conservarlo permite conservar el propietario de las facturas. No lo sustituya por el archivo de otra instalación.

## 4. Recorrido de evaluación

1. Abra el listado de facturas y el formulario de creación.
2. Añada detalles, pruebe valores vacíos/no válidos y el umbral de descuento de `500000`.
3. Guarde una factura, compruebe el número asignado y consulte su detalle en el listado.
4. Configure Novasoft con configurar-novasoft.cmd, reinicie y abra Cuentas para consultar o registrar cuentas en el proveedor. Sin credenciales verá el estado de configuración pendiente. Las facturas se almacenan en SQL.
5. Detenga y vuelva a iniciar: las facturas deben permanecer disponibles.

La sesión local se establece al abrir la aplicación y las escrituras requieren antiforgery. No necesita una cuenta cloud. Accounts real permanece bloqueado en este host incluso si se configura accidentalmente una bandera de activación.

## 5. Detener, diagnosticar y respaldar

| Acción | Comando | Efecto |
|---|---|---|
| Detener | `detener.cmd` | Detiene contenedores; conserva volumen SQL y configuración. |
| Diagnosticar | `diagnosticar.cmd` | Muestra estado de servicios, integridad y salud sin imprimir secretos. |
| Crear backup | `backup.cmd` | Genera y verifica un `.bak` en `backups/`. Contiene los datos locales actuales. |
| Restaurar una copia | `restaurar.cmd -BackupPath "C:\ruta\backup.bak"` | Crea una base nueva; no sustituye la base usada por la aplicación. |

El backup `database/demo.bak` de la entrega contiene **el esquema migrado vacío**, sin facturas ni datos personales. CI restaura este backup en una base diferente, comprueba sus tablas, historial de migraciones, ausencia de datos y consistencia; después crea y consulta una factura ficticia mediante la API conectada a esa restauración.

Para cambiar a una copia restaurada, haga primero un backup, detenga la aplicación y cambie únicamente `DATABASE_NAME` en `local.env` por el nombre nuevo mostrado por el comando de restauración. Ejecute `iniciar.cmd`. Las facturas conservan su propietario original: restaurar un backup de otra instalación no transfiere la titularidad de sus registros.

## Contenido de la entrega

```text
README.md / LEEME.html             Inicio y guía visual
iniciar.cmd / detener.cmd         Operación cotidiana
diagnosticar.cmd                  Comprobaciones sin secretos
backup.cmd / restaurar.cmd        Respaldo y copia independiente
compose.yaml / scripts/           Contenedores y automatización
app/                             Angular y .NET compilados
database/migrator/ / demo.bak     Migrador y backup vacío verificado
source/                          Fuentes del mismo commit, incluidos tests y CI
docs/                            Arquitectura, decisiones, requisitos, seguridad y operación
release.json / SBOM.cdx.json      Trazabilidad e inventario de componentes
SHA256SUMS.txt                    Integridad de los archivos extraídos
```

Los resultados de CI, sus limitaciones y las incidencias pendientes se revisan junto con la entrega. Un análisis estático o ZAP baseline no equivale a una auditoría completa ni acredita ausencia de vulnerabilidades.

## Distribución posterior

El alcance actual es local. Si se reanuda la distribución, copie ZIP, checksum y evidencias; compruebe permisos y descarga del evaluador. No incluya secretos ni backups personales.

Consulte `docs/operations.md` para soporte, actualizaciones controladas y detalles técnicos.

## Novasoft real

Ejecute configurar-novasoft.cmd, introduzca credenciales de prueba, detenga/inicie y elija Novasoft real en Cuentas. No se incluyen credenciales en el ZIP. Consulte [la guía](novasoft-real.md).
