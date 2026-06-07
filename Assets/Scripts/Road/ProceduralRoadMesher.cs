using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Extrudes a cross-section profile along sampled spline points to produce a Unity Mesh.
/// Vertices are built in world-space then offset by worldOrigin so the owning GO
/// sits at worldOrigin with identity rotation — no InverseTransformPoint needed.
/// Submesh 0 = asphalt, Submesh 1 = kerbs.
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class ProceduralRoadMesher : MonoBehaviour
{
    [System.Serializable]
    public struct CrossSection
    {
        public float KerbWidth;
        public float KerbHeight;
    }

    [Header("Cross-section")]
    public CrossSection Section = new CrossSection { KerbWidth = 0.55f, KerbHeight = 0.14f };

    [Header("UV")]
    public float AsphaltTileLength = 8f;
    public float KerbStripeLength  = 1.2f;

    MeshFilter mf;
    Mesh mesh;

    // Per-ring layout (9 verts):
    // 0  left kerb outer-bottom
    // 1  left kerb outer-top
    // 2  left kerb inner-top  (= left asphalt edge)
    // 3  left asphalt edge (duplicate for UV seam)
    // 4  road centre
    // 5  right asphalt edge
    // 6  right kerb inner-top
    // 7  right kerb outer-top
    // 8  right kerb outer-bottom
    const int VPR = 9; // verts per ring

    void Awake()
    {
        mf   = GetComponent<MeshFilter>();
        mesh = new Mesh { name = "ProceduralRoad" };
        mesh.MarkDynamic();
        mf.sharedMesh = mesh;
    }

    public struct SamplePoint
    {
        public Vector3    Position;
        public Quaternion Rotation;
        public float      HalfWidth;
        public float      Banking;
        public float      DistanceAlongSpline;
    }

    /// <summary>
    /// Rebuild the mesh.  worldOrigin must equal the owning GO's world position (identity rotation assumed).
    /// </summary>
    public void RebuildMesh(List<SamplePoint> pts, Vector3 worldOrigin)
    {
        if (pts == null || pts.Count < 2) { mesh.Clear(); return; }

        int rings      = pts.Count;
        int totalVerts = rings * VPR;

        var verts   = new Vector3[totalVerts];
        var uvs     = new Vector2[totalVerts];
        var normals = new Vector3[totalVerts];

        for (int r = 0; r < rings; r++)
        {
            SamplePoint sp = pts[r];

            // Matrix that maps road-local → world
            var m   = Matrix4x4.TRS(sp.Position, sp.Rotation, Vector3.one);
            float hw = sp.HalfWidth;
            float kw = Section.KerbWidth;
            float kh = Section.KerbHeight;
            float v  = sp.DistanceAlongSpline;

            // Road-local cross-section positions (X=right, Y=up, Z=forward)
            Vector3[] lp = new Vector3[VPR]
            {
                new Vector3(-(hw+kw), -kh, 0f),  // 0 kerb outer-bottom L
                new Vector3(-(hw+kw),  0f, 0f),  // 1 kerb outer-top    L
                new Vector3(-hw,       0f, 0f),  // 2 kerb inner-top    L
                new Vector3(-hw,       0f, 0f),  // 3 asphalt left edge
                new Vector3( 0f,       0f, 0f),  // 4 asphalt centre
                new Vector3( hw,       0f, 0f),  // 5 asphalt right edge
                new Vector3( hw,       0f, 0f),  // 6 kerb inner-top    R
                new Vector3( hw+kw,    0f, 0f),  // 7 kerb outer-top    R
                new Vector3( hw+kw,   -kh, 0f),  // 8 kerb outer-bottom R
            };

            int b = r * VPR;
            for (int vi = 0; vi < VPR; vi++)
            {
                // World pos → local pos relative to worldOrigin (GO has identity rotation)
                verts  [b+vi] = m.MultiplyPoint3x4(lp[vi]) - worldOrigin;
                normals[b+vi] = m.MultiplyVector(Vector3.up);
            }

            // UVs
            float av = v / AsphaltTileLength;
            float kv = v / KerbStripeLength;

            uvs[b+0] = new Vector2(0f,   kv);
            uvs[b+1] = new Vector2(0.5f, kv);
            uvs[b+2] = new Vector2(1f,   kv);
            uvs[b+3] = new Vector2(0f,   av);
            uvs[b+4] = new Vector2(0.5f, av);
            uvs[b+5] = new Vector2(1f,   av);
            uvs[b+6] = new Vector2(1f,   kv);
            uvs[b+7] = new Vector2(0.5f, kv);
            uvs[b+8] = new Vector2(0f,   kv);
        }

        int segs = rings - 1;
        var asphaltTris = new List<int>(segs * 6);
        var kerbTris    = new List<int>(segs * 24);

        for (int r = 0; r < segs; r++)
        {
            int a = r * VPR;
            int b2 = (r+1) * VPR;

            // Asphalt (verts 3-5)
            Quad(asphaltTris, a+3, a+5, b2+3, b2+5);

            // Left kerb: outer wall (0-1) and top (1-2)
            Quad(kerbTris, a+0, a+1, b2+0, b2+1);
            Quad(kerbTris, a+1, a+2, b2+1, b2+2);

            // Right kerb: top (6-7) and outer wall (7-8)
            Quad(kerbTris, a+6, a+7, b2+6, b2+7);
            Quad(kerbTris, a+7, a+8, b2+7, b2+8);
        }

        mesh.Clear();
        mesh.SetVertices(verts);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);
        mesh.subMeshCount = 2;
        mesh.SetTriangles(asphaltTris, 0);
        mesh.SetTriangles(kerbTris,    1);
        mesh.RecalculateBounds();
    }

    // Counter-clockwise winding viewed from above (Unity left-hand Y-up)
    // bl=bottom-left, br=bottom-right, tl=top-left, tr=top-right
    static void Quad(List<int> t, int bl, int br, int tl, int tr)
    {
        t.Add(bl); t.Add(tl); t.Add(tr);
        t.Add(bl); t.Add(tr); t.Add(br);
    }
}
