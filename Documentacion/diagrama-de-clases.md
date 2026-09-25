# Diagrama de clases

El entregable editable es [diagrama-de-clases.drawio](./diagrama-de-clases.drawio). Abrilo con doble clic (si tenés Draw.io Desktop) o en [diagrams.net / Draw.io](https://app.diagrams.net/) con *File → Open*.

Está organizado en 7 páginas: un árbol de jerarquía en una sola página más 6 páginas de detalle, una idea por página. El árbol muestra todo de un vistazo; cada página de detalle tiene como máximo ~10 nodos con sus contratos concretos.

| Página | Contenido |
|---|---|
| `00 Arbol jerarquia (una pagina)` | Todo el modelo en una sola vista en forma de árbol con `GameManager` como raíz. Ramas: partida pura, jugador/input, coop, combate y presentación. Líneas curvas (`curved=1`) con flechas `classic` gruesas para que se vean. |
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
- **Línea curva:** solo en la página árbol, para que el jerárquico se lea sin cruces rectos.
- **Caja gris con `(see p.XX)`:** referencia a una clase que vive en otra página; existe para que ninguna flecha quede sin sus dos extremos a la vista. En la página árbol no se usan: cada clase aparece una sola vez.
- Cada flecha lleva el dato concreto que viaja: atributo, método, evento y tipo de payload.

## Verificación

El archivo sigue la [referencia XML oficial de Draw.io](https://github.com/jgraph/drawio-mcp/blob/main/shared/xml-reference.md): aristas con `curved=1` en la página árbol y rectas UML en las 6 páginas de detalle, sin ruteo manual, etiquetas cortas, IDs únicos por página, celdas estructurales `0`/`1` y geometría en cada arista. Validado por parseo XML (7 páginas, 53 aristas etiquetadas en el árbol + 51 en el detalle, sin referencias colgadas ni aristas sin etiqueta). Las 6 páginas originales quedaron intactas (mismo conteo de celdas).
