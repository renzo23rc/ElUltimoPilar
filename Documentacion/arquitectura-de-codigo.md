# Arquitectura de código

## Objetivo

Esta guía establece una base modular para Último Pilar. La regla principal es separar las reglas de la partida de Unity y mantener los sistemas de escena reemplazables y testeables.

## Capas y carpetas objetivo

- `Unity/Assets/Scripts/Core/`: modelo de partida y adaptadores de aplicación (`MatchState`, `MatchFlow`, `PlayerRoster`, `IPlayerRosterMember`, `PilarHealthSnapshot`, `MatchResult`, `ScorePolicy`, `DamageRequest`, `IDamageable`, `PlayerCommand`, `IInputAdapter`, `PlayerInputAdapter`, `GameManager`), composición multijugador (`PlayerJoinCoordinator`, `SplitScreenCameraCoordinator`, `TestSceneSetup`) y presentación (`Hud`, `CombatFeedback`, `AudioAdapter`).
- `Unity/Assets/Scripts/Arena/`: transformaciones y reglas específicas de la arena.
- `Unity/Assets/Scripts/Enemies/`: enemigos, spawner y comportamiento del Enjambre.
- `Unity/Assets/Scripts/Weapons/`: armas, energía y pickups.
- `Unity/Assets/Tests/PlayMode/` (futuro): pruebas de integración con escenas y tiempo.
- `Unity/Assets/Tests/Editor/`: pruebas EditMode para código puro.
- `Unity/Assets/Tests/Scenes/` y `Prefabs/`: composición y configuración de Unity, no reglas de dominio.

## Reglas de dependencia

1. Los tipos puros de `Core` (`MatchState`, `MatchFlow`, `PlayerRoster`, `IPlayerRosterMember`, `PilarHealthSnapshot`, `MatchResult`, `ScorePolicy`, `DamageRequest`, `IDamageable`, `PlayerCommand` e `IInputAdapter`) no dependen de `UnityEngine`, escenas, prefabs ni singletons.
2. `PlayerRoster<TPlayer>` es la frontera pura de registro, orden, capacidad, estado derribado y reposición de munición; los adaptadores de Unity implementan `IPlayerRosterMember`.
3. `PilarHealthSnapshot` es un valor puro e inmutable con salud restante y máxima validadas, además de un porcentaje restante factual. `ScorePolicy` convierte ese porcentaje en un puntaje entero determinista. `MatchResult` es una clase pura e inmutable que solo acepta `Victory` o `Defeat`, contiene el snapshot y expone el puntaje calculado; no agrega una causa de derrota.
4. Los adaptadores de Unity pueden depender del modelo y coordinar Arena, Enemies y Weapons.
5. UI y audio consumen una API pública y eventos; no modifican directamente el estado interno.
6. `PlayerCommand` representa un snapshot inmutable por jugador; `IInputAdapter` define su ciclo de vida y lectura. `PlayerInputAdapter` traduce el mapa `Player` del `PlayerInput` asignado, sin descubrir dispositivos ni crear jugadores. `PlayerJoinCoordinator` crea jugadores adicionales y empareja cada gamepad de forma exclusiva; `SplitScreenCameraCoordinator` aplica el viewport y conserva el audio en la cámara primaria. El modelo no lee teclado, mouse ni gamepad.

## Frontera gradual de daño

`DamageRequest` e `IDamageable` son tipos puros y no dependen de `UnityEngine`. En esta primera etapa, `Enemy` actúa como adaptador receptor explícito y `WeaponSystem` como primer adaptador llamador para los impactos existentes. La API pública `RecibirDaño(float)` se conserva como compatibilidad y mantiene el dispatch virtual de las subclases.

`DamageRequest` solo transporta `Amount` y preserva sus valores. Todavía no se agrega atribución de origen ni se incorporan reglas de facción, validación o semántica de muerte instantánea. La migración de otros receptores y llamadores queda diferida para una etapa posterior.

## Frontera de resultado de partida

