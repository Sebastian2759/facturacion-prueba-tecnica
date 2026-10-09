# Seguridad del proyecto

La política vigente está en [SECURITY.md de la raíz](../SECURITY.md).

El alcance es evaluación local Windows x64, repositorio GitHub privado y distribución controlada. El host web opcional queda fuera de la entrega; no hay publicación Azure/Cloudflare ni IdP requerido. La [CI 37994136672](https://github.com/Sebastian2759/facturacion-prueba-tecnica/actions/runs/37994136672), commit `b0b7e25eb93f93b2870281bc2bc716a38ff2b463`, aprobó 159 pruebas .NET, backup/restore, CHECKDB, migradores, siete respuestas API/no-store, ambos OpenAPI y 1 navegador live SQL con CSP/estilos corregidos. El gate ZAP pasó con informativos risk 0 revisados. Semgrep 1.179.0 aprobó 31 archivos sin hallazgos/errores desde PyPI con hashes y venv aislado. Trivy SQL mantiene el único gate previo a empaquetado bloqueante. Esas verificaciones no se presentan como pentest o auditoría exhaustiva.

Véanse también [arquitectura](ARCHITECTURE.md), [decisiones](decisions.md) y [estado de pruebas](TESTING.md).

La [evaluación del runtime SQL](sql-runtime-risk-assessment.md) conserva los digests y los hallazgos investigados, con fuentes y límites del análisis. No hay exclusiones aplicadas; el candidato SQL Server 2025 también falla el diagnóstico del run6. El análisis de símbolos del run7 confirma funciones asociadas a CVE en dos binarios SQL 2022 y mantiene el bloqueo, sin afirmar explotabilidad remota demostrada.
