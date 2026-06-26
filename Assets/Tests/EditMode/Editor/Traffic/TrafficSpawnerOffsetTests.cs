using NUnit.Framework;
using UnityEngine;

public class TrafficSpawnerOffsetTests
{
    GameObject root;
    TrafficSpawner spawner;

    [SetUp]
    public void SetUp()
    {
        root = new GameObject("TrafficSpawnerRoot");
        spawner = root.AddComponent<TrafficSpawner>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(root);
    }

    [Test]
    public void ChooseLateralOffset_stays_within_road_bounds()
    {
        for (int i = 0; i < 128; i++)
        {
            float offset = spawner.ChooseLateralOffset(3.5f);
            Assert.That(offset, Is.InRange(-3.5f, 3.5f));
        }
    }

    [Test]
    public void ChooseLateralOffset_avoids_reserved_center_lane_when_width_allows()
    {
        spawner.CenterSafetyHalfWidth = 0.75f;
        spawner.EdgePadding = 0.25f;

        for (int i = 0; i < 128; i++)
        {
            float offset = spawner.ChooseLateralOffset(3f);
            Assert.That(Mathf.Abs(offset), Is.GreaterThanOrEqualTo(0.75f));
            Assert.That(offset, Is.InRange(-2.75f, 2.75f));
        }
    }

    [Test]
    public void ChooseLateralOffset_allows_center_when_road_is_too_narrow()
    {
        spawner.CenterSafetyHalfWidth = 1f;
        spawner.EdgePadding = 0.25f;

        float offset = spawner.ChooseLateralOffset(0.6f);
        Assert.That(offset, Is.InRange(-0.35f, 0.35f));
    }
}
