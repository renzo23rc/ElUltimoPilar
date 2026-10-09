using UnityEngine;

namespace UltimoPilar.Core.Shared
{
    /// <summary>
    /// Animates a body that has no skeleton or clips: it breathes while idle and bounces, squashes and
    /// sways while moving. Put it on the moving root; the speed comes from the root's own displacement.
    /// </summary>
    public sealed class ProceduralLocomotionAnimator : MonoBehaviour
    {
        private const float DefaultReferenceSpeedMetersPerSecond = 5f;
        private const float SpeedSmoothingSharpness = 12f;

        [SerializeField] private Transform visual;
        [SerializeField, Min(0.01f)] private float referenceSpeedMetersPerSecond = DefaultReferenceSpeedMetersPerSecond;
        [SerializeField] private LocomotionPoseSettings settings = new();

        private LocomotionPoseModel model;
        private Vector3 baseScale;
        private Vector3 basePosition;
        private Quaternion baseRotation;
        private Vector3 lastRootPosition;
        private float smoothedSpeed;
        private bool visualIsRoot;

        /// <summary>Sets what to animate and the speed that counts as full effort.</summary>
        /// <param name="target">The body to animate; the root itself when it has no separate visual.</param>
        /// <param name="referenceSpeed">The speed, in meters per second, that counts as full effort.</param>
        public void Configure(Transform target, float referenceSpeed)
        {
            RestoreRestingPose();
            visual = target;
            referenceSpeedMetersPerSecond = referenceSpeed;
            Capture();
        }

        private void OnEnable()
        {
            Capture();
        }

        private void OnDisable()
        {
            RestoreRestingPose();
        }

        private void LateUpdate()
        {
            if (model == null || visual == null || Time.deltaTime <= 0f)
            {
                return;
            }

            Vector3 displacement = transform.position - lastRootPosition;
            lastRootPosition = transform.position;
            displacement.y = 0f;
            float speed = displacement.magnitude / Time.deltaTime;
            smoothedSpeed = Mathf.Lerp(smoothedSpeed, speed, 1f - Mathf.Exp(-SpeedSmoothingSharpness * Time.deltaTime));

            LocomotionPose pose = model.Advance(smoothedSpeed, Time.deltaTime);
            visual.localScale = Vector3.Scale(baseScale, new Vector3(pose.HorizontalScale, pose.VerticalScale, pose.HorizontalScale));

            // Un modelo que es el propio root lo mueven la física y la IA: solo se anima la escala.
            if (!visualIsRoot)
            {
                visual.localPosition = basePosition + (Vector3.up * pose.BobMeters);
                visual.localRotation = baseRotation * Quaternion.Euler(0f, 0f, pose.RollDegrees);
            }
        }

        private void Capture()
        {
            if (visual == null)
            {
                visual = transform;
            }

            visualIsRoot = visual == transform;
            baseScale = visual.localScale;
            basePosition = visual.localPosition;
            baseRotation = visual.localRotation;
            lastRootPosition = transform.position;
            smoothedSpeed = 0f;
            model = new LocomotionPoseModel(settings, referenceSpeedMetersPerSecond);
        }

        private void RestoreRestingPose()
        {
            if (model == null || visual == null)
            {
                return;
            }

            visual.localScale = baseScale;
            if (!visualIsRoot)
            {
                visual.localPosition = basePosition;
                visual.localRotation = baseRotation;
            }
        }
    }
}
