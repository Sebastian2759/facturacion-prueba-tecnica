# Capturas históricas de interfaz con respuestas controladas

Este registro corresponde a la versión anterior a la retirada de la portada de demostración. No acredita la interfaz de la nueva entrega.

Se revisaron las capturas `invoice-desktop.png` e `invoice-mobile.png` producidas por `e2e/invoices.spec.ts`, prueba `invoice and demo screens pass WCAG AA automated checks`. El escenario intercepta sesión y API con Playwright y usa exclusivamente datos ficticios. **Estas imágenes acreditan composición visual; no acreditan API real, persistencia SQL ni conectividad Novasoft.**

En la entrega de trabajo se copiaron sin alteraciones a `outputs/ui-evidence/` con los nombres:

| Captura                                   | Dimensiones | SHA-256                                                            |
| ----------------------------------------- | ----------- | ------------------------------------------------------------------ |
| `nexo-factura-escritorio-ui-simulada.png` | 1280 × 975  | `af9304a01fb32f3e738430759eb8fba88a45f77386df333ba96ca9fc9ff789cb` |
| `nexo-factura-movil-ui-simulada.png`      | 390 × 1785  | `9062f35a3aa1942a5edf7ea8eb6fd8c76ae5939c251f74d3a6044910f4588ce4` |

Son capturas de página completa; el viewport móvil de prueba fue 390 × 844. Se verificaron disposición de campos y resumen, legibilidad y ausencia de desbordamiento horizontal. El mismo escenario superó axe con etiquetas WCAG A/AA; eso no constituye una certificación ni sustituye la revisión con tecnologías de asistencia.

Reproducción desde `Frontend`, con Edge instalado:

```powershell
$env:PLAYWRIGHT_CHANNEL='msedge'
npm run test:e2e -- e2e/invoices.spec.ts --grep 'invoice and demo screens'
```

Los originales se generan en `test-results/invoices-UI-with-controlle-800b6-ss-WCAG-AA-automated-checks-chromium/`. El directorio `test-results` está excluido de Git y puede borrarse al volver a ejecutar Playwright; las copias entregadas permanecen separadas de esos archivos temporales.
