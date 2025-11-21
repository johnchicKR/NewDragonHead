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

    [Header("Icon (타입별 아이콘)")]
    [SerializeField] private SpriteRenderer _iconRenderer;   // 자식 Icon의 SR

    [SerializeField] private Sprite _emptySprite;
    [SerializeField] private Sprite _blockSprite;
    [SerializeField] private Sprite _keySprite;
    [SerializeField] private Sprite _lockSprite;
    [SerializeField] private Sprite _arrowSprite;   // 기본 위쪽 화살표 하나만 써도 됨
    [SerializeField] private Sprite _poisonSprite;
    [SerializeField] private Sprite _switchASprite;
    [SerializeField] private Sprite _toggleASprite;
    [SerializeField] private Sprite _portalSprite;

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
        //Filled = false;

        switch (Type)
        {
            case TileType.Empty:
                Blocked = false;
                _cellRenderer.color = _emptyColor;
                break;

            case TileType.Block:
                Blocked = true;
                _cellRenderer.color = _blockedColor;
                break;

            case TileType.Lock:
                Blocked = true;                 // 🔒 기본적으로 못 지나가는 칸
                _cellRenderer.color = _blockedColor;
                break;

            default:
                Blocked = false;
                _cellRenderer.color = _emptyColor;
                break;
        }

        UpdateVisualByType();

        ClearPathVisual();
    }

    private void UpdateVisualByType()
    {
        // 1) 기본 배경 색 / 점 색
        switch (Type)
        {
            case TileType.Empty:
                Blocked = false;
                _cellRenderer.color = _emptyColor;
                break;

            case TileType.Block:
                Blocked = true;
                _cellRenderer.color = _blockedColor;
                break;

            default:
                // 기믹 타일은 기본적으로 지나갈 수 있는 칸(필요하면 바꿀 수 있음)
                Blocked = false;
                _cellRenderer.color = _emptyColor;
                break;
        }

        // 2) 아이콘 스프라이트 & 회전
        if (_iconRenderer == null) return;

        _iconRenderer.enabled = false;
        _iconRenderer.sprite = null;
        _iconRenderer.transform.localRotation = Quaternion.identity;

        switch (Type)
        {
            case TileType.Empty:
                if (_emptySprite != null)
                {
                    _iconRenderer.enabled = true;
                    _iconRenderer.sprite = _emptySprite;
                }
                break;

            case TileType.Block:
                Blocked = true;
                _cellRenderer.color = _blockedColor;

                if (_blockSprite != null)
                {
                    _iconRenderer.enabled = true;
                    _iconRenderer.sprite = _blockSprite;
                }
                break;

            case TileType.Key:
                _iconRenderer.enabled = true;
                _iconRenderer.sprite = _keySprite;
                break;

            case TileType.Lock:
                _iconRenderer.enabled = true;
                _iconRenderer.sprite = _lockSprite;
                break;

            case TileType.ArrowUp:
            case TileType.ArrowRight:
            case TileType.ArrowDown:
            case TileType.ArrowLeft:
                if (_arrowSprite != null)
                {
                    _iconRenderer.enabled = true;
                    _iconRenderer.sprite = _arrowSprite;

                    float angle = 0f;
                    if (Type == TileType.ArrowUp) angle = 0f;
                    if (Type == TileType.ArrowRight) angle = -90f;
                    if (Type == TileType.ArrowDown) angle = 180f;
                    if (Type == TileType.ArrowLeft) angle = 90f;

                    _iconRenderer.transform.localRotation = Quaternion.Euler(0, 0, angle);
                }
                break;

            case TileType.Poison:
                _iconRenderer.enabled = true;
                _iconRenderer.sprite = _poisonSprite;
                break;

            case TileType.SwitchA:
                _iconRenderer.enabled = true;
                _iconRenderer.sprite = _switchASprite;
                break;

            case TileType.ToggleA:
                _iconRenderer.enabled = true;
                _iconRenderer.sprite = _toggleASprite;
                break;

            case TileType.PortalA1:
            case TileType.PortalA2:
                _iconRenderer.enabled = true;
                _iconRenderer.sprite = _portalSprite;
                break;
        }
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

    public void ConsumeKey()
    {
        if (Type != TileType.Key) return;

        // 이제 이 셀은 "열쇠 없는 일반 빈 칸"으로 취급
        Type = TileType.Empty;
        UpdateVisualByType();   // 아이콘/색깔 다시 적용 (Empty로)
                                // Filled 값은 건드리지 않음 → 이미 지나간 칸 유지
    }
}
