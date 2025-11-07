using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [SerializeField] private Level _level;
    [SerializeField] private Cell _cellPrefab;
    [SerializeField] private Transform _edgePrefab;

    private bool hasGameFinished;
    private Cell[,] cells;
    private List<Vector2Int> filledPoints;
    private List<Transform> edges;

    private Vector2Int startPos;
    private Vector2Int endPos;

    private void Awake()
    {
        Instance = this;

        hasGameFinished = false;
        cells = new Cell[_level.Row, _level.Col];
        filledPoints = new List<Vector2Int>();
        edges = new List<Transform>();

        SpawnLevel();
    }

    private void SpawnLevel()
    {
        Vector3 cameraPos = Camera.main.transform.position;
        cameraPos.x = _level.Col * 0.5f;
        cameraPos.y = _level.Row * 0.5f;
        Camera.main.transform.position = cameraPos;
        Camera.main.orthographicSize = Mathf.Max(_level.Row, _level.Col) + 2f;

        for (int r = 0; r < _level.Row; r++)
        {
            for (int c = 0; c < _level.Col; c++)
            {
                Cell cell = Instantiate(_cellPrefab);
                int data = _level.Data[r * _level.Col + c];
                cell.Init(data);
                cell.transform.position = new Vector3(c + 0.5f, r + 0.5f, 0f);

                cells[r, c] = cell;
            }
        }
    }

    private void Update()
    {
        if (hasGameFinished) return;

        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            startPos = new Vector2Int(Mathf.FloorToInt(mouseWorld.y), Mathf.FloorToInt(mouseWorld.x));
            endPos = startPos;

            if (IsValid(startPos))
            {
                AddEmpty();
            }
        }
        else if (Input.GetMouseButton(0))
        {
            Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            endPos = new Vector2Int(Mathf.FloorToInt(mouseWorld.y), Mathf.FloorToInt(mouseWorld.x));

            if (IsNeighbour())
            {
                if (AddEmpty())
                {
                    CreateEdge();
                }
                else if (RemoveFromEnd())
                {
                    RemoveEdge();
                }

                startPos = endPos;

                if (CheckWin())
                {
                    hasGameFinished = true;
                    StartCoroutine(GameFinished());
                }
            }
        }
    }

    private bool IsValid(Vector2Int pos)
    {
        return pos.x >= 0 && pos.y >= 0 && pos.x < _level.Row && pos.y < _level.Col;
    }

    private bool IsNeighbour()
    {
        int dx = Mathf.Abs(startPos.x - endPos.x);
        int dy = Mathf.Abs(startPos.y - endPos.y);
        return dx + dy == 1;
    }

    private bool AddEmpty()
    {
        if (!IsValid(endPos)) return false;
        if (cells[endPos.x, endPos.y].Blocked) return false;
        if (filledPoints.Contains(endPos)) return false;

        cells[endPos.x, endPos.y].Add();
        filledPoints.Add(endPos);
        return true;
    }

    private bool RemoveFromEnd()
    {
        if (filledPoints.Count == 0) return false;
        if (filledPoints[filledPoints.Count - 1] == endPos)
        {
            Vector2Int last = filledPoints[filledPoints.Count - 1];
            cells[last.x, last.y].Remove();
            filledPoints.RemoveAt(filledPoints.Count - 1);
            return true;
        }
        return false;
    }

    private void RemoveEdge()
    {
        if (edges.Count == 0) return;
        Transform lastEdge = edges[edges.Count - 1];
        Destroy(lastEdge.gameObject);
        edges.RemoveAt(edges.Count - 1);
    }

    private void CreateEdge()
    {
        Transform edge = Instantiate(_edgePrefab);
        edges.Add(edge);
        edge.position = new Vector3(
            startPos.y * 0.5f + 0.5f + endPos.y * 0.5f,
            startPos.x * 0.5f + 0.5f + endPos.x * 0.5f,
            0f
        );
        bool horizontal = (endPos.y - startPos.y) != 0;
        edge.eulerAngles = new Vector3(0, 0, horizontal ? 90f : 0f);
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
        yield return new WaitForSeconds(1f);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
