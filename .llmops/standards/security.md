# Estándar de Seguridad — .NET

**Documento**: Contenido normativo de Capa 1 (Gobernanza). Reside en el repositorio central y se sincroniza a cada proyecto en `.llmops/standards/security.md`, conforme al mecanismo pendiente de definición en `modelo-capas-llmops.md`, Sección 9.1.
**Versión**: 0.1 (borrador)
**Estado**: En revisión — Secciones 5, 6 y 9 validadas en el template `Plantilla`; Secciones 7, 10 y 11 son propuesta pendiente de confirmación
**Aplica a**: Todo proyecto .NET generado a partir del template `Plantilla`

---

## 1. Objeto

Este documento establece las reglas de seguridad obligatorias para proyectos .NET: autenticación, gestión de secretos, autorización, auditoría de dependencias y manejo de información sensible.

## 2. Alcance

Aplica a todo proyecto .NET sujeto al Estándar. No regula estructura de capas (ver `standards/architecture.md`) ni convenciones de nomenclatura (ver `standards/clean-code.md`).

## 3. Definiciones

| Término | Definición |
|---|---|
| Secreto | Valor sensible que otorga acceso o identidad (clave JWT, API Key, cadena de conexión, credencial) |
| Proveedor seguro de secretos | Mecanismo que evita que un secreto quede en texto plano en el repositorio (User Secrets, Azure Key Vault) |

## 4. Responsabilidades

| Rol | Responsabilidad |
|---|---|
| Responsable del Estándar | Aprobar modificaciones a este documento |
| Referente técnico .NET | Confirmar o ajustar las Secciones 7, 10 y 11 (propuesta pendiente) |
| Agente de control de Pull Request | Verificar cumplimiento de este documento en la dimensión "seguridad" de cada Pull Request |

## 5. Autenticación — obligatorio (validado en template)

El proyecto DEBE soportar, como mínimo, los esquemas ya provistos por el template: JWT Bearer y API Key, mediante las políticas `BearerOrApiKey`, `JwtOnly` y `ApiKeyOnly`.

El template NO incluye login, usuarios de demostración ni un endpoint que emita tokens. El JWT DEBE provenir del proveedor de identidad que defina el proyecto consumidor. Está prohibido implementar un emisor de tokens propio sin justificación documentada (ver Sección 8, Excepciones, de `estandar-operacional-por-stack.md`).

## 6. Gestión de secretos — obligatorio (validado en template)

Los valores `Jwt:Secret` y `ApiKey:Key` DEBEN permanecer vacíos en los archivos de configuración versionados. La API DEBE fallar al iniciar si no se proporcionan valores seguros.

En desarrollo local, estos valores PUEDEN establecerse mediante variables de entorno. En todo ambiente distinto de desarrollo local, DEBEN provenir de un proveedor seguro (User Secrets, Azure Key Vault, u otro equivalente aprobado).

Está prohibido comitear un secreto real, aun de forma temporal, en cualquier rama del repositorio.

## 7. Autorización por caso de uso — propuesta, pendiente de confirmación

No existe precedente explícito en el template más allá de la política de autenticación a nivel de controlador. Se propone, sujeto a confirmación del Referente técnico .NET:

- Todo caso de uso que module datos DEBERÍA declarar explícitamente qué política de autorización aplica, no heredarla implícitamente del controlador que lo invoca.
- Las reglas de autorización que dependan del estado del dominio (ej. "un usuario solo puede editar sus propios recursos") DEBERÍAN verificarse dentro del `Command` o `Query`, no únicamente en el atributo `[Authorize]` del controlador.

## 8. Validación de entrada

La validación de entrada se rige por `standards/architecture.md`, Sección 7 (patrón de casos de uso, `Validator` con FluentValidation). Este documento no duplica esa regla; remite a ella. Se agrega la siguiente disposición de seguridad específica:

Todo campo de entrada que se use en una consulta, ruta de archivo, o comando del sistema operativo DEBE validarse contra un conjunto explícito de valores o patrones permitidos, no solo contra su tipo de dato.

## 9. Auditoría de dependencias — obligatorio (validado en template)

Antes de entregar cambios de dependencias, DEBE ejecutarse:

```powershell
dotnet list package --vulnerable --include-transitive
dotnet list package --deprecated
dotnet list package --outdated
```

Está prohibido silenciar una vulnerabilidad mediante `NoWarn` sin una decisión formal y documentada. `Directory.Build.props` DEBE mantener `NU1901`-`NU1904` como errores durante `restore`.

## 10. Manejo de excepciones y fuga de información — obligatorio (validado en template)

El manejo global de excepciones DEBE traducir: `ValidationException` a `400`, `DomainException` a `422`, y cualquier excepción no controlada a `500` genérico. Está prohibido exponer detalles de una excepción no controlada (stack trace, mensaje interno, cadena de conexión) en la respuesta HTTP.

## 11. Registro de datos sensibles — propuesta, pendiente de confirmación

No existe precedente explícito en el template. Se propone, sujeto a confirmación:

- Los registros de log NO DEBEN incluir el valor de un secreto, token, contraseña, ni datos personales identificables completos (se sugiere enmascarar, ej. mostrar solo los últimos 4 caracteres de un identificador sensible).
- Todo agente de IA que procese logs como parte de su contexto DEBE tratar su contenido como potencialmente sensible, conforme a los principios generales de manejo de datos de la organización.

## 12. Gate de Pull Request

La verificación de seguridad de un Pull Request incluye, como mínimo, la ejecución de la Sección 9 (auditoría de dependencias) y la ausencia de secretos en el diff. Este control se referencia desde `standards/architecture.md`, Sección 14.

## 13. Control de versiones de este documento

| Versión | Fecha | Cambio |
|---|---|---|
| 0.1 | 2026-08-18 | Borrador inicial. Secciones 5, 6, 9 y 10 extraídas del template `Plantilla`. Secciones 7 y 11 son propuesta pendiente de confirmación. |

## 14. Referencias

- `standards/architecture.md` — patrón de casos de uso y validación de entrada
- `standards/clean-code.md` — convenciones de nomenclatura
- Template `Plantilla` (Azure DevOps: `ServiciosAndesTool/_git/Template`) — fuente de las secciones validadas
