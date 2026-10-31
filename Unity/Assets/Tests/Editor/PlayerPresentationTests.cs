using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

public sealed class PlayerPresentationTests
{
    private const string SecondaryModelPath = "Assets/Resources/Models/Personajes/p2.fbx";
    private const string AnimatorControllerPath = "Assets/Resources/Animation/PlayerLocomotion.controller";
    private const string WeaponResourcePath = "Models/Armas/bazooka/bazooka";
    private const string WeaponObjectName = "bazooka";
    private const float AnimationSampleSeconds = 0.35f;
    private const float MinimumPoseChangeMeters = 0.000001f;
    private const float MinimumPoseChangeDegrees = 0.01f;
    private const float MinimumGripOffsetMeters = 0.001f;
    private const float WeaponAxisToleranceDegrees = 1f;
    private const float MinimumWeaponLengthToBodyHeight = 0.2f;
    private const float MaximumWeaponLengthToBodyHeight = 0.8f;

    private GameObject playerObject;
    private GameObject templateObject;
    private InputActionAsset inputActions;

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        if (templateObject != null)
        {
            Object.Destroy(templateObject);
        }

        if (playerObject != null)
        {
            Object.Destroy(playerObject);
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
    public IEnumerator P1NativeAnimationsResolveAndMoveTheSkinnedMesh()
    {
        yield return new EnterPlayMode();
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(AnimatorControllerPath);
        Assert.That(controller, Is.Not.Null, "The P1 locomotion controller must load.");
        Assert.That(HasResolvedClip(controller), Is.True, "Every controller motion must resolve to a clip.");
        int loopingClips = 0;
        foreach (AnimationClip clip in controller.animationClips)
        {
            if (clip.isLooping)
            {
                loopingClips++;
            }
        }

        Assert.That(loopingClips, Is.GreaterThanOrEqualTo(3), "Idle, walking and running must loop.");
        PlayerController player = CreatePrimary();
        Transform visual = player.transform.Find("JugadorVisual");
        Animator animator = visual.GetComponentInChildren<Animator>(true);
        Assert.That(animator, Is.Not.Null, "P1 must have a skeletal Animator.");
        Assert.That(animator.runtimeAnimatorController, Is.SameAs(controller));

        SkinnedMeshRenderer renderer = visual.GetComponentInChildren<SkinnedMeshRenderer>(true);
        Assert.That(renderer, Is.Not.Null, "The P1 visual must include a skinned mesh.");
        string[] requiredBones =
        {
            "DEF-spine", "DEF-spine.002", "DEF-upper_arm.L", "DEF-upper_arm.R",
            "DEF-hand.R", "DEF-thigh.L", "DEF-thigh.R"
        };
        Transform[] bones = new Transform[requiredBones.Length];
        Vector3[] initialPositions = new Vector3[requiredBones.Length];
        Quaternion[] initialRotations = new Quaternion[requiredBones.Length];
        animator.Rebind();
        animator.Update(0f);
        for (int index = 0; index < requiredBones.Length; index++)
        {
            bones[index] = FindChildByName(visual, requiredBones[index]);
            Assert.That(bones[index], Is.Not.Null, $"Missing P1 bone: {requiredBones[index]}.");
            initialPositions[index] = bones[index].localPosition;
            initialRotations[index] = bones[index].localRotation;
        }

        Mesh beforeMesh = new Mesh();
        Mesh afterMesh = new Mesh();
        try
        {
            renderer.BakeMesh(beforeMesh);
            Vector3[] initialVertices = beforeMesh.vertices;
            animator.Play("Base Layer.Locomotion", 0, 0.2f);
            animator.SetFloat("Speed", 8f);
            animator.Update(AnimationSampleSeconds);
            renderer.BakeMesh(afterMesh);

            Assert.That(CountChangedBones(bones, initialPositions, initialRotations), Is.GreaterThanOrEqualTo(3),
                "Animator evaluation must move multiple P1 body bones.");
            Assert.That(HasMeshPoseChanged(initialVertices, afterMesh.vertices), Is.True,
                "The animation must deform P1's rendered skinned mesh.");
        }
        finally
        {
            Object.DestroyImmediate(beforeMesh);
            Object.DestroyImmediate(afterMesh);
        }
    }

    [UnityTest]
    public IEnumerator BazookaUsesP1RightHandAndFollowsWeaponSelectionAndReset()
    {
        yield return new EnterPlayMode();
        PlayerController player = CreatePrimary();
        WeaponSystem weapons = player.GetComponent<WeaponSystem>();
        Transform visual = player.transform.Find("JugadorVisual");
        Transform rightHand = FindChildByName(visual, "DEF-hand.R");
        Transform bazooka = FindBazooka(player.transform);

        Assert.That(rightHand, Is.Not.Null, "P1's right hand must be available.");
        Assert.That(bazooka, Is.Not.Null, "The P1 bazooka resource must be instantiated.");
        Assert.That(bazooka.parent, Is.SameAs(rightHand), "The bazooka must be attached to P1's right hand.");
        Assert.That(bazooka.localPosition.sqrMagnitude, Is.GreaterThan(MinimumGripOffsetMeters * MinimumGripOffsetMeters),
            "The grip must use the measured palm and weapon bounds, not an origin placeholder.");
        AssertBazookaBarrelFacesForward(bazooka, visual);
        Assert.That(bazooka.gameObject.activeSelf, Is.False, "Directa is the initial weapon, so the bazooka starts hidden.");

        weapons.ConsumeCommand(new PlayerCommand(0f, 0f, 0f, 0f, weaponSlot: 2));
        Assert.That(bazooka.gameObject.activeSelf, Is.True);
        Bounds sourceBounds = GetLocalRenderBounds(Resources.Load<GameObject>(WeaponResourcePath).transform);
        float weaponLength = bazooka.TransformVector(Vector3.right * sourceBounds.size.x).magnitude;
        float bodyHeight = visual.GetComponentInChildren<SkinnedMeshRenderer>(true).bounds.size.y;
        Assert.That(weaponLength / bodyHeight,
            Is.InRange(MinimumWeaponLengthToBodyHeight, MaximumWeaponLengthToBodyHeight),
            $"The launcher must be proportionate to P1 (weapon={weaponLength}, body height={bodyHeight}).");

        weapons.CambiarArma(WeaponSystem.TipoArma.CuerpoACuerpo);
        Assert.That(bazooka.gameObject.activeSelf, Is.False);
        weapons.CambiarArma(WeaponSystem.TipoArma.Area);
        Assert.That(bazooka.gameObject.activeSelf, Is.True);
        weapons.CambiarArma(WeaponSystem.TipoArma.Directa);
        Assert.That(bazooka.gameObject.activeSelf, Is.False);

        weapons.CambiarArma(WeaponSystem.TipoArma.Area);
        Assert.That(bazooka.gameObject.activeSelf, Is.True);
        weapons.ResetState();
        Assert.That(weapons.armaEquipada, Is.EqualTo(WeaponSystem.TipoArma.Directa));
        Assert.That(bazooka.gameObject.activeSelf, Is.False);
    }

    [UnityTest]
    public IEnumerator P1WeaponVisibilityRemainsCorrectAcrossFrames()
    {
        yield return new EnterPlayMode();
        PlayerController player = CreatePrimary();
        WeaponSystem weapons = player.GetComponent<WeaponSystem>();
        Transform bazooka = FindBazooka(player.transform);
        Assert.That(bazooka.gameObject.activeSelf, Is.False, "The initial Directa state must be applied on activation.");

        yield return null;
        Assert.That(bazooka.gameObject.activeSelf, Is.False, "The first evaluated frame must keep Directa hidden.");

        weapons.CambiarArma(WeaponSystem.TipoArma.Area);
        Assert.That(bazooka.gameObject.activeSelf, Is.True);
        yield return null;
        Assert.That(bazooka.gameObject.activeSelf, Is.True, "The next frame must keep the selected Area weapon visible.");

        weapons.CambiarArma(WeaponSystem.TipoArma.Directa);
        yield return null;
        Assert.That(bazooka.gameObject.activeSelf, Is.False, "Directa must hide the bazooka on a later frame.");
    }

    [UnityTest]
    public IEnumerator ClonedJoinTemplatePreservesP2RigAndSelectionIsolation()
    {
        yield return new EnterPlayMode();
        GameObject p2Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SecondaryModelPath);
        Assert.That(p2Prefab, Is.Not.Null);
        Animator baselineP2Animator = p2Prefab.GetComponentInChildren<Animator>(true);
        Assert.That(baselineP2Animator, Is.Not.Null);

        PlayerController primary = CreatePrimary();
        primary.GetComponent<WeaponSystem>().CambiarArma(WeaponSystem.TipoArma.Area);
        primary.gameObject.SetActive(false);
        templateObject = TestScenePlayerBuilder.CreateJoinTemplate(primary).gameObject;
        primary.gameObject.SetActive(true);
        templateObject.SetActive(true);

        WeaponSystem primaryWeapons = primary.GetComponent<WeaponSystem>();
        WeaponSystem joinedWeapons = templateObject.GetComponent<WeaponSystem>();
        Animator joinedAnimator = templateObject.transform.Find("JugadorVisual").GetComponentInChildren<Animator>(true);
        Transform primaryBazooka = FindBazooka(primary.transform);
        Transform joinedBazooka = FindBazooka(templateObject.transform);

        Assert.That(joinedAnimator.avatar, Is.SameAs(baselineP2Animator.avatar),
            "P2 must retain its FBX-imported avatar.");
        Assert.That(joinedAnimator.runtimeAnimatorController, Is.SameAs(Resources.Load<RuntimeAnimatorController>("Animation/PlayerLocomotion")),
            "P2 keeps its pre-existing locomotion controller assignment.");
        Assert.That(templateObject.GetComponent<PlayerAnimatorDriver>(), Is.Not.Null,
            "P2 keeps the existing Animator driver path.");
        Assert.That(primaryBazooka.gameObject.activeSelf, Is.True);
        Assert.That(joinedWeapons.armaEquipada, Is.EqualTo(WeaponSystem.TipoArma.Area));
        Assert.That(joinedBazooka.gameObject.activeSelf, Is.True);

        joinedWeapons.CambiarArma(WeaponSystem.TipoArma.Directa);
        Assert.That(joinedBazooka.gameObject.activeSelf, Is.False);
        Assert.That(primaryBazooka.gameObject.activeSelf, Is.True,
            "Changing the cloned player's selection must not change the primary player's presentation.");

        joinedWeapons.ResetState();
        Assert.That(joinedBazooka.gameObject.activeSelf, Is.False);
        primaryWeapons.ResetState();
        Assert.That(primaryBazooka.gameObject.activeSelf, Is.False);
    }

