# Seguridad del proyecto

La política vigente está en [SECURITY.md de la raíz](../SECURITY.md).

El alcance es evaluación local Windows x64, repositorio GitHub privado y distribución controlada. El host web opcional queda fuera de la entrega; no hay publicación Azure/Cloudflare ni IdP requerido. La [CI 37989162596](https://github.com/Sebastian2759/facturacion-prueba-tecnica/actions/runs/37989162596), commit `c6b44269ac0944289cb1d848a48510efcd1e5523`, aprobó 150 pruebas .NET, backup/restore, CHECKDB, migradores, smoke API y 1 navegador live con SQL. Trivy SQL bloquea la release y ZAP terminó con 4 advertencias. Las correcciones CSP/estilos y cabeceras posteriores esperan nueva CI; la captura había revelado CSS bloqueado pese al éxito funcional. Esas verificaciones no se presentan como pentest o auditoría exhaustiva.

Véanse también [arquitectura](ARCHITECTURE.md), [decisiones](decisions.md) y [estado de pruebas](TESTING.md).

La [evaluación del runtime SQL](sql-runtime-risk-assessment.md) conserva los digests y los hallazgos investigados, con fuentes y límites del análisis. No hay exclusiones aplicadas; el candidato SQL Server 2025 también falla el diagnóstico del run6. El análisis de símbolos del run7 confirma funciones asociadas a CVE en dos binarios SQL 2022 y mantiene el bloqueo, sin afirmar explotabilidad remota demostrada.
