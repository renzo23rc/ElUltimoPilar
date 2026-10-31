using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

public sealed class PlayerPrefabAnimationTests
{
    private const string PrefabPath = "Assets/Resources/Prefabs/JugadorP";
    private const string ModelPath = "Assets/Resources/Models/Personajes/p";

    [TestCase(1, "PlayerLocomotion")]
    [TestCase(2, "PlayerP2Locomotion")]
    public void PlayerPrefabSerializesItsGameplayAndAnimatorReferences(int playerNumber, string controllerName)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath + playerNumber + ".prefab");
        Assert.That(prefab, Is.Not.Null);
        PlayerController player = prefab.GetComponent<PlayerController>();
        PlayerInput input = prefab.GetComponent<PlayerInput>();
        PlayerAnimatorDriver driver = prefab.GetComponent<PlayerAnimatorDriver>();
        Transform visual = prefab.transform.Find("JugadorVisual");
        Assert.That(player, Is.Not.Null);
        Assert.That(prefab.GetComponent<CharacterController>(), Is.Not.Null);
        Assert.That(prefab.GetComponent<WeaponSystem>(), Is.Not.Null);
        Assert.That(prefab.GetComponent<EnergySystem>(), Is.Not.Null);
        Assert.That(input, Is.Not.Null);
        Assert.That(input.actions, Is.Not.Null);
        Assert.That(player.camaraJugador, Is.Not.Null);
        Assert.That(player.puntoDisparo, Is.Not.Null);
        Assert.That(visual, Is.Not.Null);
        Assert.That(PrefabUtility.IsPartOfPrefabInstance(visual.gameObject), Is.True);

        Animator animator = visual.GetComponentInChildren<Animator>(true);
        Assert.That(animator, Is.Not.Null);
        Assert.That(animator.avatar, Is.Not.Null);
        Assert.That(animator.runtimeAnimatorController.name, Is.EqualTo(controllerName));
        Assert.That(animator.applyRootMotion, Is.False);
        Assert.That(animator.cullingMode, Is.EqualTo(AnimatorCullingMode.AlwaysAnimate));
        Assert.That(new SerializedObject(driver).FindProperty("animator").objectReferenceValue, Is.SameAs(animator));
    }

    [TestCase(1)]
    [TestCase(2)]
    public void BazookaDoesNotContainAnEnabledImportedCameraOrLight(int playerNumber)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath + playerNumber + ".prefab");
        Transform bazooka = null;
        foreach (Transform child in prefab.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == "bazooka")
            {
                bazooka = child;
                break;
            }
        }

        Assert.That(bazooka, Is.Not.Null);
        foreach (Camera extra in bazooka.GetComponentsInChildren<Camera>(true))
        {
            Assert.That(extra.enabled && extra.gameObject.activeSelf, Is.False,
                "The bazooka's imported camera must not render when the weapon becomes active.");
        }

        foreach (Light extra in bazooka.GetComponentsInChildren<Light>(true))
        {
            Assert.That(extra.enabled && extra.gameObject.activeSelf, Is.False,
                "The bazooka's imported light must not affect the scene.");
        }
    }

    [TestCase(1, "idle", "WalkForward", "RunForward", "RifleJump", "GrabRifle", "knife", "knife2")]
    [TestCase(2, "rig|iddle", "rig|walkrifle", "rig|runningrifle", "rig|jumprifle", "rig|agarrar el arma", "rig|knife1v2", "rig|knife3v2")]
    public void ControllerUsesItsOwnSelectedClips(
        int playerNumber, string idle, string walk, string run, string jump, string equip, string firstKnife, string secondKnife)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath + playerNumber + ".prefab");
        Animator animator = prefab.GetComponentInChildren<Animator>(true);
        var controller = (AnimatorController)animator.runtimeAnimatorController;
        AnimatorStateMachine machine = controller.layers[0].stateMachine;
        int matchedStates = 0;
        foreach (ChildAnimatorState child in machine.states)
        {
            AnimatorState state = child.state;
            if (state.name == "Locomotion")
            {
                var blend = (BlendTree)state.motion;
                Assert.That(blend.children.Length, Is.EqualTo(3));
                Assert.That(blend.children[0].motion.name, Is.EqualTo(idle));
                Assert.That(blend.children[1].motion.name, Is.EqualTo(walk));
                Assert.That(blend.children[2].motion.name, Is.EqualTo(run));
                Assert.That(blend.children[0].threshold, Is.LessThan(blend.children[1].threshold));
                Assert.That(blend.children[1].threshold, Is.LessThan(blend.children[2].threshold));
                matchedStates++;
            }
            else if (state.name == "Jump")
            {
                Assert.That(state.motion.name, Is.EqualTo(jump));
                matchedStates++;
            }
            else if (state.name == "Equip")
            {
                Assert.That(state.motion.name, Is.EqualTo(equip));
                matchedStates++;
            }
            else if (state.name == "Knife1")
            {
                Assert.That(state.motion.name, Is.EqualTo(firstKnife));
                matchedStates++;
            }
            else if (state.name == "knife2")
            {
                Assert.That(state.motion.name, Is.EqualTo(secondKnife));
                matchedStates++;
            }
        }

        Assert.That(matchedStates, Is.EqualTo(5));
    }

    [TestCase(1, "idle")]
    [TestCase(1, "WalkForward")]
    [TestCase(1, "RunForward")]
    [TestCase(1, "RifleJump")]
    [TestCase(1, "GrabRifle")]
    [TestCase(1, "knife")]
    [TestCase(1, "knife2")]
    [TestCase(2, "rig|iddle")]
    [TestCase(2, "rig|walkrifle")]
    [TestCase(2, "rig|runningrifle")]
    [TestCase(2, "rig|jumprifle")]
    [TestCase(2, "rig|agarrar el arma")]
    [TestCase(2, "rig|knife1v2")]
    [TestCase(2, "rig|knife3v2")]
    public void NativeClipBindsAndMovesItsOwnRig(int playerNumber, string clipName)
    {
        string path = ModelPath + playerNumber + ".fbx";
        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        AnimationClip clip = FindClip(path, clipName);
        Assert.That(source, Is.Not.Null);
        Assert.That(clip, Is.Not.Null);

        GameObject model = Object.Instantiate(source);
        try
        {
            EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(clip);
            Assert.That(bindings, Is.Not.Empty);
            foreach (EditorCurveBinding binding in bindings)
            {
                Assert.That(AnimationUtility.GetAnimatedObject(model, binding), Is.Not.Null,
                    $"Unbound {clipName} path: {binding.path}");
            }

            Transform[] bones = model.GetComponentsInChildren<Transform>(true);
            clip.SampleAnimation(model, 0f);
            Quaternion[] before = new Quaternion[bones.Length];
            for (int index = 0; index < bones.Length; index++)
            {
                before[index] = bones[index].localRotation;
            }

            clip.SampleAnimation(model, Mathf.Min(clip.length * 0.4f, 0.5f));
            int movedBones = 0;
            for (int index = 0; index < bones.Length; index++)
            {
                if (Quaternion.Angle(before[index], bones[index].localRotation) > 0.05f)
                {
                    movedBones++;
                }
            }

            Assert.That(movedBones, Is.GreaterThan(3), $"{clipName} must animate the rig.");
        }
        finally
        {
            Object.DestroyImmediate(model);
        }
    }

    [UnityTest]
    public IEnumerator EachAnimatorTransitionsThroughBothAttacksAndEquip()
    {
        yield return new EnterPlayMode();
        foreach (int playerNumber in new[] { 1, 2 })
        {
            string path = ModelPath + playerNumber + ".fbx";
            GameObject model = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));
            try
            {
                Animator animator = model.GetComponentInChildren<Animator>(true);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath + playerNumber + ".prefab");
                animator.runtimeAnimatorController = prefab.GetComponentInChildren<Animator>(true).runtimeAnimatorController;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.Rebind();
                animator.Update(0f);
                animator.SetBool("Grounded", true);
                animator.SetBool("Floating", false);
                animator.SetBool("Downed", false);

                animator.SetInteger("Swing", 0);
                animator.SetTrigger("Knife1");
                animator.Update(0.2f);
                animator.Update(0.2f);
                Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName("Knife1"), Is.True,
                    $"P{playerNumber} must enter the first attack.");

                animator.SetInteger("Swing", 1);
                animator.SetTrigger("Knife1");
                animator.Update(0.2f);
                animator.Update(0.2f);
                Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName("knife2"), Is.True,
                    $"P{playerNumber} must enter the second attack.");

                animator.SetTrigger("Equip");
                animator.Update(0.2f);
                animator.Update(0.2f);
                Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName("Equip"), Is.True,
                    $"P{playerNumber} must enter the equip animation.");

                animator.Update(1f);
                float previousProgress = animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
                animator.SetTrigger("Equip");
                animator.Update(0.2f);
                animator.Update(0.2f);
                AnimatorStateInfo repeatedEquip = animator.GetCurrentAnimatorStateInfo(0);
                Assert.That(repeatedEquip.IsName("Equip"), Is.True);
                Assert.That(repeatedEquip.normalizedTime, Is.LessThan(previousProgress),
                    $"P{playerNumber} must restart equip immediately rather than replaying it later.");

                animator.SetBool("Grounded", false);
                animator.Update(0.2f);
                animator.Update(0.2f);
                Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName("Jump"), Is.True,
                    $"P{playerNumber} must leave equip for the jump animation.");

                animator.SetBool("Floating", true);
                animator.Update(0.2f);
                animator.Update(0.2f);
                animator.Update(0.2f);
                Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName("Floating"), Is.True,
                    $"P{playerNumber} must enter the gravity-zone animation.");
            }
            finally
            {
                Object.Destroy(model);
            }
        }

        yield return null;
        yield return new ExitPlayMode();
    }

    private static AnimationClip FindClip(string path, string name)
    {
        foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
        {
            if (asset is AnimationClip clip && clip.name == name)
            {
                return clip;
            }
        }

        return null;
    }
}
