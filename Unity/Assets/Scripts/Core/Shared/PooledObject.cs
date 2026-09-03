using System.Collections;
using UnityEngine;

/// <summary>
/// Associates a pooled object with its pool and schedules its release.
/// </summary>
public class PooledObject : MonoBehaviour
{
    public string poolKey;
    private Coroutine releaseCo;

    /// <summary>
    /// Schedules this object for release after the specified delay.
    /// </summary>
    /// <param name="delay">The delay in seconds before release.</param>
    public void ScheduleRelease(float delay)
    {
        if (releaseCo != null)
        {
            StopCoroutine(releaseCo);
        }

        releaseCo = StartCoroutine(ReleaseAfter(delay));
    }

    private IEnumerator ReleaseAfter(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (PoolManager.Instance != null && !string.IsNullOrEmpty(poolKey))
        {
            PoolManager.Instance.Release(poolKey, gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDisable()
    {
        if (releaseCo != null)
        {
            StopCoroutine(releaseCo);
            releaseCo = null;
        }
    }
}
