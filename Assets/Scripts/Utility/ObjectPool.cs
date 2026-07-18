using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Minimal reusable GameObject pool. Avoids runtime Instantiate/Destroy churn,
/// which is especially costly for WebGL builds (GC pauses are more visible there).
/// </summary>
public class ObjectPool
{
    readonly GameObject prefab;
    readonly Queue<GameObject> free = new Queue<GameObject>();

    public ObjectPool(GameObject prefab)
    {
        this.prefab = prefab;
    }

    public GameObject Get(Vector3 position, Quaternion rotation, Transform parent)
    {
        GameObject go = free.Count > 0 ? free.Dequeue() : Object.Instantiate(prefab);
        go.transform.SetParent(parent, false);
        go.transform.SetPositionAndRotation(position, rotation);
        go.SetActive(true);
        return go;
    }

    public void Release(GameObject go)
    {
        go.SetActive(false);
        go.transform.SetParent(null, false);
        free.Enqueue(go);
    }
}