`GameManager.CurrentResult` es nulo antes de un resultado terminal y después de cada reinicio o nuevo inicio. Cuando `MatchFlow` acepta una transición terminal, `GameManager` captura el estado factual del Pilar y publica `OnMatchResult` una sola vez, sin retirar ni cambiar las firmas existentes de `OnVictoria` y `OnDerrota`. La derrota puede producirse porque el Pilar llega a cero o porque todos los jugadores registrados están derribados; el resultado no expone una causa. Si el Pilar falta o sus valores son inválidos, se registra el problema, se conservan la transición y los eventos existentes, y no se publica un resultado incompleto.

`RemainingPercentage` describe la salud factual en escala 0–100 y `RemainingRatio` la expresa entre 0 y 1; ninguno es un puntaje. `ScorePolicy` toma el porcentaje factual, lo limita a 0–100 y lo redondea al entero más cercano con los puntos medios hacia arriba. `MatchResult.Score` conserva ese valor calculado junto con el snapshot factual.

## Reinicio determinista de estado propio

`GameManager` centraliza el reinicio compartido por `ReiniciarJuego()` y el nuevo inicio. `Start` prepara ese estado una sola vez; si el inicio espera input, `IniciarJuego()` lo reutiliza y no repite la limpieza. Los reinicios y nuevos partidos posteriores a un estado previo vuelven a ejecutar la misma secuencia. Antes de iniciar la oleada 1 restaura `Time.timeScale`, reinicia `MatchFlow`, resultado, temporizadores y flags, limpia `EnemySpawner`, detiene y reinicia `ArenaTransform`, restaura `Pilar` y sus torretas dinámicas, y finalmente reinicia jugadores, armas y energía. El roster, las suscripciones y las transformaciones de los jugadores se conservan porque todavía no existe un ancla formal de aparición.

`WeaponSystem` es la fuente de verdad de la munición configurada; al reiniciar repone sus armas base, sincroniza las representaciones heredadas de `PlayerController` y libera el cooldown. El reinicio no realiza limpieza global de proyectiles, pickups ni `WeaverZones` sin un propietario coordinado; esa limpieza queda diferida a una slice posterior.

## Estándar de implementación

- Una clase, enum o tipo público por archivo, con el mismo nombre que el archivo.
- Identificadores técnicos nuevos en inglés. Se conservan los nombres públicos españoles existentes para no romper escenas ni consumidores.
- La configuración de balance se serializa en componentes o assets de configuración. No se agregan números mágicos de gameplay al modelo puro.
- El modelo recibe la configuración necesaria por constructor o método; no conoce valores concretos de balance.
- Las suscripciones usan handlers con nombre. Suscribir una vez durante el ciclo de vida del objeto y desuscribir siempre durante su destrucción o desactivación. No usar lambdas anónimas cuando luego sea necesario desuscribir.

## Límites de input, UI y audio

`GameManager` expone operaciones de flujo y estado de solo lectura. La UI muestra ese estado y solicita comandos, pero no asigna campos internos. `PlayerController` ya resuelve su `PlayerInputAdapter`, lee un único `PlayerCommand` por frame y publica `OnCommandIssued`; `WeaponSystem` consume ese mismo snapshot para disparo y selección de arma, y `GameManager` usa el evento nombrado del jugador primario para el inicio. `TestSceneSetup` compone el jugador primario inactivo, clona una plantilla inactiva, configura `PlayerJoinCoordinator` y `SplitScreenCameraCoordinator`, y activa únicamente al primario al terminar la composición. Las cámaras se enlazan por referencias explícitas y permanecen sin tag `MainCamera`. `Hud` es el HUD definitivo: filas por jugador, overlays de inicio/pausa/resultado con puntaje, crosshair con flash y timer de variante. `CombatFeedback` centraliza shake por jugador y hitstop; `AudioAdapter` sintetiza efectos procedurales desde eventos; `Enemy` y `EnergyPickup` resuelven al jugador registrado más cercano. Quedan diferidos hotplug, lobby, HUD por jugador separado y el cambio de arma con rueda del mouse. Audio reaccionará a eventos de dominio/aplicación y no decidirá transiciones.

## Pruebas

