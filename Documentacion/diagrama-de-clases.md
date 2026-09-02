# Diagrama de clases planificado

Este diagrama muestra la estructura que vamos a construir de forma incremental. Separa las reglas puras de los adaptadores de Unity y deja explícitas las integraciones que todavía no vamos a implementar.

```mermaid
classDiagram
    direction LR

    class MatchState {
        <<pure enumeration>>
        WaitingToStart
        Playing
        Paused
        Victory
        Defeat
    }

    class MatchFlow {
        <<pure>>
        +MatchState State
        +int CurrentWave
        +int TotalWaves
        +Start() bool
        +Pause() bool
        +Resume() bool
        +TryStartNextWave() bool
        +SetVictory() bool
        +SetDefeat() bool
        +Reset()
    }

    class PilarHealthSnapshot {
        <<pure value>>
        +float Remaining
        +float Maximum
        +float RemainingRatio
        +float RemainingPercentage
        +TryCreate() bool
    }

    class MatchResult {
        <<pure immutable>>
        +MatchState Outcome
        +PilarHealthSnapshot PilarHealth
    }

    class IPlayerRosterMember {
        <<pure interface>>
        +bool IsDowned
        +ReplenishWaveAmmo()
    }

    class PlayerRoster~TPlayer~ {
        <<pure>>
        +int Capacity
        +int Count
        +int DownedCount
        +int StandingCount
        +bool AreAllDowned
        +Register(TPlayer) bool
        +Unregister(TPlayer) bool
        +ReplenishWaveAmmo()
    }

    class DamageRequest {
        <<pure value>>
        +float Amount
    }

    class IDamageable {
        <<pure interface>>
        +ReceiveDamage(DamageRequest)
    }

    class GameManager {
        <<Unity adapter>>
        +MatchState EstadoActual
        +MatchResult CurrentResult
        +IReadOnlyList~PlayerController~ Players
        +IniciarJuego()
        +PausarJuego()
        +ReanudarJuego()
        +ReiniciarJuego()
        +Victoria()
        +Derrota()
        +RegisterPlayer(PlayerController) bool
        +UnregisterPlayer(PlayerController) bool
        +OnMatchResult
        +OnVictoria
        +OnDerrota
    }

    class PlayerController {
        <<Unity adapter>>
        +bool IsDowned
        +RecibirDaño(float)
        +Curar(float)
        +Reanimar()
        +ReponerMunicion()
        +ReplenishWaveAmmo()
    }

    class Pilar {
        <<Unity adapter>>
        +float VidaActual
        +float PorcentajeVida
        +RecibirDaño(float)
        +RestaurarVida()
        +OnFaseCambiada
    }

    class Enemy {
        <<Unity adapter>>
        +RecibirDaño(float)
        +ReceiveDamage(DamageRequest)
        +OnMuerte
    }

    class Runner {
        <<Enemy adapter>>
    }
    class Artillery {
        <<Enemy adapter>>
    }
    class Explosive {
        <<Enemy adapter>>
    }
    class Weaver {
        <<Enemy adapter>>
    }
    class Nest {
        <<Enemy adapter>>
    }
    class Colossus {
        <<Enemy adapter>>
    }

    class WeaponSystem {
        <<Unity adapter>>
        +DispararActual()
        +CambiarArma(...)
        +ReponerMunicion()
        +OnDisparo
        +OnCambioArma
    }

    class EnergySystem {
        <<Unity adapter>>
        +GastarEnCuracion() bool
        +ActivarHabilidad() bool
        +OnEnergiaCambiada
    }

    class EnemySpawner {
        <<Unity adapter>>
        +bool OleadaEnProgreso
        +int EnemigosVivos
        +IniciarOleada(int)
        +EnemigoEliminado(Enemy)
        +LimpiarTodos()
    }

    class ArenaTransform {
        <<Unity adapter>>
        +OnTransformacionIniciada
        +OnTransformacionCompletada
    }

    class TestHUD {
        <<transitional presentation>>
    }

    class InputAdapter {
        <<planned>>
        +ReadPlayerCommands()
    }

    class MatchUiAdapter {
        <<planned>>
        +ShowMatchResult(MatchResult)
        +RequestPause()
        +RequestRestart()
    }

    class AudioAdapter {
        <<planned>>
        +HandleMatchEvents()
        +HandleArenaEvents()
    }

    class ScorePolicy {
        <<planned product decision>>
        +Calculate(MatchResult) int
    }

    MatchFlow --> MatchState : manages
    MatchResult *-- PilarHealthSnapshot : contains
    MatchResult --> MatchState : outcome

    PlayerRoster~TPlayer~ ..> IPlayerRosterMember : constrains
    PlayerController ..|> IPlayerRosterMember
    Enemy ..|> IDamageable

    GameManager *-- MatchFlow : owns
    GameManager *-- PlayerRoster~TPlayer~ : owns PlayerRoster<PlayerController>
    GameManager ..> MatchResult : publishes
    GameManager ..> Pilar : reads/restores
    GameManager ..> EnemySpawner : coordinates
    GameManager ..> PlayerController : registers
    PlayerRoster~TPlayer~ o-- PlayerController : tracks

    PlayerController --> GameManager : registers through current boundary
    PlayerController --> WeaponSystem : delegates firing
    PlayerController --> EnergySystem : delegates abilities
    WeaponSystem ..> IDamageable : first caller
    WeaponSystem ..> DamageRequest : creates
    Enemy --> Pilar : current target
    Enemy --> EnemySpawner : reports death

    Runner --|> Enemy
    Artillery --|> Enemy
    Explosive --|> Enemy
    Weaver --|> Enemy
    Nest --|> Enemy
    Colossus --|> Enemy

    ArenaTransform --> Pilar : consumes phase events
    TestHUD ..> GameManager : consumes state/events
    TestHUD ..> PlayerController : displays primary player

    InputAdapter ..> GameManager : sends commands
    MatchUiAdapter ..> GameManager : reads state/requests actions
    MatchUiAdapter ..> ArenaTransform : displays warnings
    AudioAdapter ..> GameManager : consumes events
    AudioAdapter ..> ArenaTransform : consumes events
    ScorePolicy ..> MatchResult : future score formula
```

