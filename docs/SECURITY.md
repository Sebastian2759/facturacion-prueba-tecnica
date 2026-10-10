# Seguridad del proyecto

La política vigente está en [SECURITY.md de la raíz](../SECURITY.md).

## Interpretación de los hallazgos de terceros

Detectar, analizar y gestionar vulnerabilidades de dependencias es una actividad habitual de DevSecOps. Una imagen oficial puede contener componentes con fallos conocidos; documentarlos permite decidir con evidencia y comprobar posteriormente su corrección. El proceso de referencia incluye identificación, reporte y remediación ([OWASP VMG](https://owasp.org/projects/vulnerability-management-guide)), junto con prácticas para reducir vulnerabilidades y su impacto ([NIST SSDF](https://csrc.nist.gov/pubs/sp/800/218/final)). Estas referencias no certifican esta aplicación.

Los hallazgos SQL documentados están en componentes de la imagen del proveedor. No evidencian que la demo carezca de protección: existen loopback, SQL sin puerto publicado, red Docker interna, validación, sesión/CSRF y mínimo privilegio. Esos controles reducen exposición, pero no corrigen los componentes afectados ni prueban que todos los CVE sean inaccesibles.

**Es habitual revisar y encontrar hallazgos; no se considera normal ignorarlos.** No se ha demostrado explotación mediante la aplicación ni se han clasificado todos como falsos positivos. El tratamiento consiste en investigar aplicabilidad, conservar evidencias, evaluar versiones corregidas y repetir análisis/pruebas. Cualquier aceptación temporal exige una decisión explícita y registrada, independiente de esta explicación. **Los hallazgos siguen abiertos con riesgo aceptado por el propietario para evaluación local.** Véase la [decisión SQL-LOCAL-2026-10-09](sql-risk-acceptance.md). La nueva CI debe comprobar su alcance y los demás controles antes de generar el ZIP; no equivale a un escaneo sin vulnerabilidades.

El alcance es evaluación local Windows x64, repositorio GitHub privado y distribución controlada. El host web opcional queda fuera de la entrega; no hay publicación Azure/Cloudflare ni IdP requerido. La [CI 37994136672](https://github.com/Sebastian2759/facturacion-prueba-tecnica/actions/runs/37994136672), commit `b0b7e25eb93f93b2870281bc2bc716a38ff2b463`, aprobó 159 pruebas .NET, backup/restore, CHECKDB, migradores, siete respuestas API/no-store, ambos OpenAPI y 1 navegador live SQL con CSP/estilos corregidos. El gate ZAP pasó con informativos risk 0 revisados. Semgrep 1.179.0 aprobó 31 archivos sin hallazgos/errores desde PyPI con hashes y venv aislado. Trivy SQL mantiene el único gate previo a empaquetado bloqueante. Esas verificaciones no se presentan como pentest o auditoría exhaustiva.

Véanse también [arquitectura](ARCHITECTURE.md), [decisiones](decisions.md) y [estado de pruebas](TESTING.md).

La [evaluación del runtime SQL](sql-runtime-risk-assessment.md) conserva los digests y los hallazgos investigados, con fuentes y límites del análisis. La aceptación conserva el escaneo y verifica un inventario acotado; el candidato SQL Server 2025 también falla el diagnóstico del run6. El análisis de símbolos del run7 confirma funciones asociadas a CVE en dos binarios SQL 2022 y mantiene el bloqueo, sin afirmar explotabilidad remota demostrada.
