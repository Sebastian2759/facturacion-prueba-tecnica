# Seguridad y hallazgos de terceros

La política vigente está en [SECURITY de la raíz](../SECURITY.md). Véase [aceptación SQL](sql-risk-acceptance.md), [análisis de runtime](sql-runtime-risk-assessment.md) y [pruebas](TESTING.md).

Detectar, analizar y corregir vulnerabilidades forma parte de DevSecOps. Encontrar hallazgos en una imagen oficial no significa que la aplicación no tenga protección. Aceptar temporalmente un inventario concreto tampoco lo corrige ni demuestra que sea inexplotable. El propietario autorizó SQL-LOCAL-2026-10-09 para evaluación local con datos ficticios; los demás controles siguen vigentes.

La evaluación actual no publica el servicio ni modifica GitHub/SharePoint. La conexión saliente al servicio de pruebas Novasoft requiere configuración privada explícita y conserva Host/Origin/CSRF y autorización.
