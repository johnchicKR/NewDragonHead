using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class LevelGenerator : MonoBehaviour
{
    [Header("Grid Size")]
    [SerializeField] private int _row = 5;
    [SerializeField] private int _col = 5;

    [Header("Data (ScriptableObject)")]
    [SerializeField] private Level _level;

    [Header("Prefabs")]
    [SerializeField] private Cell _cellPrefab;
    [SerializeField] private Transform _edgePrefab;

    [Header("Brush")]
    [SerializeField] private TileType _currentBrush = TileType.Block;
    // CustomEditor에서 쓰려고 프로퍼티도 하나 열어두기
    public TileType CurrentBrush
    {
        get => _currentBrush;
        set => _currentBrush = value;
    }

    // --- Runtime state ---
    private Cell[,] cells;
    private List<Vector2Int> filledPoints;
    private List<Transform> edges;
    private Vector2Int startPos, endPos;

    private bool IsNeighbour()
    {
        return IsValid(startPos) && IsValid(endPos) && directions.Contains(startPos - endPos);
    }

    private readonly List<Vector2Int> directions = new()
    {
        Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
    };

    /// <summary>Editor 전용 접근용: 현재 할당된 Level 에셋</summary>
    public Level LevelAsset => _level;

    private void Awake()
    {
        filledPoints = new List<Vector2Int>();
        edges = new List<Transform>();

        // _level이 있으면 그 크기를 신뢰
        if (_level != null && _level.Row > 0 && _level.Col > 0 &&
            _level.Data != null && _level.Data.Count == _level.Row * _level.Col)
        {
            _row = _level.Row;
            _col = _level.Col;
        }

        cells = new Cell[_row, _col];

        CreateLevel();   // _level이 없으면 패스, 있으면 사이즈 보정
        SpawnLevel();    // 씬에 셀 인스턴스 생성
    }

    private void CreateLevel()
    {
        if (_level == null) return;

        // 사이즈가 다르거나 데이터가 비어있으면 재구성
        if (_level.Row != _row || _level.Col != _col ||
            _level.Data == null || _level.Data.Count != _row * _col)
        {
            _level.Row = _row;
            _level.Col = _col;
            _level.Data = new List<int>(_row * _col);
            for (int i = 0; i < _row * _col; i++) _level.Data.Add(0);

#if UNITY_EDITOR
            EditorUtility.SetDirty(_level);
#endif
        }
    }

    private void SpawnLevel()
    {
        // 카메라 정렬
        var cam = Camera.main;
        if (cam != null)
        {
            Vector3 camPos = cam.transform.position;
            camPos.x = _col * 0.5f;
            camPos.y = _row * 0.5f;
            cam.transform.position = camPos;
            cam.orthographicSize = Mathf.Max(_row, _col) + 2f;
        }

        // 셀 배치
        for (int r = 0; r < _row; r++)
        {
            for (int c = 0; c < _col; c++)
            {
                var cell = Instantiate(_cellPrefab, new Vector3(c + 0.5f, r + 0.5f, 0f), Quaternion.identity, transform);
                cells[r, c] = cell;

                int v = (_level != null && _level.Data != null && _level.Data.Count == _row * _col)
                        ? _level.Data[r * _col + c]
                        : 0;
                cell.Init(v);
            }
        }
    }

    private void Update()
    {
        // 우클릭: 블록 토글(맵 에디팅)
        if (Input.GetMouseButtonDown(1))
        {
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            startPos = new Vector2Int(Mathf.FloorToInt(mousePos.y), Mathf.FloorToInt(mousePos.x));
            if (!IsValid(startPos)) return;

            var cell = cells[startPos.x, startPos.y];
            if (cell == null) return;

            // ✅ 브러시 타입으로 셀 타입 설정
            cell.SetType(_currentBrush);

            // ✅ Level.Data에 (int)TileType 코드 저장
            if (_level != null && _level.Data != null && _level.Data.Count == _row * _col)
            {
                _level.Data[startPos.x * _col + startPos.y] = (int)_currentBrush;
#if UNITY_EDITOR
                EditorUtility.SetDirty(_level);
#endif
            }
        }

        // 좌클릭 드래그: 경로 그리기(기존 로직 그대로)
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            startPos = new Vector2Int(Mathf.FloorToInt(mousePos.y), Mathf.FloorToInt(mousePos.x));
            endPos = startPos;
        }
        else if (Input.GetMouseButton(0))
        {
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            endPos = new Vector2Int(Mathf.FloorToInt(mousePos.y), Mathf.FloorToInt(mousePos.x));

            if (!IsNeighbour()) return;

            if (AddEmpty())
            {
                filledPoints.Add(startPos);
                filledPoints.Add(endPos);
                cells[startPos.x, startPos.y].Add();
                cells[endPos.x, endPos.y].Add();

                Transform edge = Instantiate(_edgePrefab, transform);
                edges.Add(edge);
                edge.position = new Vector3(
                    startPos.y * 0.5f + 0.5f + endPos.y * 0.5f,
                    startPos.x * 0.5f + 0.5f + endPos.x * 0.5f,
                    0f
                );
                bool horizontal = (endPos.y - startPos.y) != 0;
                edge.eulerAngles = new Vector3(0, 0, horizontal ? 90f : 0);
            }
            else if (AddToEnd())
            {
                filledPoints.Add(endPos);
                cells[endPos.x, endPos.y].Add();

                Transform edge = Instantiate(_edgePrefab, transform);
                edges.Add(edge);
                edge.position = new Vector3(
                    startPos.y * 0.5f + 0.5f + endPos.y * 0.5f,
                    startPos.x * 0.5f + 0.5f + endPos.x * 0.5f,
                    0f
                );
                bool horizontal = (endPos.y - startPos.y) != 0;
                edge.eulerAngles = new Vector3(0, 0, horizontal ? 90f : 0);
            }
            else if (AddToStart())
            {
                filledPoints.Insert(0, endPos);
                cells[endPos.x, endPos.y].Add();

                Transform edge = Instantiate(_edgePrefab, transform);
                edges.Insert(0, edge);
                edge.position = new Vector3(
                    startPos.y * 0.5f + 0.5f + endPos.y * 0.5f,
                    startPos.x * 0.5f + 0.5f + endPos.x * 0.5f,
                    0f
                );
                bool horizontal = (endPos.y - startPos.y) != 0;
                edge.eulerAngles = new Vector3(0, 0, horizontal ? 90f : 0);
            }
            else if (RemoveFromEnd())
            {
                Transform removeEdge = edges[edges.Count - 1];
                edges.RemoveAt(edges.Count - 1);
                Destroy(removeEdge.gameObject);
                filledPoints.RemoveAt(filledPoints.Count - 1);
                cells[startPos.x, startPos.y].Remove();
            }
            else if (RemoveFromStart())
            {
                Transform removeEdge = edges[0];
                edges.RemoveAt(0);
                Destroy(removeEdge.gameObject);
                filledPoints.RemoveAt(0);
                cells[startPos.x, startPos.y].Remove();
            }

            RemoveEmpty();
            startPos = endPos;
        }
    }

    // ----- Helpers -----
    private bool AddEmpty()
    {
        if (edges.Count > 0) return false;
        if (cells[startPos.x, startPos.y].Filled) return false;
        if (cells[endPos.x, endPos.y].Filled) return false;
        return true;
    }

    private bool AddToEnd()
    {
        if (filledPoints.Count < 2) return false;
        Vector2Int pos = filledPoints[filledPoints.Count - 1];
        Cell lastCell = cells[pos.x, pos.y];
        if (cells[startPos.x, startPos.y] != lastCell) return false;
        if (cells[endPos.x, endPos.y].Filled) return false;
        return true;
    }

    private bool AddToStart()
    {
        if (filledPoints.Count < 2) return false;
        Vector2Int pos = filledPoints[0];
        Cell lastCell = cells[pos.x, pos.y];
        if (cells[startPos.x, startPos.y] != lastCell) return false;
        if (cells[endPos.x, endPos.y].Filled) return false;
        return true;
    }

    private bool RemoveFromEnd()
    {
        if (filledPoints.Count < 2) return false;
        Vector2Int pos = filledPoints[filledPoints.Count - 1];
        Cell lastCell = cells[pos.x, pos.y];
        if (cells[startPos.x, startPos.y] != lastCell) return false;
        pos = filledPoints[filledPoints.Count - 2];
        lastCell = cells[pos.x, pos.y];
        if (cells[endPos.x, endPos.y] != lastCell) return false;
        return true;
    }

    private bool RemoveFromStart()
    {
        if (filledPoints.Count < 2) return false;
        Vector2Int pos = filledPoints[0];
        Cell lastCell = cells[pos.x, pos.y];
        if (cells[startPos.x, startPos.y] != lastCell) return false;
        pos = filledPoints[1];
        lastCell = cells[pos.x, pos.y];
        if (cells[endPos.x, endPos.y] != lastCell) return false;
        return true;
    }

    private void RemoveEmpty()
    {
        if (filledPoints.Count != 1) return;
        cells[filledPoints[0].x, filledPoints[0].y].Remove();
        filledPoints.RemoveAt(0);
    }

    public bool IsValid(Vector2Int pos)   // ← Editor에서도 쓰니까 public로
    {
        if (_level == null) return false;
        return pos.x >= 0 && pos.y >= 0 && pos.x < _level.Row && pos.y < _level.Col;
    }

    // ====== Editor Utilities ======
