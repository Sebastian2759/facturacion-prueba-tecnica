# Seguridad del proyecto

La política vigente está en [SECURITY.md de la raíz](../SECURITY.md).

El alcance es evaluación local Windows x64, repositorio GitHub privado y distribución controlada. El host web opcional queda fuera de la entrega; no hay publicación Azure/Cloudflare ni IdP requerido. La [CI 37986378549](https://github.com/Sebastian2759/facturacion-prueba-tecnica/actions/runs/37986378549) aprobó pruebas .NET/SQL y varios gates de seguridad, pero Trivy de la imagen SQL bloquea la release. Esas verificaciones no se presentan como pentest o auditoría exhaustiva.

Véanse también [arquitectura](ARCHITECTURE.md), [decisiones](decisions.md) y [estado de pruebas](TESTING.md).

La [evaluación del runtime SQL](sql-runtime-risk-assessment.md) conserva los digests y los hallazgos investigados, con fuentes y límites del análisis. No hay exclusiones aplicadas; el candidato SQL Server 2025 también falla el diagnóstico del run6. El análisis de símbolos sigue pendiente.
