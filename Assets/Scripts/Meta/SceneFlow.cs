using UnityEngine.SceneManagement;

namespace DriftTherapy
{
    /// <summary>
    /// Central scene navigation. Scene names must be in Build Settings. Routes
    /// through <see cref="SceneLoader"/> for a fade transition instead of a
    /// hard-cut <c>SceneManager.LoadScene</c> — callers are unaffected, the
    /// public API here is unchanged.
    /// </summary>
    public static class SceneFlow
    {
        public const string Menu = "MainMenu";
        public const string Game = "DriftEndless";

        public static void GoToGame() => SceneLoader.Instance.Load(Game);
        public static void GoToMenu() => SceneLoader.Instance.Load(Menu);
        public static void Reload()   => SceneLoader.Instance.Load(SceneManager.GetActiveScene().name);
    }
}
