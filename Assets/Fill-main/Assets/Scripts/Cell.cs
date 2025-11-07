using UnityEngine;

public class Cell : MonoBehaviour
{
    [HideInInspector] public bool Blocked;
    [HideInInspector] public bool Filled;
    [HideInInspector] public bool Tail;

    [SerializeField] private Color _blockedColor;
    [SerializeField] private Color _emptyColor;
    [SerializeField] private Color _filledColor;
    [SerializeField] private Color _tailColor;
    [SerializeField] private Color _normalColor;
    [SerializeField] private SpriteRenderer _cellRenderer;

    public void Init(int fill)
    {
        Blocked = fill == 1;
        Filled = Blocked;
        _cellRenderer.color = Blocked ? _blockedColor : _emptyColor;
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

    public void ChangeState()
    {
        Blocked = !Blocked;
        Filled = Blocked;
        _cellRenderer.color = Blocked ? _blockedColor : _emptyColor;
    }

    public void SetTail(bool isTail)
    {
        Tail = isTail;
        if (Tail)
        {
            Filled = true;                 // 꼬리는 시작 시 채워진 상태로
            _cellRenderer.color = _tailColor;
        }
        else
        {
            // 꼬리 해제 시엔 일반 채움 색으로 돌리거나, 필요하면 비우기
            _cellRenderer.color = _normalColor;
            // 꼬리만 해제하고 채움은 유지할지/초기화할지 선택
            // Filled = false;             // 리셋 때는 보통 모두 초기화에서 처리
        }
    }
}
