using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Threading;
using UnityEngine;

// 먹물 범람. 보스가 두 손을 들면 종이 전체가 점점 먹물로 덮이고, 끝나는 순간 플레이어 물감 위에 서 있어야 버틴다.
// 칠한 자리가 없어도 해낼 수 있도록, 시작할 때 플레이어 둘레에 물감 섬 몇 개를 깔아 준다.
[CreateAssetMenu(fileName = "InkFloodGimmick", menuName = "Scriptable Objects/Boss/Gimmick/InkFlood")]
public class InkFloodGimmick : PunishingBossGimmick
{
    [Header("예고")]
    [SerializeField, Min(0f)] private float _warnDuration = 4f;
    [SerializeField] private GameObject _floodPrefab;
    [SerializeField] private Color _floodColor = new Color(0.06f, 0.06f, 0.08f, 0.55f);
    // 종이를 다 덮도록 넉넉히 잡는다.
    [SerializeField, Min(1f)] private float _floodSize = 200f;

    [Header("물감 섬")]
    [SerializeField, Min(0)] private int _islandCount = 3;
    [SerializeField] private FloatRange _islandDistance = new FloatRange(5f, 9f);
    [SerializeField] private SplatStyle _islandStyle;
    [SerializeField, Min(0f)] private float _islandScale = 2.2f;
    // 발 크기. 물감 자국과 이만큼 겹쳐 있으면 물감 위에 선 것으로 본다.
    [SerializeField, Min(0f)] private float _footRadius = 0.3f;

    [Header("범람")]
    [SerializeField, Range(0f, 1f)] private float _surgeAlpha = 0.85f;
    [SerializeField, Min(0f)] private float _surgeDuration = 0.15f;
    [SerializeField, Min(0f)] private float _holdDuration = 0.5f;
    [SerializeField, Min(0f)] private float _recedeDuration = 0.6f;
    [SerializeField] private ImpactSettings _surgeImpact = new();

    [Header("어둠")]
    // 범람이 다가오는 동안 화면 전체를 살짝 어둡게 한다. 유닛까지 덮도록 이펙트 정렬 층에 깐다.
    [SerializeField] private GameObject _dimPrefab;
    [SerializeField, Range(0f, 1f)] private float _dimAlpha = 0.25f;
    [SerializeField, Min(0f)] private float _dimFadeDuration = 0.6f;

    public override void Prewarm()
    {
        if (null != _floodPrefab)
        {
            GameManager.Pool.Prewarm(_floodPrefab, 1);
        }

        if (null != _dimPrefab)
        {
            GameManager.Pool.Prewarm(_dimPrefab, 1);
        }
    }

    public override async UniTask<bool> PlayAsync(BattleUnit boss, CancellationToken token)
    {
        if (null == _floodPrefab)
        {
            Debug.LogError($"먹물 범람 : {name}에 먹물 프리팹이 지정되지 않았습니다.");
            return true;
        }

        AutoAttacker attacker = boss.Attacker;
        AttackTarget target = attacker.Target;
        Color clear = new Color(_floodColor.r, _floodColor.g, _floodColor.b, 0f);
        SpriteRenderer flood = Telegraph.Spawn(_floodPrefab, boss.transform.position, 0f, new Vector2(_floodSize, _floodSize), clear);
        SpriteRenderer dim = SpawnDim(boss.transform.position);

        try
        {
            if (null != attacker.Hands)
            {
                attacker.Hands.PoseAsync(HandGesture.BothRaise, target.GetAimPoint(Vector3.zero), _warnDuration, token).Forget();
            }

            LeaveIslands(attacker, target.GetAimPoint(Vector3.zero));
            FadeDim(dim, _dimAlpha);

            await flood.DOFade(_floodColor.a, _warnDuration)
                .SetEase(Ease.InQuad)
                .ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, token);

            await flood.DOFade(_surgeAlpha, _surgeDuration)
                .ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, token);

            ImpactPlayer.Play(_surgeImpact, target.GetAimPoint(Vector3.zero));

            bool isSafe = null != attacker.PaintCanvas && attacker.PaintCanvas.IsPaintedAt(target.FollowTransform.position, _footRadius);

            if (false == isSafe)
            {
                PunishTarget(boss);
            }

            await UniTask.Delay(TimeSpan.FromSeconds(_holdDuration), cancellationToken: token);

            FadeDim(dim, 0f);

            await flood.DOFade(0f, _recedeDuration)
                .ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, token);

            return isSafe;
        }
        finally
        {
            Telegraph.Despawn(flood);
            Telegraph.Despawn(dim);

            if (null != attacker.Hands)
            {
                attacker.Hands.ReturnToRest();
            }
        }
    }

    private SpriteRenderer SpawnDim(Vector3 center)
    {
        if (null == _dimPrefab)
        {
            return null;
        }

        return Telegraph.Spawn(_dimPrefab, center, 0f, new Vector2(_floodSize, _floodSize), Color.clear);
    }

    private void FadeDim(SpriteRenderer dim, float alpha)
    {
        if (null == dim)
        {
            return;
        }

        dim.DOKill();
        dim.DOFade(alpha, _dimFadeDuration);
    }

    // 플레이어 팔레트 색으로 칠해, 플레이어가 칠한 물감과 같은 것임을 알 수 있게 한다.
    private void LeaveIslands(AutoAttacker attacker, Vector3 center)
    {
        PaintCanvas canvas = attacker.PaintCanvas;
        PaintPalette palette = attacker.Target.Unit.Data.Palette;

        if (null == canvas || null == _islandStyle)
        {
            return;
        }

        float startAngle = UnityEngine.Random.Range(0f, 360f);

        for (int i = 0; i < _islandCount; i++)
        {
            Vector3 direction = AngleUtility.ToDirection(startAngle + 360f / _islandCount * i);
            Color color = null != palette ? palette.Pick(i) : Color.white;

            canvas.LeaveSplat(center + direction * _islandDistance.PickRandom(), _islandStyle, color, false, _islandScale);
        }
    }
}
