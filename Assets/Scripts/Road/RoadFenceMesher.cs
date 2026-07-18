using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Extrudes a thin continuous guard fence along both sides of the road, following
/// the same SamplePoint list ProceduralRoadMesher uses for the road surface.
/// A separate mesh/GameObject (not merged into the road mesh) so it can carry its
/// own material/collider independently. Vertices are built in world-space then
/// offset by worldOrigin, exactly like ProceduralRoadMesher.
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class RoadFenceMesher : MonoBehaviour
{
    [Header("Fence Shape")]
    [Tooltip("Gap between the road edge (HalfWidth) and the fence's inner face.")]
    public float FenceGap = 0.6f;
    public float FenceHeight = 1.1f;
    public float FenceThickness = 0.12f;
    public float UvTileLength = 2f;

    MeshFilter mf;
    Mesh mesh;

    // Per-ring layout (8 verts): left wall (0-3), right wall (4-7).
    // 0 left bottom-inner   1 left top-inner   2 left top-outer   3 left bottom-outer
    // 4 right bottom-inner  5 right top-inner  6 right top-outer  7 right bottom-outer
    const int VPR = 8;

    void Awake()
    {
        mf   = GetComponent<MeshFilter>();
        mesh = new Mesh { name = "RoadFence" };
        mesh.MarkDynamic();
        mf.sharedMesh = mesh;
    }

    /// <summary>Rebuild the mesh. worldOrigin must equal the owning GO's world position (identity rotation assumed).</summary>
    public void RebuildMesh(List<ProceduralRoadMesher.SamplePoint> pts, Vector3 worldOrigin)
    {
        if (pts == null || pts.Count < 2) { mesh.Clear(); return; }

        int rings      = pts.Count;
        int totalVerts = rings * VPR;

        var verts   = new Vector3[totalVerts];
        var uvs     = new Vector2[totalVerts];
        var normals = new Vector3[totalVerts];

        for (int r = 0; r < rings; r++)
        {
            var sp = pts[r];
            var m  = Matrix4x4.TRS(sp.Position, sp.Rotation, Vector3.one);

            float innerOff = sp.HalfWidth + FenceGap;
            float outerOff = innerOff + FenceThickness;
            float h        = FenceHeight;
            float v        = sp.DistanceAlongSpline / Mathf.Max(0.01f, UvTileLength);

            Vector3[] lp = new Vector3[VPR]
            {
                new Vector3(-innerOff, 0f, 0f), // 0 left bottom-inner
                new Vector3(-innerOff, h,  0f), // 1 left top-inner
                new Vector3(-outerOff, h,  0f), // 2 left top-outer
                new Vector3(-outerOff, 0f, 0f), // 3 left bottom-outer
                new Vector3( innerOff, 0f, 0f), // 4 right bottom-inner
                new Vector3( innerOff, h,  0f), // 5 right top-inner
                new Vector3( outerOff, h,  0f), // 6 right top-outer
                new Vector3( outerOff, 0f, 0f), // 7 right bottom-outer
            };

            int b = r * VPR;
            for (int vi = 0; vi < VPR; vi++)
            {
                verts  [b+vi] = m.MultiplyPoint3x4(lp[vi]) - worldOrigin;
                // Flat "up" normals — same simplification ProceduralRoadMesher uses for its side/kerb faces.
                normals[b+vi] = m.MultiplyVector(Vector3.up);
            }

            uvs[b+0] = new Vector2(0f,   v);
            uvs[b+1] = new Vector2(0.3f, v);
            uvs[b+2] = new Vector2(0.6f, v);
            uvs[b+3] = new Vector2(1f,   v);
            uvs[b+4] = new Vector2(0f,   v);
            uvs[b+5] = new Vector2(0.3f, v);
            uvs[b+6] = new Vector2(0.6f, v);
            uvs[b+7] = new Vector2(1f,   v);
        }

        int segs = rings - 1;
        var tris = new List<int>(segs * 36);

        for (int r = 0; r < segs; r++)
        {
            int a  = r * VPR;
            int b2 = (r + 1) * VPR;

            // Left wall: inner face, top cap, outer face
            Quad(tris, a+0, a+1, b2+0, b2+1);
            Quad(tris, a+1, a+2, b2+1, b2+2);
            Quad(tris, a+2, a+3, b2+2, b2+3);

            // Right wall: inner face, top cap, outer face
            Quad(tris, a+4, a+5, b2+4, b2+5);
            Quad(tris, a+5, a+6, b2+5, b2+6);
            Quad(tris, a+6, a+7, b2+6, b2+7);
        }

        mesh.Clear();
        mesh.SetVertices(verts);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);
        mesh.subMeshCount = 1;
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();
    }

    static void Quad(List<int> t, int bl, int br, int tl, int tr)
    {
        t.Add(bl); t.Add(tl); t.Add(tr);
        t.Add(bl); t.Add(tr); t.Add(br);
    }
}
