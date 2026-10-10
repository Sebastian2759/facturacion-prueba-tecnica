# Contexto vigente

Nexo resuelve facturación HTML5/C#/SQL y Accounts sobre Template, base 3393c2e7b02f7d0ea439b461b8fba5d561107681.

El propietario autorizó publicar la última versión y documentación en su GitHub personal público, y preparar la carpeta para SharePoint con referencias cruzadas. La aplicación mantiene ejecución local Windows; esta autorización no implica desplegar Azure/Cloudflare ni publicar un servicio web.

La versión Windows 1.0.2-local.2 conserva su ZIP y checksum probado. La publicación incorpora el mismo código funcional y documentación actualizada; sus evidencias se identifican en docs/evidence/README.md. No atribuir los resultados locales a una CI nueva.

Accounts usa exclusivamente Novasoft con configuración privada del servidor. Sin simuladores en runtime. Los dobles de prueba viven en Test y Frontend/e2e. No publicar credenciales del PDF, tokens, novasoft.local.json, local.env o backups privados.

VARCHAR UTF-8 conserva textos mediante migración; no editar migraciones ya aplicadas. La tercera migración retira la tabla del ejemplo sólo si está vacía. Docker/WSL funcionan en el equipo de preparación. Se conserva WDAC y las pruebas backend completas se ejecutaron en Linux Docker. SQL-LOCAL-2026-10-09 mantiene el inventario y plazo aprobados.

Referencias: [README](../README.md), [requisitos](requirements.md), [arquitectura](ARCHITECTURE.md), [decisiones](decisions.md), [evidencias](evidence/README.md) y [seguridad](../SECURITY.md).
