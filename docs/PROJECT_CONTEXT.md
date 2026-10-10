# Contexto vigente

Nexo resuelve facturación HTML5/C#/SQL y Accounts sobre Template (base 3393c2e7b02f7d0ea439b461b8fba5d561107681).

El propietario pidió corregir el checklist y después indicó **sólo local por el momento**. No subir archivos ni modificar GitHub/SharePoint o publicar Azure/Cloudflare. Mantener repo y paquete locales con evidencia separada. La CI15 del código anterior se conserva como historial, no como validación de nuevos cambios.

La facturación usa VARCHAR UTF-8 para los cuatro textos del cliente, mediante una segunda migración; no editar la migración inicial aplicada. Mantener límites de UI y validar conservación de datos. Accounts simulado es el modo predeterminado; el modo real se habilita exclusivamente con configuración privada del servidor. Nunca incluir credenciales del PDF, tokens, novasoft.local.json o local.env en entrega/evidencias.

Referencias actuales: [README](../README.md), [requisitos](requirements.md), [arquitectura](ARCHITECTURE.md), [decisiones](decisions.md), [Novasoft](novasoft-real.md), [pruebas](TESTING.md), [seguridad](../SECURITY.md).

Docker/WSL ya funcionan en este PC. No pedir nuevamente habilitar SVM. WDAC se conserva; las pruebas backend completas se ejecutan con Linux en Docker. Riesgo SQL aceptado según SQL-LOCAL-2026-10-09, con inventario y vigencia exactos. Los diagramas DemoArchitecture heredados no son un grafo actual de facturas/Accounts.
