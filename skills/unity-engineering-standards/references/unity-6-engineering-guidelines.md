# Unity 6 Engineering Guidelines

Use this reference as supporting context for work in the live `Unity/` project. It separates facts verified in this repository from recommendations for agents working on the project.

## Repository evidence (verified)

- `Unity/ProjectSettings/ProjectVersion.txt` pins the project to Editor `6000.3.22f1`.
- `Unity/Packages/manifest.json` is the live package manifest. It declares Input System `com.unity.inputsystem` `1.20.0` and Test Framework `com.unity.test-framework` `1.6.0`, among the other project packages.
- `Documentacion/arquitectura-de-codigo.md` requires incremental changes, English identifiers for new technical code, preservation of existing Spanish public APIs, named event handlers with balanced lifecycle subscription, and test evidence from the corresponding Unity runner.
- That architecture document describes input as an adapter translating player actions to public operations; pure model code does not read keyboard, mouse, or gamepad devices. It also distinguishes deterministic model tests from scene, event, spawner, and time integration tests.
- The repository also contains root-level `Packages/` and `ProjectSettings/` directories. They are not the authority for this skill: inspect the live project under `Unity/`.

## Actionable recommendations

### Project and compatibility

1. Read the relevant files under `Unity/` before changing code. Do not infer package or Editor facts from the root-level duplicate directories.
2. Preserve scenes, serialized references, existing public signatures, and Spanish public names unless the task explicitly authorizes a migration. Name new technical identifiers in English. Use the project C# toolchain without inventing a language-version pin.
3. Keep the first change small and easy to review. Prefer a focused component or adapter composed with existing behavior over speculative frameworks, DI containers, ECS, or production abstractions.

### Input and lifecycle

- For new input, add an action to the appropriate Input System asset/map and translate it at the Unity boundary into an existing or newly requested public operation. Do not force current direct device polling to migrate.
- Use named handlers for events. Subscribe once at the appropriate enable/start point and always unsubscribe at the matching disable/destroy point. Avoid anonymous lambdas when a later unsubscribe is required.
- Keep device, scene, and Inspector concerns out of deterministic pure logic.

### Inspector data and serialization

- Prefer private `[SerializeField]` fields for data that must be edited in the Inspector. Keep mutable state private and expose only the smallest needed operation or read-only view.
- Treat serialized fields as scene/prefab compatibility surfaces: rename or move them only with an explicit migration plan. Check references after Inspector changes rather than assuming serialization will repair them.

### Tests and evidence

- Put deterministic pure logic in EditMode tests, including invalid and terminal cases where applicable.
- Use PlayMode tests with `[UnityTest]` only when frames, time, scenes, `MonoBehaviour` lifecycle, or scene events materially affect behavior.
- Follow the architecture document's TDD sequence: RED, GREEN, TRIANGULATE, then REFACTOR. A passing claim requires evidence from the relevant Unity Test Framework runner; report unavailable runner evidence honestly.

### Performance

- Profile the observed behavior with the Unity Profiler before optimizing. Record the measured symptom and the changed path.
- Introduce pooling only after measurement shows a meaningful allocation or instantiation problem. Reset every reusable state on acquire, release subscriptions and transient references, and deactivate returned objects so pooled instances cannot continue participating in gameplay.

## Official documentation (general Unity 6/package guidance)

These official pages provide general Unity 6 or package guidance; they do not override the project's exact pin. The exact Editor pin comes from `Unity/ProjectSettings/ProjectVersion.txt`, and package versions come from `Unity/Packages/manifest.json`.

- [Input System actions](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.20/manual/Actions.html)
- [Test Framework introduction](https://docs.unity3d.com/6000.2/Documentation/Manual/test-framework/test-framework-introduction.html)
- [EditMode versus PlayMode tests](https://docs.unity3d.com/6000.2/Documentation/Manual/test-framework/edit-mode-vs-play-mode-tests.html)
- [Script serialization rules](https://docs.unity3d.com/6000.0/Documentation/Manual/script-serialization-rules.html)
- [Script serialization best practices](https://docs.unity3d.com/6000.0/Documentation/Manual/script-serialization-best-practices.html)
- [`SerializeField` Scripting API](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/SerializeField.html)
- [Naming and code style tips for C# scripting](https://unity.com/how-to/naming-and-code-style-tips-c-scripting-unity)
- [Profiler: profiling applications](https://docs.unity3d.com/6000.0/Documentation/Manual/profiler-profiling-applications.html)
- [Reusable code and object pooling](https://docs.unity3d.com/6000.0/Documentation/Manual/performance-reusable-code.html)

Deeper architecture guidance is intentionally deferred to the planned future skill path `skills/unity-code-architecture/SKILL.md`; that file is not part of this change.
