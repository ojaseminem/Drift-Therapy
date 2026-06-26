using NUnit.Framework;
using UnityEngine;

public class TrafficSpawnerOffsetTests
{
    GameObject root;
    TrafficDirector director;

    [SetUp]
    public void SetUp()
    {
        root = new GameObject("TrafficDirectorRoot");
        director = root.AddComponent<TrafficDirector>();
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
            int lanes = director.LaneCount(halfWidth);
            Assert.That(lanes, Is.GreaterThanOrEqualTo(2), $"halfWidth {halfWidth}");
            Assert.That(lanes % 2, Is.EqualTo(0), $"halfWidth {halfWidth} lanes {lanes}");
        }
    }

    [Test]
    public void Lane_count_matches_design_width()
    {
        Assert.AreEqual(2, director.LaneCount(3.5f));   // 2-lane
        Assert.AreEqual(4, director.LaneCount(7f));     // 4-lane
        Assert.AreEqual(8, director.LaneCount(14f));    // 8-lane
    }

    [Test]
    public void Wider_road_has_at_least_as_many_lanes()
    {
        Assert.That(director.LaneCount(14f), Is.GreaterThanOrEqualTo(director.LaneCount(7f)));
        Assert.That(director.LaneCount(7f), Is.GreaterThanOrEqualTo(director.LaneCount(3.5f)));
    }

    [Test]
    public void Lane_offsets_stay_within_road_bounds()
    {
        foreach (float halfWidth in new[] { 3.5f, 7f, 14f })
        {
            director.BuildLanes(halfWidth);
            foreach (var lane in director.Lanes)
                Assert.That(lane.Offset, Is.InRange(-halfWidth, halfWidth), $"halfWidth {halfWidth}");
        }
    }

    [Test]
    public void Lanes_split_into_oncoming_and_forward()
    {
        foreach (float halfWidth in new[] { 3.5f, 7f, 14f })
        {
            director.BuildLanes(halfWidth);
            bool hasOncoming = false, hasForward = false;
            foreach (var lane in director.Lanes)
            {
                if (lane.Direction < 0) hasOncoming = true;
                if (lane.Direction > 0) hasForward = true;
            }
            Assert.IsTrue(hasOncoming, $"halfWidth {halfWidth} should have an oncoming lane");
            Assert.IsTrue(hasForward, $"halfWidth {halfWidth} should have a forward lane");
        }
    }
}
