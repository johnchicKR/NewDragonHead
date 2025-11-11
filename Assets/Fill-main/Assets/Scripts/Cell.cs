using UnityEngine;

public class Cell : MonoBehaviour
{
    [HideInInspector] public bool Blocked;
    [HideInInspector] public bool Filled;

    [Header("Base (점/색)")]
    [SerializeField] private Color _blockedColor = new Color(0.15f, 0.15f, 0.15f, 1f);
    [SerializeField] private Color _emptyColor = Color.white;
    [SerializeField] private Color _filledColor = new Color(0.2f, 0.9f, 0.2f, 1f);
    [SerializeField] private SpriteRenderer _cellRenderer; // 한 개짜리 원 스프라이트

    [Header("Dragon Path (Optional)")]
    [SerializeField] private SpriteRenderer _pathRenderer;     // 용 몸통/코너 전용 레이어 (자식 오브젝트)
    [SerializeField] private Sprite _pathStraightSprite;       // 직선 몸통 스프라이트
    [SerializeField] private Sprite _pathCornerSprite;         // 코너 스프라이트

    public void Init(int fill)
    {
        Blocked = (fill == 1);
        Filled = false;

        _cellRenderer.color = Blocked ? _blockedColor : _emptyColor;

        ClearPathVisual();
    }

    public void Add()
    {
        Filled = true;
        _cellRenderer.color = _filledColor;
        // Path 비주얼은 GameManager에서 따로 갱신하므로 여기선 건들지 않는다.
    }

    public void Remove()
    {
        Filled = false;
        _cellRenderer.color = _emptyColor;
        // Path 비주얼은 GameManager가 RefreshCellsPathVisual에서 다시 계산
    }

    public void ChangeState()
    {
        // 에디터에서 Blocked 토글용 (LevelGenerator)
        Blocked = !Blocked;
        Filled = Blocked;

        _cellRenderer.color = Blocked ? _blockedColor : _emptyColor;

        ClearPathVisual();
    }

    // ===== 용 경로 비주얼 =====

    public void ClearPathVisual()
    {
        if (_pathRenderer == null) return;

        _pathRenderer.enabled = false;
        _pathRenderer.sprite = null;
        _pathRenderer.transform.localRotation = Quaternion.identity;
        _pathRenderer.transform.localScale = Vector3.one;
    }

    // 직선 몸통 (칸 중앙에, 가로/세로만 회전)
    public void SetStraightVisual(bool horizontal)
    {
        // 일부러 비워둠
    }

    // 코너 몸통 (ㄱ 모양, 각도만 넘겨받음)
    public void SetCornerVisual(float angle)
    {
        if (_pathRenderer == null || _pathCornerSprite == null) return;

        _pathRenderer.enabled = true;
        _pathRenderer.sprite = _pathCornerSprite;
        _pathRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, angle);

        _pathRenderer.transform.localScale = Vector3.one;
    }
}
