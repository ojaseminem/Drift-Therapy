using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class TrafficPoolTests
{
    GameObject root;
    GameObject prefab;
    TrafficPool pool;

    [SetUp]
    public void SetUp()
    {
        root = new GameObject("TrafficPoolRoot");
        prefab = new GameObject("TrafficPrefab");
        prefab.SetActive(false);
        prefab.AddComponent<TrafficAgent>();

        pool = root.AddComponent<TrafficPool>();
        SetPrivateField(pool, "agentPrefab", prefab.GetComponent<TrafficAgent>());
        SetPrivateField(pool, "initialSize", 2);
        pool.Prewarm();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(root);
        Object.DestroyImmediate(prefab);
    }

    [Test]
    public void Acquire_returns_prewarmed_agents_before_growing()
    {
        var first = pool.Acquire();
        var second = pool.Acquire();

        Assert.That(first, Is.Not.Null);
        Assert.That(second, Is.Not.Null);
        Assert.That(first, Is.Not.SameAs(second));
        Assert.That(pool.FreeCount, Is.EqualTo(0));
        Assert.That(pool.ActiveCount, Is.EqualTo(2));
    }

    [Test]
    public void Release_deactivates_agent_and_makes_it_reusable()
    {
        var acquired = pool.Acquire();

        pool.Release(acquired);

        var reacquired = pool.Acquire();
        Assert.That(reacquired, Is.SameAs(acquired));
        Assert.That(reacquired.gameObject.activeSelf, Is.True);
    }

    static void SetPrivateField(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, $"Expected private field '{fieldName}' to exist.");
        field.SetValue(target, value);
    }
}
