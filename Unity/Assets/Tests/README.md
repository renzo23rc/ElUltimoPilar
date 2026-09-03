# Entorno de Pruebas - Último Pilar

Este entorno permite testear todas las mecánicas de programación sin depender de assets finales, animaciones, ni del trabajo de otros departamentos.

## Estructura

```
Assets/
├── Scripts/
│   ├── Core/
│   │   ├── AudioAdapter.cs         # Audio reactivo procedural
│   │   ├── CombatFeedback.cs       # Screen shake e hitstop
│   │   ├── DamageRequest.cs        # Solicitud pura de daño
│   │   ├── GameManager.cs          # Control de oleadas y estado del juego
│   │   ├── Hud.cs                  # HUD definitivo de partida
│   │   ├── IDamageable.cs          # Contrato de recepción de daño
│   │   ├── IInputAdapter.cs        # Contrato de adaptación de input
│   │   ├── IPlayerRosterMember.cs  # Contrato de miembro del registro
│   │   ├── MatchFlow.cs            # Flujo de estados de la partida
│   │   ├── MatchResult.cs          # Resultado terminal de la partida
│   │   ├── MatchState.cs           # Estados de la partida
│   │   ├── Pilar.cs                # Vida, fases y transformaciones del Pilar
│   │   ├── PilarHealthSnapshot.cs  # Snapshot de salud del Pilar
│   │   ├── PlayerCommand.cs        # Snapshot de comando del jugador
│   │   ├── PlayerController.cs     # Movimiento, cámara, input
│   │   ├── PlayerInputAdapter.cs   # Adaptador del input del jugador
│   │   ├── PlayerJoinCoordinator.cs # Join con gamepads independientes
│   │   ├── PlayerRoster.cs         # Registro de jugadores
│   │   ├── PoolManager.cs          # Gestión de objetos reutilizables
│   │   ├── PooledObject.cs          # Objeto reutilizable
│   │   ├── ScorePolicy.cs          # Cálculo determinista del puntaje
│   │   ├── SplitScreenCameraCoordinator.cs # Viewports split-screen
│   │   ├── TestSceneSetup.cs       # Generador automático de escena de prueba
│   │   └── Torreta.cs              # Torreta defensiva
│   ├── Enemies/
│   │   ├── Artillery.cs            # Artillero (dispara a distancia)
│   │   ├── Colossus.cs             # Coloso (mini-jefe)
│   │   ├── Enemy.cs                # Clase base de enemigos
│   │   ├── EnemySpawner.cs         # Spawner de oleadas
│   │   ├── Explosive.cs            # Explosivo/Kamikaze
│   │   ├── Nest.cs                 # Nido/Incubadora
│   │   ├── Projectile.cs           # Proyectil de artillero
│   │   ├── Runner.cs               # Corredor (rápido, va al Pilar)
│   │   └── Weaver.cs               # Tejedor (campos de ralentización)
│   ├── Weapons/
│   │   ├── EnergyPickup.cs         # Orbes de energía
│   │   ├── EnergySystem.cs         # Energía, curación y habilidades
│   │   ├── WeaponSystem.cs         # Sistema de 3 armas + variantes
│   │   └── WeaponVariantPickup.cs  # Drop de variante temporal
│   └── Arena/
│       ├── ArenaTransform.cs       # Transformaciones del escenario
│       └── PozoKill.cs              # Pozo central de la arena
└── Tests/
    ├── Editor/
    │   ├── DamageRequestTests.cs
    │   ├── MatchFlowTests.cs
    │   ├── MatchResultTests.cs
    │   ├── PlayerCommandTests.cs
    │   ├── PlayerRosterTests.cs
    │   └── ScorePolicyTests.cs
    └── Prefabs/
```

## Cómo empezar

### Opción 1: Setup Automático (Recomendado)

1. Crear una **escena vacía** (`File > New Scene`)
2. Guardarla como `Assets/Tests/Scenes/TestEnvironment.unity`
3. Crear un GameObject vacío llamado `"Setup"`
4. Agregar el script `TestSceneSetup.cs`
5. Apretar **Play**

El script genera automáticamente:

- Pilar central (cilindro)
- Jugador (capsule + cámara)
- Suelo circular
- Spawner con 8 puntos de spawn
- GameManager
- Prefabs de todos los enemigos (cubos de colores)
- Pickups de energía
- Iluminación básica

### Opción 2: Setup Manual

Si preferís armar la escena vos mismo:

1. Crear GameObject `"GameManager"` con `GameManager.cs`
2. Crear un cilindro `"Pilar"` en (0, 2, 0) con `Pilar.cs`
3. Crear un plane `"Arena"` en (0, 0, 0)
4. Crear un capsule `"Jugador"` con:
   - `PlayerController.cs`
   - `EnergySystem.cs`
   - `WeaponSystem.cs`
   - `CharacterController`
   - Cámara hija en (0, 0.8, 0)
