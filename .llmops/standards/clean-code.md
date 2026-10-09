# Estándar de Clean Code — .NET

**Documento**: Contenido normativo de Capa 1 (Gobernanza). Reside en el repositorio central y se sincroniza a cada proyecto en `.llmops/standards/clean-code.md`, conforme al mecanismo pendiente de definición en `modelo-capas-llmops.md`, Sección 9.1.
**Versión**: 0.1
**Estado**: Parcialmente vigente — Secciones 5 a 7 validadas en el template `Plantilla`; Sección 8 es propuesta general pendiente de confirmación por el Referente técnico .NET
**Aplica a**: Todo proyecto .NET generado a partir del template `Plantilla`

---

## 1. Objeto

Este documento establece las convenciones de nomenclatura, formato y estilo de código obligatorias para proyectos .NET. No regula estructura de capas ni reglas de dependencia (ver `standards/architecture.md`).

## 2. Alcance

Aplica a la nomenclatura de entidades, DTOs, casos de uso y configuraciones, y al formato de código verificado automáticamente en cada Pull Request.

## 3. Definiciones

Ver `standards/architecture.md`, Sección 3.

## 4. Responsabilidades

| Rol | Responsabilidad |
|---|---|
| Responsable del Estándar | Aprobar modificaciones a este documento |
| Referente técnico .NET | Confirmar o ajustar la Sección 8 (propuesta general) |
| Agente de control de Pull Request | Verificar `dotnet format --verify-no-changes` en cada Pull Request |

## 5. Nomenclatura de entidades — obligatorio (validado en template)

```text
Domain/Entities/<SingularName>Entity.cs
```

- Nombre en singular, sufijo obligatorio `Entity`.
- DEBE heredar de `EntityBase`.
- Las propiedades de auditoría comunes NO DEBEN repetirse; provienen de `EntityBase`.
- Las invariantes puras de dominio PUEDEN vivir en métodos de la entidad. La validación de entrada HTTP/aplicación NO DEBE ubicarse en la entidad; corresponde al `Validator`.

## 6. Nomenclatura de configuraciones EF Core — obligatorio (validado en template)

```text
Persistence/Configuration/<SingularName>Configuration.cs
```

- Una configuración por entidad. NO DEBE configurarse una entidad directamente en `AppDbContext`, salvo casos transversales.
- Las tablas se nombran en plural; las entidades, en singular.

## 7. Nomenclatura de DTOs y casos de uso — obligatorio (validado en template)

```text
Application/Dtos/<SingularName>DetailDto.cs
Application/Dtos/<SingularName>ListDto.cs

Application/UseCases/<PluralFeature>/<UseCaseName>/
  <UseCaseName>Request.cs
  <UseCaseName>Validator.cs
  <UseCaseName>Command.cs | <UseCaseName>Query.cs
  <UseCaseName>Response.cs
```

- Sufijo obligatorio `Dto`. Los DTOs pertenecen a `Application`, no a `Domain`.
- El nombre del DTO DEBE comenzar con el nombre base de la entidad, para que el mapeo por convención funcione (ver `standards/architecture.md`, Sección 9).
- NO DEBE exponerse una entidad directamente desde un caso de uso.

## 8. Convenciones generales — propuesta, pendiente de confirmación

Los siguientes puntos no tienen precedente explícito en el template y se dejan como propuesta inicial, sujeta a validación del Referente técnico .NET:

- Las clases que no están diseñadas para herencia DEBERÍAN declararse `sealed`, conforme al patrón ya usado en el template para comandos y queries.
- Las respuestas inmutables (`Response`) DEBERÍAN declararse como `record class`, conforme al patrón ya usado en el template.
- Los métodos DEBERÍAN evitar superar un umbral de complejidad ciclomática a definir (propuesta: 10).
- Los nombres de variables y métodos DEBEN ser autoexplicativos; se DESALIENTA el uso de abreviaturas no estándar.
- Los comentarios DEBEN explicar el "por qué", no el "qué"; el código autoexplicativo se prefiere sobre el comentario descriptivo.

## 9. Formato automático — obligatorio

Todo Pull Request DEBE pasar `dotnet format --verify-no-changes` como condición de aprobación. Este control corresponde a la Sección 14 (Gate de Pull Request) de `standards/architecture.md`.

## 10. Control de versiones de este documento

| Versión | Fecha | Cambio |
|---|---|---|
| 0.1 | 2026-08-18 | Versión inicial. Secciones 5-7 extraídas del template `Plantilla`. Sección 8 es propuesta pendiente de confirmación. |

## 11. Referencias

- `standards/architecture.md` — estructura de capas y reglas de dependencia
- Template `Plantilla` (Azure DevOps: `ServiciosAndesTool/_git/Template`) — fuente de las convenciones validadas
