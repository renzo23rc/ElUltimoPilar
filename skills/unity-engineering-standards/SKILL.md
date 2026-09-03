---
name: unity-engineering-standards
description: "Trigger: Unity engineering standards, Unity coding standards, Unity 6, C# Unity, Input System, Unity Test Framework, Unity profiling. Apply project conventions."
license: Apache-2.0
metadata:
  author: gentleman-programming
  version: "1.0"
---

## Activation Contract

Load for agent work in live `Unity/`: Unity 6, C#, input, serialization, performance, or tests.

## Hard Rules

- Target `Unity/`; ignore root `Packages/`/`ProjectSettings/`. Pin Editor `6000.3.22f1`; manifest versions are Input System `1.20.0` and Test Framework `1.6.0`. Use C# without inventing a language version.
- Keep solutions readable, teachable, incremental. Preserve APIs/scenes unless explicitly migrating; new identifiers are English; retain Spanish public APIs.
- Use Input System actions for new input; do not force polling migration. Prefer private `[SerializeField]` Inspector data, small components, named handlers, and safe lifecycle subscription/unsubscription.
- Profile first; pool only after measured need, with reset and deactivation. Avoid speculative frameworks, DI containers, ECS, and production abstractions.
- Use `Documentacion/arquitectura-de-codigo.md` for architecture and TDD/evidence guidance. Deeper guidance is planned at `skills/unity-code-architecture/SKILL.md`; do not create it now.
- Non-goals: gameplay redesign; forced Input System migration; moving/renaming scripts or public APIs; final HUD/audio/networking.

## Decision Gates

| Situation | Choose |
| --- | --- |
| Deterministic pure logic | EditMode tests |
| Frames, time, scenes, or `MonoBehaviour` behavior | PlayMode `[UnityTest]` |
| Performance concern | Profile first; pool only after evidence |
| Architecture ambiguity | Consult the architecture document |

## Execution Steps

1. Inspect live `Unity/`, APIs/scenes, manifest, and architecture guidance.
2. Make the smallest readable change through composition.
3. Select the test mode, follow RED/GREEN/TRIANGULATE/REFACTOR, and record runner evidence.
4. Report compatibility, evidence, assumptions, and risks.

## Output Contract

Return summary, paths, evidence, compatibility, and risks.

## References

- `references/unity-6-engineering-guidelines.md`
- `Documentacion/arquitectura-de-codigo.md`
