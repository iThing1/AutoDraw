using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

// 플레이어 둘레를 격자로 나눠 한 칸씩 걸러 먹물을 떨어뜨리고, 다음에는 반대 칸에 떨어뜨린다.
// 탄을 쏘지 않고 칸 단위로 판정해, 안전한 칸으로 옮겨 다니게 한다. 격자는 시작할 때 플레이어 자리에 맞춰 고정된다.
[CreateAssetMenu(fileName = "CheckerHazardPattern", menuName = "Scriptable Objects/Hazard/Checker")]
public class CheckerHazardPattern : HazardPattern
{
    [Header("예고")]
    [SerializeField] private AudioClip _warningSound;
    [SerializeField] private GameObject _cellPrefab;
    [SerializeField] private Color _telegraphColor = new Color(0.1f, 0.1f, 0.12f, 0.3f);
    [SerializeField, Min(0f)] private float _warnDuration = 1f;

    [Header("격자")]
    [SerializeField, Min(1)] private int _columns = 7;
    [SerializeField, Min(1)] private int _rows = 5;
    [SerializeField, Min(0.5f)] private float _cellSize = 4f;
    // 칸 사이를 조금 띄워 경계가 보이게 한다.
    [SerializeField, Range(0.5f, 1f)] private float _cellFillRatio = 0.92f;

    [Header("낙하")]
    [SerializeField, Min(1)] private int _waveCount = 2;
    [SerializeField, Min(0f)] private float _waveInterval = 0.4f;
    [SerializeField] private Color _impactColor = new Color(0.05f, 0.05f, 0.06f, 0.85f);
    [SerializeField, Min(0f)] private float _impactFadeDuration = 0.3f;
    [SerializeField] private ShotSettings _hit = new();
    [SerializeField] private ImpactSettings _impact = new();

    // 탄은 쏘지 않는다. 한 칸에 예고(바탕·채움)가 뜬 채로 앞 물결의 번쩍임이 남아 있을 수 있어 칸마다 세 장씩 만든다.
    public override void Prewarm(GameObject defaultProjectile)
    {
        if (null != _cellPrefab)
        {
            GameManager.Pool.Prewarm(_cellPrefab, (_columns * _rows + 1) / 2 * 3);
        }
    }

    public override async UniTask PlayAsync(AutoAttacker shooter, CancellationToken token)
    {
        if (null == _cellPrefab)
        {
            Debug.LogError($"체크무늬 낙하 : {name}에 칸 프리팹이 지정되지 않았습니다.");
            return;
        }

        GameManager.Sound.PlaySfx(_warningSound);

        Vector3 origin = GetGridOrigin(shooter.Target.FollowTransform.position);
        int parity = UnityEngine.Random.Range(0, 2);

        for (int wave = 0; wave < _waveCount; wave++)
        {
            await PlayWaveAsync(shooter, origin, (parity + wave) % 2, token);

            if (wave < _waveCount - 1)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(_waveInterval), cancellationToken: token);
            }
        }
    }

    private async UniTask PlayWaveAsync(AutoAttacker shooter, Vector3 origin, int parity, CancellationToken token)
    {
        List<Vector3> cells = GetCells(origin, parity);
        List<UniTask> telegraphs = new List<UniTask>(cells.Count);
        float size = _cellSize * _cellFillRatio;

        for (int i = 0; i < cells.Count; i++)
        {
            telegraphs.Add(Telegraph.PlayAsync(_cellPrefab, cells[i], 0f, new Vector2(size, size), Vector2.zero, _telegraphColor, _warnDuration, token));
        }

        await UniTask.WhenAll(telegraphs);

        ImpactPlayer.Play(_impact, origin + new Vector3(_columns, _rows, 0f) * (_cellSize * 0.5f));

        for (int i = 0; i < cells.Count; i++)
        {
            FlashCell(cells[i], size);
        }

        // 환경 탄막은 보스의 공격이 아니라서 게이지를 쌓지 않는다. 방패를 들고 있으면 막힌다.
        if (IsInCells(shooter.Target.FollowTransform.position, cells))
        {
            shooter.Strike(_hit, shooter.Target.GetAimPoint(Vector3.zero), 0f);
        }
    }

    // 떨어진 칸이 진하게 번쩍였다가 사라진다. 판정은 이미 끝나 기다리지 않는다.
    private void FlashCell(Vector3 center, float size)
    {
        SpriteRenderer cell = PooledSprite.Spawn(_cellPrefab, center, 0f, new Vector2(size, size), _impactColor);

        cell.DOFade(0f, _impactFadeDuration)
            .SetEase(Ease.InQuad)
            .OnComplete(() => PooledSprite.Despawn(cell));
    }

    // 플레이어가 가운데 칸에 오도록 격자 왼쪽 아래 모서리를 잡는다.
    private Vector3 GetGridOrigin(Vector3 center)
    {
        return center - new Vector3(_columns * _cellSize, _rows * _cellSize, 0f) * 0.5f;
    }

    private List<Vector3> GetCells(Vector3 origin, int parity)
    {
        List<Vector3> cells = new List<Vector3>((_columns * _rows + 1) / 2);

        for (int column = 0; column < _columns; column++)
        {
            for (int row = 0; row < _rows; row++)
            {
                if (parity == (column + row) % 2)
                {
                    cells.Add(origin + new Vector3((column + 0.5f) * _cellSize, (row + 0.5f) * _cellSize, 0f));
                }
            }
        }

        return cells;
    }

    private bool IsInCells(Vector3 position, List<Vector3> cells)
    {
        float half = _cellSize * 0.5f;

        for (int i = 0; i < cells.Count; i++)
        {
            if (Mathf.Abs(position.x - cells[i].x) <= half && Mathf.Abs(position.y - cells[i].y) <= half)
            {
                return true;
            }
        }

        return false;
    }
}
