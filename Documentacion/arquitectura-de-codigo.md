# Arquitectura de código

## Objetivo

Esta guía establece una base modular para Último Pilar. La regla principal es separar las reglas de la partida de Unity y mantener los sistemas de escena reemplazables y testeables.

## Capas y carpetas objetivo

- `Unity/Assets/Tests/Scripts/Core/`: modelo de partida y adaptadores de aplicación (`MatchState`, `MatchFlow`, `PlayerRoster`, `IPlayerRosterMember`, `PilarHealthSnapshot`, `MatchResult`, `DamageRequest`, `IDamageable`, `GameManager`).
- `Unity/Assets/Tests/Scripts/Arena/`: transformaciones y reglas específicas de la arena.
- `Unity/Assets/Tests/Scripts/Enemies/`: enemigos, spawner y comportamiento del Enjambre.
- `Unity/Assets/Tests/Scripts/Weapons/`: armas, energía y pickups.
- `Unity/Assets/Tests/UI/` (futuro): HUD, menús y presentación.
- `Unity/Assets/Tests/Audio/` (futuro): música, efectos y respuesta a eventos.
- `Unity/Assets/Tests/Editor/`: pruebas EditMode para código puro.
- `Unity/Assets/Tests/Scenes/` y `Prefabs/`: composición y configuración de Unity, no reglas de dominio.

## Reglas de dependencia

1. Los tipos puros de `Core` (`MatchState`, `MatchFlow`, `PlayerRoster`, `IPlayerRosterMember`, `PilarHealthSnapshot`, `MatchResult`, `DamageRequest` e `IDamageable`) no dependen de `UnityEngine`, escenas, prefabs ni singletons.
2. `PlayerRoster<TPlayer>` es la frontera pura de registro, orden, capacidad, estado derribado y reposición de munición; los adaptadores de Unity implementan `IPlayerRosterMember`.
3. `PilarHealthSnapshot` es un valor puro e inmutable con salud restante y máxima validadas, además de un porcentaje restante factual. `MatchResult` es una clase pura e inmutable que solo acepta `Victory` o `Defeat` y contiene ese snapshot; no calcula ni almacena puntaje.
4. Los adaptadores de Unity pueden depender del modelo y coordinar Arena, Enemies y Weapons.
5. UI y audio consumen una API pública y eventos; no modifican directamente el estado interno.
6. Input traduce acciones del jugador a operaciones públicas del coordinador. El modelo no lee teclado, mouse ni gamepad.

## Frontera gradual de daño

`DamageRequest` e `IDamageable` son tipos puros y no dependen de `UnityEngine`. En esta primera etapa, `Enemy` actúa como adaptador receptor explícito y `WeaponSystem` como primer adaptador llamador para los impactos existentes. La API pública `RecibirDaño(float)` se conserva como compatibilidad y mantiene el dispatch virtual de las subclases.

`DamageRequest` solo transporta `Amount` y preserva sus valores. Todavía no se agrega atribución de origen ni se incorporan reglas de facción, validación o semántica de muerte instantánea. La migración de otros receptores y llamadores queda diferida para una etapa posterior.

## Frontera de resultado de partida

`GameManager.CurrentResult` es nulo antes de un resultado terminal y después de cada reinicio o nuevo inicio. Cuando `MatchFlow` acepta una transición terminal, `GameManager` captura el estado factual del Pilar y publica `OnMatchResult` una sola vez, sin retirar ni cambiar las firmas existentes de `OnVictoria` y `OnDerrota`. Si el Pilar falta o sus valores son inválidos, se registra el problema, se conservan la transición y los eventos existentes, y no se publica un resultado incompleto.

`RemainingPercentage` describe la salud factual en escala 0–100 y `RemainingRatio` la expresa entre 0 y 1; ninguno es un puntaje. La fórmula de puntaje, su normalización y su redondeo quedan deliberadamente diferidos hasta contar con una definición de producto.

## Estándar de implementación