5. Crear GameObject `"Spawner"` con `EnemySpawner.cs`
6. Crear GameObject `"ArenaManager"` con `ArenaTransform.cs`

## Controles

| Tecla | Acción |
|-------|--------|
| `WASD` | Movimiento |
| `Mouse` | Mirar alrededor |
| `Click Izq` | Disparar |
| `1` | Arma Directa |
| `2` | Arma de Área |
| `3` | Cuerpo a Cuerpo |
| `Scroll` | Cambio de arma con rueda del mouse (diferido/futuro) |
| `H` | Curarse (gasta energía) |
| `J` | Activar habilidad (gasta energía) |
| `R` | **Debug:** Dañar Pilar (-10%) |

## Enemigos (visuales temporales)

| Enemigo | Color | Comportamiento |
|---------|-------|----------------|
| Corredor | Rojo | Va directo al Pilar, rápido |
| Artillero | Azul | Dispara proyectiles a distancia |
| Explosivo | Amarillo | Explota al morir o al llegar al Pilar |
| Tejedor | Magenta | Lanza zonas de ralentización |
| Nido | Gris | Estacionario, genera corredores |
| Coloso | Bordó | Lento, mucha vida, inmune a disparos directos |

## Testear Transformaciones de Arena

Para forzar las transformaciones sin esperar a que los enemigos dañen el Pilar:

1. Apretar **Play**
2. Apretar la tecla **R** repetidamente
3. Cada -10% de vida del Pilar activa una nueva fase:
   - **75%**: Se abre el pozo central
   - **50%**: Aparece zona de gravedad alterada
   - **25%**: Protocolo de emergencia (torretas + escombros)

## Testear Oleadas

El `EnemySpawner` puede configurarse de dos formas:

### Configuración manual (Inspector)

En `configuracionOleadas` definir cada oleada con cantidades específicas de cada tipo de enemigo.

### Configuración automática

Si no hay configuración definida, el spawner genera oleadas automáticamente:

- Oleada 1: 5+ enemigos, solo corredores
- Oleada 3+: Empiezan a aparecer artilleros y explosivos
- Oleada 5+: Aparecen nidos
- Oleada 7+: Aparecen colosos
- La cantidad y dificultad escalan con cada oleada

## Sistema de Energía

- Cada enemigo dropea energía al morir
- Corredor: 2 energía
- Artillero: 3 energía
- Explosivo: 5 energía
- Tejedor: 4 energía
- Nido: 10 energía
- Coloso: 20 energía

**Gasto:**

- `H` - Curarse: 15 energía = +8% vida
- `J` - Habilidad: 28 energía (pulso de daño o ralentización)

## Armas

| Arma | Munición | Daño | Uso |
|------|----------|------|-----|
| Directa | 80 | 16 | Confiable, media cadencia |
| Área | 16 | 42 | Explosión en punto de impacto |
| Melee | ∞ | 50 | Empuja enemigos |

La munición se repone automáticamente al final de cada oleada.

## Eventos Disponibles

Todos los sistemas principales emiten eventos C# que podés suscribirte para testear:

```csharp
// GameManager
gameManager.OnOleadaIniciada += (numero) => Debug.Log($"Oleada {numero}");
gameManager.OnVictoria += () => Debug.Log("Ganaste!");
gameManager.OnDerrota += () => Debug.Log("Perdiste!");

// Pilar
pilar.OnFaseCambiada += (fase) => Debug.Log($"Fase {fase}");
pilar.OnVidaCambiada += (vida) => Debug.Log($"Vida: {vida}%");

// Arena
arenaTransform.OnTransformacionIniciada += (fase) => { };
arenaTransform.OnTransformacionCompletada += (fase) => { };

// WeaponSystem
weaponSystem.OnDisparo += (arma) => { };
weaponSystem.OnSinMunicion += () => { };

// EnergySystem
energySystem.OnEnergiaCambiada += (cantidad) => { };
energySystem.OnHabilidadActivada += () => { };
```

## Notas para Programadores

- Los scripts runtime están en `Assets/Scripts/` y las pruebas EditMode en `Assets/Tests/Editor/`.
- Los enemigos usan cubos de colores como placeholders.
- El Pilar usa un cilindro con cambio de color según su fase.
- El suelo es un plane escalado.
- Todo el feedback visual es temporal (flashes de color, partículas básicas).
- Los scripts están documentados con comentarios XML para IntelliSense.

## Próximos pasos sugeridos

1. Ajustar balance de daño/vida en el Inspector
2. Implementar el sistema de roles de defensor (Chatarrero, Técnica, etc.)
3. Ajustar chance de drop y balance de variantes de armas temporales
4. Mejorar IA de enemigos (patrullaje, evasión)
5. Implementar sistema de gravedad alterada real
6. Agregar más feedback visual/sonoro

---

**Dudas o problemas:** Revisar la consola de Unity, todos los sistemas loguean en Debug.Log con prefijo identificador.