#if UNITY_EDITOR

    [ContextMenu("Save (Overwrite)")]
    public void SaveOverwrite()
    {
        if (_level == null)
        {
            Debug.LogWarning("Level 에셋이 비어있습니다. 'Save As New...'를 사용하세요.");
            return;
        }
        SyncGridToLevel(_level);
        EditorUtility.SetDirty(_level);
        AssetDatabase.SaveAssets();
        Debug.Log($"Saved: {_level.name}");
    }

    [ContextMenu("Save As New...")]
    public void SaveAsNew()
    {
        string path = EditorUtility.SaveFilePanelInProject(
            "Save Level As", "Level_New", "asset",
            "저장 위치를 선택하세요 (예: Assets/Levels/Stage_01.asset)",
            "Assets/Levels"
        );
        if (string.IsNullOrEmpty(path)) return;

        var newLevel = ScriptableObject.CreateInstance<Level>();
        newLevel.Row = _row;
        newLevel.Col = _col;
        newLevel.Data = CaptureGridData();

        AssetDatabase.CreateAsset(newLevel, path);
        AssetDatabase.SaveAssets();

        Selection.activeObject = newLevel;
        _level = newLevel;

        Debug.Log($"Created: {path}");
    }

    [ContextMenu("Load From Assigned Level")]
    public void LoadFromAssignedLevel()
    {
        if (_level == null) { Debug.LogWarning("Level 에셋을 먼저 할당하세요."); return; }

        _row = _level.Row; _col = _level.Col;

        // 기존 셀 제거
        if (cells != null)
        {
            foreach (var cell in cells)
                if (cell != null) DestroyImmediate(cell.gameObject);
        }

        cells = new Cell[_row, _col];
        SpawnLevel();  // _level.Data로 Init
        Debug.Log($"Loaded: {_level.name}");
    }

    [ContextMenu("Spawn Level (Editor Mode)")]
    public void SpawnLevelInEditor()
    {
        // 기존 셀 제거
        if (cells != null)
        {
            foreach (var c in cells)
                if (c != null) DestroyImmediate(c.gameObject);
        }

        cells = new Cell[_row, _col];

        for (int r = 0; r < _row; r++)
        {
            for (int c = 0; c < _col; c++)
            {
                var cell = PrefabUtility.InstantiatePrefab(_cellPrefab, transform) as Cell;
                cell.Init((_level != null && _level.Data != null && _level.Data.Count == _row * _col)
                          ? _level.Data[r * _col + c]
                          : 0);
                cell.transform.position = new Vector3(c + 0.5f, r + 0.5f, 0f);
                cells[r, c] = cell;
            }
        }

        var cam = Camera.main;
        if (cam != null)
        {
            cam.transform.position = new Vector3(_col / 2f, _row / 2f, -10f);
            cam.orthographicSize = Mathf.Max(_row, _col) + 2f;
        }

        Debug.Log("✅ 에디터 모드에서 셀 생성 완료!");
    }

    // --- Inspector 없이 Scene 뷰에서 좌클릭으로 토글 ---
    [CustomEditor(typeof(LevelGenerator))]
    public class LevelGeneratorEditor : Editor
    {
        private LevelGenerator generator;

        private void OnEnable()
        {
            generator = (LevelGenerator)target;
            SceneView.duringSceneGui += OnSceneGUIHandler;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUIHandler;
        }

        private void OnSceneGUIHandler(SceneView sceneView)
        {
            Event e = Event.current;

            // 좌클릭이나 드래그할 때 실행
            if ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && e.button == 0)
            {
                // 1. 마우스 위치에서 카메라가 보는 방향으로 레이저 발사
                Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);

                // 2. ★ 중요: Z=0 평면과의 교차점 계산 (평면 객체 없이 순수 수학으로 계산)
                // 공식: t = (타겟Z - 레이저시작.z) / 레이저방향.z
                // 우리는 타겟Z가 0이므로: t = -ray.origin.z / ray.direction.z
                float t = -ray.origin.z / ray.direction.z;

                // 3. 정확한 바닥 위치(World Position) 가져오기
                Vector3 world = ray.GetPoint(t);

                // 4. 좌표 계산 (FloorToInt가 가장 정확합니다)
                // 셀은 0.5위치에 있지만, 0.0~1.0 범위를 0번 인덱스로 잡으려면 Floor가 맞습니다.
                var gridPos = new Vector2Int(Mathf.FloorToInt(world.y), Mathf.FloorToInt(world.x));

                // 5. 유효성 검사 및 칠하기
                if (generator.IsValid(gridPos))
                {
                    var cell = generator.GetCell(gridPos);
                    if (cell != null)
                    {
                        // 실행 취소(Ctrl+Z) 지원을 위한 기록
                        Undo.RecordObject(cell, "Paint Cell");

                        // 브러시 적용
                        TileType brush = generator.CurrentBrush;

                        // 이미 같은 타입이면 굳이 또 안 칠함 (최적화)
                        if (cell.Type != brush)
                        {
                            cell.SetType(brush);
                            generator.UpdateLevelData(gridPos, (int)brush);

                            EditorUtility.SetDirty(cell);
                            if (generator.LevelAsset != null) EditorUtility.SetDirty(generator.LevelAsset);
                        }
                    }
                }

                // 이벤트를 여기서 썼으니 다른 놈(카메라 회전 등)은 건드리지 마라
                e.Use();
            }
        }
    }

    [ContextMenu("Full Reset (Delete All and Respawn Empty Grid)")]
    public void FullReset()
    {
        Debug.Log("🔥 Full Reset 실행");

        // 1) 모든 기존 Cell 게임오브젝트 삭제
        List<GameObject> toDelete = new List<GameObject>();
        foreach (Transform child in transform)
        {
            if (child.GetComponent<Cell>() != null)
                toDelete.Add(child.gameObject);
        }

        foreach (var obj in toDelete)
            DestroyImmediate(obj);

        // 2) Level 데이터 초기화
        if (_level != null)
        {
            _level.Data = new List<int>(_row * _col);
            for (int i = 0; i < _row * _col; i++)
                _level.Data.Add((int)TileType.Empty);

            EditorUtility.SetDirty(_level);
        }

        // 3) 셀 배열 다시 생성
        cells = new Cell[_row, _col];

        // 4) 새로 Empty 셀들 respawn
        for (int r = 0; r < _row; r++)
        {
            for (int c = 0; c < _col; c++)
            {
                var cell = PrefabUtility.InstantiatePrefab(_cellPrefab, transform) as Cell;
                cell.Init(TileType.Empty);
                cell.transform.position = new Vector3(c + 0.5f, r + 0.5f, 0f);
                cells[r, c] = cell;
            }
        }

        Debug.Log("🧹 Full Reset 완료! 모든 셀이 Empty로 재생성되었습니다.");
    }

    // Editor에서 쓰는 Helper
    public Cell GetCell(Vector2Int pos)
    {
        // 1. 유효 범위 체크
        if (!IsValid(pos)) return null;

        // 2. ★ 중요: 기억상실(Null) 체크!
        // 배열이 비어있다면, 화면에 있는 애들을 다시 찾아서 채워넣는다.
        if (cells == null || cells.Length == 0)
        {
            RebuildGridReference();
        }

        // 3. 그래도 없으면 진짜 없는 것
        if (cells == null) return null;

        return cells[pos.x, pos.y];
    }

    // 화면에 이미 있는 Cell들을 찾아서 배열에 다시 연결하는 함수
    private void RebuildGridReference()
    {
        cells = new Cell[_row, _col];

        // 내 자식으로 있는 모든 Cell을 찾는다
        foreach (Transform child in transform)
        {
            Cell c = child.GetComponent<Cell>();
            if (c != null)
            {
                // 위치를 기반으로 인덱스 역계산 (x=0.5 -> col=0)
                int col = Mathf.FloorToInt(child.localPosition.x);
                int row = Mathf.FloorToInt(child.localPosition.y);

                // 범위 안에 있다면 배열에 연결
                if (row >= 0 && row < _row && col >= 0 && col < _col)
                {
                    cells[row, col] = c;
                }
            }
        }
    }

    public void UpdateLevelData(Vector2Int pos, int value)  // value = (int)TileType
    {
        if (_level == null) return;
        if (!IsValid(pos)) return;
        if (_level.Data == null) _level.Data = new List<int>(_row * _col);
        _level.Data[pos.x * _col + pos.y] = value;
    }

    private void SyncGridToLevel(Level target)
    {
        target.Row = _row;
        target.Col = _col;
        target.Data = CaptureGridData();
    }

    private List<int> CaptureGridData()
    {
        var list = new List<int>(_row * _col);
        for (int r = 0; r < _row; r++)
        {
            for (int c = 0; c < _col; c++)
            {
                var cell = cells[r, c];
                if (cell != null)
                    list.Add((int)cell.Type);   // ✅ 현재 타일 타입 코드 저장
                else
                    list.Add((int)TileType.Empty);
            }
        }
        return list;
    }

#endif
}
