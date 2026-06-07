using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

/// <summary>
/// Auto-wires RoadSystem references in DriftEndless scene on domain reload.
/// Runs once, guarded by EditorPrefs. Safe to delete after setup is confirmed.
/// </summary>
[InitializeOnLoad]
public static class RoadSystemSetup
{
    const string DoneKey = "DriftTherapy_RoadSystemSetup_Done";

    static RoadSystemSetup()
    {
        if (EditorPrefs.GetBool(DoneKey, false)) return;
        EditorApplication.delayCall += RunSetup;
    }

    // Also expose as menu item for manual re-runs
    [MenuItem("Tools/Drift Therapy/Setup Road System")]
    public static void RunSetup()
    {
        // Must be in DriftEndless scene
        var activeScene = SceneManager.GetActiveScene();
        if (!activeScene.name.Equals("DriftEndless"))
        {
            Debug.LogWarning("[RoadSetup] Open DriftEndless scene first. Active: " + activeScene.name);
            // Try to open it
            var path = "Assets/Scenes/DriftEndless.unity";
            var loaded = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            if (!loaded.IsValid()) { Debug.LogError("[RoadSetup] Could not open DriftEndless scene."); return; }
        }

        // ── Materials ──────────────────────────────────────────────────────
        var asphalt = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Road/M_Asphalt.mat");
        var kerb    = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Road/M_Kerb.mat");

        if (asphalt == null) { Debug.LogError("[RoadSetup] M_Asphalt.mat not found."); return; }
        if (kerb    == null) { Debug.LogError("[RoadSetup] M_Kerb.mat not found.");    return; }

        // ── PlayerCar ──────────────────────────────────────────────────────
        var playerGO = GameObject.Find("PlayerCar");
        if (playerGO == null) { Debug.LogError("[RoadSetup] PlayerCar not found."); return; }

        // ── RoadSystem GO ──────────────────────────────────────────────────
        var roadSystemGO = GameObject.Find("RoadSystem");
        if (roadSystemGO == null)
        {
            roadSystemGO = new GameObject("RoadSystem");
            Undo.RegisterCreatedObjectUndo(roadSystemGO, "Create RoadSystem");
        }

        // ── RoadSegmentPool ────────────────────────────────────────────────
        var pool = roadSystemGO.GetComponent<RoadSegmentPool>();
        if (pool == null)
        {
            pool = Undo.AddComponent<RoadSegmentPool>(roadSystemGO);
        }

        var poolSO = new SerializedObject(pool);
        poolSO.FindProperty("player").objectReferenceValue              = playerGO.transform;
        poolSO.FindProperty("poolSize").intValue                        = 10;
        poolSO.FindProperty("segmentArcLength").floatValue              = 40f;
        poolSO.FindProperty("samplesPerSegment").intValue               = 20;
        poolSO.FindProperty("spawnAheadDistance").floatValue            = 180f;
        poolSO.FindProperty("recycleDistance").floatValue               = 60f;
        poolSO.FindProperty("randomizeSeed").boolValue                  = true;
        poolSO.FindProperty("seed").intValue                            = 42;
        poolSO.FindProperty("DifficultyT").floatValue                   = 0f;

        var cs = poolSO.FindProperty("curveSettings");
        cs.FindPropertyRelative("BaseRoadHalfWidth").floatValue  = 4f;
        cs.FindPropertyRelative("BankPerDegreeYaw").floatValue   = 0.65f;
        cs.FindPropertyRelative("MaxBankAngle").floatValue       = 20f;
        cs.FindPropertyRelative("StraightMinNodes").intValue     = 3;
        cs.FindPropertyRelative("StraightMaxNodes").intValue     = 6;
        cs.FindPropertyRelative("CurveMinNodes").intValue        = 8;
        cs.FindPropertyRelative("CurveMaxNodes").intValue        = 15;
        cs.FindPropertyRelative("GentleYaw").floatValue          = 11f;
        cs.FindPropertyRelative("MediumYaw").floatValue          = 16f;
        cs.FindPropertyRelative("TightYaw").floatValue           = 22f;
        cs.FindPropertyRelative("YawSmoothing").floatValue       = 0.15f;
        cs.FindPropertyRelative("MaxAccumulatedYaw").floatValue  = 85f;

        var matsArr = poolSO.FindProperty("roadMaterials");
        matsArr.arraySize = 2;
        matsArr.GetArrayElementAtIndex(0).objectReferenceValue = asphalt;
        matsArr.GetArrayElementAtIndex(1).objectReferenceValue = kerb;
        poolSO.ApplyModifiedProperties();

        // ── RoadVisuals ────────────────────────────────────────────────────
        var visuals = roadSystemGO.GetComponent<RoadVisuals>();
        if (visuals == null) visuals = Undo.AddComponent<RoadVisuals>(roadSystemGO);

        var vSO = new SerializedObject(visuals);
        vSO.FindProperty("asphaltMaterial").objectReferenceValue = asphalt;
        vSO.FindProperty("kerbMaterial").objectReferenceValue    = kerb;
        vSO.FindProperty("asphaltScrollSpeed").floatValue        = 1.4f;
        vSO.FindProperty("kerbScrollSpeed").floatValue           = 1.4f;
        vSO.ApplyModifiedProperties();

        // ── Disable old RoadTemplate ───────────────────────────────────────
        var rt = GameObject.Find("RoadTemplate");
        if (rt != null && rt.activeSelf)
        {
            Undo.RecordObject(rt, "Disable RoadTemplate");
            rt.SetActive(false);
        }

        // ── Save scene ─────────────────────────────────────────────────────
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());

        EditorPrefs.SetBool(DoneKey, true);

        Debug.Log("[RoadSetup] ✓ Done — Player=" + playerGO.name +
                  " | Asphalt=" + asphalt.name + " | Kerb=" + kerb.name +
                  " | RoadTemplate disabled | Scene saved.");
    }

    [MenuItem("Tools/Drift Therapy/Reset Road Setup Flag")]
    static void ResetFlag() => EditorPrefs.DeleteKey(DoneKey);
}
