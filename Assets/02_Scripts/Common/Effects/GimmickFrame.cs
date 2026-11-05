using Cysharp.Threading.Tasks;
using DG.Tweening;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

// 기믹이 판에 거는 네모 액자 하나의 그림. 테두리 네 변과, 남은 시간이나 진행을 아래부터 차오르며 보여 주는 안쪽 면으로 이뤄진다.
// 1x1 네모 그림 프리팹을 풀에서 꺼내 늘려 쓰므로, 다 쓰면 Despawn으로 돌려준다.
public sealed class GimmickFrame
{
    // 테두리 네 변과 안쪽 면. 풀을 미리 채울 때 액자 하나에 드는 그림 수다.
    public const int SPRITE_COUNT = EDGE_COUNT + 1;

    private const int EDGE_COUNT = 4;
    private const float VERTICAL_ANGLE = 90f;

    private readonly SpriteRenderer[] _edges = new SpriteRenderer[EDGE_COUNT];
    private readonly SpriteRenderer _fill;
    private readonly Vector2 _size;
    private readonly float _edgeWidth;
    private Vector3 _center;

    public GimmickFrame(GameObject prefab, Vector2 size, float edgeWidth, Color edgeColor, Color fillColor)
    {
        _size = size;
        _edgeWidth = edgeWidth;

        for (int i = 0; i < EDGE_COUNT; i++)
        {
            _edges[i] = GameManager.Pool.SpawnSprite(prefab, Vector3.zero, 0f, Vector2.zero, edgeColor);
        }

        _fill = GameManager.Pool.SpawnSprite(prefab, Vector3.zero, 0f, Vector2.zero, fillColor);
    }

    public Vector3 Center { get { return _center; } }

    // 위·아래 변은 굵기만큼 길게 그어 모서리가 비지 않게 한다.
    public void Place(Vector3 center)
    {
        _center = center;

        Vector3 halfWidth = new Vector3(_size.x * 0.5f, 0f, 0f);
        Vector3 halfHeight = new Vector3(0f, _size.y * 0.5f, 0f);
        Vector3 horizontal = new Vector3(_size.x + _edgeWidth, _edgeWidth, 1f);
        Vector3 vertical = new Vector3(_size.y, _edgeWidth, 1f);

        PlaceEdge(_edges[0], center + halfHeight, 0f, horizontal);
        PlaceEdge(_edges[1], center - halfHeight, 0f, horizontal);
        PlaceEdge(_edges[2], center + halfWidth, VERTICAL_ANGLE, vertical);
        PlaceEdge(_edges[3], center - halfWidth, VERTICAL_ANGLE, vertical);
    }

    public bool Contains(Vector3 position)
    {
        return Mathf.Abs(position.x - _center.x) <= _size.x * 0.5f && Mathf.Abs(position.y - _center.y) <= _size.y * 0.5f;
    }

    // 액자 오른쪽 아래, 서명이 들어갈 자리.
    public Vector3 GetSignaturePoint(float inset)
    {
        return _center + new Vector3(_size.x * 0.5f - inset, -_size.y * 0.5f + inset, 0f);
    }

    public void SetEdgeColor(Color color)
    {
        for (int i = 0; i < EDGE_COUNT; i++)
        {
            _edges[i].color = color;
        }
    }

    public void SetFillColor(Color color)
    {
        _fill.color = color;
    }

    // 0이면 비어 있고 1이면 안쪽이 다 찬다. 아래 변에서부터 차오른다.
    public void SetFill(float progress)
    {
        ApplyFill(progress, -1f);
    }

    // SetFill과 같되 위 변에서부터 내려온다.
    public void SetFillDown(float progress)
    {
        ApplyFill(progress, 1f);
    }

    public void AddFades(float edgeAlpha, float fillAlpha, float duration, List<UniTask> fades, CancellationToken token)
    {
        for (int i = 0; i < EDGE_COUNT; i++)
        {
            fades.Add(_edges[i].DOFade(edgeAlpha, duration).ToUniTaskKillOnCancel(token));
        }

        fades.Add(_fill.DOFade(fillAlpha, duration).ToUniTaskKillOnCancel(token));
    }

    public void Despawn()
    {
        for (int i = 0; i < EDGE_COUNT; i++)
        {
            GameManager.Pool.DespawnSprite(_edges[i]);
        }

        GameManager.Pool.DespawnSprite(_fill);
    }

    // side가 -1이면 아래 변, 1이면 위 변에 붙여 채운다.
    private void ApplyFill(float progress, float side)
    {
        float height = _size.y * Mathf.Clamp01(progress);

        _fill.transform.position = _center + new Vector3(0f, (_size.y - height) * 0.5f * side, 0f);
        _fill.transform.localScale = new Vector3(_size.x, height, 1f);
    }

    private static void PlaceEdge(SpriteRenderer edge, Vector3 position, float angle, Vector3 scale)
    {
        edge.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, 0f, angle));
        edge.transform.localScale = scale;
    }
}
