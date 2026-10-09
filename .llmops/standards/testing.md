# Estándar de Testing — .NET

**Documento**: Contenido normativo de Capa 1 (Gobernanza). Reside en el repositorio central y se sincroniza a cada proyecto en `.llmops/standards/testing.md`, conforme al mecanismo pendiente de definición en `modelo-capas-llmops.md`, Sección 9.1.
**Versión**: 0.1 (borrador)
**Estado**: En revisión — Secciones 5 a 8 validadas en el template `Plantilla`; Secciones 9 y 10 son propuesta pendiente de confirmación
**Aplica a**: Todo proyecto .NET generado a partir del template `Plantilla`

---

## 1. Objeto

Este documento establece las reglas de testing obligatorias para proyectos .NET: framework, ubicación, cobertura mínima por caso de uso y nomenclatura de archivos de test.

## 2. Alcance

Aplica a todo proyecto .NET sujeto al Estándar. No regula estructura de capas (ver `standards/architecture.md`) ni convenciones de nomenclatura de producción (ver `standards/clean-code.md`).

## 3. Definiciones

| Término | Definición |
|---|---|
| Test unitario | Prueba que ejercita una unidad de código en aislamiento, con dependencias externas sustituidas |
| Test de persistencia | Prueba que ejercita el mapeo entidad-tabla contra una base de datos real o en memoria |

## 4. Responsabilidades

| Rol | Responsabilidad |
|---|---|
| Responsable del Estándar | Aprobar modificaciones a este documento |
| Referente técnico .NET | Confirmar o ajustar las Secciones 9 y 10 (propuesta pendiente) |
| Agente de control de Pull Request | Verificar cumplimiento de la Sección 7 (cobertura mínima por caso de uso) en cada Pull Request |

## 5. Framework y herramientas — obligatorio (validado en template)

Todo proyecto DEBE utilizar: `xunit.v3`, `FluentAssertions`, `Moq` y `coverlet` para instrumentación de cobertura. Está prohibida la introducción de un framework de testing alternativo sin justificación documentada.

## 6. Ubicación de tests — obligatorio (validado en template)

Los tests DEBEN residir en `Test/` a nivel raíz del repositorio, con una estructura que refleja la de `Application/UseCases/`:

```text
Test/<PluralFeature>/
  <UseCaseName>QueryTests.cs | <UseCaseName>CommandTests.cs
  <UseCaseName>ValidatorTests.cs
  <Feature>ControllerTests.cs
  <Feature>MappingTests.cs
  <Feature>PersistenceTests.cs

Test/Application/
  ApplicationServiceRegistrationTests.cs
  ValidationBehaviorTests.cs
```

## 7. Cobertura mínima por caso de uso — obligatorio (validado en template)

Todo caso de uso nuevo DEBE incluir, como mínimo, las siguientes pruebas (demostradas en el ejemplo `DemoArchitecture` del template):

| Tipo de prueba | Qué verifica |
|---|---|
| Test del `Command`/`Query` | El handler produce el `Response` esperado dado un `Request` válido |
| Test del `Validator` | Las reglas de validación aceptan y rechazan los casos esperados |
| Test del controlador | El endpoint invoca el `mediator.Send` correcto y traduce la respuesta vía `ToResult` |
| Test de mapeo | La conversión `Entity -> Dto` produce el resultado esperado |
| Test de persistencia | La configuración EF Core (`IEntityTypeConfiguration<T>`) persiste y recupera correctamente |

Un Pull Request que agregue un caso de uso sin alguno de estos cinco tipos de prueba DEBE ser observado por el agente de control de Pull Request en la dimensión de testing.

## 8. Nomenclatura de archivos de test — obligatorio (validado en template)

```text
<UseCaseName>QueryTests.cs
<UseCaseName>CommandTests.cs
<UseCaseName>ValidatorTests.cs
<Feature>ControllerTests.cs
<Feature>MappingTests.cs
<Feature>PersistenceTests.cs
```

Sufijo obligatorio `Tests`. El nombre DEBE corresponder exactamente a la clase o componente bajo prueba.

## 9. Umbral de cobertura de código — propuesta, pendiente de confirmación

No existe un umbral explícito definido en el template. Se propone, sujeto a confirmación del Referente técnico .NET:

- Cobertura mínima de línea del 70% en `Application` (casos de uso), medida con `coverlet`.
- `Domain` e `Infraestructure` quedan fuera de este umbral obligatorio en esta primera versión, dado su menor contenido de lógica de negocio.
- El incumplimiento del umbral NO DEBERÍA bloquear automáticamente el Pull Request en la Fase 1 del piloto; se sugiere registrarlo como observación no bloqueante hasta validar el umbral con datos reales.

## 10. Política de mocking — propuesta, pendiente de confirmación

No existe una política explícita en el template más allá del uso de `Moq` en general. Se propone, sujeto a confirmación:

- Se DEBERÍA mockear únicamente en las fronteras de puerto definidas en `Domain` (repositorios, adaptadores), nunca tipos concretos de infraestructura (ej. no mockear `DbContext` directamente).
- Los tests de persistencia (Sección 7) DEBERÍAN usar una base de datos en memoria o real de prueba, no mocks, dado que su objetivo es verificar el mapeo real.

## 11. Gate de Pull Request

La verificación de testing de un Pull Request incluye, como mínimo, `dotnet test` sin fallos y el cumplimiento de la Sección 7 (cobertura mínima por caso de uso). Este control se referencia desde `standards/architecture.md`, Sección 14.

## 12. Control de versiones de este documento

| Versión | Fecha | Cambio |
|---|---|---|
| 0.1 | 2026-08-18 | Borrador inicial. Secciones 5 a 8 extraídas del ejemplo `DemoArchitecture` del template `Plantilla`. Secciones 9 y 10 son propuesta pendiente de confirmación. |

## 13. Referencias

- `standards/architecture.md` — patrón de casos de uso y Gate de Pull Request
- `standards/clean-code.md` — convenciones de nomenclatura de producción
- Template `Plantilla` (Azure DevOps: `ServiciosAndesTool/_git/Template`) — fuente de las secciones validadas, ejemplo `DemoArchitecture`
