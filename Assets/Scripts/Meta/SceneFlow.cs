using UnityEngine.SceneManagement;

namespace DriftTherapy
{
    /// <summary>Central scene navigation. Scene names must be in Build Settings.</summary>
    public static class SceneFlow
    {
        public const string Menu = "MainMenu";
        public const string Game = "DriftEndless";

        public static void GoToGame() => SceneManager.LoadScene(Game);
        public static void GoToMenu() => SceneManager.LoadScene(Menu);
        public static void Reload()   => SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
