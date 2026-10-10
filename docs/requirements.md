# Requisitos y trazabilidad

**Estado vigente — Windows verificado:** `Aplicacion-1.0.0-ci.15.zip`, commit `89be6447e9f69fb7b6b24be2dd90b9fd0fbff6c6`, generado por [CI run15](https://github.com/Sebastian2759/facturacion-prueba-tecnica/actions/runs/38022085155), fue descargado y probado sin modificar en Windows 11 x64 con Docker Desktop y los lanzadores de PowerShell 5.1. Pasaron arranque desde extracción nueva, factura por API y navegador, persistencia después de detener/iniciar, backup, restauración en una base nueva y CHECKDB. SHA-256: `fd333c43e0ad90a9ef1055481761c211557b1fa95eb6975373ecc15f74799652`. SQL mantiene riesgo aceptado, no corregido. Las referencias a SVM desactivado o pruebas Windows pendientes que se conservan más abajo pertenecen a cortes históricos anteriores a esta validación.

[Detalle de la prueba Windows](windows-validation.md).

Fuente: examen práctico proporcionado por el usuario y plantilla Template. El PDF define el ejercicio; no se distribuye con el código si contiene credenciales. Las decisiones de producto/seguridad adicionales se distinguen del enunciado. La entrega online fue cancelada; queda ZIP Windows x64 con internet, GitHub privado y SharePoint.

## Facturación

| Requisito del ejercicio | Implementación / criterio de aceptación |
|---|---|
| Formulario HTML5 con backend C#; arquitectura libre. | Angular y API .NET siguiendo las capas de Template. |
| Encabezado con documento, cliente, dirección/teléfono; documento obligatorio. | Documento máximo 15. Se adoptó exigir también nombres, apellidos, dirección y teléfono como mejora. |
| Número consecutivo no editable. | Number int IDENTITY de SQL, separado del Guid interno. |
| Fecha del día no editable. | Servidor America/Bogota, persistida datetime. |
| Detalle con al menos un artículo. | 1–100 filas; código/cantidad int y precio money. |
| Código, cantidad y precio obligatorios. | Enteros/precios positivos; límites de tamaño/rango y precio máximo dos decimales. |
| Bruto, descuento, IVA y total no editables. | Calculador C# compartido entre preview/guardado; respuesta definitiva del servidor. |
| Descuento 5 % desde bruto 500.000. | Umbral inclusivo antes del descuento. |
| IVA 19 %. | Se adopta bruto menos descuento como base, al no precisarla el PDF. |
| Guardar y consultar facturas. | Encabezado/detalles atómicos, listado paginado y consulta por número. |

Nombres/apellidos: máximo 100 cada uno; dirección 200; teléfono 30. El documento se trata como texto recortado y no se inventa una restricción de sólo dígitos. Detalles y dinero se validan en aplicación y dominio; límites numéricos evitan overflow decimal/MONEY.

## Fórmula acordada

1. Línea = cantidad × precio, redondeada a dos decimales.
2. Bruto = suma de líneas, dos decimales.
3. Descuento = 5 % del bruto si bruto ≥ 500.000; en otro caso, cero.
4. IVA = 19 % de (bruto − descuento).
5. Total = bruto − descuento + IVA.

Redondeo de cada importe: MidpointRounding.AwayFromZero. MONEY conserva el tipo SQL solicitado; se admiten/previsualizan dos decimales. Ejemplos que deben cumplir los tests:

| Bruto | Descuento | IVA | Total |
|---:|---:|---:|---:|
| 100.000,00 | 0,00 | 19.000,00 | 119.000,00 |
| 499.999,99 | 0,00 | 95.000,00 | 594.999,99 |
| 500.000,00 | 25.000,00 | 90.250,00 | 565.250,00 |
| 500.000,01 | 25.000,00 | 90.250,00 | 565.250,01 |

Estos resultados son criterios del ejercicio, no un sistema de facturación electrónica ni una validación fiscal/legal.

## Parte Accounts / Novasoft

El ejercicio pide login para obtener token y consumo C# de GET/POST Accounts, mostrando el resultado. El formulario Accounts es opcional en el PDF y se implementó como mejora.

El contrato revisado difiere del ejemplo PDF: fecIng es fecha/hora y estCli no figura en el esquema consultado. El adaptador normaliza nombres y no envía un campo sin respaldo del contrato. El token y su expiración permanecen en backend.

El cliente real está implementado y sus pruebas HTTP controladas pasaron en CI; **no se ha verificado una llamada real**. LocalEvaluation exige simulación visible y rechaza modo real con 403. Una simulación aprobada no cierra la comprobación de conectividad real del ejercicio. Si se requiere esa evidencia, ejecutar una prueba posterior autorizada con credenciales válidas, entorno/datos definidos y resultado depurado, sin necesidad de desplegar en Azure.

Accounts simulado no es persistencia SQL: se reinicia al reiniciar el proceso. POST devuelve aceptación y permite consultar después por código; no inventa una respuesta de cuenta completa.

## Mejoras y límites acordados

- Propiedad por instalación, permisos en casos de uso y repositorio, idempotencia por owner/key y huella de cuerpo.
- JSON máximo 256 KiB, profundidad 16, 100 líneas y páginas de hasta 100 registros.
- Errores con rutas de campo y traceId; estados HTTP coherentes.
- Sesión local automática, Host/Origin/CSRF, rate limits y secretos generados.
- Rutas Angular diferidas, componentes compartidos, móvil, teclado y accesibilidad.
- Migraciones, tests, CI, backup/restore, SBOM y guía de operación.

No se incluye edición/eliminación de facturas, emisión fiscal, multiusuario local, producción, demo pública, login cloud ni transacciones reales contra Novasoft en el ZIP.

## Aceptación y estado

El cierre requiere crear y recuperar una factura de múltiples líneas contra SQL Server real, verificar número/fecha/totales, errores, idempotencia y propietario A/B; luego repetir el recorrido después de restaurar el backup en una base distinta.

El backup final será esquema vacío. Los ejemplos de portada son ficticios, públicos y separados; el smoke crea su propia factura. El ZIP y documentos deben identificar el mismo commit, con acceso comprobado al repositorio privado y a la copia SharePoint.

A 9 de octubre de 2026: [CI 37994136672](https://github.com/Sebastian2759/facturacion-prueba-tecnica/actions/runs/37994136672), commit `b0b7e25eb93f93b2870281bc2bc716a38ff2b463`, aprobó 159 tests .NET sin omisiones, incluidas cinco integraciones SQL y modelo EF. Angular en CI: build/formato, 28 unitarias, 4 proxy, 7 UI controladas/axe y 1 live API/SQL. Backup/restore, CHECKDB, migradores, siete respuestas API/no-store, ambos OpenAPI y gate ZAP pasaron. Gitleaks, NuGet/npm, Semgrep (31 archivos, 0 hallazgos/errores) y Trivy filesystem/ASP.NET pasaron. Trivy SQL 2022 mantiene el único gate previo a empaquetado bloqueante, sin excepción: 71 ocurrencias HIGH y 24 CVE únicos; el candidato SQL 2025 presenta el mismo resultado.

El CSS global bloqueado detectado en la captura run7 está corregido: el navegador real run8 comprobó estilos efectivos y nonce/CSP. ZIP final, instalación Windows con Docker y acceso del evaluador siguen pendientes. Node 24.21.0 LTS/npm 11.13.0 portable verificado; no hubo llamadas reales a Novasoft.
