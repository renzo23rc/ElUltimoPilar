using UnityEngine;
using System.Collections;

// Helper mono para que objetos sepan a qué pool pertenecen y auto-release
public class PooledObject : MonoBehaviour
{
    public string poolKey;
    private Coroutine releaseCo;

    public void ScheduleRelease(float delay)
    {
        if (releaseCo != null) StopCoroutine(releaseCo);
        releaseCo = StartCoroutine(ReleaseAfter(delay));
    }

    IEnumerator ReleaseAfter(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (PoolManager.Instance != null && !string.IsNullOrEmpty(poolKey))
            PoolManager.Instance.Release(poolKey, gameObject);
        else
            Destroy(gameObject);
    }

    void OnDisable()
    {
        if (releaseCo != null)
        {
            StopCoroutine(releaseCo);
            releaseCo = null;
        }
    }
}
