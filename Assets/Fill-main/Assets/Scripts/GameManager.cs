using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 용두사미 퍼즐용 GameManager
// - 첫 클릭한 칸이 꼬리(Tail) 시작점
// - 드래그로 상하좌우 인접 칸만 이동 가능
// - 막힌 칸(Blocked)은 통과 불가
// - 이미 채운 칸 재방문 불가 (자기 몸 밟기 X)
// - 단, "모든 활성 칸을 채운 뒤" 마지막에 Tail 칸으로 돌아오면 클리어

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [SerializeField] private Level _level;      // 에디터에서 설정한 레벨 데이터 (Row, Col, Data)
    [SerializeField] private Cell _cellPrefab;  // 셀 프리팹
    [SerializeField] private Transform _edgePrefab; // 칸과 칸 사이에 그릴 선(몸통) 프리팹

    private bool hasGameFinished;               // 클리어 후 입력 막기
    private bool hasTail;                       // Tail(꼬리) 생성 여부

    private Cell[,] cells;                      // 보드에 깔린 셀들
    private List<Vector2Int> path;              // 지나간 경로(좌표들)
    private List<Transform> edges;              // 칸 사이에 그려진 선들

    private Vector2Int lastPos;                 // 마지막으로 채운 칸(머리 위치)

    // 상하좌우 이동만 허용
    private readonly List<Vector2Int> directions = new List<Vector2Int>()
    {
        Vector2Int.up,
        Vector2Int.down,
        Vector2Int.left,
        Vector2Int.right
    };

    private void Awake()
    {
        Instance = this;

        hasGameFinished = false;
        hasTail = false;

        cells = new Cell[_level.Row, _level.Col];
        path = new List<Vector2Int>();
        edges = new List<Transform>();

        SpawnLevel();
    }

    /// <summary>
    /// 레벨 데이터(Level ScriptableObject)를 읽어서 셀 생성 + 카메라 위치 세팅
    /// </summary>
    private void SpawnLevel()
    {
        // 카메라를 보드 중앙으로 이동
        Vector3 camPos = Camera.main.transform.position;
        camPos.x = _level.Col * 0.5f;
        camPos.y = _level.Row * 0.5f;
        Camera.main.transform.position = camPos;

        // 보드가 전부 보이도록 사이즈 조절 (여유 2칸)
        Camera.main.orthographicSize = Mathf.Max(_level.Row, _level.Col) + 2f;

        // 셀 생성
        for (int r = 0; r < _level.Row; r++)
        {
            for (int c = 0; c < _level.Col; c++)
            {
                Cell cell = Instantiate(_cellPrefab);
                int data = _level.Data[r * _level.Col + c]; // 0 = 빈칸, 1 = 막힌칸
                cell.Init(data);
                cell.transform.position = new Vector3(c + 0.5f, r + 0.5f, 0);

                cells[r, c] = cell;
            }
        }
    }

    private void Update()
    {
        if (hasGameFinished) return;

        // 1. 마우스를 누른 순간 (또는 터치 시작)
        if (Input.GetMouseButtonDown(0))
        {
            Vector2Int cellPos;
            if (!TryGetCellUnderMouse(out cellPos)) return;
            if (!IsValid(cellPos)) return;

            Cell cell = cells[cellPos.x, cellPos.y];
            if (cell.Blocked) return; // 막힌 칸에서 시작 불가

            // 아직 Tail이 없으면 -> 첫 클릭한 칸을 Tail로 사용
            if (!hasTail)
            {
                hasTail = true;
                cell.SetTail(true);      // 이 함수에서 Tail = true, Filled = true, 색 변경
                path.Clear();
                path.Add(cellPos);
                lastPos = cellPos;
                return;
            }

            // (선택) 이미 Tail이 있는데 다른 칸을 눌렀을 때의 처리:
            // 지금은 무시. 나중에 "리셋" 버튼 따로 만드는 걸 추천.
        }
        // 2. 마우스를 누르고 있는 동안(드래그 중)
        else if (Input.GetMouseButton(0) && hasTail)
        {
            Vector2Int nextPos;
            if (!TryGetCellUnderMouse(out nextPos)) return;
            if (!IsValid(nextPos)) return;

            // 같은 칸이면 아무 것도 안 함
            if (nextPos == lastPos) return;

            // 인접(상하좌우) 칸이 아니면 무시
            if (!IsNeighbour(lastPos, nextPos)) return;

            Cell nextCell = cells[nextPos.x, nextPos.y];

            // 2-1. 막힌 칸이면 못 감
            if (nextCell.Blocked) return;

            // 2-2. 이미 채운 칸에 들어가려는 경우
            if (nextCell.Filled)
            {
                // 만약 그 칸이 Tail이고,
                // Tail을 제외한 모든 활성 칸을 이미 채운 상태라면 -> 루프 완성 = 클리어
                if (nextCell.Tail && AllActiveFilledExceptTail())
                {
                    CreateEdge(lastPos, nextPos);
                    path.Add(nextPos);
                    lastPos = nextPos;

                    hasGameFinished = true;
                    StartCoroutine(GameFinished());
                }

                // Tail이 아니거나, 아직 모든 칸을 안 채웠으면 -> 자기 몸 밟기라서 무시
                return;
            }

            // 2-3. 새로운 빈 칸으로 이동하는 정상적인 경우
            nextCell.Add();                 // 칸을 채움 (색 변경)
            CreateEdge(lastPos, nextPos);   // 이전 칸과의 연결 선 생성
            path.Add(nextPos);              // 경로에 추가
            lastPos = nextPos;              // 머리 위치 갱신
        }
        // 3. 마우스를 뗐을 때 처리 (선택)
        else if (Input.GetMouseButtonUp(0))
        {
            // 여기서는 아무 것도 안 해도 됨.
            // "중간에 손 떼면 실패" 같은 규칙을 넣고 싶으면 여기서 처리 가능.
        }
    }

    /// <summary>
    /// 마우스 위치 아래의 셀 좌표를 얻는 함수
    /// (카메라 world → grid index 로 변환)
    /// </summary>
    private bool TryGetCellUnderMouse(out Vector2Int cellPos)
    {
        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        // x -> col, y -> row
        int col = Mathf.FloorToInt(mouseWorld.x);
        int row = Mathf.FloorToInt(mouseWorld.y);

        cellPos = new Vector2Int(row, col);
        return true;
    }

    /// <summary>
    /// 그리드 범위 안인지 체크
    /// </summary>
    private bool IsValid(Vector2Int pos)
    {
        return pos.x >= 0 && pos.y >= 0 &&
               pos.x < _level.Row && pos.y < _level.Col;
    }

    /// <summary>
    /// 두 좌표가 상하좌우로 인접한지 확인
    /// </summary>
    private bool IsNeighbour(Vector2Int from, Vector2Int to)
    {
        Vector2Int diff = to - from;
        return directions.Contains(diff);
    }

    /// <summary>
    /// Tail을 제외한 모든 "활성 칸(Blocked가 아닌 칸)"이 Filled인지 확인
    /// - Tail 칸은 마지막에 다시 들어올 거라서 여기선 제외
    /// </summary>
    private bool AllActiveFilledExceptTail()
    {
        for (int r = 0; r < _level.Row; r++)
        {
            for (int c = 0; c < _level.Col; c++)
            {
                Cell cell = cells[r, c];

                if (cell.Blocked) continue; // 막힌 칸은 대상 아님
                if (cell.Tail) continue;    // Tail은 마지막에 닫힐 칸

                if (!cell.Filled)
                {
                    // 하나라도 안 채워졌으면 아직 조건 미달
                    return false;
                }
            }
        }

        return true; // Tail 제외한 모든 칸이 채워진 상태
    }

    /// <summary>
    /// 두 칸 사이에 Edge(선) 프리팹을 생성해서 용의 몸처럼 연결
    /// </summary>
    private void CreateEdge(Vector2Int from, Vector2Int to)
    {
        Transform edge = Instantiate(_edgePrefab);
        edges.Add(edge);

        // from과 to의 중간 지점에 배치
        float midX = from.y * 0.5f + 0.5f + to.y * 0.5f;
        float midY = from.x * 0.5f + 0.5f + to.x * 0.5f;
        edge.position = new Vector3(midX, midY, 0f);

        // 수평/수직 방향에 따라 회전
        bool horizontal = (to.y - from.y) != 0;
        edge.eulerAngles = new Vector3(0, 0, horizontal ? 90f : 0f);
    }

    /// <summary>
    /// 클리어 후 간단한 연출 뒤에 씬 재시작
    /// </summary>
    private IEnumerator GameFinished()
    {
        // TODO: 여기서 용이 꼬리를 "쩝" 먹는 애니메이션, 사운드 넣으면 됨
        yield return new WaitForSeconds(1.0f);
        UnityEngine.SceneManagement.SceneManager.LoadScene(0);
    }
}
