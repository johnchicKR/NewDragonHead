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
            if (e.type == EventType.MouseDown && e.button == 0)
            {
                Vector3 world = HandleUtility.GUIPointToWorldRay(e.mousePosition).origin;
                var gridPos = new Vector2Int(Mathf.FloorToInt(world.y), Mathf.FloorToInt(world.x));

                if (!generator.IsValid(gridPos)) return;

                var cell = generator.GetCell(gridPos);
                if (cell != null)
                {
                    Undo.RecordObject(cell, "Paint Cell");

                    // ✅ 현재 브러시 타입으로 셀 설정
                    TileType brush = generator.CurrentBrush;
                    cell.SetType(brush);

                    // ✅ Level.Data에 (int)TileType 코드 저장
                    generator.UpdateLevelData(gridPos, (int)brush);

                    EditorUtility.SetDirty(cell);
                    if (generator.LevelAsset != null) EditorUtility.SetDirty(generator.LevelAsset);
                }

                e.Use();
            }
        }
    }

    // Editor에서 쓰는 Helper
    public Cell GetCell(Vector2Int pos)
    {
        if (!IsValid(pos)) return null;
        return cells[pos.x, pos.y];
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
