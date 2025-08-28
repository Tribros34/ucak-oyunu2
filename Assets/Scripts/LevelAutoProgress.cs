using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelAutoProgress : MonoBehaviour
{
    private int totalPlanes;
    private int tookOff;

    private void OnEnable()
    {
        AirplaneController.OnPlaneTookOff += HandlePlaneTookOff;
    }

    private void OnDisable()
    {
        AirplaneController.OnPlaneTookOff -= HandlePlaneTookOff;
    }

    private void Start()
    {
#if UNITY_2023_1_OR_NEWER
        var planes = Object.FindObjectsByType<AirplaneController>(FindObjectsSortMode.None);
        totalPlanes = planes.Length;
#else
        totalPlanes = FindObjectsOfType<AirplaneController>(true).Length;
#endif
        tookOff = 0;

        // Sahnedeki uçak yoksa doğrudan sıradakine geç
        if (totalPlanes == 0)
            LoadNextScene();
    }

    private void HandlePlaneTookOff()
    {
        tookOff++;
        if (tookOff >= totalPlanes)
            LoadNextScene();
    }

    private void LoadNextScene()
    {
        int curr = SceneManager.GetActiveScene().buildIndex;
        int total = SceneManager.sceneCountInBuildSettings;

        if (curr + 1 < total)
            SceneManager.LoadScene(curr + 1, LoadSceneMode.Single);
        else
            SceneManager.LoadScene(0, LoadSceneMode.Single); // bitti → menü
    }
}
