# Verificación de Node LTS

Verificación local realizada el 9 de octubre de 2026 con **Node 24.21.0 LTS**, **npm 11.13.0**, Angular 22.2.2 y TypeScript 6.0.2.

Se utilizó el archivo portable oficial `node-v24.21.0-win-x64.zip`, descargado desde [nodejs.org](https://nodejs.org/dist/v24.21.0/node-v24.21.0-win-x64.zip). Su SHA-256 se contrastó con el [manifiesto oficial de la versión](https://nodejs.org/dist/v24.21.0/SHASUMS256.txt) antes de extraerlo y ejecutarlo:

```text
158f7685b44de51f6c0df1d153526cbcd3e1bc739a8dfc607721cef75de9e541
```

El runtime quedó en `work/tools/node-24.21.0/node-v24.21.0-win-x64` del workspace de trabajo. Se invocaron por rutas explícitas ese `node.exe` y el `npm-cli.js` 11.13.0 existente; el PATH se antepuso únicamente en el proceso de prueba para que los procesos hijos también usaran Node 24. La instalación de Node del usuario no se modificó y el runtime portable no forma parte del código ni del ZIP del evaluador.

| Verificación                                       | Resultado                                      |
| -------------------------------------------------- | ---------------------------------------------- |
| Engines de todas las dependencias en los dos locks | Compatibles con 24.21.0; ninguna exige Node 26 |
| `npm run build`                                    | Correcto; bundle inicial 344,37 kB             |
| `npm test`                                         | 16 pruebas correctas, 4 archivos               |
| `npm run test:proxy`                               | 4 pruebas HTTP correctas                       |
| Prettier de los archivos cambiados                 | Correcto                                       |

El lock de la aplicación y el del generador sólo cambian su requisito de Node; no se actualizaron versiones resueltas de Angular ni de dependencias. El esquema real y la comprobación de drift de OpenAPI continúan pendientes de CI. Esta verificación local del frontend no sustituye las pruebas Linux, API y SQL del pipeline.

Fuentes de compatibilidad: [Node LTS](https://nodejs.org/en/about/previous-releases) y [Angular](https://angular.dev/reference/versions).
