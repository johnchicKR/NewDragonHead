using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance;

    [Header("UI & Object")]
    [SerializeField] private TutorialHand tutorialHand;
    [SerializeField] private GameObject textPanel; // "따라해보세요!" 텍스트 패널
    [SerializeField] private Text guideText;

    private bool isTutorialActive = false;

    private void Awake()
    {
        Instance = this;
        if (tutorialHand) tutorialHand.Hide();
        if (textPanel) textPanel.SetActive(false);
    }

    private void Start()
    {
        // 게임 시작 시 현재 스테이지 확인 후 튜토리얼 실행
        CheckAndStartTutorial(StageManager.SelectedStageIndex);
    }

    private void Update()
    {
        // 플레이어가 화면을 터치하면 튜토리얼 숨기기
        if (isTutorialActive && Input.GetMouseButtonDown(0))
        {
            StopTutorial();
        }
    }

    private void CheckAndStartTutorial(int stageIndex)
    {
        // 스테이지 0 (1-1) 튜토리얼
        if (stageIndex == 0)
        {
            // 3x3 도넛 모양 맵의 정답 경로 (예시)
            // (1,0)에서 시작해서 한 바퀴 도는 경로
            List<Vector2Int> path = new List<Vector2Int>()
            {
                new Vector2Int(1,0), // 시작
                new Vector2Int(0,0),
                new Vector2Int(0,1),
                new Vector2Int(0,2),
                new Vector2Int(1,2),
                new Vector2Int(2,2),
                new Vector2Int(2,1),
                new Vector2Int(2,0),
                new Vector2Int(1,0)  // 끝 (제자리)
            };

            StartTutorial("화면을 드래그해서\n모든 칸을 채워보세요!", path);
        }
        // 나중에 Key/Lock 스테이지(예: 스테이지 2) 튜토리얼 추가 가능
        else if (stageIndex == 2)
        {
            // 열쇠 먹으러 가는 경로 설정 등...
        }
    }

    private void StartTutorial(string message, List<Vector2Int> path)
    {
        isTutorialActive = true;

        // 1. 손가락 애니메이션 시작
        if (tutorialHand) tutorialHand.StartPathAnimation(path);

        // 2. 텍스트 표시
        if (textPanel)
        {
            textPanel.SetActive(true);
            if (guideText) guideText.text = message;
        }
    }

    public void StopTutorial()
    {
        if (!isTutorialActive) return;

        isTutorialActive = false;
        if (tutorialHand) tutorialHand.Hide();
        if (textPanel) textPanel.SetActive(false);
    }
}