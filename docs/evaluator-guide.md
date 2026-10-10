# Guía del evaluador

[Volver al proyecto](../README.md) · [Resultados](evidence/README.md) · [Operación detallada](delivery.md)

## Ejecutar el paquete

En la carpeta de entrega abra `COMENZAR-AQUI.html`. El archivo `Aplicacion-1.0.2-local.2.zip` es el ejecutable preparado; compruebe su SHA-256, extráigalo por completo y ejecute `iniciar.cmd`. Requiere Windows x64, Docker Desktop en modo Linux con Compose e internet para descargar imágenes.

La aplicación abre en http://127.0.0.1:5080. El paquete lleva frontend/backend compilados y el esquema SQL vacío, además de los fuentes. No requiere SDK .NET ni Node para ejecutarse.

## Recorrido funcional

Use datos ficticios: documento 990000001, nombres Evaluación, apellidos Local, dirección Calle de prueba 123 y teléfono 0000000000.

| Acción | Resultado esperado |
|---|---|
| Intentar guardar vacíos o cantidad 0 | Errores de validación sin persistir una factura inválida. |
| Artículo 101, cantidad 1, precio 100.000 | Bruto 100.000; descuento 0; IVA 19.000; total 119.000. |
| Cambiar cantidad a 5 | Bruto 500.000; descuento 25.000; IVA 90.250; total 565.250. |
| Añadir artículo 102, cantidad 1, precio 50.000 | Bruto 550.000; descuento 27.500; IVA 99.275; total 621.775. |
| Guardar y consultar | Número/fecha del servidor, cliente, artículos e importes consistentes. |
| Detener e iniciar desde la misma carpeta | Misma factura disponible después del reinicio. |

## Novasoft

Ejecute `configurar-novasoft.cmd` después del primer inicio. Introduzca en privado usuario, conexión y contraseña de prueba; ejecute `detener.cmd` e `iniciar.cmd`. Cuentas consume el servicio real desde C#.

Puede consultar el código ficticio previamente creado `NEXOUI261010B`; el resultado registrado fue **Prueba Formulario**. Las altas escriben en el servicio externo de pruebas: use un código nuevo y datos ficticios con permiso del responsable. Ante timeout, consulte antes de repetir el alta. [Configuración y contrato](novasoft-real.md).

## Operación

| Comando | Función |
|---|---|
| iniciar.cmd | Comprobar integridad e iniciar aplicación y SQL. |
| detener.cmd | Detener conservando los datos. |
| diagnosticar.cmd | Verificar estado e integridad. |
| backup.cmd | Crear y verificar un respaldo privado. |
| restaurar.cmd -BackupPath "C:\ruta\backup.bak" | Restaurar a una base nueva sin sustituir automáticamente la activa. |

Conserve `local.env`, `novasoft.local.json` y backups de uso en privado. Para redistribuir utilice el ZIP original, no una instalación configurada. [Operación completa](delivery.md).

## Código y documentación pública

Repositorio: https://github.com/Sebastian2759/facturacion-prueba-tecnica

La carpeta para SharePoint incluye LEEME, guía HTML, aplicación ZIP y checksum. El repositorio permite revisar la implementación, arquitectura, decisiones y evidencias sin necesitar acceso a SharePoint.
