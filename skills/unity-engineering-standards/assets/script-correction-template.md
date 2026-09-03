# Single-Script Correction Template

Use for delegated correction tasks: one input script, one corrected output, judged against `skills/unity-engineering-standards/SKILL.md`.

## Input contract

The delegator supplies:

- Script path under `Unity/Assets/Editor` or `Unity/Assets/Scripts`.
- Whether public signatures, scenes, prefabs, or serialized fields may change.
- Whether Unity runner evidence is required.

## Audit checklist

1. Microsoft C# conventions: naming, aliases, `var` use, interpolation, exceptions, `using` placement, layout.
2. SOLID: single responsibility, extension without modifying callers, substitutable subclasses, small interfaces, abstractions over concrete types.
3. DRY/KISS/YAGNI: duplicated logic, overcomplicated constructs, speculative behavior.
4. Magic numbers: every gameplay, timing, range, chance, score, wave, UI, tag, layer, and path literal.
5. Modern C#: blocking calls, undisposed resources, unreadable collection loops.
6. Unity boundaries: input handling, lifecycle subscription, serialized compatibility, pure-model isolation.

## Output shape

```text
## Script correction: <path>

### Violations
- <rule>: <line or member> — <correction applied>

### Corrected content
<full corrected file or unified diff>

### Compatibility
- Public API: <preserved|changed with approval>
- Scenes/prefabs/serialization: <impact>

### Evidence
- Tests: <EditMode|PlayMode runner result or unavailable>
- Unresolved: <ambiguities or deferred work>
```
