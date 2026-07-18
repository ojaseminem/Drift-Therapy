#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace DriftTherapy.EditorTools
{
    /// <summary>Editor-only utilities for wiping local save data during development.</summary>
    public static class PlayerDataMenu
    {
        [MenuItem("Drift Therapy/Player Data/Clear Data")]
        public static void ClearData()
        {
            if (!EditorUtility.DisplayDialog(
                "Clear Player Data",
                "This deletes the local save (coins, gems, owned vehicles/skins, missions, leaderboard) and resets to a fresh install. This cannot be undone.",
                "Clear Data", "Cancel"))
            {
                return;
            }

            GameApp.ClearSavedData();
            Debug.Log("[PlayerDataMenu] Player data cleared.");
        }
    }
}
#endif
