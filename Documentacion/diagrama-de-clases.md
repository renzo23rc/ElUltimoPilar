# Diagrama de clases

El entregable editable es [diagrama-de-clases.drawio](./diagrama-de-clases.drawio). Abrilo con doble clic (si tenés Draw.io Desktop) o en [diagrams.net / Draw.io](https://app.diagrams.net/) con *File → Open*.

Está organizado en 6 páginas, una idea por página: el overview muestra la arquitectura de un vistazo y cada página de detalle tiene como máximo ~10 nodos con sus contratos concretos.

| Página | Contenido |
|---|---|
| `00 Overview` | GameManager como hub: quién se registra, quién recibe eventos y con qué payload. |
| `01 Pure domain` | Clases puras sin dependencias de Unity: estados, snapshots, resultado, puntaje, roster y daño. |
| `02 Player input` | Un snapshot `PlayerCommand` por frame: del `PlayerInput` al `PlayerController`. |
| `03 Coop setup` | Composición multijugador: join con gamepads y split-screen. Sin reglas de partida. |
| `04 Combat` | El daño viaja por `IDamageable` como `DamageRequest`; variantes, energía y feedback. |
| `05 Match presentation` | Hub de partida, Pilar, spawner, arena y presentación (solo lee eventos, no escribe estado). Incluye colaboradores de Pilar (fases, visual, torretas) y de fases de arena (estado, handlers, efectos, avisos). |

## Cómo leerlo

- **Flecha con rombo lleno (◆):** composición / ownership.
- **Flecha con bloque vacío (▷):** herencia o implementación de interfaz.
- **Flecha abierta (→):** llamada o asociación.
- **Línea discontinua:** dependencia o suscripción a evento.
- **Caja gris con `(see p.XX)`:** referencia a una clase que vive en otra página; existe para que ninguna flecha quede sin sus dos extremos a la vista.
- Cada flecha lleva el dato concreto que viaja: atributo, método, evento y tipo de payload.

## Verificación

El archivo sigue la [referencia XML oficial de Draw.io](https://github.com/jgraph/drawio-mcp/blob/main/shared/xml-reference.md): aristas rectas UML sin ruteo manual, etiquetas cortas, IDs únicos, celdas estructurales `0`/`1` y geometría en cada arista. Validado por parseo XML contra el checklist 1–14 (6 páginas, 51 aristas etiquetadas, sin referencias colgadas ni aristas sin etiqueta).
