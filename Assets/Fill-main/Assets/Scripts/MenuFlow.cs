using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MenuFlow : MonoBehaviour
{
    [Header("Groups / Panels")]
    [SerializeField] private GameObject mainMenuGroup;  // 메인메뉴(배경, Home, Setting, Chapter 버튼)
    [SerializeField] private GameObject settingPanel;   // 설정 패널
    [SerializeField] private GameObject stagePanel;     // ★ 스테이지 선택 패널 (새로 추가)

    [Header("Audio")]
    [SerializeField] private Slider volumeSlider;
    private const string KEY_VOL = "master_volume";

    [Header("Scenes")]
    [SerializeField] private string gameplaySceneName = "Gameplay"; // Gameplay 씬 이름

    private void Awake()
    {
        if (settingPanel) settingPanel.SetActive(false);
        if (stagePanel) stagePanel.SetActive(false);

        // 볼륨 복원 & 슬라이더 연결
        float saved = PlayerPrefs.GetFloat(KEY_VOL, 1f);
        AudioListener.volume = saved;
        if (volumeSlider)
        {
            volumeSlider.minValue = 0f;
            volumeSlider.maxValue = 1f;
            volumeSlider.value = saved;
            volumeSlider.onValueChanged.AddListener(SetVolume);
        }
    }

    // ===== 화면 전환 =====

    public void OnClickHome()       // 노란 집
    {
        SceneManager.LoadScene("StartScene");
    }

    public void OnClickSettings()   // 초록 톱니
    {
        if (!settingPanel) return;
        settingPanel.SetActive(!settingPanel.activeSelf);
        if (settingPanel.activeSelf && stagePanel) stagePanel.SetActive(false);
    }

    // ★ Chapter 1 버튼 → StagePanel 열기
    public void OnClickChapter1()
    {
        if (!stagePanel) return;
        mainMenuGroup?.SetActive(true);
        settingPanel?.SetActive(false);
        stagePanel.SetActive(true);
    }

    // ★ StagePanel 내 Back 버튼
    public void OnClickStageBack()
    {
        stagePanel?.SetActive(false);
        ShowMainMenu();
    }

    // ===== 스테이지 선택 후 Gameplay 로드 =====
    public void OnClickStage(int stageIndex) // 0~4
    {
        // 선택 스테이지 전달 (멀티 씬 구조면 사용)
        StageManager.SelectedStageIndex = stageIndex;

        // Gameplay 씬 로드
        if (!string.IsNullOrEmpty(gameplaySceneName))
            SceneManager.LoadScene(gameplaySceneName);
        else
            Debug.LogWarning("Gameplay scene name is empty.");
    }

    // ===== 오디오 =====
    private void SetVolume(float v)
    {
        AudioListener.volume = v;
        PlayerPrefs.SetFloat(KEY_VOL, v);
    }

    // ===== 내부 헬퍼 =====

    private void ShowMainMenu()
    {
        mainMenuGroup?.SetActive(true);
        settingPanel?.SetActive(false);
        stagePanel?.SetActive(false);
    }
}
