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
    public void LaneCount_is_even_and_at_least_two()
    {
        foreach (float halfWidth in new[] { 3.5f, 7f, 14f, 1.5f, 10f })
        {
            int lanes = spawner.LaneCount(halfWidth);
            Assert.That(lanes, Is.GreaterThanOrEqualTo(2), $"halfWidth {halfWidth}");
            Assert.That(lanes % 2, Is.EqualTo(0), $"halfWidth {halfWidth} lanes {lanes}");
        }
    }

    [Test]
    public void Wider_road_has_at_least_as_many_lanes()
    {
        Assert.That(spawner.LaneCount(14f), Is.GreaterThanOrEqualTo(spawner.LaneCount(7f)));
        Assert.That(spawner.LaneCount(7f), Is.GreaterThanOrEqualTo(spawner.LaneCount(3.5f)));
    }

    [Test]
    public void LaneOffsets_stay_within_road_bounds()
    {
        foreach (float halfWidth in new[] { 3.5f, 7f, 14f })
        {
            int lanes = spawner.LaneCount(halfWidth);
            for (int lane = 0; lane < lanes; lane++)
            {
                float offset = spawner.LaneOffset(lane, lanes, halfWidth);
                Assert.That(offset, Is.InRange(-halfWidth, halfWidth),
                    $"halfWidth {halfWidth} lane {lane}");
            }
        }
    }

    [Test]
    public void Lanes_split_into_oncoming_and_forward()
    {
        foreach (float halfWidth in new[] { 3.5f, 7f, 14f })
        {
            int lanes = spawner.LaneCount(halfWidth);
            bool hasOncoming = false; // negative offset
            bool hasForward = false;  // positive offset
            for (int lane = 0; lane < lanes; lane++)
            {
                float offset = spawner.LaneOffset(lane, lanes, halfWidth);
                if (offset < 0f) hasOncoming = true;
                if (offset > 0f) hasForward = true;
            }
            Assert.IsTrue(hasOncoming, $"halfWidth {halfWidth} should have an oncoming lane");
            Assert.IsTrue(hasForward, $"halfWidth {halfWidth} should have a forward lane");
        }
    }
}
