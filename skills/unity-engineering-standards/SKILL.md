---
name: unity-engineering-standards
description: "Trigger: Unity C# scripting, Unity coding conventions, SOLID clean code, magic numbers, script review correction, Editor Scripts. Review and fix Unity scripting per Microsoft C# conventions."
license: Apache-2.0
metadata:
  author: gentleman-programming
  version: "2.0"
---

## Activation Contract

Load before any read, write, review, test, or correction in `Unity/Assets/Editor`, `Unity/Assets/Scripts`, or scripting-related `Unity/Assets/Tests`. Apply to single-script correction delegations. Do not use for gameplay redesign or asset production.

## Hard Rules

- Target live `Unity/`; ignore root `Packages/`/`ProjectSettings/`. Pin Editor `6000.3.22f1`; Input System `1.20.0`, Test Framework `1.6.0`. Never invent language or package versions.
- Preserve scenes, prefabs, serialized references, and existing public signatures. Keep existing Spanish public APIs; name new technical identifiers in English. One public type per file, filename matches type name.
- Follow Microsoft C# conventions: PascalCase types/methods/properties/events, camelCase locals/params, `string`/`int` aliases over runtime names, `var` only when the type is obvious from the right side, string interpolation, collection expressions, specific exceptions (never bare `System.Exception`), `&&`/`||`, `using` outside namespace, 4 spaces with Allman braces, one statement and one declaration per line.
- Apply SOLID scaled to Unity: SRP via small components with one reason to change; OCP via composition/interfaces, not by editing stable callers; LSP substitutable subclasses; ISP small interfaces; DIP with pure model depending on abstractions via constructor or method injection. No DI containers, ECS, or speculative frameworks.
- Apply DRY/KISS/YAGNI: extract real duplication into methods or extension helpers, keep the simplest readable solution, implement only requested behavior.
- Eliminate magic numbers: no raw gameplay, balance, timing, range, chance, or UI literals in logic. Put tunable values in private `[SerializeField]` fields, config assets, or named `const` values with units. Pure model types receive config through constructors or methods and never hardcode balance.
- Use modern C# idiomatically: `async`/`await` for I/O-bound work without blocking the main thread; `using`/`IDisposable` for unmanaged resources; LINQ for readable collection logic with meaningful query names.
- Organize by feature: place new scripts per `references/unity-script-organization.md`. Never move or rename existing scripts without explicit migration approval; every move travels with its `.meta` file, without renames, one folder per slice.
- Keep Unity boundaries clean: new input uses Input System actions translated by adapters; device, scene, and Inspector concerns stay out of pure logic; event handlers are named with matched subscribe/unsubscribe; mutable state stays private behind the smallest public operation.
- Profile before optimizing; pool only after measured allocation or instantiation evidence, with full reset and deactivation. Follow RED/GREEN/TRIANGULATE/REFACTOR and report the Unity runner evidence used.
- Non-goals: gameplay redesign; forced input migration; moving/renaming scripts or public APIs without migration approval; final HUD/audio/networking.

## Decision Gates

| Situation | Choose |
| --- | --- |
| Deterministic pure logic | EditMode test |
| Frames, time, scenes, or `MonoBehaviour` behavior | PlayMode `[UnityTest]` |
| Raw literal controls gameplay, balance, timing, or chance | Named `const`, config asset, or `[SerializeField]` |
| Logic needs Unity APIs versus pure rules | Adapter versus pure model |
| One script supplied for correction | Audit and rewrite through `assets/script-correction-template.md` |
| Performance concern | Profile first; pool only after evidence |
| Architecture ambiguity | Consult `Documentacion/arquitectura-de-codigo.md` and `references/csharp-clean-code.md` |
| New script placement or folder move | Follow `references/unity-script-organization.md`; moves need approval |

## Execution Steps

1. Inspect the target script, its callers, serialized usages, manifest pins, and architecture guidance.
2. Audit conventions, SOLID, DRY/KISS/YAGNI, magic numbers, resource handling, and Unity boundaries.
3. Make the smallest readable correction through composition; preserve compatibility unless migration is authorized.
4. Select the test mode, follow TDD, and record runner evidence or report it as unavailable.
5. Report compatibility, evidence, assumptions, and risks.

## Output Contract

Return summary, changed paths, test evidence, compatibility notes, and risks. For single-script corrections also return the violations table, the corrected file content or diff, and unresolved ambiguities.

## References

- `references/unity-6-engineering-guidelines.md`
- `references/csharp-clean-code.md`
- `references/unity-script-organization.md`
- `assets/script-correction-template.md`
- `Documentacion/arquitectura-de-codigo.md`
