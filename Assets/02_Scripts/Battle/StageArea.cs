using UnityEngine;

// 전투 스테이지의 바닥 종이 영역. 카메라는 이 밖을 비추지 않고, 유닛은 이 안에서만 움직인다.
// 이동 범위를 숫자로 따로 적지 않아, 종이 크기를 바꿀 때 콜라이더만 고치면 된다.
[RequireComponent(typeof(Collider2D))]
public class StageArea : MonoBehaviour
{
    // 유닛이 종이 가장자리에서 몸 크기 외에 더 떨어져 있을 거리.
    [SerializeField, Min(0f)] private float _edgePadding;

    private Collider2D _collider;

    // 씬 뷰 기즈모처럼 Awake 전에도 불리므로 처음 쓸 때 찾는다.
    public Collider2D Collider
    {
        get
        {
            if (null == _collider)
            {
                _collider = GetComponent<Collider2D>();
            }

            return _collider;
        }
    }

    // 영역에서 몸 크기의 절반과 여백만큼 안쪽. 큰 유닛일수록 가장자리에서 더 떨어져 몸이 종이 밖으로 나가지 않는다.
    public Rect GetMoveArea(BattleUnit unit)
    {
        Bounds stage = Collider.bounds;
        Vector2 inset = unit.GetBodyExtents() + Vector2.one * _edgePadding;
        Vector2 min = (Vector2)stage.min + inset;
        Vector2 max = (Vector2)stage.max - inset;

        // 영역이 몸보다 좁으면 범위가 뒤집히므로 가운데 한 줄로 모은다.
        if (min.x > max.x)
        {
            min.x = max.x = stage.center.x;
        }

        if (min.y > max.y)
        {
            min.y = max.y = stage.center.y;
        }

        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }
}
