# Estándar de Arquitectura — .NET

**Documento**: Contenido normativo de Capa 1 (Gobernanza). Reside en el repositorio central y se sincroniza a cada proyecto en `.llmops/standards/architecture.md`, conforme al mecanismo pendiente de definición en `modelo-capas-llmops.md`, Sección 9.1.
**Versión**: 0.1
**Estado**: Vigente — extraído y renumerado desde `estandar-operacional-por-stack.md` v0.3
**Aplica a**: Todo proyecto .NET generado a partir del template `Plantilla`

---

## 1. Objeto

Este documento establece las reglas de arquitectura obligatorias para proyectos .NET, con independencia del mecanismo por el cual el proyecto fue creado o adoptado.

## 2. Alcance

Aplica a la estructura de capas, reglas de dependencia, patrones de casos de uso y convenciones de diseño arquitectónico. No regula el mecanismo de creación del proyecto (ver `estandar-operacional-por-stack.md`), ni las convenciones de nomenclatura y estilo (ver `standards/clean-code.md`), ni las reglas de seguridad (ver `standards/security.md`), ni las reglas de testing (ver `standards/testing.md`).

## 3. Definiciones

| Término | Definición |
|---|---|
| Puerto | Interfaz definida en `Domain` que representa una frontera arquitectónica real |
| Adaptador | Implementación concreta de un puerto, ubicada fuera de `Domain` |
| Caso de uso | Unidad de lógica de aplicación compuesta por `Request`, `Validator`, `Command` o `Query`, y `Response` |

## 4. Responsabilidades

| Rol | Responsabilidad |
|---|---|
| Responsable del Estándar | Aprobar modificaciones a este documento |
| Referente técnico .NET | Mantener el template `Plantilla` conforme a estas disposiciones |
| Agente de control de Pull Request | Verificar cumplimiento de la Sección 6 en cada Pull Request y bloquear la aprobación ante infracción |

## 5. Estructura de capas — obligatoria

El proyecto DEBE mantener la siguiente estructura de carpetas:

```text
Core/Domain/
Core/Application/
Infraestructure/Adapters/
Infraestructure/Persistence/
Infraestructure/Infraestructure/
Api/Api/
Test/
```

## 6. Regla de dependencia — obligatoria

| Capa | Puede depender de |
|---|---|
| `Domain` | Ninguna capa del proyecto |
| `Application` | Únicamente `Domain` |
| `Adapters` | `Application`, `Domain`, `Infraestructure` |
| `Persistence` | `Domain`, `Infraestructure` |
| `Infraestructure` (helper de DI) | `Domain` |
| `Api` | `Application`, `Domain`, `Adapters`, `Persistence` |

Está prohibido que `Application` importe símbolos de `Persistence`, `Adapters`, `Infraestructure` o `Api`. Toda infracción detectada por el agente de control de Pull Request DEBE bloquear su aprobación.

## 7. Patrón de casos de uso — obligatorio

Toda operación de aplicación DEBE implementarse como caso de uso bajo `Application/UseCases/<PluralFeature>/<UseCaseName>/`, compuesto por: `Request`, `Validator` (FluentValidation), `Command` o `Query` (con `IRequestHandler` propio, sin clase `Handler` separada), y `Response`.

Está prohibida la creación de carpetas genéricas de tipo `Services` para lógica de aplicación.

## 8. Repositorios — obligatorio

Todo repositorio concreto DEBE heredar de `RepositoryGeneric<T>`. Está prohibida la creación de un repositorio específico cuando las operaciones genéricas (`FindAsync`, `ExistsAsync`, `Add`, `Edit`, `Delete`, `CommitAsync`) resuelven la necesidad.

## 9. Mapeo — obligatorio

El mapeo entre entidades y DTOs DEBE resolverse mediante `IMapperAdapter` y convención de nombres (`<BaseName>Entity -> <BaseName>*Dto`). Está prohibida la creación de un perfil de AutoMapper por entidad cuando la convención ya resuelve el mapeo.

## 10. Inyección de dependencias — obligatorio

El registro de dependencias DEBE realizarse mediante `DependencyInjectionHelper` cuando exista una interfaz en `Domain` y una implementación concreta en infraestructura. Está prohibido el registro manual de pares que el helper ya resuelve por convención. Está prohibida la creación de un puerto (interfaz) que no cruce una frontera arquitectónica real.

## 11. Respuesta de casos de uso — obligatorio

Todo caso de uso DEBE retornar `ResponseBase<T>`. Los controladores DEBEN utilizar `ToResult(...)` para la conversión a `IActionResult`. Los controladores NO DEBEN contener lógica de negocio.

## 12. Documentación de arquitectura

La documentación técnica de arquitectura DEBE generarse mediante el pipeline de Graphify ya integrado en el template (`docs/architecture/`). La carpeta `.llmops/graph/` DEBE referenciar este mismo pipeline. Está prohibida la creación de una integración de Graphify paralela para el mismo proyecto.

## 13. Disposición transitoria — convención preexistente (AuthPortal)

Existe una segunda convención Clean Architecture vigente en el repositorio AndesTool AuthPortal (namespace `AT.*`, sin CQRS/MediatR explícito), anterior a este documento. Hasta que el Responsable del Estándar resuelva si dicha convención converge hacia la aquí descripta o si ambas coexisten como variantes válidas, AuthPortal se mantiene bajo régimen de adopción retroactiva sin obligación de migrar su convención interna.

## 14. Gate de Pull Request — disposición provisional

La validación de arquitectura en un Pull Request incluye, como mínimo, la compilación exitosa (`dotnet build`) y la ausencia de infracciones a la Sección 6. Los siguientes controles adicionales, mencionados aquí de forma provisional, se redistribuyen a su documento correspondiente cuando este exista:

| Control | Documento destino |
|---|---|
| `dotnet test` sin fallos | `standards/testing.md`, Sección 11 |
| `dotnet format --verify-no-changes` | `standards/clean-code.md` |
| `dotnet list package --vulnerable --include-transitive` | `standards/security.md`, Sección 9 |

## 15. Control de versiones de este documento

| Versión | Fecha | Cambio |
|---|---|---|
| 0.1 | 2026-08-18 | Extraído desde `estandar-operacional-por-stack.md` v0.3, Secciones 5.4 a 5.13 |

## 16. Referencias

- `estandar-operacional-por-stack.md` — mecanismo de creación del proyecto
- `standards/clean-code.md` — convenciones de nomenclatura y estilo
- `standards/security.md` — reglas de seguridad
- `standards/testing.md` — reglas de testing
- `modelo-capas-llmops.md` — modelo general de capas
