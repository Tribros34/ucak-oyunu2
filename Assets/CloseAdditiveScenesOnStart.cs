using UnityEngine;
using UnityEngine.SceneManagement;

public class CloseAdditiveScenesOnStart : MonoBehaviour
{
    void Awake()
    {
        // Sadece play sırasında çalışsın
        if (!Application.isPlaying) return;

        var active = SceneManager.GetActiveScene();
        // Kendisi dışındaki tüm sahneleri kapat
        for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
        {
            var scn = SceneManager.GetSceneAt(i);
            if (scn != active && scn.isLoaded)
            {
                SceneManager.UnloadSceneAsync(scn);
            }
        }
    }
}
