using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    [SerializeField] private string gameplaySceneName = "Gameplay";

    // 버튼에서 호출할 함수
    public void LoadGame()
    {
        SceneManager.LoadScene(gameplaySceneName);
    }
}
