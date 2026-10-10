# Evaluación del riesgo de la imagen SQL Server

**Decisión vigente:** el propietario aprobó el riesgo residual SQL para evaluación local con datos ficticios. La [aceptación SQL-LOCAL-2026-10-09](sql-risk-acceptance.md) y su inventario exacto permiten tratar sólo los hallazgos conocidos; los demás controles siguen siendo obligatorios. Los resultados que siguen conservan el historial anterior a esta decisión. La nueva CI y el ZIP aún están pendientes, al igual que el arranque completo en Windows.

La detección de estos componentes afectados es un resultado del proceso de revisión DevSecOps. No se atribuye a una demo sin controles ni implica que los fallos sean aceptables por ser habituales. Su gestión requiere análisis, reducción de exposición y corrección verificada; la [explicación para el evaluador](SECURITY.md#interpretación-de-los-hallazgos-de-terceros) distingue este contexto de una aceptación formal del riesgo. Los hallazgos permanecen abiertos con riesgo aceptado por el propietario según la decisión vigente indicada arriba.

## Alternativa oficial RHEL comprobada el 9 de octubre de 2026

La [ejecución diagnóstica 38014461532](https://github.com/Sebastian2759/facturacion-prueba-tecnica/actions/runs/38014461532), commit `f13866bd3926b9bfe64a49f60ea10711141327ed`, analizó sin ejecutar la imagen oficial `mcr.microsoft.com/mssql/rhel/server:2022-latest@sha256:a28ec29377c9a45f3afe7411c27b75124e3c6dd3e4f5adab930eb4bea2951dcc`. Trivy detectó RHEL 9.8 y **113 ocurrencias HIGH, 50 CVE únicos y cero CRITICAL**: 44 ocurrencias en paquetes del sistema y las mismas 69 ocurrencias Go en tres binarios. Cambiar de Ubuntu a esta imagen no elimina el bloqueo. La imagen de la aplicación no se modificó y no se aplicó una excepción.

El artefacto de diagnóstico `11655243964` se descargó y verificó contra SHA-256 `424407544c524bb23b2ac3492b9afa7540a23b166151770ab6e53404426fec7d`; Gitleaks de los reportes descargados no encontró secretos. El workflow manual `sql-candidate.yml` sólo produce evidencia; no publica ni autoriza el ZIP. En el equipo local, WDAC impidió ejecutar Trivy, por lo que el diagnóstico se realizó en GitHub Actions sin cambiar controles de Windows.

Fecha de corte: 9 de octubre de 2026, después de la CI 37994136672 (run10). Estado: SQL 2022 mantiene el único gate previo a empaquetado bloqueante; funcionalidad, CSP/CSS, OpenAPI, ZAP y Semgrep pasaron. **No se aplicaron exclusiones, excepciones ni cambios de umbral al análisis SQL.** La revisión confirma versiones y símbolos afectados presentes, pero no demuestra explotabilidad ni ausencia de riesgo.

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

El TRX del run6 confirma **150 pruebas .NET aprobadas y cero omitidas**. El frontend registró 16 pruebas unitarias, cuatro de proxy y siete de navegador con respuestas controladas. Esa ejecución publicó el payload y completó `init` del migrador, pero falló en el lote backup/restore. Este resultado se conserva como historial; los avances posteriores figuran en el corte del run7 siguiente.

La [CI 37989162596](https://github.com/Sebastian2759/facturacion-prueba-tecnica/actions/runs/37989162596), commit `c6b44269ac0944289cb1d848a48510efcd1e5523`, volvió a aprobar las 150 pruebas .NET y completó backup/restore, CHECKDB, el migrador, el smoke API del paquete y la prueba de navegador live. Los controles de configuración SQL también pasaron. La fase funcional terminó con cuatro advertencias ZAP; el gate de imagen SQL sigue fallando. No se declara ZIP liberado ni ejecución del paquete en Docker Desktop Windows.

El run8, [CI 37991580284](https://github.com/Sebastian2759/facturacion-prueba-tecnica/actions/runs/37991580284), commit `8d4469a0e47c3cd4b61ca8ebb397d8b9327708c2`, vuelve a analizar los mismos digests literales anteriores. Los JSON registran SQL 2022 a las 21:16:10 UTC, SQL 2025 a las 21:16:22 UTC y ASP.NET a las 21:15:06 UTC: cada imagen SQL conserva **71 ocurrencias HIGH, 24 CVE únicos y 0 CRITICAL**; ASP.NET, 0 HIGH/CRITICAL. No se sustituye el análisis de símbolos histórico del run7 por una afirmación de explotabilidad.

El TRX run8 confirma **159/159 aprobadas, 0 omitidas**, incluidas cinco SQL. El recibo funcional, los reportes del smoke/OpenAPI y el navegador real acreditan backup/restore, CHECKDB, migradores, API/no-store y nonce/CSS corregidos. `zap-gate.json` indica passed, exit 2 del escáner revisado, sólo informativos risk 0 aceptados y ninguna alerta rechazada. Semgrep no se ejecutó por el 429; no es un hallazgo del código ni una aprobación. `functional-evidence.json` se identifica como `functional-verified-not-authorized-for-release`: ZIP final y Windows con Docker siguen pendientes.

El cierre [run10 / CI 37994136672](https://github.com/Sebastian2759/facturacion-prueba-tecnica/actions/runs/37994136672), commit `b0b7e25eb93f93b2870281bc2bc716a38ff2b463`, confirma el mismo conjunto de 24 CVE y los mismos digests SQL. Los informes registran SQL 2022 a las 21:39:30 UTC y SQL 2025 a las 21:39:42 UTC: **71 ocurrencias HIGH, 24 CVE únicos y 0 CRITICAL por imagen**. ASP.NET, a las 21:38:47 UTC, conserva 0 HIGH/CRITICAL. SQL 2025 sigue siendo sólo diagnóstico y no sustituye el runtime requerido.

El TRX confirma **159/159 aprobadas y 0 omitidas**, incluidas cinco SQL. También pasaron 28 unitarias Angular, 4 proxy, 7 navegador controladas y 1 live; ambos checks OpenAPI, backup/restore/CHECKDB, migradores, siete respuestas API/no-store, nonce/CSS y gate ZAP. Semgrep 1.179.0 se ejecutó desde PyPI con hashes en venv Python 3.12.3: **31 archivos, 0 hallazgos y 0 errores**. El bloqueo de disponibilidad de SAST quedó resuelto sin relajar reglas. El recibo funcional del run10 conserva `functional-verified-not-authorized-for-release`; no hay ZIP final aprobado ni prueba de instalación Windows.

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

Microsoft sitúa `launchpadd` y `launchpad` en la ejecución de lenguajes externos. `external scripts enabled` tiene valor predeterminado `0`; el run7 verificó ese valor efectivo y tres permisos externos ausentes, como se detalla abajo. Esto no demuestra por sí solo que los tres ejecutables nunca se ejecuten, y no se ha documentado aquí una función exacta de `setnetbr`. [Extensibilidad](https://learn.microsoft.com/en-us/sql/language-extensions/concepts/extensibility-framework?view=sql-server-ver17), [opciones de servidor](https://learn.microsoft.com/en-us/sql/database-engine/configure-windows/server-configuration-options-sql-server?view=sql-server-ver16).

El [issue Microsoft 960](https://github.com/microsoft/mssql-docker/issues/960) informa versiones Go vulnerables en esas rutas. Es un reporte de terceros dentro del repositorio del proveedor; no constituye un análisis de alcanzabilidad ni una aprobación de excepción de Microsoft.

## Diagnóstico binario ejecutado en run7

Se revisaron completos los seis flujos JSON (`module` y `symbol` por ejecutable), sus seis stderr, los tres metadatos, `summary.json`, `tool-version.txt` y `sql-runtime-configuration.txt`. Pertenecen al artefacto `11644926583` del run7; SHA-256 del archivo de artefacto verificado: `7722e78ad3d2ffd96e478407e6bbe4cde52ef7f0f697748a6fb16c24dcb3ac2e`. Los metadatos de extracción están fechados entre 20:54:46 y 20:54:48 UTC del 9 de octubre.

El [script de diagnóstico](../scripts/diagnose-sql-binaries.ps1) copió los tres archivos desde un contenedor creado y nunca iniciado de la imagen SQL 2022 fijada. No ejecutó ni modificó los ELF del proveedor. La herramienta fue `govulncheck@v1.8.0`, compilada con Go 1.26.9; todos los registros `config` señalan `scan_mode=binary`, el nivel solicitado y `https://vuln.go.dev`, con base modificada en `2026-10-08T22:31:09Z`.

Los tres metadatos identifican ELF Linux x86-64, `not stripped`, información de depuración y `GOOS=linux`, `GOARCH=amd64`, `CGO_ENABLED=1`. Sus SBOM e información de compilación coinciden en **Go 1.26.1 dentro del binario**. Go 1.26.9 es la versión de la herramienta de diagnóstico; no actualiza el runtime SQL analizado. El módulo propietario aparece como `mssql-server/extensibility (devel)`, sin una versión pública utilizable para un inventario completo de vulnerabilidades del código Microsoft.

| Archivo | SHA-256 del ELF extraído |
| --- | --- |
| launchpad | `fed01857e8c311ea681f7df77d4b70f7a29205fec41acfcf13b7d2ad8229ffe9` |
| launchpadd | `3c0756482a482fba9923f4125c288c0da91294d8d4f0ba3560582d421d0975bd` |
| setnetbr | `7db6773f71639aefa5183544d1a164fa51ba924b3d8fc04c39bb1564eb4920f8` |

### Interpretación de los registros

Los seis flujos se pudieron decodificar hasta el final, incluyen configuración, SBOM y las etapas de progreso esperadas; sus stderr están vacíos. El código cero de las seis ejecuciones confirma que terminó el diagnóstico, **no** que no haya vulnerabilidades. Los registros `osv` incluyen avisos sin filtrar y no se contaron como hallazgos: se analizaron los registros `finding`, separados por la presencia de `trace.package` y `trace.function`. [Formato y limitaciones oficiales](https://pkg.go.dev/golang.org/x/vuln@v1.8.0/cmd/govulncheck).

| Binario | CVE únicos a nivel módulo | CVE con paquete detectado | CVE con función detectada | Registros finding de función |
| --- | ---: | ---: | ---: | ---: |
| launchpad | 40 | 15 | 10 | 20 |
| launchpadd | 40 | 15 | 11 | 22 |
| setnetbr | 40 | 5 | 0 | 0 |

Los conjuntos de paquete incluyen a los de función: las columnas no se suman. Cada flujo `module` tiene 45 registros finding para 40 identificadores Go/CVE distintos. Los flujos `symbol` conservan esos registros de módulo y añaden paquete/función, para un total de 80, 82 y 50 registros, respectivamente. Govulncheck no se filtró por severidad, por lo que sus 40 CVE no se comparan directamente con los 23 CVE Go HIGH de Trivy.

La extracción discrimina efectivamente paquetes y símbolos: no presenta una lista idéntica de funciones para todos los binarios. El código oficial recurre a los símbolos vulnerables conocidos cuando no puede extraerlos; ese comportamiento no concuerda con la selección observada, especialmente los cinco paquetes y cero funciones de `setnetbr`. Junto con los metadatos y la ausencia de errores, esto sustenta una extracción operativa válida, sin certificar que sea un análisis completo de todas las rutas o bibliotecas nativas. [Implementación binaria de govulncheck](https://raw.githubusercontent.com/golang/vuln/v1.8.0/internal/vulncheck/binary.go), [extracción de información](https://raw.githubusercontent.com/golang/vuln/v1.8.0/internal/scan/binary.go).

### Funciones asociadas a CVE presentes

| CVE de 2026 | Símbolos informados | Binarios |
| --- | --- | --- |
| 33810, 32281, 32280 | `crypto/x509.Certificate.Verify` | launchpad y launchpadd |
| 27145 | `crypto/x509.Certificate.Verify`, `Certificate.VerifyHostname`, `HostnameError.Error` | launchpad y launchpadd |
| 32283, 56862, 97031 | `crypto/tls.Conn.Handshake`, `Conn.HandshakeContext` | launchpad y launchpadd |
| 33818 | `encoding/asn1.Unmarshal`, `UnmarshalWithParams` | launchpad y launchpadd |
| 56860 | `net/url.URL.Parse`, `URL.ResolveReference` | Sólo launchpadd; en launchpad queda a nivel paquete |
| 39836 | `net.Dial`, `Dialer.Dial`, `Dialer.DialContext`, `Resolver.LookupPort` | launchpad y launchpadd; el aviso exige Windows y los ELF son Linux |
| 42505 | `crypto/tls.Conn.Handshake`, `Conn.HandshakeContext` | launchpad y launchpadd; aviso adicional fuera del conjunto HIGH inicial de Trivy |

Las fuentes de los primeros diez identificadores están en la tabla de CVE anterior. El adicional [CVE-2026-42505 / GO-2026-5856](https://pkg.go.dev/vuln/GO-2026-5856) describe pérdida de privacidad de ECH. Se conserva para revisión, sin asignarle aquí una severidad no verificada.

El caso 39836 demuestra un límite importante: coincidir en el nombre de una función no basta para establecer aplicabilidad, pues el aviso describe un defecto de Windows. Sin descontar formalmente ningún hallazgo del gate, la unión de los otros resultados contiene **nueve CVE HIGH previamente detectados por Trivy con funciones presentes y sin esa restricción Windows**: 27145, 32280, 32281, 32283, 33810, 33818, 56860, 56862 y 97031.

En `setnetbr` quedaron a nivel paquete 32282, 39822, 39836, 33811 y 46600; no se detectó ninguna función afectada. Esto acota el análisis de ese archivo, pero no elimina los resultados de los otros binarios ni el hallazgo OpenSSL. La ausencia de coincidencia no genera VEX ni una exclusión automática.

### Configuración efectiva y decisión

`sql-runtime-configuration.txt` acredita `external scripts enabled=0` y que, bajo el login `facturacion_runtime`, `EXECUTE ANY EXTERNAL SCRIPT`, `ALTER ANY EXTERNAL LANGUAGE` y `ALTER ANY EXTERNAL LIBRARY` devuelven `0`. El [control del paquete](../scripts/verify-and-package.ps1) falla ante NULL o permisos presentes. Son restricciones verificadas que reducen la superficie de ejecución de extensiones.

No se obtuvo el grafo de llamadas ni evidencia del origen y control de las entradas que llegan a las funciones Go detectadas. Tampoco se demostró que ningún proceso use OpenSSL/DTLS. La presencia de funciones/versiones afectadas está confirmada; la explotabilidad remota en este perfil permanece sin demostrar. El aislamiento y los permisos no sustituyen el parche del proveedor.

**El bloqueo de la imagen SQL sigue sustentado.** El diagnóstico descarta tratar las 71 ocurrencias como idénticamente explotables, pero tampoco permite declararlas falsos positivos en conjunto: hay funciones asociadas a nueve CVE HIGH Linux en launchpad/launchpadd, además del paquete OpenSSL pendiente de corrección. Se mantiene el gate sin excepciones hasta disponer de una imagen corregida o de evidencia individual suficiente y revisada. La copia de archivos y este análisis no se extienden a los ELF de SQL 2025; de ese candidato sólo se dispone aquí del escaneo de imagen.

## Opciones de corrección y criterio de entrega

1. Preferir un digest oficial actualizado que supere el gate. El candidato SQL 2025 analizado en run6 también falla y no resuelve el bloqueo. Repetir el escaneo y las pruebas SQL, migración, backup/restore y aplicación sobre cualquier nueva imagen exacta elegida. Una nueva etiqueta no acredita que esté corregida.
2. Una imagen derivada de MCR podría incorporar el paquete Ubuntu corregido para OpenSSL. Debe conservar trazabilidad y volver a validarse; no resuelve por sí sola Go incorporado.
3. Conservar los binarios del proveedor. Microsoft condiciona el soporte de personalizaciones a no modificar `/opt/mssql/*` ni `/usr/share/doc/*`; eliminar o reemplazar `launchpadd`/`setnetbr` para silenciar el escáner incumpliría esa condición. [Política de personalización](https://learn.microsoft.com/en-us/troubleshoot/sql/database-engine/install/windows/support-policy-sql-server#customizing-sql-server-containers).
4. Si permanecen hallazgos sin corrección ni evidencia suficiente de no aplicabilidad, mantener bloqueada la release. Cualquier futura evaluación individual debe ser trazable y revisada; este documento no concede excepciones ni solicita cambios de política.

El perfil local configurado conserva SQL sin puerto publicado, red Docker `internal`, credencial runtime con SELECT/INSERT y migrador separado. Esto reduce la superficie, pero no elimina riesgos de procesos locales, otro contenedor comprometido o componentes aún no caracterizados. No se ha ejecutado un pentest, ni se ha probado el ZIP con Docker en el equipo Windows, ni se promete riesgo cero.
