# Revisión previa de nulabilidad y campos requeridos

Estado: lectura del código C# y TypeScript al 9 de octubre de 2026; **pendiente contrastar con el OpenAPI real**. Las nulabilidades confirmadas se corrigieron en los modelos manuales y se añadió el rechazo de éxito sin datos. No se creó un esquema ni se modificó el backend. La opcionalidad `required` y los modos esperan la comprobación del export real.

## Diferencias detectadas antes de la corrección

| Modelo manual                                          | Declaración del backend                              | Consecuencia para la integración                                                                                                                               |
| ------------------------------------------------------ | ---------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `Envelope<T>.data: T`                                  | `ResponseBase<T>.Data` es `T?`                       | El transporte debe admitir `null`. El resultado de éxito debe refinarse después de comprobar estado y contenido.                                               |
| `errors?`, `fieldErrors?`, `traceId?` sin `null`       | Las tres propiedades C# son anulables                | Ausencia y `null` son casos diferentes. El tipo bruto deberá reflejar ambos según el esquema real.                                                             |
| `Envelope.code?`                                       | `Code` es string no anulable, con valor inicial      | El tipo manual es menos estricto; no deducir `required` JSON sólo de esa diferencia.                                                                           |
| `Session.csrfToken?`, `oidc?` sin `null`               | `SessionDto` declara `string?` y `OidcSettings?`     | El perfil local puede devolver `oidc: null`; el modelo de transporte debe admitirlo.                                                                           |
| Opcionales de `Account` sin `null`                     | `AccountDto` y `CreateAccountRequest` usan `string?` | Revisar `firstName`, `lastName`, `address`, `additionalAddress`, `phone`, `website`, `externalCode`.                                                           |
| `Session.mode` y `AccountsMode` como uniones literales | `Mode` es string en los DTO C#                       | Es una restricción de la UI/validación del servidor, no necesariamente un enum descrito por OpenAPI. No ampliar artificialmente todos los strings al comparar. |

Fuentes: [ResponseBase](../../Core/Application/Base/ResponseBase.cs), [SessionDto](../../Api/Api/Session/SessionDto.cs), [AccountDto](../../Core/Application/Dtos/AccountDto.cs), [CreateAccountRequest](../../Core/Application/UseCases/Accounts/CreateAccount/CreateAccountRequest.cs), [contrato manual](../src/app/core/api-contract.ts).

Los campos de factura son no anulables en C# y los campos de cliente son obligatorios por FluentValidation. Esa validación de negocio no permite afirmar por adelantado qué propiedades aparecerán en el array `required` del OpenAPI. En particular, las clases con valores iniciales, los records posicionales y las reglas de FluentValidation pueden producir metadatos diferentes. Se inspeccionará el export sin aplicar `Required<T>` ni `NonNullable<T>` globalmente.

## Comparación que evita falsas alarmas sin ocultar errores

1. Preferir aliases desde los tipos generados para el **transporte**. Mantener por separado modelos de formulario o estados de UI cuando necesiten restricciones adicionales.
2. Para peticiones, comprobar que lo construido por la UI es asignable a la petición aceptada por el servidor. Una UI que siempre envía un campo opcional o limita `mode` a valores conocidos sigue siendo compatible. La comparación inversa no es una condición general de compatibilidad.
3. Para respuestas, comprobar que toda respuesta descrita por el servidor cabe en el tipo bruto recibido por el frontend. No declarar un `string` del servidor como unión literal sin una comprobación en ejecución.
4. Si OpenAPI mantiene `mode: string`, conservar ese tipo en el transporte y refinar exclusivamente los modos conocidos con un guard explícito. Un modo desconocido debe producir un estado de error, sin presentarse como simulación o conexión real. No sustituir todos los strings del esquema ni omitir arbitrariamente la propiedad `mode` de la comparación.
5. Reflejar fielmente `null` y opcionalidad. Una propiedad presente con valor `null` no equivale a una propiedad ausente. Resolver errores de compilación con manejo real del caso, no con `as`, operadores `!` o transformaciones globales que borren nulabilidad.
6. Verificar explícitamente las envolturas de preview, creación, detalle, listado, sesión y Accounts. La igualdad bidireccional sí es adecuada entre aliases cuyo propósito es representar exactamente el mismo DTO; no debe imponerse entre un formulario más restrictivo y un contrato de transporte más amplio.

`statusCode` puede aparecer como enum numérico del backend mientras el normalizador trabaja con `number`; los valores declarados por el servidor son consumibles como números. `date`, `registeredAt` e `id` deben seguir como strings de transporte si el esquema declara formatos de fecha/UUID: el formato no los convierte automáticamente en objetos `Date`.

## Comprobaciones en ejecución a cerrar con la integración

`unwrap` comprueba `isSuccess` y rechaza respuestas de éxito con `data: null` o sin `data`, conservando su referencia de seguimiento y el código `response.invalid`. Las pruebas dirigidas cubren ambas respuestas malformadas; el manejo de errores de negocio con `data: null` se conserva. La prueba de éxito también incluye los metadatos anulables que declara C#. Los normalizadores de errores tratan las propiedades recibidas como datos desconocidos antes de leer arrays y mapas.

La referencia íntegra de esquema detectará cambios de metadatos aunque no rompan TypeScript; la comprobación de modelos comprobará el consumo real. Ambas se requieren. Su ejecución satisfactoria sigue pendiente del primer export real y de escribir `api-contract.compatibility.ts` contra ese documento.
