using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

public sealed class PlayerFirearmPresentationTests
{
    private const float TestTimeScale = 8f;
    private const float ReadyPoseSeconds = 3.25f;
    private const float ClipSeconds = 7.5f;
    private const float GripToleranceMeters = 0.04f;
    private const float AimToleranceDegrees = 15f;

    private GameObject primaryObject;
    private GameObject secondaryObject;
    private GameObject floor;
    private InputActionAsset inputActions;
    private int shots;

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        Time.timeScale = 1f;
        if (secondaryObject != null)
        {
            Object.Destroy(secondaryObject);
        }

        if (primaryObject != null)
        {
            Object.Destroy(primaryObject);
        }

        if (floor != null)
        {
            Object.Destroy(floor);
        }

        if (inputActions != null)
        {
            Object.Destroy(inputActions);
        }

        if (Application.isPlaying)
        {
            yield return null;
            yield return new ExitPlayMode();
        }
    }

    [UnityTest]
    public IEnumerator P1HoldsAndAimsBothWeaponsWhileSwitchingWithoutChangingP2()
    {
        yield return new EnterPlayMode();
        Time.timeScale = TestTimeScale;
        floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.transform.position = new Vector3(0f, -1.5f, 0f);
        floor.transform.localScale = new Vector3(30f, 1f, 30f);
        Physics.SyncTransforms();

        inputActions = ScriptableObject.CreateInstance<InputActionAsset>();
        inputActions.AddActionMap("Player");
        PlayerController player = TestScenePlayerBuilder.CreatePrimary(inputActions, null, null, Vector3.zero);
        primaryObject = player.gameObject;
        primaryObject.SetActive(true);
        secondaryObject = TestScenePlayerBuilder.CreateJoinTemplate(player).gameObject;
        secondaryObject.SetActive(true);
        AudioListener secondaryListener = secondaryObject.GetComponentInChildren<AudioListener>(true);
        if (secondaryListener != null)
        {
            secondaryListener.enabled = false;
        }

        WeaponSystem weapons = player.GetComponent<WeaponSystem>();
        WeaponSystem secondaryWeapons = secondaryObject.GetComponent<WeaponSystem>();
        PlayerWeaponSwitchCoordinator coordinator = player.GetComponent<PlayerWeaponSwitchCoordinator>();
        Animator animator = player.GetComponentInChildren<Animator>(true);
        Assert.That(coordinator.IsAvailable, Is.True);
        Assert.That(secondaryObject.GetComponent<PlayerWeaponSwitchCoordinator>(), Is.Null);
        yield return new WaitForSeconds(0.5f);

        float heldTime = animator.GetFloat("P1GrabTime");
        Assert.That(heldTime, Is.EqualTo(ReadyPoseSeconds / ClipSeconds).Within(0.001f));
        yield return new WaitForSeconds(1f);
        Assert.That(animator.GetFloat("P1GrabTime"), Is.EqualTo(heldTime).Within(0.001f));
        AssertReadyGrip(player, "rifleSupport", "directWeapon");

        shots = 0;
        weapons.OnDisparo += RecordShot;
        int rifleAmmo = weapons.armaDirecta.municionActual;
        weapons.CambiarArma(WeaponSystem.TipoArma.Area);
        Assert.That(weapons.armaEquipada, Is.EqualTo(WeaponSystem.TipoArma.Directa));
        Assert.That(coordinator.ActionBlocked, Is.True);
        weapons.DispararActual();
        Assert.That(weapons.armaDirecta.municionActual, Is.EqualTo(rifleAmmo));
        Assert.That(shots, Is.Zero);

        secondaryWeapons.CambiarArma(WeaponSystem.TipoArma.Area);
        Assert.That(secondaryWeapons.armaEquipada, Is.EqualTo(WeaponSystem.TipoArma.Area),
            "P2 retains its immediate weapon selection.");
        float switchDeadline = Time.realtimeSinceStartup + 15f;
        while (weapons.armaEquipada != WeaponSystem.TipoArma.Area && Time.realtimeSinceStartup < switchDeadline)
        {
            yield return null;
        }

        Assert.That(weapons.armaEquipada, Is.EqualTo(WeaponSystem.TipoArma.Area),
            $"Requested={coordinator.RequestedWeapon}, blocked={coordinator.ActionBlocked}, clip={animator.GetFloat("P1GrabTime")}, downed={player.IsDowned}");
        Assert.That(coordinator.ActionBlocked, Is.True, "The new firearm must finish drawing.");
        int areaAmmo = weapons.armaArea.municionActual;
        weapons.DispararActual();
        Assert.That(weapons.armaArea.municionActual, Is.EqualTo(areaAmmo));
        Assert.That(shots, Is.Zero);
        float readyDeadline = Time.realtimeSinceStartup + 15f;
        while (coordinator.ActionBlocked && Time.realtimeSinceStartup < readyDeadline)
        {
            yield return null;
        }

        Assert.That(coordinator.ActionBlocked, Is.False, "The bazooka draw must finish.");
        yield return WaitForRig(player, "areaSupport");
        AssertReadyGrip(player, "areaSupport", "areaWeapon");

        Transform holder = player.camaraJugador.transform.parent;
        holder.localRotation = Quaternion.Euler(30f, 0f, 0f);
        yield return new WaitForSeconds(0.35f);
        AssertReadyGrip(player, "areaSupport", "areaWeapon");
        holder.localRotation = Quaternion.Euler(-30f, 0f, 0f);
        yield return new WaitForSeconds(0.35f);
        AssertReadyGrip(player, "areaSupport", "areaWeapon");
        holder.localRotation = Quaternion.identity;

        weapons.CambiarArma(WeaponSystem.TipoArma.CuerpoACuerpo);
        Assert.That(coordinator.ActionBlocked, Is.True);
        float meleeDeadline = Time.realtimeSinceStartup + 15f;
        while (weapons.armaEquipada != WeaponSystem.TipoArma.CuerpoACuerpo && Time.realtimeSinceStartup < meleeDeadline)
        {
            yield return null;
        }

        Assert.That(weapons.armaEquipada, Is.EqualTo(WeaponSystem.TipoArma.CuerpoACuerpo));
        Assert.That(coordinator.ActionBlocked, Is.False);
        Assert.That(secondaryWeapons.armaEquipada, Is.EqualTo(WeaponSystem.TipoArma.Area));
    }

    private void RecordShot(WeaponSystem.Arma weapon)
    {
        shots++;
    }

    private static IEnumerator WaitForRig(PlayerController player, string supportField)
    {
        var rig = new SerializedObject(player.GetComponent<PlayerWeaponRigController>());
        var support = (TwoBoneIKConstraint)rig.FindProperty(supportField).objectReferenceValue;
        float deadline = Time.realtimeSinceStartup + 15f;
        while (support.weight < 0.99f && Time.realtimeSinceStartup < deadline)
        {
            yield return null;
        }

        Animator animator = player.GetComponentInChildren<Animator>(true);
        PlayerWeaponSwitchCoordinator coordinator = player.GetComponent<PlayerWeaponSwitchCoordinator>();
        Assert.That(support.weight, Is.GreaterThan(0.99f),
            $"Grounded={animator.GetBool("Grounded")}, ready={coordinator.FirearmReady}, downed={player.IsDowned}, gravity={player.enZonaGravedad}");
        yield return null;
    }

    private static void AssertReadyGrip(PlayerController player, string supportField, string weaponField)
    {
        var rig = new SerializedObject(player.GetComponent<PlayerWeaponRigController>());
        var support = (TwoBoneIKConstraint)rig.FindProperty(supportField).objectReferenceValue;
        var primary = (TwoBoneIKConstraint)rig.FindProperty("primaryHand").objectReferenceValue;
        var presenter = new SerializedObject(player.GetComponent<PlayerVisualPresenter>());
        var weapon = (GameObject)presenter.FindProperty(weaponField).objectReferenceValue;

        Assert.That(support.weight, Is.GreaterThan(0.99f));
        Assert.That(primary.weight, Is.GreaterThan(0.99f));
        Assert.That(Vector3.Distance(support.data.tip.position, support.data.target.position),
            Is.LessThan(GripToleranceMeters), "The supporting hand must reach its weapon grip.");
        Assert.That(Vector3.Distance(primary.data.tip.position, primary.data.target.position),
            Is.LessThan(GripToleranceMeters), "The primary hand must reach its target.");
        Assert.That(Vector3.Angle(weapon.transform.TransformDirection(Vector3.right), player.camaraJugador.transform.forward),
            Is.LessThan(AimToleranceDegrees), "The barrel must follow the camera within natural aim limits.");
    }
}