    private PlayerController CreatePrimary()
    {
        inputActions = ScriptableObject.CreateInstance<InputActionAsset>();
        inputActions.AddActionMap("Player");
        PlayerController player = TestScenePlayerBuilder.CreatePrimary(inputActions, null, null, Vector3.zero);
        playerObject = player.gameObject;
        playerObject.SetActive(true);
        return player;
    }

    private static Transform FindBazooka(Transform root)
    {
        return FindChildByName(root, WeaponObjectName);
    }

    private static Transform FindChildByName(Transform root, string name)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == name)
            {
                return child;
            }
        }

        return null;
    }

    private static bool HasResolvedClip(AnimatorController controller)
    {
        bool hasResolvedClip = false;
        foreach (AnimatorControllerLayer layer in controller.layers)
        {
            if (!HasResolvedMotion(layer.stateMachine, ref hasResolvedClip))
            {
                return false;
            }
        }

        return hasResolvedClip;
    }

    private static bool HasResolvedMotion(AnimatorStateMachine stateMachine, ref bool hasResolvedClip)
    {
        foreach (ChildAnimatorState childState in stateMachine.states)
        {
            Motion motion = childState.state.motion;
            if (motion == null && childState.state.name == "Empty")
            {
                continue;
            }

            if (!HasResolvedMotion(motion, ref hasResolvedClip))
            {
                return false;
            }
        }

        foreach (ChildAnimatorStateMachine childMachine in stateMachine.stateMachines)
        {
            if (!HasResolvedMotion(childMachine.stateMachine, ref hasResolvedClip))
            {
                return false;
            }
        }

        return true;
    }

    private static bool HasResolvedMotion(Motion motion, ref bool hasResolvedClip)
    {
        if (motion == null)
        {
            return false;
        }

        BlendTree blendTree = motion as BlendTree;
        if (blendTree == null)
        {
            hasResolvedClip = motion is AnimationClip;
            return hasResolvedClip;
        }

        foreach (ChildMotion childMotion in blendTree.children)
        {
            if (!HasResolvedMotion(childMotion.motion, ref hasResolvedClip))
            {
                return false;
            }
        }

        return true;
    }

    private static int CountChangedBones(
        Transform[] bones,
        Vector3[] initialPositions,
        Quaternion[] initialRotations)
    {
        int changedBones = 0;
        for (int index = 0; index < bones.Length; index++)
        {
            if ((bones[index].localPosition - initialPositions[index]).sqrMagnitude > MinimumPoseChangeMeters
                || Quaternion.Angle(bones[index].localRotation, initialRotations[index]) > MinimumPoseChangeDegrees)
            {
                changedBones++;
            }
        }

        return changedBones;
    }

    private static bool HasMeshPoseChanged(Vector3[] initialVertices, Vector3[] currentVertices)
    {
        if (initialVertices.Length != currentVertices.Length)
        {
            return true;
        }

        for (int index = 0; index < initialVertices.Length; index++)
        {
            if ((initialVertices[index] - currentVertices[index]).sqrMagnitude > MinimumPoseChangeMeters)
            {
                return true;
            }
        }

        return false;
    }

    private static void AssertBazookaBarrelFacesForward(Transform bazooka, Transform visual)
    {
        GameObject prefab = Resources.Load<GameObject>(WeaponResourcePath);
        Bounds localBounds = GetLocalRenderBounds(prefab.transform);
        Vector3 longestAxis = GetAxis(GetLongestAxisIndex(localBounds.size));
        float angle = Vector3.Angle(bazooka.TransformDirection(longestAxis), visual.forward);
        Assert.That(angle, Is.LessThan(WeaponAxisToleranceDegrees),
            "The bazooka's measured long mesh axis must point forward from the hand.");
    }

    private static Bounds GetLocalRenderBounds(Transform root)
    {
        bool hasBounds = false;
        Bounds bounds = default(Bounds);
        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            MeshFilter meshFilter = renderer.GetComponent<MeshFilter>();
            Mesh mesh = meshFilter != null ? meshFilter.sharedMesh : null;
            SkinnedMeshRenderer skinnedRenderer = renderer as SkinnedMeshRenderer;
            if (skinnedRenderer != null)
            {
                mesh = skinnedRenderer.sharedMesh;
            }

            if (mesh == null)
            {
                continue;
            }

            Bounds meshBounds = mesh.bounds;
            for (int cornerIndex = 0; cornerIndex < 8; cornerIndex++)
            {
                Vector3 corner = new Vector3(
                    (cornerIndex & 1) == 0 ? meshBounds.min.x : meshBounds.max.x,
                    (cornerIndex & 2) == 0 ? meshBounds.min.y : meshBounds.max.y,
                    (cornerIndex & 4) == 0 ? meshBounds.min.z : meshBounds.max.z);
                Vector3 rootCorner = root.InverseTransformPoint(renderer.transform.TransformPoint(corner));
                if (!hasBounds)
                {
                    bounds = new Bounds(rootCorner, Vector3.zero);
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(rootCorner);
                }
            }
        }

        Assert.That(hasBounds, Is.True, "The bazooka prefab must have measurable mesh bounds.");
        return bounds;
    }

    private static int GetLongestAxisIndex(Vector3 size)
    {
        if (size.x >= size.y && size.x >= size.z)
        {
            return 0;
        }

        return size.y >= size.z ? 1 : 2;
    }

    private static Vector3 GetAxis(int index)
    {
        if (index == 0)
        {
            return Vector3.right;
        }

        return index == 1 ? Vector3.up : Vector3.forward;
    }
}
