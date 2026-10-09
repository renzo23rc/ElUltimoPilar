using UnityEngine;

public class ParticulaFlotante : MonoBehaviour
{
    private const float MinimumSpeed = 0.8f;
    private const float MaximumSpeed = 1.8f;
    private const float MinimumAmplitude = 0.15f;
    private const float MaximumAmplitude = 0.35f;
    private const float RandomOffsetMaximum = 6.28f;
    private const float YRotationDegreesPerSecond = 45f;
    private const float XRotationDegreesPerSecond = 30f;

    private Vector3 initialPosition;
    private float speed;
    private float amplitude;
    private float offset;

    private void Start()
    {
        initialPosition = transform.localPosition;
        speed = UnityEngine.Random.Range(MinimumSpeed, MaximumSpeed);
        amplitude = UnityEngine.Random.Range(MinimumAmplitude, MaximumAmplitude);
        offset = UnityEngine.Random.Range(0f, RandomOffsetMaximum);
    }

    private void Update()
    {
        transform.localPosition = initialPosition + Vector3.up * Mathf.Sin(Time.time * speed + offset) * amplitude;
        transform.Rotate(Vector3.up, YRotationDegreesPerSecond * Time.deltaTime);
        transform.Rotate(Vector3.right, XRotationDegreesPerSecond * Time.deltaTime);
    }
}
