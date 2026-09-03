# C# Clean Code for Unity Scripting

Companion to `SKILL.md`. Applies Microsoft .NET C# conventions plus SOLID, DRY/KISS/YAGNI, and magic-number elimination, scaled to Unity constraints.

Source: `https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions`.

## Naming

| Element | Casing | Example |
| --- | --- | --- |
| Class, struct, enum, interface | PascalCase (`I` prefix for interfaces) | `MatchFlow`, `IDamageable` |
| Method, property, event | PascalCase | `Calculate`, `CurrentResult`, `OnMatchResult` |
| Local variable, parameter | camelCase | `remainingPercentage`, `attackRangeMeters` |
| Private field | camelCase, underscore prefix only to disambiguate | `_attackTimerSeconds` or `attackTimerSeconds` |
| Named constant | PascalCase | `AttackCooldownSeconds` |

New technical identifiers are English. Existing Spanish public APIs stay untouched for scene and consumer compatibility.

## File layout

- One public type per file; the filename matches the type (`ScorePolicy` lives in `ScorePolicy.cs`).
- `using` directives go outside the namespace. A `using` inside a namespace resolves against that namespace first and can break when a dependency adds a matching nested namespace.

Bad:

```csharp
namespace CoolStuff.AwesomeFeature
{
    using Azure;
}
```

Good:

```csharp
using Azure;

namespace CoolStuff.AwesomeFeature
{
}
```

- Prefer file-scoped namespaces for new pure-model files. Keep existing files namespace-free unless a migration is explicitly authorized; adding a namespace to an existing `MonoBehaviour` is allowed only with a serialization check.

```csharp
namespace UltimoPilar.Core.Match;

public sealed class ScorePolicy
{
}
```

- Four spaces, Allman braces (each brace on its own line), one statement per line, one declaration per line, blank line between members. Break long statements before binary operators and parenthesize mixed conditions:

```csharp
if ((startX > endX) && (startX > previousX))
{
    // Take appropriate action.
}
```

## Type safety and readability

- Use language aliases (`string`, `int`, `float`, `bool`), never runtime names (`System.String`, `System.Int32`).
- Use `var` only when the type is obvious from the right side (`new`, cast, literal, collection expression). Otherwise write the explicit type so reviewers without IDE hover can read the code.

```csharp
var roster = new PlayerRoster<PlayerController>(); // Obvious: keep var.
int playerCount = roster.Count; // Not obvious from the name alone: explicit type.
```

- Prefer string interpolation for short strings; use `StringBuilder` for text built in loops.
- Prefer collection expressions for initialization:

```csharp
string[] vowels = ["a", "e", "i", "o", "u"];
```

- Name LINQ query variables by meaning, filter with `where` before ordering or projecting, and rename ambiguous projected properties:

```csharp
var seattleCustomers = from customer in customers
                       where customer.City == "Seattle"
                       orderby customer.Name
                       select customer.Name;
```

## Exceptions and resources

- Catch only exceptions you can handle, with specific types. Never swallow bare `System.Exception` without a filter, and rethrow with `throw;` to preserve the stack.

```csharp
try
{
    return MatchResult.FromSnapshot(snapshot);
}
catch (ArgumentOutOfRangeException ex)
{
    Debug.LogError($"Invalid pilar snapshot: {ex.Message}");
    throw;
}
```

- Release unmanaged resources with `using` declarations or `IDisposable`; never rely on finalizers for gameplay objects:

```csharp
using Font normalStyle = new Font("Arial", 10.0f);
byte charset = normalStyle.GdiCharSet;
```

## SOLID in Unity

- SRP: one component, one reason to change. Split validation, scoring, spawning, presentation, and audio into separate types.
- OCP: extend through new components, subclasses, or interfaces. Do not edit stable callers to add variants.
- LSP: subclasses honor base contracts, including `IDamageable` behavior and `Enemy` dispatch semantics.
- ISP: small interfaces such as `IDamageable` and `IInputAdapter`. Never force unused members.
- DIP: pure model depends on abstractions. Pass config and collaborators through constructors or methods. `MonoBehaviour` adapters wire Unity objects at the boundary; the model never calls `FindFirstObjectByType`, scenes, or devices.

```csharp
// Pure model: config arrives by parameter, no balance literals inside.
public static int Calculate(float remainingPercentage)
{
}
```

## Hygiene

- DRY: extract duplicated gameplay math, cooldown handling, targeting, or formatting into methods or helpers after the second occurrence.
- KISS: choose the readable solution over clever generics, reflection, or premature optimization.
- YAGNI: implement the requested slice only. Leave deferred work named explicitly instead of scaffolding speculative systems.

## Modern language usage

- `async`/`await` only for genuinely I/O-bound work. Never block the Unity main thread with `.Result`, `.Wait()`, or busy loops.
- Use `Func<>`/`Action<>` for delegates unless a named delegate type materially improves readability.
- Document public members with XML comments; keep inline `//` comments on their own line, starting uppercase and ending with a period.

```csharp
/// <summary>
/// Converts the factual Pilar health percentage into the final integer score.
/// </summary>
public static int Calculate(float remainingPercentage)
```

## Magic numbers

Definition: any raw numeric or string literal that controls gameplay meaning, including damage, health, speed, range, cooldown, spawn timing, drop chance, score bounds, wave counts, UI thresholds, layer names, tag strings, and scene paths.

Rules:

1. No magic literals in logic branches, formulas, loops, or comparisons.
2. Tunable values become private `[SerializeField]` fields, config assets, or explicitly named constants including units, for example `AttackCooldownSeconds` or `MaxPlayers`.
3. Pure model files use `const` values or injected parameters only. They never contain scene-tuned balance.
4. Ranges stay with the field through `[Range]`, `Clamp`, or validation. Document units in the field name or Inspector header.
5. Repeated literals become one named source. Different meanings never share one constant.

Bad:

```csharp
if (distance < 2f) { Attack(); }
timer -= Time.deltaTime;
if (timer <= 0f) { timer = 1f; }
```

Good:

```csharp
private const float AttackRangeMeters = 2f;
private const float AttackCooldownSeconds = 1f;

if (distance < AttackRangeMeters) { Attack(); }
attackTimerSeconds -= Time.deltaTime;
if (attackTimerSeconds <= 0f) { attackTimerSeconds = AttackCooldownSeconds; }
```

Better for tunable balance: expose `attackRangeMeters` and `attackCooldownSeconds` as private `[SerializeField]` fields and keep limits validated.
