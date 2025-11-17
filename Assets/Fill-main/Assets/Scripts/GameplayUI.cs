using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameplayUI : MonoBehaviour
{
    [Header("Settings Panel")]
    [SerializeField] private GameObject settingsPanel;   // 설정창 UI (패널)

    [Header("Stage Clear Panel")]
    [SerializeField] private GameObject stageClearPanel; // 스테이지 클리어 패널
    [SerializeField] private Text stageText;             // "STAGE 1" 같은 텍스트

    private void Awake()
    {
        // 시작할 때는 클리어 패널 꺼두기
        if (stageClearPanel != null)
            stageClearPanel.SetActive(false);

        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    public void ShowStageClear(int stageIndex)
    {
        if (stageClearPanel != null)
            stageClearPanel.SetActive(true);

        if (stageText != null)
        {
            // 0 기반 인덱스를 1부터 시작하게 보여주기
            int displayIndex = stageIndex + 1;
            stageText.text = $"STAGE {displayIndex}";
        }
    }

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

    public void OnClickNextStage()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.LoadNextStageOrMenu();
        }
    }
}
