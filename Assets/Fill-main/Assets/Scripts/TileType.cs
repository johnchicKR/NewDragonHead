// TileType.cs

// using UnityEngine; 도 사실 필요 없지만, 있어도 상관 없음.

public enum TileType
{
    Empty = 0,  // 그냥 지나갈 수 있는 기본 칸
    Block = 1,  // 벽 (지나갈 수 없음)

    Key = 2,  // 열쇠
    Lock = 3,  // 잠긴 문

    ArrowUp = 4,  // 방향 타일 (위로만 나갈 수 있음)
    ArrowRight = 5,  // 오른쪽
    ArrowDown = 6,  // 아래
    ArrowLeft = 7,  // 왼쪽

    Poison = 8,  // 독 (새 버전: 길이 감소)

    SwitchA = 9,  // 스위치 A
    ToggleA = 10, // 토글 블록 A

    PortalA1 = 11, // 포탈 A 입구/출구 1
    PortalA2 = 12, // 포탈 A 입구/출구 2

    // 나중에 B, C 세트 필요하면 여기 계속 추가하면 됨
}