- Cada transición determinista del modelo debe tener pruebas EditMode y cubrir también transiciones inválidas y estados terminales.
- La integración con `MonoBehaviour`, eventos de escena, spawner y tiempo debe cubrirse con pruebas PlayMode cuando se implemente.
- Aplicar TDD: RED (prueba primero), GREEN (mínimo cambio), TRIANGULATE (casos alternativos y negativos) y REFACTOR.
- Una prueba no se considera pasada sin evidencia del runner de Unity correspondiente.

## Estado actual y migración

El árbol de scripts runtime ya está ubicado en `Unity/Assets/Scripts/`, organizado en `Core`, `Arena`, `Enemies` y `Weapons`. Las pruebas EditMode permanecen en `Unity/Assets/Tests/Editor/`. `Hud` es la implementación definitiva actual: consume `OnMatchResult` y el roster, muestra filas por jugador, overlays de inicio/pausa/resultado con puntaje, crosshair y variante temporal. La migración arquitectónica será incremental: primero se extraen reglas puras y se les agregan pruebas; después los adaptadores reciben dependencias explícitas; finalmente se actualizan consumidores y se retiran duplicaciones. Cada extracción debe conservar la API pública existente y validarse antes de continuar.

## Alcance de esta fase: issues #15–#23

| Issue | Incluido en esta fase | Fuera de alcance en esta fase |
|---|---|---|
| #15 HUD final de partida | `Hud` definitivo: filas por jugador registrado, overlays de inicio/pausa, pantalla de resultado con puntaje, crosshair con flash y timer de variante; compuesto por `TestSceneSetup`. | HUD por jugador separado y lobby. |
| #16 Menú inicial, pausa y reinicio | Base de estados y operaciones públicas más overlays de inicio/pausa y atajos (Esc pausa/continúa, Enter reinicia en pausa o resultado). | Escena de menú dedicada, botones y navegación completa. |
| #17 Resultado y puntaje final | Frontera de código puro `PilarHealthSnapshot`/`MatchResult`/`ScorePolicy`, puntaje 0–100 redondeado al entero más cercano con midpoint hacia arriba, derrota por Pilar en cero o por todos los jugadores registrados derribados, `GameManager.CurrentResult`, `OnMatchResult` y publicación terminal compatible con `OnVictoria`/`OnDerrota`; pantalla de resultado con puntaje en `Hud`; pruebas EditMode y documentación. | Escenas, prefabs, modelos, assets y build settings. |
| #18 Audio y música reactiva | `AudioAdapter` con efectos procedurales (sin assets): disparo, impacto, muerte, explosión, oleada, victoria, derrota, curación, habilidad y variante; suscrito a `GameManager`, `CombatFeedback` y sistemas de cada jugador. | Clips grabados, mixer y música por capas. |
| #19 Crosshair, animaciones y feedback | Crosshair central con flash de impacto/muerte, `CombatFeedback` con shake por jugador y hitstop en muertes; se conserva el feedback procedural existente (trazadores, impactos, explosiones). | Animaciones con `Animator`/clips y screen shake por cámara física. |
| #20 Variantes temporales de armas | `WeaponSystem.ApplyVariant` con timer y expiración, `WeaponVariantPickup` recogible, drop por chance en `Muerte` enemiga y pickup fijo en escena; `Hud` muestra variante y tiempo. | Balance fino de drops y variantes adicionales. |
| #21 Validación de partida completa y build PC | Pruebas unitarias del modelo como base verificable. | Playtest completo, validación de build y generación de ejecutable. |
| #22 Assets temporales coherentes | Ningún cambio de assets. | Modelos, materiales, prefabs y reemplazo de placeholders. |
| #23 Cooperativo local de hasta 4 jugadores | Frontera pura `PlayerRoster<TPlayer>`, migración del consumidor primario a `PlayerCommand`, evento de comando para el inicio, composición del jugador generado con `PlayerInput` y asset serializado, coordinación de join, emparejamiento independiente de gamepads, creación de jugadores adicionales, cámaras split-screen y targeting al jugador registrado más cercano en `Enemy` y `EnergyPickup`; se conserva la compatibilidad actual. | Lobby, HUD por jugador, hotplug, cambio de arma con rueda del mouse y UX cooperativa completa. |
