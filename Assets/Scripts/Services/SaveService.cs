using UnityEngine;

/// <summary>
/// Thin static persistence layer over <see cref="PlayerPrefs"/> for Drift Therapy.
///
/// Stores the player's best score and a small set of options (audio volumes,
/// haptics, remove-ads). All setters write through to PlayerPrefs immediately;
/// <see cref="Save"/> additionally flushes PlayerPrefs to disk (PlayerPrefs.Save),
/// which is worth calling at safe checkpoints (run end, app pause) on mobile.
///
/// Keep this allocation-light and free of scene dependencies so any system can
/// read/write settings without a reference.
/// </summary>
public static class SaveService
{
    // ── Keys ────────────────────────────────────────────────────────────────
    const string KeyBestScore   = "dt_best_score";
    const string KeyMusicVolume = "dt_music_volume";
    const string KeySfxVolume   = "dt_sfx_volume";
    const string KeyHaptics     = "dt_haptics";
    const string KeyRemoveAds   = "dt_remove_ads";

    // ── Defaults ────────────────────────────────────────────────────────────
    const int   DefaultBestScore   = 0;
    const float DefaultMusicVolume = 1f;
    const float DefaultSfxVolume   = 1f;
    const bool  DefaultHaptics     = true;
    const bool  DefaultRemoveAds   = false;

    // ── Best score ──────────────────────────────────────────────────────────
    /// <summary>Returns the persisted best score (0 if none stored yet).</summary>
    public static int GetBestScore()
    {
        return PlayerPrefs.GetInt(KeyBestScore, DefaultBestScore);
    }

    /// <summary>Persists a new best score. Writes through immediately.</summary>
    public static void SetBestScore(int value)
    {
        PlayerPrefs.SetInt(KeyBestScore, value);
    }

    // ── Music volume (0..1) ─────────────────────────────────────────────────
    /// <summary>Returns the music volume in the 0..1 range (default 1).</summary>
    public static float GetMusicVolume()
    {
        return Mathf.Clamp01(PlayerPrefs.GetFloat(KeyMusicVolume, DefaultMusicVolume));
    }

    /// <summary>Persists the music volume, clamped to 0..1.</summary>
    public static void SetMusicVolume(float value)
    {
        PlayerPrefs.SetFloat(KeyMusicVolume, Mathf.Clamp01(value));
    }

    // ── SFX volume (0..1) ───────────────────────────────────────────────────
    /// <summary>Returns the SFX volume in the 0..1 range (default 1).</summary>
    public static float GetSfxVolume()
    {
        return Mathf.Clamp01(PlayerPrefs.GetFloat(KeySfxVolume, DefaultSfxVolume));
    }

    /// <summary>Persists the SFX volume, clamped to 0..1.</summary>
    public static void SetSfxVolume(float value)
    {
        PlayerPrefs.SetFloat(KeySfxVolume, Mathf.Clamp01(value));
    }

    // ── Haptics ─────────────────────────────────────────────────────────────
    /// <summary>Returns whether haptic feedback is enabled (default true).</summary>
    public static bool GetHaptics()
    {
        return PlayerPrefs.GetInt(KeyHaptics, DefaultHaptics ? 1 : 0) != 0;
    }

    /// <summary>Persists the haptics toggle.</summary>
    public static void SetHaptics(bool value)
    {
        PlayerPrefs.SetInt(KeyHaptics, value ? 1 : 0);
    }

    // ── Remove ads ──────────────────────────────────────────────────────────
    /// <summary>Returns whether the remove-ads entitlement is owned (default false).</summary>
    public static bool GetRemoveAds()
    {
        return PlayerPrefs.GetInt(KeyRemoveAds, DefaultRemoveAds ? 1 : 0) != 0;
    }

    /// <summary>Persists the remove-ads entitlement.</summary>
    public static void SetRemoveAds(bool value)
    {
        PlayerPrefs.SetInt(KeyRemoveAds, value ? 1 : 0);
    }

    // ── Flush ───────────────────────────────────────────────────────────────
    /// <summary>
    /// Flushes any pending PlayerPrefs writes to disk. Setters write the value
    /// into PlayerPrefs immediately, but this guarantees persistence even if the
    /// app is killed before Unity's automatic flush.
    /// </summary>
    public static void Save()
    {
        PlayerPrefs.Save();
    }
}
