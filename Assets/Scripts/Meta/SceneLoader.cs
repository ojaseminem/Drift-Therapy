using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DriftTherapy
{
    /// <summary>
    /// Persistent (DontDestroyOnLoad) scene-transition driver: fades a
    /// full-screen black overlay (with a progress bar) to opaque, loads the
    /// target scene asynchronously, then fades back out. This is what
    /// <see cref="SceneFlow"/> routes through instead of a hard-cut
    /// <c>SceneManager.LoadScene</c>.
    ///
    /// Built at runtime on first use — a single full-screen Image is trivial
    /// enough that authoring a UIBuilder prefab for it would be overkill.
    /// Sorting order is set high so it renders above both the menu and gameplay
    /// canvases regardless of which scene is currently loaded.
    /// </summary>
    [DisallowMultipleComponent]
    public class SceneLoader : MonoBehaviour
    {
        const int SortingOrder = 1000;
        const float FadeDuration = 0.25f;

        /// <summary>
        /// Where the bar sits when the real scene load kicks off. The first
        /// stretch (0 -> this) plays alongside the fade-in so there's always
        /// visible forward progress before Unity has loaded anything; the rest
        /// (this -> 1) tracks the actual AsyncOperation, so by the time the bar
        /// reads 100% the scene is fully loaded — not just "loading started."
        /// </summary>
        const float SceneLoadStartsAtProgress = 0.6f;

        /// <summary>
        /// Extra frames held under full-black cover after the scene activates,
        /// so heavy first-frame work in the new scene's Awake()/Start() (e.g.
        /// PlayerVehicleSpawner's Instantiate, GarageVehicleDisplay's pooled
        /// spawn) finishes before the reveal instead of popping in after it.
        /// </summary>
        const int SettleFrames = 3;

        static SceneLoader instance;
        public static SceneLoader Instance
        {
            get
            {
                if (instance == null)
                {
                    var go = new GameObject("SceneLoader");
                    Object.DontDestroyOnLoad(go);
                    instance = go.AddComponent<SceneLoader>();
                    instance.BuildOverlay();
                }
                return instance;
            }
        }

        CanvasGroup overlay;
        RectTransform progressFill;
        bool loading;

        void BuildOverlay()
        {
            var canvasGo = new GameObject("LoadingCanvas", typeof(Canvas), typeof(CanvasGroup), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = SortingOrder;

            var bgGo = new GameObject("Bg", typeof(RectTransform), typeof(Image));
            bgGo.transform.SetParent(canvasGo.transform, false);
            var bgRt = (RectTransform)bgGo.transform;
            bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero; bgRt.offsetMax = Vector2.zero;
            bgGo.GetComponent<Image>().color = Color.black;

            var trackGo = new GameObject("ProgressTrack", typeof(RectTransform), typeof(Image));
            trackGo.transform.SetParent(canvasGo.transform, false);
            var trackRt = (RectTransform)trackGo.transform;
            trackRt.anchorMin = new Vector2(0.5f, 0f); trackRt.anchorMax = new Vector2(0.5f, 0f);
            trackRt.pivot = new Vector2(0.5f, 0.5f);
            trackRt.sizeDelta = new Vector2(480f, 10f);
            trackRt.anchoredPosition = new Vector2(0f, 140f);
            trackGo.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.12f);

            var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillGo.transform.SetParent(trackGo.transform, false);
            progressFill = (RectTransform)fillGo.transform;
            progressFill.anchorMin = new Vector2(0f, 0f); progressFill.anchorMax = new Vector2(0f, 1f);
            progressFill.offsetMin = Vector2.zero; progressFill.offsetMax = Vector2.zero;
            fillGo.GetComponent<Image>().color = new Color(0.98f, 0.79f, 0.29f, 1f);

            overlay = canvasGo.GetComponent<CanvasGroup>();
            overlay.alpha = 0f;
            overlay.blocksRaycasts = false;

            SetProgress(0f);
        }

        void SetProgress(float t)
        {
            if (progressFill == null) return;
            var a = progressFill.anchorMax;
            a.x = Mathf.Clamp01(t);
            progressFill.anchorMax = a;
        }

        /// <summary>Fades to black, loads <paramref name="sceneName"/>, fades back in. No-op if already loading.</summary>
        public void Load(string sceneName)
        {
            if (loading) return;
            StartCoroutine(LoadRoutine(sceneName));
        }

        IEnumerator LoadRoutine(string sceneName)
        {
            loading = true;
            overlay.blocksRaycasts = true;
            SetProgress(0f);

            bool fadedIn = false;
            UiJuice.FadeCanvasGroup(overlay, 1f, FadeDuration, () => fadedIn = true);
            float t = 0f;
            while (t < FadeDuration)
            {
                t += Time.unscaledDeltaTime;
                SetProgress(Mathf.Lerp(0f, SceneLoadStartsAtProgress, t / FadeDuration));
                yield return null;
            }
            yield return new WaitUntil(() => fadedIn);
            SetProgress(SceneLoadStartsAtProgress);

            var op = SceneManager.LoadSceneAsync(sceneName);
            op.allowSceneActivation = false;
            while (op.progress < 0.9f)
            {
                SetProgress(Mathf.Lerp(SceneLoadStartsAtProgress, 1f, op.progress / 0.9f));
                yield return null;
            }
            SetProgress(1f);

            op.allowSceneActivation = true;
            while (!op.isDone) yield return null;

            for (int i = 0; i < SettleFrames; i++) yield return null;

            bool fadedOut = false;
            UiJuice.FadeCanvasGroup(overlay, 0f, FadeDuration, () => fadedOut = true);
            yield return new WaitUntil(() => fadedOut);

            overlay.blocksRaycasts = false;
            loading = false;
        }
    }
}
