using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DriftTherapy
{
    /// <summary>
    /// Persistent (DontDestroyOnLoad) scene-transition driver: fades a
    /// full-screen black overlay to opaque, loads the target scene
    /// asynchronously, then fades back out. This is what <see cref="SceneFlow"/>
    /// now routes through instead of a hard-cut <c>SceneManager.LoadScene</c>.
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

            overlay = canvasGo.GetComponent<CanvasGroup>();
            overlay.alpha = 0f;
            overlay.blocksRaycasts = false;
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

            bool fadedIn = false;
            UiJuice.FadeCanvasGroup(overlay, 1f, 0.25f, () => fadedIn = true);
            yield return new WaitUntil(() => fadedIn);

            var op = SceneManager.LoadSceneAsync(sceneName);
            while (op != null && !op.isDone)
            {
                yield return null;
            }

            bool fadedOut = false;
            UiJuice.FadeCanvasGroup(overlay, 0f, 0.25f, () => fadedOut = true);
            yield return new WaitUntil(() => fadedOut);

            overlay.blocksRaycasts = false;
            loading = false;
        }
    }
}
