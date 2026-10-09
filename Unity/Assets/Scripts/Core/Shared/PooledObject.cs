using System.Collections;
using UnityEngine;

/// <summary>
/// Associates a pooled object with its pool and schedules its release.
/// </summary>
public class PooledObject : MonoBehaviour
{
    /// <summary>The key of the pool this object returns to.</summary>
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
        PoolManager.ReleaseOrDestroy(gameObject);
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
