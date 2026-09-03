# Unity Script Organization Proposal

Status: proposal only. No file has been moved. This document maps the current flat `Unity/Assets/Scripts/Core/` folder (24 mixed files) to feature subfolders and states the migration risks. It becomes actionable only after explicit user approval.

## Why

One folder mixing audio, game flow, match rules, Pilar, player, combat, and infrastructure hides ownership and slows reviews. Feature folders make each slice findable and keep future extractions small.

## Proposed map

New scripts land directly in the matching folder. Existing files move only with their `.meta` files, without renames and without namespace changes.

| Folder | Files |
| --- | --- |
| `Core/Audio/` | `AudioAdapter.cs`, `CombatFeedback.cs` |
| `Core/Game/` | `GameManager.cs`, `TestSceneSetup.cs`, `Hud.cs` |
| `Core/Match/` | `MatchFlow.cs`, `MatchState.cs`, `MatchResult.cs`, `ScorePolicy.cs` |
| `Core/Pilar/` | `Pilar.cs`, `PilarHealthSnapshot.cs`, `Torreta.cs` |
| `Core/Player/` | `PlayerController.cs`, `PlayerCommand.cs`, `PlayerInputAdapter.cs`, `IInputAdapter.cs`, `PlayerJoinCoordinator.cs`, `PlayerRoster.cs`, `IPlayerRosterMember.cs`, `SplitScreenCameraCoordinator.cs` |
| `Core/Combat/` | `DamageRequest.cs`, `IDamageable.cs` |
| `Core/Shared/` | `PoolManager.cs`, `PooledObject.cs` |

`Enemies/`, `Weapons/`, `Arena/`, and `Editor/` stay as they are. `Enemies/` has several types but each enemy already owns its file, so no split is proposed there.

## Placement rules for new scripts

1. One public class, enum, or interface per file; filename matches the type.
2. Choose the folder by feature ownership (who changes together), not by technical kind. A player damage rule belongs with `Player/`, not with `Combat/`, when only player logic changes it.
3. Pure model files stay free of `UnityEngine`, scenes, and devices regardless of folder.
4. New pure-model files may use file-scoped namespaces; existing files keep their current namespace-free shape.

## Migration risks and approval gates

- Move files only inside the Unity Editor or with `git mv` keeping each `.cs` together with its `.cs.meta`. A move without its `.meta` changes the script GUID and breaks scene and prefab references.
- Never rename a class or file in the same step as a move; renames and moves ship separately so breakage is attributable.
- The project has no `asmdef` files (single default assembly), so folder moves do not change compilation visibility. EditMode tests under `Unity/Assets/Tests/Editor/` keep compiling as long as type names are unchanged.
- After approval, migrate one folder per slice: move, open the affected scenes, run the EditMode suite, and report runner evidence before continuing.
- Rollback is a plain `git mv` back while `.meta` files are intact; do not delete `.meta` files at any point.