- Una clase, enum o tipo público por archivo, con el mismo nombre que el archivo.
- Identificadores técnicos nuevos en inglés. Se conservan los nombres públicos españoles existentes para no romper escenas ni consumidores.
- La configuración de balance se serializa en componentes o assets de configuración. No se agregan números mágicos de gameplay al modelo puro.
- El modelo recibe la configuración necesaria por constructor o método; no conoce valores concretos de balance.
- Las suscripciones usan handlers con nombre. Suscribir una vez durante el ciclo de vida del objeto y desuscribir siempre durante su destrucción o desactivación. No usar lambdas anónimas cuando luego sea necesario desuscribir.

## Límites de input, UI y audio

`GameManager` expone operaciones de flujo y estado de solo lectura. La UI muestra ese estado y solicita comandos, pero no asigna campos internos. El input se conectará a esos comandos desde un adaptador de Unity. Audio reaccionará a eventos de dominio/aplicación y no decidirá transiciones.

## Pruebas

- Cada transición determinista del modelo debe tener pruebas EditMode y cubrir también transiciones inválidas y estados terminales.
- La integración con `MonoBehaviour`, eventos de escena, spawner y tiempo debe cubrirse con pruebas PlayMode cuando se implemente.
- Aplicar TDD: RED (prueba primero), GREEN (mínimo cambio), TRIANGULATE (casos alternativos y negativos) y REFACTOR.
- Una prueba no se considera pasada sin evidencia del runner de Unity correspondiente.

## Migración desde `Assets/Tests/Scripts`

La migración será incremental: primero se extraen reglas puras y se les agregan pruebas; después los adaptadores reciben dependencias explícitas; finalmente se actualizan consumidores y se retiran duplicaciones. En esta fase no se mueven ni renombran scripts, escenas o prefabs. Cada extracción debe conservar la API pública existente y validarse antes de continuar.

## Alcance de esta fase: issues #15–#23

| Issue | Incluido en esta fase | Fuera de alcance en esta fase |
|---|---|---|
| #15 HUD final de partida | API de estado de solo lectura y eventos que el futuro HUD puede consumir. | Diseño, layout y comportamiento del HUD final. |
| #16 Menú inicial, pausa y reinicio | Estados y operaciones públicas de iniciar, pausar, reanudar y reiniciar; limpieza determinista de eventos. | Escena de menú, botones, navegación y wiring de UI/input. |
| #17 Resultado y puntaje final | Frontera de código puro `PilarHealthSnapshot`/`MatchResult`, `GameManager.CurrentResult`, `OnMatchResult` y publicación terminal compatible con `OnVictoria`/`OnDerrota`; pruebas EditMode y documentación. | Fórmula, normalización o redondeo del puntaje; razón de derrota a nivel de producto; pantalla de resultado y presentación; escenas, prefabs, modelos, assets, audio, input, cámaras, build settings y cambios en `PlayerRoster`, `PlayerController`, `TestHUD`, `MatchFlow` o `MatchState`. |
| #18 Audio y música reactiva | Ningún cambio de audio; solo se preserva la posibilidad de consumir eventos. | Clips, mixer, música y respuesta sonora. |
| #19 Crosshair, animaciones y feedback | Ningún cambio de presentación de combate; la nueva frontera de daño no altera el feedback existente. | Crosshair, animaciones y feedback visual. |
| #20 Variantes temporales de armas | Ningún cambio en variantes, selección, duración ni balance; `WeaponSystem` solo adapta los impactos existentes a `IDamageable`. | Variantes, selección y duración de armas. |
| #21 Validación de partida completa y build PC | Pruebas unitarias del modelo como base verificable. | Playtest completo, validación de build y generación de ejecutable. |
| #22 Assets temporales coherentes | Ningún cambio de assets. | Modelos, materiales, prefabs y reemplazo de placeholders. |
| #23 Cooperativo local de hasta 4 jugadores | Frontera pura `PlayerRoster<TPlayer>`, ciclo de vida de registro/desregistro, consultas co-op, reposición de munición y migración de `GameManager`, `PlayerController` y `TestHUD` al roster; se conserva la compatibilidad actual. | Integración de `PlayerInput`, asignación de dispositivos, cámaras adicionales y UX cooperativa. |
