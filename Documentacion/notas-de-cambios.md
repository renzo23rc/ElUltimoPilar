# Notas de cambios

> El proyecto no tenía un changelog: esta es la primera entrada. Se usa este archivo para anotar cambios chicos de debugging y arquitectura sin armar un sistema de versiones completo (YAGNI).

## 2026-10-30 — Materiales sin `.material` en caliente + `TestSceneSetup.componerEnRuntime`

- Materiales: se eliminó el uso de `renderer.material` por frame/en caliente en `PilarVisualPresenter`, `Pilar`, `Torreta`, `Explosive`, `WeaponVariantPickup`, `ZonaGravedadEffect` y `ArenaWarningPresenter`. El tinte persistente usa un único clon propio registrado con `OwnedMaterialCleanup`; el flash temporal sigue usando `RendererFlash` (property block). Comportamiento visual idéntico; APIs públicas en español intactas. Detalle por sitio en el reporte de la sesión (archivos y líneas en `git diff`).
- `TestSceneSetup`: nuevo `[SerializeField] private bool componerEnRuntime = true` (default arma todo como hoy). Con `false` no compone nada y verifica `GameManager`, `Pilar`, `EnemySpawner`, `PlayerController` y `ArenaTransform` en escena, con `Debug.LogError` nombrando cada faltante. Sin cambios a escenas `.unity`.
- Sin tests nuevos: no hay EditMode adyacente para comportamiento visual/`MonoBehaviour` (corresponde PlayMode, futuro). Pendiente correr `tools/run-editmode-tests.sh` con el Editor cerrado para validar que la suite existente sigue en verde.
