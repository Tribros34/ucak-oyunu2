using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    [Header("Açılacak oyun sahnesi")]
    public string gameSceneName = "SampleScene";   // İlk level sahnenin adı
 
    // Menüdeki START butonunun çağıracağı fonksiyon
    public void StartGameAtFirstLevel()
    {
        // Varsayılan olarak CurrentIndex = 0
 
        // Oyun sahnesini yükle
        SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
    }
 }
    