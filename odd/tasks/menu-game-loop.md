# ODD: menú y flujo de partida

## Objetivo

Agregar una escena de menú y conectar el inicio, la pausa y el resultado de las partidas existentes. El jugador podrá ajustar preferencias y consultar puntajes locales sin guardar ni cargar una partida a mitad de la noche.

## Decisiones y límites

- PC con teclado/mouse y mandos; conservar la incorporación cooperativa existente.
- Mantener `SampleScene` y `EstatuaDeLaLibertad`, sus referencias y GUID. La escena de menú es adicional.
- El lobby y la elección de roles se dejan para más adelante, por decisión del usuario.
- Una partida es una instancia cerrada; no se implementan guardados de partida a mitad de noche.
- Respetar las APIs públicas actuales, el modelo de `MatchFlow` y la organización por feature de `skills/unity-engineering-standards/`.
- El usuario pidió expresamente no escribir ni ejecutar tests. La revisión será estructural; no se afirmará validación del runner ni funcionamiento en Unity sin evidencia.

## Punto de partida tras actualizar el proyecto

- El árbol estaba limpio y los archivos de menú y el ODD de la sesión anterior no estaban presentes.
- `Unity/ProjectSettings/EditorBuildSettings.asset` incluye `SampleScene` y `EstatuaDeLaLibertad`.
- El HUD y el `GameManager` existentes presentan estados de inicio, pausa y resultado, pero no un menú principal dedicado.
- Las transformaciones de arena ya tienen un mecanismo visual y sonoro de aviso previo. La composición actual no asigna un clip; se necesita un respaldo sonoro si no hay uno configurado.

## Trabajo

- [x] Crear escena de menú, incluirla en Build Settings y conservar las dos escenas existentes.
- [x] Añadir navegación con teclado, mouse y mando; jugar, opciones, puntajes, ayuda y salir.
- [x] Conectar controles de inicio, pausa y resultado con el flujo existente, incluyendo gestión del cursor y del input.
- [x] Implementar preferencias y puntajes locales persistentes, sin guardar el estado de una partida activa.
- [x] Cubrir el aviso de transformación cuando no exista un clip configurado, sin duplicar el mecanismo existente.
- [x] Revisar estructuralmente la integración y documentar límites, referencias y evidencia disponible.

## Resultado

- `Menu.unity` es la primera escena de Build Settings; desde ella se elige cualquiera de las dos escenas de partida.
- `Core/Hud/Menu/` contiene el menú principal, los controles de partida, los ajustes y los puntajes. El HUD y el menú de partida se componen desde `TestSceneSetup`.
- Se guardan preferencias y hasta diez resultados finales en archivos JSON dentro de `Application.persistentDataPath`. Una noche en curso no se guarda ni se carga.
- La arquitectura y los límites de input se describen en `Documentacion/arquitectura-de-codigo.md`.

## Ajustes menú primero

- El menú crea su propia cámara con fondo negro y sin culling, sin depender de cámaras de partida.
- `MenuSceneLoader` carga solo la escena elegida en modo `Single`; en el Editor, salir detiene Play.
- La pestaña de pantalla edita valores pendientes y aplica con el botón "Aplicar pantalla"; audio y controles siguen aplicando al momento.
- `PoolManager` es por escena y al componer la partida se apagan cámaras de escena ajenas a jugadores.

## Validación y riesgos

Solo se realizó lectura estática y revisión del diff; no se escribieron ni ejecutaron tests, ni se corrieron Unity, builds o compilación, por indicación del usuario. La interacción real en Unity, incluidos mando, escenas, persistencia, presentación y coexistencia con input de juego, permanece sin verificar.

El analizador señaló referencias a tipos de menú nuevos que sí están presentes en el árbol. El archivo local generado `Unity/Assembly-CSharp.csproj` desactiva las inclusiones predeterminadas y no enumeraba esos scripts; se añadieron sus rutas a la copia local para el IDE, sin regenerarla con Unity. Esa copia se sobrescribirá cuando Unity genere el proyecto y no forma parte del cambio versionado. La navegación del lenguaje ya encuentra las definiciones de los tipos señalados, pero una consulta posterior de diagnósticos fue inconclusa: esto no demuestra que el proyecto compile.

## Referencias

- `Documentacion/gdd.md` y `Documentacion/arquitectura-de-codigo.md`.
- [Input System 1.20: InputSystemUIInputModule](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.20/api/UnityEngine.InputSystem.UI.InputSystemUIInputModule.html).
- [Unity 6: JsonUtility](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/JsonUtility.html).
