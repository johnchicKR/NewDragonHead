using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialHand : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float pauseDuration = 0.5f;

    // ★ [추가] 위치 미세 조정을 위한 오프셋 변수
    [Header("Position Adjustment")]
    [SerializeField] private Vector3 handOffset = new Vector3(0.2f, -0.3f, 0f);

    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.sortingOrder = 999;
    }

    public void Show() => gameObject.SetActive(true);
    public void Hide() => gameObject.SetActive(false);

    // 경로를 받아 반복 재생하는 함수
    public void StartPathAnimation(List<Vector2Int> pathPoints)
    {
        Show();
        StopAllCoroutines();
        StartCoroutine(MoveRoutine(pathPoints));
    }

    private IEnumerator MoveRoutine(List<Vector2Int> path)
    {
        if (path == null || path.Count == 0) yield break;

        while (true) // 무한 반복
        {
            // 1. 시작 위치로 순간이동
            Vector2Int start = path[0];
            transform.position = GetWorldPos(start);

            // 깜빡이며 등장 효과 (선택사항)
            Color c = spriteRenderer.color;
            c.a = 0; spriteRenderer.color = c;
            for (float t = 0; t < 1; t += Time.deltaTime * 5)
            {
                c.a = t; spriteRenderer.color = c;
                yield return null;
            }

            yield return new WaitForSeconds(0.2f);

            // 2. 경로 따라 이동
            for (int i = 1; i < path.Count; i++)
            {
                Vector3 targetPos = GetWorldPos(path[i]);

                while (Vector3.Distance(transform.position, targetPos) > 0.01f)
                {
                    transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
                    yield return null;
                }
                transform.position = targetPos; // 정확한 위치 보정
            }

            yield return new WaitForSeconds(pauseDuration);

            // 3. 사라지기
            for (float t = 1; t > 0; t -= Time.deltaTime * 5)
            {
                c.a = t; spriteRenderer.color = c;
                yield return null;
            }
            yield return new WaitForSeconds(0.3f);
        }
    }

    // 그리드 좌표 -> 월드 좌표 변환 (GameManager와 동일한 공식)
    private Vector3 GetWorldPos(Vector2Int gridPos)
    {
        // 기본 위치(셀 중앙) + 오프셋(미세 조정)
        Vector3 basePos = new Vector3(gridPos.y + 0.5f, gridPos.x + 0.5f, -2f);
        return basePos + handOffset;
    }
}