using UnityEngine;
using UnityEngine.SceneManagement;

public class GameplayUI : MonoBehaviour
{
    [Header("Settings Panel")]
    [SerializeField] private GameObject settingsPanel;   // 설정창 UI (패널)

    // 🏠 홈 버튼
    public void OnClickHome()
    {
        SceneManager.LoadScene("MainMenu");
    }

    // ⚙ 설정 버튼 (열기)
    public void OnClickOpenSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(true);
    }

    // ❌ 설정 닫기 버튼
    public void OnClickCloseSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    // 🔁 다시하기 버튼 같은 것까지 넣고 싶으면
    public void OnClickRetry()
    {
        Scene current = SceneManager.GetActiveScene();
        SceneManager.LoadScene(current.buildIndex);
    }
}
