# Evaluación del riesgo de la imagen SQL Server

Fecha de corte: 9 de octubre de 2026, después de la CI 37987889714 (run6). Estado: investigación abierta; la entrega sigue bloqueada por el gate de la imagen SQL y la verificación funcional del paquete está incompleta. **No se aplicaron exclusiones, excepciones ni cambios de umbral.** Esta revisión documental no demuestra explotabilidad ni ausencia de riesgo.

## Imagen y evidencia de origen

La imagen fijada en [compose.yaml](../compose.yaml) y evaluada en la [CI 37986378549](https://github.com/Sebastian2759/facturacion-prueba-tecnica/actions/runs/37986378549), commit `21a36f9a246727db3f602d58cb1055dc4528c515`, es:

```text
mcr.microsoft.com/mssql/server:2022-latest@sha256:4402d880dd4c34bfa7d8705e56a86cd6c88da80a1f6bbbe741f999e76264a090
SQL Server 2022 CU27 / 16.0.4295.3 / Ubuntu 22.04 / linux-amd64
```

El registro de esa ejecución identifica OpenSSL/libssl3 `3.0.2-0ubuntu1.29` y Go stdlib `1.26.1` en tres ejecutables: `/opt/mssql/bin/launchpadd`, `/opt/mssql-extensibility/bin/launchpad` y `/opt/mssql/bin/setnetbr`. La repetición de un CVE en tres ejecutables no equivale a tres vulnerabilidades distintas.

Las 149 pruebas .NET aprobadas de esa ejecución siguen siendo su evidencia histórica. La [CI 37987889714](https://github.com/Sebastian2759/facturacion-prueba-tecnica/actions/runs/37987889714), commit `d17f159934d5d49f4ada0372e7684e34a920aab8`, aporta el diagnóstico adicional siguiente. Los JSON de seguridad y el TRX se descargaron y se verificó el SHA del artefacto antes de analizarlos.

| Imagen analizada | Hora del informe, 9 de octubre (UTC) | Ocurrencias HIGH | Ocurrencias CRITICAL | CVE únicos HIGH/CRITICAL |
| --- | --- | ---: | ---: | ---: |
| SQL 2022, digest `4402d880…` | 20:39:54 | 71 | 0 | 24 |
| SQL 2025 CU9 / Ubuntu 24.04, digest `2b5b5816…` | 20:40:13 | 71 | 0 | 24 |
| ASP.NET 10, digest `222759b3…` | 20:39:30 | 0 | 0 | 0 |

Digests exactos de los otros dos informes:

```text
mcr.microsoft.com/mssql/server:2025-CU9-ubuntu-24.04@sha256:2b5b581621126574f3d1f75e78d3eebe8d05aedb59ad0cfdf9aa42cb0634d726
mcr.microsoft.com/dotnet/aspnet:10.0@sha256:222759b391a1aaf241166672c8f99b2d4ada452e7b5319f3c6e8f265a37b5ad4
```

Ambas imágenes SQL presentan los mismos 24 identificadores CVE: **23 de Go repetidos en tres binarios (69 ocurrencias), más un CVE OpenSSL repetido en dos paquetes (2 ocurrencias)**. Por tanto, 71 no significa 71 CVE diferentes. SQL 2025 conserva Go `v1.26.1`; sus paquetes OpenSSL son `openssl` y `libssl3t64` `3.0.13-0ubuntu3.15`, con corrección indicada en `.16`. El candidato SQL 2025 también devuelve exit code 1 y no está aprobado ni sustituye la imagen de entrega. El cero de ASP.NET se limita a los hallazgos HIGH/CRITICAL del informe; no es una garantía de ausencia de cualquier vulnerabilidad.

La comparación se calculó desde `trivy-sqlserver.json`, `trivy-sql2025-candidate.json`, `trivy-aspnet.json` y `runtime-images-summary.json` del artefacto de seguridad del run6, contando cada registro de vulnerabilidad y deduplicando `VulnerabilityID` por imagen.

El TRX del run6 confirma **150 pruebas .NET aprobadas y cero omitidas**. El frontend registra 16 pruebas unitarias, cuatro de proxy y siete de navegador con respuestas controladas. El job funcional publicó el payload y completó `init` del migrador para `FacturacionCI_ReleaseDemo`, pero falló después en el lote backup/restore. El punto exacto de fallo aún requiere un diagnóstico SQL legible; la corrección del lote está en preparación. No se declara backup restaurado ni se alcanzaron el smoke API del paquete, E2E live o ZAP. Estos resultados no liberan el ZIP.

## OpenSSL: paquete afectado y condición de activación

Ubuntu identifica `3.0.2-0ubuntu1.30` como corrección de CVE-2026-84782 para Ubuntu 22.04 y `3.0.13-0ubuntu3.16` para Ubuntu 24.04. Las versiones `.29` y `.15` detectadas, respectivamente, son anteriores a los paquetes corregidos. Debe compararse la revisión del paquete Ubuntu, que incorpora parches, y no sólo el número upstream. [Aviso Ubuntu](https://ubuntu.com/security/CVE-2026-84782).

El fallo afecta la retransmisión DTLS cuando una escritura de handshake queda suspendida y se reutiliza incorrectamente su posición de buffer. Puede divulgar memoria o causar una caída. [Aviso OpenSSL del 29 de septiembre](https://openssl-library.org/news/secadv/20260929.txt).

Microsoft documenta la conexión de clientes SQL mediante TDS/TLS sobre TCP. Por ello, inferimos que ese flujo de conexión no activa la condición DTLS descrita. Esto no demuestra que ningún otro proceso de la imagen utilice DTLS: falta inventariar usos, procesos y bibliotecas cargadas. No se clasifica toda la imagen como no afectada. [TDS y TLS](https://learn.microsoft.com/en-us/sql/relational-databases/security/networking/tds-8-and-tls-1-3?view=sql-server-ver16).

## Go: presencia, plataforma y funciones

Los 23 CVE high reportados para cada uno de los tres binarios se contrastaron con los registros oficiales de Go. La tabla resume condiciones que deben investigarse; no acredita su presencia o ejecución en esos binarios.

| CVE de 2026 | Función o condición descrita por Go | Fuente primaria |
| --- | --- | --- |
| 27145, 32280, 32281, 33810 | Validación de nombres, cadenas, políticas o restricciones de certificados X.509. Algunos requieren una cadena de confianza; 27145 también puede procesar certificados no confiables. | [5037](https://pkg.go.dev/vuln/GO-2026-5037), [4947](https://pkg.go.dev/vuln/GO-2026-4947), [4946](https://pkg.go.dev/vuln/GO-2026-4946), [4866](https://pkg.go.dev/vuln/GO-2026-4866) |
| 32283, 56862, 97031 | Procesamiento TLS: KeyUpdate/post-handshake o referencias ECH malformadas. | [4870](https://pkg.go.dev/vuln/GO-2026-4870), [6090](https://pkg.go.dev/vuln/GO-2026-6090), [6607](https://pkg.go.dev/vuln/GO-2026-6607) |
| 33814, 56853, 78669 | HTTP/2: SETTINGS, preámbulo sin cifrado o cambios repetidos de ventana. | [4918](https://pkg.go.dev/vuln/GO-2026-4918), [6089](https://pkg.go.dev/vuln/GO-2026-6089), [6611](https://pkg.go.dev/vuln/GO-2026-6611) |
| 78667 | Servicio de archivos/contenido HTTP con cabeceras Range patológicas. | [6609](https://pkg.go.dev/vuln/GO-2026-6609) |
| 39821 | Conversión IDNA de nombres que podría alterar una decisión de permisos. | [5026](https://pkg.go.dev/vuln/GO-2026-5026) |
| 33811, 46600 | Resolución DNS CNAME con cgo o registros DNS malformados. | [4981](https://pkg.go.dev/vuln/GO-2026-4981), [5942](https://pkg.go.dev/vuln/GO-2026-5942) |
| 39822 | Operaciones os.Root en Unix con enlace simbólico y barra final. | [4970](https://pkg.go.dev/vuln/GO-2026-4970) |
| 39836 | Dial/LookupPort con NUL, exclusivamente en Windows. El sistema anfitrión Windows no cambia la plataforma de los ejecutables Linux. Es evidencia para una revisión individual de aplicabilidad, no una exclusión aplicada. | [4971](https://pkg.go.dev/vuln/GO-2026-4971) |
| 33818, 56859 | Profundidad de estructuras ASN.1 o XML. | [5972](https://pkg.go.dev/vuln/GO-2026-5972), [6088](https://pkg.go.dev/vuln/GO-2026-6088) |
| 39820, 42499, 42504 | Análisis de direcciones/comentarios de correo y cabeceras MIME. | [4986](https://pkg.go.dev/vuln/GO-2026-4986), [4977](https://pkg.go.dev/vuln/GO-2026-4977), [5038](https://pkg.go.dev/vuln/GO-2026-5038) |
| 56858, 56860 | Escape JavaScript en plantillas HTML o resolución de rutas URL relativas. | [6091](https://pkg.go.dev/vuln/GO-2026-6091), [6218](https://pkg.go.dev/vuln/GO-2026-6218) |

Los avisos más recientes de la lista incluyen correcciones en Go 1.26.9 y 1.27.2. Actualizar el compilador instalado o los paquetes Ubuntu no recompila los ejecutables distribuidos por Microsoft ni sustituye automáticamente su biblioteca estándar incorporada. Hace falta un binario corregido del proveedor o evidencia concreta de no aplicabilidad por CVE.

Microsoft sitúa `launchpadd` y `launchpad` en la ejecución de lenguajes externos. `external scripts enabled` tiene valor predeterminado `0`; el valor efectivo, los permisos y los procesos deben verificarse. Ese valor no demuestra por sí solo que los tres ejecutables nunca se ejecuten, y no se ha documentado aquí una función exacta de `setnetbr`. [Extensibilidad](https://learn.microsoft.com/en-us/sql/language-extensions/concepts/extensibility-framework?view=sql-server-ver17), [opciones de servidor](https://learn.microsoft.com/en-us/sql/database-engine/configure-windows/server-configuration-options-sql-server?view=sql-server-ver16).

El [issue Microsoft 960](https://github.com/microsoft/mssql-docker/issues/960) informa versiones Go vulnerables en esas rutas. Es un reporte de terceros dentro del repositorio del proveedor; no constituye un análisis de alcanzabilidad ni una aprobación de excepción de Microsoft.

## Diagnóstico binario propuesto, todavía no ejecutado

`govulncheck -mode=binary` lee el ejecutable y puede discriminar las versiones de módulos de las funciones vulnerables cuyos símbolos estén presentes. No ejecuta los binarios SQL analizados. Sin símbolos utilizables, puede volver a hallazgos por módulo. La presencia de una función no prueba que sea alcanzable y el modo binario no reconstruye un grafo completo de llamadas. [Documentación de govulncheck v1.8.0](https://pkg.go.dev/golang.org/x/vuln@v1.8.0/cmd/govulncheck).

Se propone `golang.org/x/vuln/cmd/govulncheck@v1.8.0`, versión oficial fija publicada el 8 de septiembre de 2026, con Go 1.26.9 en un job de diagnóstico Linux. Su [go.mod](https://raw.githubusercontent.com/golang/vuln/v1.8.0/go.mod) requiere Go 1.26.0 como mínimo. Esto es una receta de análisis, no una herramienta ya instalada o ejecutada en la CI citada.

Los binarios se copiarían desde un contenedor **creado y nunca iniciado**, usando el digest ya disponible en CI. No hace falta descargar SQL al equipo Windows ni iniciar Launchpad. Ejemplo Bash para un directorio de trabajo nuevo:

```bash
set -euo pipefail
sql_image='mcr.microsoft.com/mssql/server:2022-latest@sha256:4402d880dd4c34bfa7d8705e56a86cd6c88da80a1f6bbbe741f999e76264a090'
evidence_dir="$PWD/work/sql-runtime-risk"
mkdir -p "$evidence_dir/bin" "$evidence_dir/reports" "$evidence_dir/tools"
container_id=$(docker create --entrypoint /bin/true "$sql_image")
trap 'docker rm -v "$container_id" >/dev/null' EXIT
docker cp "$container_id:/opt/mssql/bin/launchpadd" "$evidence_dir/bin/launchpadd"
docker cp "$container_id:/opt/mssql-extensibility/bin/launchpad" "$evidence_dir/bin/launchpad"
docker cp "$container_id:/opt/mssql/bin/setnetbr" "$evidence_dir/bin/setnetbr"

test "$(go env GOVERSION)" = 'go1.26.9'
GOTOOLCHAIN=local GOBIN="$evidence_dir/tools" \
  go install golang.org/x/vuln/cmd/govulncheck@v1.8.0
"$evidence_dir/tools/govulncheck" -version > "$evidence_dir/reports/tool-version.txt"
for binary in launchpadd launchpad setnetbr; do
  file "$evidence_dir/bin/$binary" > "$evidence_dir/reports/$binary.file.txt"
  sha256sum "$evidence_dir/bin/$binary" > "$evidence_dir/reports/$binary.sha256"
  go version -m "$evidence_dir/bin/$binary" > "$evidence_dir/reports/$binary.build.txt"
  "$evidence_dir/tools/govulncheck" -mode=binary -scan=module -format=json \
    "$evidence_dir/bin/$binary" > "$evidence_dir/reports/$binary.module.json"
  "$evidence_dir/tools/govulncheck" -mode=binary -scan=symbol -format=json \
    "$evidence_dir/bin/$binary" > "$evidence_dir/reports/$binary.symbol.json"
done
```

Los niveles `module` y `symbol` están admitidos por las [opciones de esa versión](https://raw.githubusercontent.com/golang/vuln/v1.8.0/internal/scan/flags.go). El formato JSON es un flujo de registros; su exit code puede ser cero aunque haya vulnerabilidades. Deben revisarse los registros de configuración, versión Go, SBOM y findings, además de stderr y fallos de extracción/feed. Cero hallazgos o una diferencia entre ambos niveles no genera automáticamente una excepción. Conservar fecha y metadatos de la base de vulnerabilidades junto con los informes: fijar sólo la herramienta no fija un feed que cambia.

Una conclusión individual necesita identificar digest, hash del ejecutable, plataforma, extracción de símbolos satisfactoria, CVE y condición que se ha descartado. Los casos sin evidencia suficiente quedan abiertos. No se generará ni consumirá VEX automáticamente a partir de la mera ausencia de una ruta observada.

## Opciones de corrección y criterio de entrega

1. Preferir un digest oficial actualizado que supere el gate. El candidato SQL 2025 analizado en run6 también falla y no resuelve el bloqueo. Repetir el escaneo y las pruebas SQL, migración, backup/restore y aplicación sobre cualquier nueva imagen exacta elegida. Una nueva etiqueta no acredita que esté corregida.
2. Una imagen derivada de MCR podría incorporar el paquete Ubuntu corregido para OpenSSL. Debe conservar trazabilidad y volver a validarse; no resuelve por sí sola Go incorporado.
3. Conservar los binarios del proveedor. Microsoft condiciona el soporte de personalizaciones a no modificar `/opt/mssql/*` ni `/usr/share/doc/*`; eliminar o reemplazar `launchpadd`/`setnetbr` para silenciar el escáner incumpliría esa condición. [Política de personalización](https://learn.microsoft.com/en-us/troubleshoot/sql/database-engine/install/windows/support-policy-sql-server#customizing-sql-server-containers).
4. Si permanecen hallazgos sin corrección ni evidencia suficiente de no aplicabilidad, mantener bloqueada la release. Cualquier futura evaluación individual debe ser trazable y revisada; este documento no concede excepciones ni solicita cambios de política.

El perfil local configurado conserva SQL sin puerto publicado, red Docker `internal`, credencial runtime con SELECT/INSERT y migrador separado. Esto reduce la superficie, pero no elimina riesgos de procesos locales, otro contenedor comprometido o componentes aún no caracterizados. No se ha ejecutado un pentest, ni se ha probado el ZIP con Docker en el equipo Windows, ni se promete riesgo cero.
