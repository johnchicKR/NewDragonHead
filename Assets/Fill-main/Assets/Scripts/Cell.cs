using UnityEngine;

public class Cell : MonoBehaviour
{
    [HideInInspector] public bool Blocked;
    [HideInInspector] public bool Filled;

    // ★ [추가] 리셋될 때 돌아갈 원래 상태를 기억하는 변수
    private bool _originBlocked;

    [Header("Base (점/색)")]
    [SerializeField] private Color _blockedColor = new Color(0.15f, 0.15f, 0.15f, 1f);
    [SerializeField] private Color _emptyColor = Color.white;
    [SerializeField] private Color _filledColor = new Color(0.2f, 0.9f, 0.2f, 1f);
    [SerializeField] private SpriteRenderer _cellRenderer;

    [Header("Icon (타입별 아이콘)")]
    [SerializeField] private SpriteRenderer _iconRenderer;

    [SerializeField] private Sprite _emptySprite;
    [SerializeField] private Sprite _blockSprite;
    [SerializeField] private Sprite _keySprite;
    [SerializeField] private Sprite _lockSprite;
    [SerializeField] private Sprite _arrowSprite;
    [SerializeField] private Sprite _poisonSprite;
    [SerializeField] private Sprite _switchASprite;
    [SerializeField] private Sprite _toggleASprite;
    [SerializeField] private Sprite _portalSprite;

    [Header("Dragon Path (Optional)")]
    [SerializeField] private SpriteRenderer _pathRenderer;
    [SerializeField] private Sprite _pathStraightSprite;
    [SerializeField] private Sprite _pathCornerSprite;

    public TileType Type { get; private set; }

    public void SetType(TileType newType)
    {
        Init(newType);
    }

    public void Init(int code)
    {
        TileType type = (TileType)code;
        Init(type);
    }

    public void Init(TileType type)
    {
        Type = type;

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
                Blocked = true;
                _cellRenderer.color = _blockedColor;
                break;

            // ★ 토글 벽은 처음에 '벽(Blocked)' 상태로 시작!
            case TileType.ToggleA:
                Blocked = true;
                _cellRenderer.color = _blockedColor;
                break;

            default:
                Blocked = false;
                _cellRenderer.color = _emptyColor;
                break;
        }

        // ★ [추가] 초기화가 끝난 직후의 상태를 '원본 상태'로 저장해둠
        _originBlocked = Blocked;

        UpdateVisualByType();
        ClearPathVisual();
    }

    private void UpdateVisualByType()
    {
        // 1) 기본 배경 색
        if (Type == TileType.Block || Type == TileType.Lock || (Type == TileType.ToggleA && Blocked))
        {
            _cellRenderer.color = _blockedColor;
        }
        else
        {
            _cellRenderer.color = _emptyColor;
        }

        // 2) 아이콘 스프라이트 & 회전
        if (_iconRenderer == null) return;

        _iconRenderer.enabled = false;
        _iconRenderer.sprite = null;
        _iconRenderer.transform.localRotation = Quaternion.identity;
        _iconRenderer.color = Color.white; // 색상 초기화 (투명도 복구)

        switch (Type)
        {
            case TileType.Empty:
                if (_emptySprite != null) { _iconRenderer.enabled = true; _iconRenderer.sprite = _emptySprite; }
                break;
            case TileType.Block:
                if (_blockSprite != null) { _iconRenderer.enabled = true; _iconRenderer.sprite = _blockSprite; }
                break;
            case TileType.Key:
                _iconRenderer.enabled = true; _iconRenderer.sprite = _keySprite;
                break;
            case TileType.Lock:
                _iconRenderer.enabled = true; _iconRenderer.sprite = _lockSprite;
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
                _iconRenderer.enabled = true; _iconRenderer.sprite = _poisonSprite;
                break;
            case TileType.SwitchA:
                _iconRenderer.enabled = true; _iconRenderer.sprite = _switchASprite;
                break;
            case TileType.ToggleA:
                _iconRenderer.enabled = true; _iconRenderer.sprite = _toggleASprite;
                break;
            case TileType.PortalA1:
            case TileType.PortalA2:
                _iconRenderer.enabled = true; _iconRenderer.sprite = _portalSprite;
                break;
        }
    }

    public void Add()
    {
        Filled = true;
        _cellRenderer.color = _filledColor;
    }

    public void Remove()
    {
        Filled = false;
        _cellRenderer.color = _emptyColor;
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
        Type = TileType.Empty;
        UpdateVisualByType();
    }

    // ===== 토글 관련 함수들 =====

    public void ToggleState()
    {
        // 1. 상태 반전
        Blocked = !Blocked;
        UpdateToggleVisual();
    }

    // ★ [추가] 경로 리셋 시 호출: 원래 상태로 강제 복구
    public void ResetToggleState()
    {
        Blocked = _originBlocked; // Init때 저장해둔 값으로 복구
        UpdateToggleVisual();
    }

    // 비주얼 업데이트 로직 분리 (중복 방지)
    private void UpdateToggleVisual()
    {
        if (Blocked)
        {
            // 벽이 됨
            _cellRenderer.color = _blockedColor;
            if (_iconRenderer != null) _iconRenderer.color = Color.white; // 불투명
        }
        else
        {
            // 빈 땅이 됨
            _cellRenderer.color = _emptyColor;
            // 빈 땅 느낌 (반투명)
            if (_iconRenderer != null)
            {
                var c = _iconRenderer.color;
                c.a = 0.3f;
                _iconRenderer.color = c;
            }
        }
    }
}