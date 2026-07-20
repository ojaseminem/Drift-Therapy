using System.Collections;
using UnityEngine;

/// <summary>
/// Plays a pooled one-shot spark burst at the contact point when the player
/// screeches along a fence. Uses the generic <see cref="ObjectPool"/> so
/// repeated screeches never Instantiate/Destroy at runtime.
/// </summary>
[DisallowMultipleComponent]
public class FenceSparkEffect : MonoBehaviour
{
    [SerializeField] GameObject sparkPrefab;

    ObjectPool pool;

    void Awake()
    {
        if (sparkPrefab != null) pool = new ObjectPool(sparkPrefab);
    }

    /// <summary>Assigns the spark prefab at runtime (e.g. when this component is added
    /// programmatically to a spawned vehicle instead of pre-wired on a prefab).</summary>
    public void SetSparkPrefab(GameObject prefab)
    {
        sparkPrefab = prefab;
        pool = prefab != null ? new ObjectPool(prefab) : null;
    }

    public void PlayAt(Vector3 worldPos, Vector3 contactNormal)
    {
        if (pool == null) return;

        Quaternion rot = contactNormal.sqrMagnitude > 0.001f
            ? Quaternion.LookRotation(contactNormal)
            : Quaternion.identity;

        GameObject go = pool.Get(worldPos, rot, null);
        var ps = go.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            ps.Play();
            go.GetComponent<ImpactFlash>()?.Play();
            StartCoroutine(ReleaseAfter(go, ps.main.duration + ps.main.startLifetime.constantMax));
        }
        else
        {
            pool.Release(go);
        }
    }

    IEnumerator ReleaseAfter(GameObject go, float seconds)
    {
        yield return new WaitForSeconds(seconds);
        pool.Release(go);
    }
}
