using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class RoadSegmentPoolSamplingTests
{
    GameObject root;
    GameObject playerObject;
    RoadSegmentPool pool;

    [SetUp]
    public void SetUp()
    {
        root = new GameObject("RoadPoolRoot");
        playerObject = new GameObject("Player");
        pool = root.AddComponent<RoadSegmentPool>();

        SetPrivateField(pool, "player", playerObject.transform);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(root);
        Object.DestroyImmediate(playerObject);
    }

    [Test]
    public void TrySampleAtArcDistance_returns_expected_pose_and_width()
    {
        SetPrivateField(pool, "spline", BuildStraightSpline());

        Assert.That(pool.TrySampleAtArcDistance(4f, out var sample), Is.True);
        Assert.That(sample.Position.z, Is.EqualTo(4f).Within(0.01f));
        Assert.That(Vector3.Dot(sample.Forward.normalized, Vector3.forward), Is.EqualTo(1f).Within(0.001f));
        Assert.That(Vector3.Dot(sample.Right.normalized, Vector3.right), Is.EqualTo(1f).Within(0.001f));
        Assert.That(sample.HalfWidth, Is.EqualTo(5f).Within(0.01f));
        Assert.That(Quaternion.Angle(sample.Rotation, Quaternion.identity), Is.EqualTo(0f).Within(0.001f));
    }

    [Test]
    public void TrySampleAhead_uses_distance_travelled_as_arc_origin()
    {
        var spline = new SplineRoadBuilder();
        spline.AddNode(new SplineRoadBuilder.SplineNode(new Vector3(0f, 0f, 0f), Quaternion.identity, 4f));
        spline.AddNode(new SplineRoadBuilder.SplineNode(new Vector3(0f, 0f, 8f), Quaternion.identity, 4f));
        spline.AddNode(new SplineRoadBuilder.SplineNode(new Vector3(0f, 0f, 16f), Quaternion.identity, 4f));

        SetPrivateField(pool, "spline", spline);
        SetPrivateField(pool, "distanceTravelled", 8f);

        Assert.That(pool.TrySampleAhead(4f, out var sample), Is.True);
        Assert.That(sample.ArcDistance, Is.EqualTo(12f).Within(0.01f));
        Assert.That(sample.Position.z, Is.InRange(8f, 16f));
    }

    [Test]
    public void TrySampleAtArcDistance_returns_false_when_spline_is_unavailable()
    {
        Assert.That(pool.TrySampleAtArcDistance(5f, out _), Is.False);
    }

    static SplineRoadBuilder BuildStraightSpline()
    {
        var spline = new SplineRoadBuilder();
        spline.AddNode(new SplineRoadBuilder.SplineNode(new Vector3(0f, 0f, 0f), Quaternion.identity, 4f));
        spline.AddNode(new SplineRoadBuilder.SplineNode(new Vector3(0f, 0f, 8f), Quaternion.identity, 6f));
        return spline;
    }

    static void SetPrivateField(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, $"Expected private field '{fieldName}' to exist.");
        field.SetValue(target, value);
    }
}
