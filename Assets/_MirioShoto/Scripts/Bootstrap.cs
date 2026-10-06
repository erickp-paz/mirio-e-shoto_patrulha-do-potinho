using UnityEngine;
using UnityEngine.SceneManagement;

namespace MirioShoto
{
    /// <summary>Monta o jogo sozinho ao dar Play na SampleScene (protótipo sem prefabs).</summary>
    public static class Bootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            SceneManager.sceneLoaded += (s, m) => Spawn();
            Spawn();
        }

        static void Spawn()
        {
            if (SceneManager.GetActiveScene().name != "SampleScene") return;
            if (Object.FindFirstObjectByType<GameManager>() != null) return;
            new GameObject("MirioShoto").AddComponent<GameManager>();
        }
    }
}
