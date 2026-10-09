# Seguridad del proyecto

La política vigente está en [SECURITY.md de la raíz](../SECURITY.md).

El alcance es evaluación local Windows x64, repositorio GitHub privado y distribución controlada. El host web opcional queda fuera de la entrega; no hay publicación Azure/Cloudflare ni IdP requerido. La [CI 37991580284](https://github.com/Sebastian2759/facturacion-prueba-tecnica/actions/runs/37991580284), commit `8d4469a0e47c3cd4b61ca8ebb397d8b9327708c2`, aprobó 159 pruebas .NET, backup/restore, CHECKDB, migradores, smoke API/no-store, OpenAPI del contenedor y 1 navegador live SQL con CSP/estilos corregidos. El gate ZAP pasó con informativos risk 0 revisados. Trivy SQL sigue bloqueando y Semgrep no se ejecutó por HTTP 429 del registro de imágenes. Esas verificaciones no se presentan como pentest o auditoría exhaustiva.

Véanse también [arquitectura](ARCHITECTURE.md), [decisiones](decisions.md) y [estado de pruebas](TESTING.md).

La [evaluación del runtime SQL](sql-runtime-risk-assessment.md) conserva los digests y los hallazgos investigados, con fuentes y límites del análisis. No hay exclusiones aplicadas; el candidato SQL Server 2025 también falla el diagnóstico del run6. El análisis de símbolos del run7 confirma funciones asociadas a CVE en dos binarios SQL 2022 y mantiene el bloqueo, sin afirmar explotabilidad remota demostrada.
