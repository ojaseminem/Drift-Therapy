using UnityEngine;

/// <summary>
/// Fades a point light's intensity to zero over a short duration then disables it —
/// a cheap extra "pop" alongside a particle burst on impact. Lives on the same
/// pooled prefab as the burst, triggered once per play, never instantiated per-flash.
/// </summary>
[DisallowMultipleComponent]
public class ImpactFlash : MonoBehaviour
{
    [SerializeField] Light flashLight;
    [SerializeField] float duration = 0.15f;
    [SerializeField] float startIntensity = 8f;

    float t = -1f;

    public void Play()
    {
        if (!flashLight) return;
        flashLight.enabled = true;
        flashLight.intensity = startIntensity;
        t = 0f;
    }

    void Update()
    {
        if (t < 0f || !flashLight) return;
        t += Time.deltaTime;
        float u = Mathf.Clamp01(t / duration);
        flashLight.intensity = Mathf.Lerp(startIntensity, 0f, u);
        if (u >= 1f)
        {
            flashLight.enabled = false;
            t = -1f;
        }
    }
}
