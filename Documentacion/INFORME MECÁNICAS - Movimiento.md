# INFORME MECÁNICAS — MOVIMIENTO

**Proyecto:** El Último Pilar
**Desarrolladores:** Valentino Aviani y Renzo Portela
**Asignación:** Búsqueda y análisis de repositorios en GitHub
**Área:** Programación / Sistemas de Movimiento

---

## Referencia 1: Movimiento sobre SplinePhysics (Faber)

**Archivo analizado:** Character.cs
**Repositorio de origen:** Faber (por samuelfenton)
**Enlace:** [Character.cs — Assets/Scripts/Entity/Character](https://github.com/samuelfenton/Faber/blob/b369e34c3609873432f98c8e61b532b5b39b3e58/Assets/Scripts/Entity/Character/Character.cs#L3)
**Motor y Lenguaje:** Unity / C#
**Arquitectura:** Movimiento basado en físicas personalizadas sobre trayectorias (SplinePhysics)

### Desglose e Implementación

**A. Caminar, Correr y Manejo de Inercia**
- **Lógica de aceleración/frenado:** en lugar de aplicar movimiento instantáneo, el método `UpdateVelocity()` calcula cuadro a cuadro la aceleración (`m_groundAccel`) y desaceleración (`m_groundedDeaccel`) multiplicadas por `Time.deltaTime`. Compara la velocidad deseada (`m_desiredVelocity.x`) con la velocidad real (`frameVelocity.x`) para lograr transiciones suaves de frenado y arranque.
- **Sprint (Correr):** implementa un modificador multiplicativo constante (`SPRINT_MODIFIER = 1.5f`) conectado a los Blend Trees del Animator.

**B. Salto, Gravedad y Control Aéreo**
- **Salto y Doble Salto:** expone variables independientes para la fuerza de salto inicial (`m_jumpVelocity = 10.0f`) y segundo salto (`m_doubleJumpSpeed = 6.0f`).
- **Control en el aire:** incluye un factor de atenuación (`m_inAirAccelModifier = 0.5f`). Cuando el sistema detecta que el personaje no toca suelo (`!m_splinePhysics.m_downCollision`), la capacidad de acelerar y maniobrar se reduce al 50%, simulando pérdida de tracción aérea.

**C. Dash / Evasión**
- **Configuración:** define una velocidad de impulso alta (`m_dashVelocity = 12.0f`) y controla el estado mediante banderas booleanas (`m_inAirDashFlag`). La ejecución del desplazamiento y su temporizador se delega a un submódulo (ManoeuvreController).

**D. Knockback (Retroceso) y Recuperación (i-frames)**
- **Respuesta a impactos:** contiene un sistema de cálculo de dirección de impacto relativo (`DealDamage()`) que aplica una fuerza de retroceso (`m_knockbackVelocity`).
- **Invulnerabilidad temporal:** utiliza una corrutina (`IEnumerator RecoverRoutine`) que activa una bandera (`m_recoveryFlag`) durante un tiempo configurable para evitar que el jugador reciba daño continuo encadenado (damage juggling).

---

## Referencia 2: Controlador por Máquina de Estados y New Input System

**Archivo analizado:** Character.cs
**Repositorio de origen:** Tamashi-The-Brave-Ninja (por Leaf-Game-Dev)
**Enlace:** [Character.cs — Assets/Scripts/Player](https://github.com/Leaf-Game-Dev/Tamashi-The-Brave-Ninja/blob/b115b8ffb8e0a5ef71ef87fa2d6cdbdf887581d4/Assets/Scripts/Player/Character.cs#L2)
**Motor y Lenguaje:** Unity / C#
**Arquitectura:** CharacterController nativo de Unity + Máquina de Estados Jerárquica (State Machine Pattern) + New Input System (PlayerInput)

### Desglose e Implementación

**A. Arquitectura por Máquina de Estados (State Machine)**
- **Organización:** en lugar de poner todo el código de movimiento en un único `Update()`, el script actúa como el contexto central e inicializa estados separados en clases independientes (StandingState, JumpingState, DashState, CrouchingState, LandingState, DamageState, AttackState, etc.).
- **Flujo de Ejecución:** en cada frame delega las responsabilidades a `movementSM.currentState.HandleInput()`, `movementSM.currentState.LogicUpdate()` y en `FixedUpdate()` a `movementSM.currentState.PhysicsUpdate()`. Esto evita variables booleanas anidadas difíciles de mantener.

**B. Sistema de Entrada (New Input System)**
- **Uso de PlayerInput:** utiliza el paquete moderno de Unity (`UnityEngine.InputSystem`), lo que facilita la lectura de periféricos tanto analógicos como digitales.

**C. Parámetros de Locomoción (Caminar, Sprint, Agacharse)**
- **Velocidades independientes:** configura `playerSpeed = 5.0f`, `sprintSpeed = 7.0f` y `crouchSpeed = 2.0f`.
- **Suavizado (Damping):** expone rangos normalizados (`[Range(0, 1)]`) para speedDampTime, velocityDampTime, rotationDampTime y control en el aire (`airControl = 0.5f`), optimizando la respuesta del Animator y el movimiento.

**D. Salto, Aterrizaje y Gravedad**
- **Cálculo de Gravedad:** ajusta la gravedad base multiplicándola por un factor de escala (`gravityValue *= gravityMultiplier`).
- **Manejo de estados aéreos:** desacopla el impulso (jumping) del momento de caída y contacto con el suelo (landing), permitiendo configurar un retardo de aterrizaje (`LandDelay = 0.75f`) y distancia de detección de suelo (`GroundCheckDisnatce = 0.3f`).

**E. Dash con Consumo de Recurso**
- **Parámetros:** configura la velocidad del impulso (`DashSpeed`), la duración del estado (`DashTime`) y el gasto de recurso (`DashCost`).

---

## Referencia 3: Controlador Basado en Físicas (Rigidbody) y Dash por Impulso

**Archivo analizado:** CharacterMovement.cs
**Repositorio de origen:** SolarFlareStudios (por auspiciousArtifice)
**Enlace:** [CharacterMovement.cs — SolarFlare/Assets/Scripts/Character](https://github.com/auspiciousArtifice/SolarFlareStudios/blob/1126c8601f53d7ab7e51cc269357d34d60bade22/SolarFlare/Assets/Scripts/Character/CharacterMovement.cs#L4)
**Motor y Lenguaje:** Unity / C#
**Arquitectura:** Movimiento basado en físicas reales y fuerzas (Rigidbody + Collider + ForceMode.Impulse)

### Desglose e Implementación

**A. Movimiento 3D y Normalización de Entrada**
- **Mapeo y Normalización:** lee los ejes crudos mediante `Input.GetAxisRaw("Horizontal")` y Vertical. Aplica un cálculo de mapeo elíptico para evitar que el movimiento diagonal duplique la velocidad (`h * sqrt(1 - 0.5 * v²)`).
- **Fuerza en el Aire y Multiplicadores:** en `FixedUpdate()`, si el jugador está en el aire, calcula la dirección perpendicular a la cámara con producto cruz (`Vector3.Cross`) y empuja al cuerpo con `m_rigidbody.AddForce(...)` escalado por `airbornSpeedMult`.

**B. Dash por Impulso Físico**
- **Implementación:** el método `Dash()` toma la dirección frontal de la cámara (`mainCamera.transform.forward.normalized`) y aplica un impulso instantáneo real al Rigidbody mediante `m_rigidbody.AddForce(dashDirection * DashDistance, ForceMode.Impulse)`.
- **Cargas de Dash:** limita los usos a través de un contador (`maxDashes`, `dashesLeft`), el cual se resta al dashear y se recarga automáticamente cuando el personaje vuelve a tocar el suelo en `OnCollisionEnter()`.

**C. Detección de Suelo y Animaciones Dinámicas**
- **Eventos de Colisión:** a diferencia del Raycast clásico, detecta el suelo mediante callbacks físicos (`OnCollisionEnter` y `OnCollisionExit`) verificando la etiqueta `collision.gameObject.tag == "ground"`.
- **Cambio Dinámico de Controlador de Animación:** alterna entre dos Animator Controllers completos en tiempo de ejecución (`ground_animator` y `air_animator`) para activar o desactivar el Root Motion según si el jugador está en el suelo o en el aire.

---

## Referencia 4: Controlador Modular 3D con Coyote Time, Jump Buffer e i-Frames por Layers

**Archivo analizado:** Character.cs
**Repositorio de origen:** CapstoneProject (por Meki2908)
**Enlace:** [Character.cs — Assets/Main Scripts/Player/New Character](https://github.com/Meki2908/CapstoneProject/blob/d4c0992b2835d8e9533d0facae0cd72b55c3d9a5/Assets/Main%20Scripts/Player/Scripts/Main%20Scripts/New%20Character/Character.cs#L2)
**Motor y Lenguaje:** Unity / C#
**Arquitectura:** CharacterController 3D + Máquina de Estados (State Machine) + New Input System (PlayerInput)

### Desglose e Implementación

**A. Game Feel Avanzado de Salto (Coyote Time & Jump Buffer)**
- **Jump Buffer** (`jumpBufferDuration = 0.12f`): registra si el jugador presionó el botón de salto una fracción de segundo antes de tocar el suelo (`UpdateGroundedAndJumpBuffer()`), ejecutando el salto automáticamente al aterrizar.
- **Coyote Time** (`coyoteTime = 0.08f`): permite saltar aunque el jugador acabe de abandonar el borde de una plataforma, evitando caídas injustas.
- **Detección de Suelo por Doble Raycast** (`ComputeGroundedFeetRays`): lanza dos rayos hacia abajo desde ambos extremos de los pies en lugar de depender únicamente de `controller.isGrounded`, eliminando el parpadeo en superficies irregulares.
- **Cooldown Antispam** (`jumpCooldownSeconds = 0.32f`): evita la acumulación de impulsos en el aire al presionar repetidamente el botón.

**B. Sistema Completo de Dash con Encadenamiento**
- **Parámetros:** configura duración (`dashDuration = 0.2f`), velocidad (`dashSpeed = 10.0f`), límite de encadenamiento (`maxConsecutiveDashes = 2`), cooldown estándar (`dashCooldown = 1f`) y penalización por agotar cargas (`dashChainCooldown = 2.5f`).
- **Prevención de Bugs:** incluye una marca de tiempo (`dashLockUntil`) para evitar que el personaje ejecute un dash automático involuntario justo después de recibir daño.

**C. Cuadros de Invulnerabilidad (i-Frames) y Traspaso Físico**
- **Eventos de Animación** (`AE_EnableDashInvincibility` / `AE_DisableDashInvincibility`): sincroniza la invulnerabilidad exactamente con los fotogramas de la animación 3D.
- **Cambio Recursivo de Capas:** durante el dash, cambia temporalmente el Layer del jugador y sus hijos a `Player_Dashing` y configura `controller.excludeLayers` para ignorar hurtboxes y enemigos, permitiendo atravesar hordas sin recibir daño ni quedar atascado.

**D. Multiplicadores de Velocidad Modulares**
- **Cálculo Desacoplado:** almacena velocidades base (`basePlayerSpeed`, `baseSprintSpeed`, `baseDashSpeed`) y calcula modificaciones porcentuales aditivas (`UpdateSpeedWithGems()`) vinculadas a eventos de armas y equipamiento.

---

## CONCLUSIÓN GENERAL Y RECOMENDACIÓN TÉCNICA

**Resumen de la Investigación:** se evaluaron diferentes enfoques técnicos para la implementación de las mecánicas de movimiento (caminar, sprint, salto, dash y control aéreo) requeridas para El Último Pilar:

- **Referencia 1 (samuelfenton/Faber):** aportó soluciones matemáticas sólidas para el suavizado de inercia/frenado y la reducción de maniobrabilidad en el aire, además de corrutinas para tiempos de invulnerabilidad post-daño. Su limitación principal es que corre sobre un sistema de rieles 2.5D (SplinePhysics).
- **Referencia 2 (Leaf-Game-Dev/Tamashi-The-Brave-Ninja):** validó la integración del New Input System (PlayerInput) con una arquitectura modular de Máquina de Estados (State Machine Pattern) y el uso de CharacterController nativo de Unity.
- **Referencia 3 (auspiciousArtifice/SolarFlareStudios):** demostró la aplicación de impulsos físicos directos (`ForceMode.Impulse` en Rigidbody) para el dash y control vectorial perpendicular a la cámara, de gran utilidad para las interacciones con la gravedad y empujes.
- **Referencia 4 (Meki2908/CapstoneProject):** resultó ser la solución más completa, profesional y alineada a los requisitos de nuestro GDD.

**Propuesta de Arquitectura y Siguientes Pasos:** se propone adoptar como base principal la arquitectura de la **Referencia 4**, integrándola con las necesidades específicas del juego:

- **Control de Entrada y Multijugador:** uso estricto de `PlayerInput` (New Input System) para soportar sin fricción el cooperativo local de hasta 4 mandos.
- **Game Feel y Respuesta:** implementación de Coyote Time y Jump Buffer para garantizar un control responsivo y preciso durante el combate.
- **Mecánica de Dash y Fases de la Arena:** utilizar el sistema de encadenamiento de dashes con invulnerabilidad temporal (i-frames) mediante exclusión de Layers, permitiendo atravesar hordas del Enjambre. Asimismo, la estructura permite desacoplar los multiplicadores de velocidad y gravedad para responder a los estados del Pilar (zona de gravedad alterada) y a los pasivos de los defensores.
