using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Level / Prefabs")]
    [SerializeField] private Level[] _levels;   // ★ 여러 스테이지 에셋 등록용
    [SerializeField] private Level _level;     // ★ 실제로 사용할 현재 레벨
    [SerializeField] private Cell _cellPrefab;
    [SerializeField] private Transform _edgePrefab;   // 직선 몸통 프리팹

    private Cell[,] cells;
    private List<Vector2Int> filledPoints;            // 지나간 셀 좌표들
    private List<Transform> edges;                    // 칸-칸 사이 Edge 오브젝트들

    private Vector2Int startPos;
    private Vector2Int endPos;
    private bool hasGameFinished = false;

    // (row, col) 기준 방향 정의
    private static readonly Vector2Int DIR_UP = new Vector2Int(1, 0);
    private static readonly Vector2Int DIR_DOWN = new Vector2Int(-1, 0);
    private static readonly Vector2Int DIR_RIGHT = new Vector2Int(0, 1);
    private static readonly Vector2Int DIR_LEFT = new Vector2Int(0, -1);

    [Header("Dragon Sprites")]
    [SerializeField] private Sprite _headSprite;
    [SerializeField] private Sprite _tailSprite;
    [SerializeField] private Sprite _bodyStraightSprite;  // Edge에 쓸 직선 스프라이트

    [Header("UI")]
    [SerializeField] private GameplayUI _gameplayUI;

    private GameObject _headObj;
    private GameObject _tailObj;

    private void Awake()
    {
        Instance = this;

        // ★ 1) 여러 레벨 중 하나 선택
        if (_levels != null && _levels.Length > 0)
        {
            // StageManager.SelectedStageIndex 를 기준으로 선택
            int idx = Mathf.Clamp(StageManager.SelectedStageIndex, 0, _levels.Length - 1);
            _level = _levels[idx];
            //Debug.Log($"GameManager: StageIndex {idx} → Level '{_level.name}' 선택");
        }

        if (_level == null)
        {
            Debug.LogError("GameManager: 사용할 Level이 설정되어 있지 않습니다. Inspector에서 _levels 또는 _level을 확인하세요.");
            return;
        }

        // ★ 2) 나머지는 기존 로직 그대로

        cells = new Cell[_level.Row, _level.Col];
        filledPoints = new List<Vector2Int>();
        edges = new List<Transform>();

        SpawnLevel();
        CreateHeadAndTail();
    }

    // ================== 레벨 생성 ==================

    private void SpawnLevel()
    {
        // 카메라 위치/사이즈 조정
        Vector3 camPos = Camera.main.transform.position;
        camPos.x = _level.Col * 0.5f;
        camPos.y = _level.Row * 0.5f;
        Camera.main.transform.position = camPos;
        Camera.main.orthographicSize = Mathf.Max(_level.Row, _level.Col) + 2f;

        // 셀 생성
        for (int r = 0; r < _level.Row; r++)
        {
            for (int c = 0; c < _level.Col; c++)
            {
                Cell cell = Instantiate(_cellPrefab);
                int data = _level.Data[r * _level.Col + c]; // 0/1
                cell.Init(data);                           // 여기서 색만 설정
                cell.transform.position = new Vector3(c + 0.5f, r + 0.5f, 0f);
                cells[r, c] = cell;
            }
        }
    }

    private void CreateHeadAndTail()
    {
        // 머리 오브젝트
        _headObj = new GameObject("DragonHead");
        var headSr = _headObj.AddComponent<SpriteRenderer>();
        headSr.sprite = _headSprite;
        headSr.sortingOrder = 30;
        _headObj.SetActive(false);

        // 꼬리 오브젝트
        _tailObj = new GameObject("DragonTail");
        var tailSr = _tailObj.AddComponent<SpriteRenderer>();
        tailSr.sprite = _tailSprite;
        tailSr.sortingOrder = 29;
        _tailObj.SetActive(false);
    }

    // ================== 입력 ==================

    private void Update()
    {
        if (hasGameFinished) return;

        if (Input.GetMouseButtonDown(0))
        {
            HandleTouchBegin();
        }
        else if (Input.GetMouseButton(0))
        {
            HandleTouchDrag();
        }
    }

    private void HandleTouchBegin()
    {
        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        startPos = new Vector2Int(Mathf.FloorToInt(mouseWorld.y), Mathf.FloorToInt(mouseWorld.x));
        endPos = startPos;

        if (!IsValid(startPos)) return;

        // 이미 경로가 그려져 있는 상태라면
        if (filledPoints.Count > 0)
        {
            // 현재 머리 위치
            Vector2Int head = filledPoints[filledPoints.Count - 1];

            // 머리 칸이 아닌 다른 칸에서 새로 드래그를 시작했다면 → "새로 시작"으로 판단하고 리셋
            if (startPos != head)
            {
                ResetPath();
            }
        }

        if (AddEmpty())               // 첫 셀 등록
        {
            UpdateHeadTail();
            RefreshCellsPathVisual(); // 코너/몸통 오버레이 갱신
        }
    }

    private void HandleTouchDrag()
    {
        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        endPos = new Vector2Int(Mathf.FloorToInt(mouseWorld.y), Mathf.FloorToInt(mouseWorld.x));

        // 0) 보드 밖(엠티 공간) → 그냥 무시 (리셋 X)
        if (!IsValid(endPos))
            return;

        // 1) 첫 출발칸(꼬리)로 되돌아온 경우 → 클리어 체크
        if (filledPoints.Count > 0 && endPos == filledPoints[0])
        {
            // 여기서는 새 칸 추가 없이, 현재 경로 상태로 승리 조건만 검사
            if (CheckWin())
            {
                hasGameFinished = true;
                StartCoroutine(GameFinished());   // 여기서 클리어 패널 뜨는 코루틴
            }
            return;    // 이 프레임에서는 더 이상 처리 안 함
        }

        // 2) 이웃칸이 아닌데, 다른 칸으로 튄 경우 → 실수 터치로 판단하고 리셋
        if (!IsNeighbour())
        {
            // 같은 칸 위에서 손가락만 살짝 흔들리는 상황은 제외
            if (endPos != startPos && filledPoints.Count > 0)
            {
                ResetPath();
            }
            return;
        }

        // 3) 정상 이웃칸으로 이동 시도
        bool movedForward = AddEmpty();

        if (movedForward)
        {
            // 앞으로 한 칸 전진 정상 처리
            CreateEdge();
            UpdateHeadTail();
            RefreshCellsPathVisual();
        }
        else if (RemoveFromEnd())
        {
            // 되돌아가기(Undo) 처리
            RemoveEdge();
            UpdateHeadTail();
            RefreshCellsPathVisual();
        }
        else
        {
            // 이웃이긴 한데,
            // - Blocked 칸이거나
            // - 이미 지나간 몸통(되돌리기도 아님)
            // 이런 경우 → 자기 몸을 밟았다고 보고 전체 리셋
            if (filledPoints.Count > 0)
            {
                ResetPath();
            }
        }

        startPos = endPos;
    }

    // ================== 기본 유틸 ==================

    private bool IsValid(Vector2Int pos)
    {
        return pos.x >= 0 && pos.y >= 0 &&
               pos.x < _level.Row && pos.y < _level.Col;
    }

    private bool IsNeighbour()
    {
        int dx = Mathf.Abs(startPos.x - endPos.x);
        int dy = Mathf.Abs(startPos.y - endPos.y);
        return dx + dy == 1;
    }

    // ================== 경로 관리 ==================

    private bool AddEmpty()
    {
        if (!IsValid(endPos)) return false;

        Cell target = cells[endPos.x, endPos.y];
        if (target.Blocked) return false;
        if (filledPoints.Contains(endPos)) return false;

        target.Add();                 // 색만 FilledColor로 변경
        filledPoints.Add(endPos);
        return true;
    }

    private bool RemoveFromEnd()
    {
        if (filledPoints.Count == 0) return false;

        // 마지막 칸이 endPos일 때만 지움 (뒤로 드래그한 경우)
        if (filledPoints[filledPoints.Count - 1] == endPos)
        {
            Vector2Int last = filledPoints[filledPoints.Count - 1];
            cells[last.x, last.y].Remove();      // 색을 다시 EmptyColor로
            filledPoints.RemoveAt(filledPoints.Count - 1);
            return true;
        }

        return false;
    }

    // ================== Edge (직선 몸통) ==================

    private void CreateEdge()
    {
        int count = filledPoints.Count;
        if (count < 2) return;

        Vector2Int from = filledPoints[count - 2];
        Vector2Int to = filledPoints[count - 1];
        Vector2Int dir = to - from;

        Transform edge = Instantiate(_edgePrefab);
        edges.Add(edge);

        var sr = edge.GetComponent<SpriteRenderer>();
        if (_bodyStraightSprite != null)
            sr.sprite = _bodyStraightSprite;

        // 두 셀 중앙에 위치
        float midX = from.y * 0.5f + 0.5f + to.y * 0.5f;
        float midY = from.x * 0.5f + 0.5f + to.x * 0.5f;
        edge.position = new Vector3(midX, midY, 0f);

        bool horizontal = (dir == DIR_RIGHT || dir == DIR_LEFT);
        edge.rotation = Quaternion.Euler(0f, 0f, horizontal ? 90f : 0f);

        sr.sortingOrder = 20;
    }

    private void RemoveEdge()
    {
        if (edges.Count == 0) return;

        Transform last = edges[edges.Count - 1];
        edges.RemoveAt(edges.Count - 1);
        Destroy(last.gameObject);
    }

    // ================== 셀 위 코너/직선(오버레이) ==================

    /// <summary>
    /// filledPoints 기준으로 각 셀의 pathRenderer에
    /// 직선/코너 스프라이트를 그려준다.
    /// 기본 점/색(_cellRenderer)은 건드리지 않는다.
    /// </summary>
    private void RefreshCellsPathVisual()
    {
        // 1) 기존 오버레이 초기화
        for (int r = 0; r < _level.Row; r++)
        {
            for (int c = 0; c < _level.Col; c++)
            {
                if (!cells[r, c].Blocked)
                    cells[r, c].ClearPathVisual();
            }
        }

        // 2) 경로 중 "중간 셀"만 검사 (머리/꼬리 제외)
        for (int i = 0; i < filledPoints.Count; i++)
        {
            if (i == 0 || i == filledPoints.Count - 1)
                continue;

            Vector2Int pos = filledPoints[i];
            Cell cell = cells[pos.x, pos.y];

            Vector2Int prev = filledPoints[i - 1];
            Vector2Int next = filledPoints[i + 1];

            Vector2Int dirIn = pos - prev;
            Vector2Int dirOut = next - pos;

            // 방향이 같으면 직선 → Edge가 이미 있으니 셀은 아무것도 안 함
            if (dirIn == dirOut)
            {
                // 직선 구간: 패스
                continue;
            }

            // 방향이 다르면 코너
            float angle = GetCellCornerAngle(dirIn, dirOut);
            cell.SetCornerVisual(angle);
        }
    }

    /// <summary>
    /// 코너 셀에 사용할 회전 계산.
    /// 코너 스프라이트 기본이
    /// "위(DIR_UP)에서 와서 오른쪽(DIR_RIGHT)으로 꺾이는 ㄱ" 모양이 0도라고 가정.
    /// </summary>
    private float GetCellCornerAngle(Vector2Int inDir, Vector2Int outDir)
    {
        if (inDir == DIR_UP && outDir == DIR_RIGHT) return 0f;   // ↗
        if (inDir == DIR_RIGHT && outDir == DIR_DOWN) return 270f; // ↘
        if (inDir == DIR_DOWN && outDir == DIR_LEFT) return 180f;// ↙
        if (inDir == DIR_LEFT && outDir == DIR_UP) return 90f;// ↖

        // [2] 반대 순서(오목 쪽으로 꺾이는 경우)는 위 각도에서 180° 반전
        if (inDir == DIR_RIGHT && outDir == DIR_UP) return 180f; // ↗ 반대
        if (inDir == DIR_DOWN && outDir == DIR_RIGHT) return 90f; // ↘ 반대
        if (inDir == DIR_LEFT && outDir == DIR_DOWN) return 0f;   // ↙ 반대
        if (inDir == DIR_UP && outDir == DIR_LEFT) return 270f;  // ↖ 반대
        return 0f;
    }

    // ================== 머리 / 꼬리 ==================

    private void UpdateHeadTail()
    {
        if (filledPoints.Count == 0)
        {
            _headObj.SetActive(false);
            _tailObj.SetActive(false);
            return;
        }

        Vector2Int tail = filledPoints[0];
        Vector2Int head = filledPoints[filledPoints.Count - 1];

        _tailObj.transform.position = new Vector3(tail.y + 0.5f, tail.x + 0.5f, -0.1f);
        _headObj.transform.position = new Vector3(head.y + 0.5f, head.x + 0.5f, -0.11f);

        _tailObj.SetActive(true);
        _headObj.SetActive(true);

        if (filledPoints.Count >= 2)
        {
            // 꼬리 방향: 두 번째 칸 쪽을 바라봄
            Vector2Int next = filledPoints[1];
            Vector2Int dirTail = next - tail;
            _tailObj.transform.rotation = Quaternion.Euler(0f, 0f, TailDirToAngle(dirTail));

            // 머리 방향: 마지막-1 칸에서 마지막 칸 방향
            Vector2Int prev = filledPoints[filledPoints.Count - 2];
            Vector2Int dirHead = head - prev;
            _headObj.transform.rotation = Quaternion.Euler(0f, 0f, HeadDirToAngle(dirHead));
        }
        else
        {
            _tailObj.transform.rotation = Quaternion.identity;
        }
    }

    private float HeadDirToAngle(Vector2Int dir)
    {
        // 머리 sprite 기본 방향: 오른쪽(→) 가정
        if (dir == DIR_RIGHT) return 0f;
        if (dir == DIR_UP) return 90f;
        if (dir == DIR_LEFT) return 180f;
        if (dir == DIR_DOWN) return -90f;
        return 0f;
    }

    private float TailDirToAngle(Vector2Int dir)
    {
        // 꼬리 sprite 기본 방향: 위(↑) 가정 (필요하면 숫자만 튜닝)
        if (dir == DIR_UP) return 90f;
        if (dir == DIR_RIGHT) return 0f;
        if (dir == DIR_DOWN) return -90f;
        if (dir == DIR_LEFT) return 180f;
        return 0f;
    }

    // ================== 클리어 & 재시작 ==================

    // === 현재 그려진 경로 전부 리셋 ===
    private void ResetPath()
    {
        // 1) 채워진 셀들 비우기
        if (filledPoints != null)
        {
            foreach (var pos in filledPoints)
            {
                cells[pos.x, pos.y].Remove();  // 색을 다시 Empty로
            }
            filledPoints.Clear();
        }

        // 2) Edge(몸통) 전부 삭제
        if (edges != null)
        {
            foreach (var edge in edges)
            {
                if (edge != null)
                    Destroy(edge.gameObject);
            }
            edges.Clear();
        }

        // 3) 머리/꼬리 숨기기 & 셀 비주얼 정리
        UpdateHeadTail();
        RefreshCellsPathVisual();
    }

    private bool CheckWin()
    {
        foreach (Cell cell in cells)
        {
            if (!cell.Blocked && !cell.Filled)
                return false;
        }
        return true;
    }

    private IEnumerator GameFinished()
    {
        yield return new WaitForSeconds(0.3f);

        if (_gameplayUI != null)
            _gameplayUI.ShowStageClear(StageManager.SelectedStageIndex);
    }

    // 🔽 여기에 추가하면 OK
    public void LoadNextStageOrMenu()
    {
        int currentStage = StageManager.SelectedStageIndex;

        if (_levels != null && _levels.Length > 0 && currentStage < _levels.Length - 1)
        {
            StageManager.SelectedStageIndex = currentStage + 1;
            SceneManager.LoadScene("Gameplay");
        }
        else
        {
            SceneManager.LoadScene("MainMenu");
        }
    }
}