## Cómo leerlo

| Marca | Significado |
|---|---|
| `<<pure>>` | Código C# independiente de Unity, testeable sin escenas ni `MonoBehaviour`. |
| `<<Unity adapter>>` | Componente que conecta las reglas con Unity, física, tiempo o escena. |
| `<<transitional presentation>>` | Código existente que se mantendrá mientras se reemplaza por presentación desacoplada. |
| `<<planned>>` | Contrato conceptual; todavía no existe como implementación final. |
| `<<planned product decision>>` | No se implementa hasta definir la regla de producto correspondiente. |

## Orden de implementación

1. **Base pura existente:** `MatchState`, `MatchFlow`, `PlayerRoster`, `PilarHealthSnapshot`, `MatchResult`, `DamageRequest` e `IDamageable`.
2. **Adaptadores estabilizados:** `GameManager`, `PlayerController` y `Enemy` conservando sus APIs públicas actuales.
3. **Migración gradual:** `Pilar`, torretas, proyectiles, energía y demás receptores pasan a `IDamageable` sin cambiar reglas de gameplay accidentalmente.
4. **Presentación:** `MatchUiAdapter` y `AudioAdapter` consumen eventos; ningún adaptador de presentación modifica el estado interno.
5. **Input y features:** `InputAdapter`, variantes temporales de armas y cámaras se incorporan después de definir sus contratos.
6. **Puntaje:** `ScorePolicy` queda separado hasta acordar normalización, redondeo y tratamiento de victoria/derrota.

## Límites deliberados

- No se agregan todavía `PlayerInput`, asignación de dispositivos, cámaras split-screen, assets, escenas ni prefabs.
- No se inventa una razón de derrota nueva: el resultado actual expone únicamente `Victory` o `Defeat` y el estado factual del Pilar.
- `RecibirDaño(float)` se conserva como API de compatibilidad mientras se completa la migración a `IDamageable`.
- `TestHUD` no representa el HUD final ni intenta resolver la UX de cuatro jugadores.
- Las relaciones marcadas como `planned` describen dirección arquitectónica, no clases listas para conectar en una escena.
