# ADR-000: Integracion de .llmops/ dentro del template Plantilla

**Estado**: Aceptado
**Fecha**: {{CREATED_AT}}

## Contexto

El template Plantilla no contenia la estructura .llmops/ definida en
modelo-capas-llmops.md. El wizard de creacion de proyectos requiere que
todo proyecto generado incluya la Capa 2 desde el primer commit.

## Decision

.llmops/ y las adiciones a docs/ se integran directamente en este
repositorio, conforme a estandar-operacional-por-stack.md, Seccion 5.13.
El wizard deja de copiar esta estructura por separado.

## Consecuencias

Toda actualizacion futura de .llmops/ se aplica como cambio versionado
sobre este template, siguiendo el mismo procedimiento de control de
cambios que cualquier otra modificacion al codigo base.
