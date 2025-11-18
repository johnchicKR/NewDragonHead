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

    public TileType Type { get; private set; }

    public void SetType(TileType newType)
    {
        // Init 로직을 그대로 재사용하면 편함
        Init(newType);
    }

    // 🔹 1. int를 받는 Init (기존 코드 호환용)
    public void Init(int code)
    {
        TileType type = (TileType)code;   // int → TileType 캐스팅
        Init(type);                       // 아래 실제 로직 호출
    }

    public void Init(TileType type)
    {
        Type = type;
        Filled = false;

        // 기본 Block 판정: 일단은 벽만 막히도록, 나중에 Lock/Toggle 도 포함 가능
        Blocked = (Type == TileType.Block);

        // 타입에 따라 기본 색상 설정 (임시: 나중에 스프라이트로 바꿀 수 있음)
        switch (Type)
        {
            case TileType.Empty:
                _cellRenderer.color = _emptyColor;
                break;

            case TileType.Block:
                _cellRenderer.color = _blockedColor;
                break;

            case TileType.Key:
                _cellRenderer.color = Color.yellow;
                break;

            case TileType.Lock:
                _cellRenderer.color = Color.cyan;
                break;

            case TileType.ArrowUp:
            case TileType.ArrowRight:
            case TileType.ArrowDown:
            case TileType.ArrowLeft:
                _cellRenderer.color = Color.magenta;
                break;

            case TileType.Poison:
                _cellRenderer.color = Color.green;
                break;

            case TileType.SwitchA:
                _cellRenderer.color = new Color(1f, 0.6f, 0.2f);
                break;

            case TileType.ToggleA:
                _cellRenderer.color = new Color(0.8f, 0.3f, 0.3f);
                break;

            case TileType.PortalA1:
            case TileType.PortalA2:
                _cellRenderer.color = Color.blue;
                break;
        }

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
