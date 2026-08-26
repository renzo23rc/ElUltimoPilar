# INFORME MECÁNICAS — ARMAS, ENEMIGOS Y SISTEMAS

**Proyecto:** El Último Pilar
**Desarrolladores:** Valentino Aviani y Renzo Portela
**Asignación:** Búsqueda y análisis de repositorios en GitHub
**Área:** Programación / Combate, IA de Enemigos y Sistemas de Apoyo

---

## MECÁNICAS DE ARMAS

### Referencia 1: Sistema de armas data-driven con ScriptableObjects

- **Repositorio:** [llamacademy/scriptable-object-based-guns](https://github.com/llamacademy/scriptable-object-based-guns)
- **Motor y Lenguaje:** Unity / C#
- **Arquitectura:** ScriptableObject (armas como assets) + disparo por raycast

**Desglose e implementación:**

- **A. Arma como asset (ScriptableObject):** cada arma es un asset con sus propios campos (daño, cadencia, munición máxima, tiempo de recarga). Permite crear las 3 armas base sin tocar código y, lo más importante, las variantes como drops temporales (rifle de precisión, señuelo, ralentización, empuje) como assets intercambiables en runtime.
- **B. Disparo hitscan por raycast:** el disparo se resuelve con un ray desde la cámara, ideal para el arma directa (confiable, cadencia media).
- **C. Munición y recarga:** cada arma lleva su contador de munición y lógica de recarga por tiempo, alineado con la regla del GDD de munición limitada repuesta al final de cada oleada.

**Evaluación:** es la base recomendada para el sistema de armas. La decisión del GDD de que las variantes sean drops temporales (no slots nuevos) se implementa intercambiando el ScriptableObject del arma equipada.

### Referencia 2: Disparo directo con hit feedback (tracer + impacto)

- **Repositorio:** [llamacademy/raycast-bullet-trails](https://github.com/llamacademy/raycast-bullet-trails)
- **Motor y Lenguaje:** Unity / C#
- **Arquitectura:** hitscan + bullet trails (tracer) + efectos de impacto + New Input System

**Evaluación:** cubre la capa de game feel del GDD. El tracer + efecto de impacto + sonido es exactamente la lista que pide el GDD (hit feedback, sonido de impacto, reacción visible del enemigo).

### Referencia 3: Proyectil con trayectoria (base para arma de área y señuelo)

- **Repositorio:** [llamacademy/projectile-trajectory](https://github.com/llamacademy/projectile-trajectory)
- **Motor y Lenguaje:** Unity / C#
- **Arquitectura:** proyectil físico con Line Renderer mostrando la trayectoria (ecuación cinemática)

**Evaluación:** base para el arma de área (explosiva, munición escasa) y punto de partida para el lanzador de señuelo (un proyectil que, en vez de explotar, genera un punto de atracción de enemigos).

### Referencia 4: Combate cuerpo a cuerpo en primera persona

- **Repositorio:** [rico345100/unity-basic-melee-combat-system-for-fps-rpg](https://github.com/rico345100/unity-basic-melee-combat-system-for-fps-rpg)
- **Motor y Lenguaje:** Unity / C#
- **Arquitectura:** melee con detección de golpe, daño alto sin munición

**Evaluación:** base para el arma cuerpo a cuerpo del GDD (sin munición, daño alto, exige exponerse cerca del Pilar). El Golpe de empuje se resuelve agregando knockback al impacto.

### Referencia 5: Swap de armas + rifle de precisión + pickups

- **Repositorio:** [Rachit-Dwivedi-R-D/Sharp-Shooter-Game](https://github.com/Rachit-Dwivedi-R-D/Sharp-Shooter-Game)
- **Motor y Lenguaje:** Unity / C#
- **Arquitectura:** pistola + ametralladora + sniper, weapon switching, pickups rotatorios de armas y munición, enemigos robots y torretas

**Evaluación:** valida la mecánica de variantes como drops temporales y el rifle de precisión. Es el repo más parecido al alcance de combate del proyecto.

### Referencia 6: Referencia integral de FPS (Unity oficial)

- **Repositorio:** [Unity-Technologies/FPSSample](https://github.com/Unity-Technologies/FPSSample)
- **Motor y Lenguaje:** Unity / C#
- **Arquitectura:** FPS multijugador oficial de Unity (armas múltiples, IA de enemigos, game feel con Cinemachine impulse, object pooling)

**Evaluación:** referencia arquitectónica de cabecera. No para copiar, sino para consultar cómo Unity resuelve producción: screen shake, pooling, daño, hit feedback.

---

## MECÁNICAS DE ENEMIGOS

### Referencia 7: IA con máquina de estados + behavior tree (persecución)

- **Repositorio:** [baponkar/zombie-ai](https://github.com/baponkar/zombie-ai)
- **Motor y Lenguaje:** Unity / C#
- **Arquitectura:** FSM + Behavior Tree, NavMeshAgent, persecución al objetivo, health

**Evaluación:** base para el **Corredor** (va directo al Pilar) y para el **Coloso** (mucha vida, resistente). La combinación FSM para estados simples + BT si hiciera falta más lógica es lo que pide el GDD (IA deliberadamente básica).

### Referencia 8: Enemigos a distancia con máquina de estados

- **Repositorio:** [blaz-cerpnjak/intelligent-opponent-shooter-game-unity](https://github.com/blaz-cerpnjak/intelligent-opponent-shooter-game-unity)
- **Motor y Lenguaje:** Unity / C#
- **Arquitectura:** FSM para enemigos que disparan al jugador (ranged)

**Evaluación:** base para el **Artillero** (fijo, dispara en línea recta). El line of sight del GDD se implementa con un raycast de LOS que se corta al rotar la arena.

### Referencia 9: FSM pluggable con ScriptableObjects (arquitectura para los 6 enemigos)

- **Repositorio:** [drewva32/Pluggable-FSM-AI](https://github.com/drewva32/Pluggable-FSM-AI)
- **Motor y Lenguaje:** Unity / C#
- **Arquitectura:** estados como ScriptableObjects (Action, Decision, Transition) que se enchufan para armar comportamientos

**Evaluación:** es la arquitectura ideal del proyecto. Cada enemigo (Corredor, Artillero, Explosivo, Tejedor, Nido, Coloso) se arma combinando estados reutilizables, sin código duplicado. Encaja con la filosofía del GDD de enemigos simples cuya profundidad viene de la combinación.

### Referencia 10: Enemigo autodestructivo (kamikaze)

- **Repositorio:** [Ashutosh806/SHARP-SHOOTER](https://github.com/Ashutosh806/SHARP-SHOOTER)
- **Motor y Lenguaje:** Unity / C#
- **Arquitectura:** enemigo que persigue y se autodestruye al acercarse al objetivo + pickups de armas y munición

**Evaluación:** base para el **Explosivo** (explota al morir o al llegar al Pilar). Faltan la explosión al morir y el empuje a pozos, que se resuelven con knockback + zonas de peligro.

### Referencia 11: Spawner de enemigos por oleadas

- **Repositorio:** [rehtse-studio/SimpleWaveSystem](https://github.com/rehtse-studio/SimpleWaveSystem) y [AdeelGameDev/Bots-vs-Aliens](https://github.com/AdeelGameDev/Bots-vs-Aliens)
- **Motor y Lenguaje:** Unity / C#
- **Arquitectura:** sistema de oleadas genérico (spawn de enemigos e items) + remake de Plants vs Zombies (wave spawning + IA)

**Evaluación:** base para el **Nido/Incubadora** (spawn periódico de Corredores) y para el gameloop de oleadas. Bots-vs-Aliens es útil porque Plants vs Zombies es una de las inspiraciones declaradas del GDD.

---

## MECÁNICAS DE SISTEMAS

### Referencia 12: Object pooling (crítico para hordas)

- **Repositorio:** [prime31/RecyclerKit](https://github.com/prime31/RecyclerKit), [annulusgames/uPools](https://github.com/annulusgames/uPools), [thefuntastic/unity-object-pool](https://github.com/thefuntastic/unity-object-pool)
- **Motor y Lenguaje:** Unity / C#
- **Arquitectura:** pool de objetos reutilizables para balas, enemigos y partículas

**Evaluación:** indispensable. Con hordas de enemigos y proyectiles, instanciar y destruir en runtime destruye el rendimiento. Se recomienda pool desde el día 1.

### Referencia 13: Torretas automáticas

- **Repositorio:** [strawhat19/Tower-Defense](https://github.com/strawhat19/Tower-Defense) y [RaunakGameDev/SharpShooter-Prototype](https://github.com/RaunakGameDev/SharpShooter-Prototype)
- **Motor y Lenguaje:** Unity / C#
- **Arquitectura:** torreta con fire rate, proyectiles, tracking de enemigos y health

**Evaluación:** base para las torretas del protocolo de emergencia del Pilar (25-0%) y para la torreta de La Técnica. SharpShooter-Prototype además combina multiple weapons + enemy spawning + ammo pickups + wave combat, casi un espejo del proyecto.

### Referencia 14: Wave/horde system con dificultad progresiva

- **Repositorio:** [rehtse-studio/SimpleWaveSystem](https://github.com/rehtse-studio/SimpleWaveSystem) y [Sachin6913/FrontlineCommander](https://github.com/Sachin6913/FrontlineCommander)
- **Motor y Lenguaje:** Unity / C#
- **Arquitectura:** wave scaling, economía y dificultad progresiva

**Evaluación:** base para la escalada de oleadas del gameloop. Ojo: el GDD ata la dificultad al estado del Pilar (no a un cronómetro), así que el spawner debe leer la vida del Pilar en vez de un timer fijo.

### Referencia 15: Cooperativo local (gap detectado)

- **Repositorio:** sin repo directo de calidad
- **Motor y Lenguaje:** Unity / C#
- **Arquitectura:** New Input System PlayerInput + 4 cámaras con viewport rects (splitscreen)

**Evaluación:** lo que hay en GitHub para coop local es flojo. La vía correcta ya está validada en el informe de movimiento (PlayerInput, como en Tamashi-The-Brave-Ninja). No hace falta repo: es configuración de proyecto.

---

## GAPS (mecánicas sin repo directo)

- **Lanzador de señuelo:** derivar de `projectile-trajectory` + un sistema de aggro que apunte a un punto.
- **Carga de ralentización:** derivar de daño AoE + modificador de velocidad por zona.
- **Tejedor** (campo de ralentización + daño por segundo en área): combinar AoE damage + debuff. Ningún repo lo tiene armado.
- **Transformación de arena:** no existe equivalente. Es el diferenciador del proyecto y se construye a medida.

---

## CONCLUSIÓN GENERAL Y RECOMENDACIÓN TÉCNICA

**Resumen:** se evaluaron repositorios de shooters en GitHub para cubrir armas, enemigos y sistemas. Las arquitecturas recomendadas son:

- **Armas:** ScriptableObject data-driven (llamacademy) + swap por variantes (Sharp-Shooter-Game).
- **Enemigos:** FSM pluggable con ScriptableObjects (drewva32/Pluggable-FSM-AI) como arquitectura única para los 6 tipos.
- **Sistemas:** object pooling (RecyclerKit/uPools) + wave system (SimpleWaveSystem) + torretas (SharpShooter-Prototype).

**Propuesta de arquitectura:**

1. Un `WeaponDefinition` ScriptableObject con todas las armas base y variantes; el swap por drops temporales reemplaza el asset equipado.
2. Un único `EnemyStateMachine` pluggable donde cada enemigo se arma combinando estados (chase, ranged, explode, spawn, debuff, tank).
3. Object pooling desde el inicio para balas y enemigos.
4. Coop local vía New Input System `PlayerInput` + splitscreen de 4 cámaras (sin repo, es configuración).
5. El spawner de oleadas lee la vida del Pilar (no un timer) para cumplir la regla de escalada del GDD.

**Siguientes pasos:**

- Abrir el código de `scriptable-object-based-guns` y `Pluggable-FSM-AI` para traer el desglose de implementación exacto (como se hizo en el informe de movimiento).
- Completar los gaps con una segunda pasada de búsqueda (requiere `gh auth login` para más rate limit).
