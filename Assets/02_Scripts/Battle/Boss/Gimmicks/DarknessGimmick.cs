using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Threading;
using UnityEngine;

// 암흑. 먹물이 화면을 뒤덮어 플레이어 둘레만 동그랗게 보이고, 보스가 어둠 속에서 자리를 옮겨 가며 탄막을 쏟아붓는다.
// 보스는 쏠 때마다 플레이어 둘레 아무 데로나 옮겨, 사방으로 퍼지는 탄과 플레이어를 노리는 탄을 함께 쏜다. 쏘기 직전 그 자리가 번쩍여 방향을 알린다.
// 시간이 갈수록 보이는 구멍이 좁아지고 쏘는 간격이 짧아진다. 따로 실패는 없고 끝까지 버티면 어둠이 걷히며 그로기로 이어진다.
[CreateAssetMenu(fileName = "DarknessGimmick", menuName = "Scriptable Objects/Boss/Gimmick/Darkness")]
public class DarknessGimmick : BossGimmick
{
    private const int REPOSITION_TRY_COUNT = 8;

    [Header("어둠")]
    // 가운데가 뚫린 흰 덮개(04_Texture/DarknessHole). 유닛(보스 포함)을 덮고 탄 아래에 그리도록 프리팹에서 Unit 층 1000번에 둔다.
    [SerializeField] private GameObject _darknessPrefab;
    [SerializeField, Min(0f)] private float _duration = 9f;
    [SerializeField, Min(0f)] private float _fadeDuration = 0.8f;
    [SerializeField] private Color _darkColor = new Color(0.02f, 0.02f, 0.03f, 0.96f);
    // 덮개 한 변의 월드 크기. 그림 속 구멍은 한 변에 맞춰 그려져 있어(240일 때 반지름 4.5, 흐림 폭 1.5) 이 값을 키우면 구멍도 같이 커진다.
    [SerializeField, Min(1f)] private float _coverSize = 240f;
    // 끝날 즈음 덮개가 이 배율로 줄어들어 구멍도 함께 좁아진다. 줄어든 뒤에도 종이 전체를 덮도록 넉넉히 잡는다.
    [SerializeField, Range(0.1f, 1f)] private float _endScale = 0.6f;

    [Header("숨어서 쏘기")]
    // 쏠 때마다 보스가 플레이어 둘레 이 거리 안 아무 데로나 옮긴다.
    [SerializeField] private FloatRange _repositionDistance = new FloatRange(7f, 11f);
    // 옮긴 자리가 번쩍이고 이만큼 뒤에 쏜다. 이펙트 층이라 어둠 위로 보인다.
    [SerializeField, Min(0f)] private float _cueDuration = 0.35f;
    [SerializeField] private ImpactSettings _cue = new();

    [Header("탄막")]
    [SerializeField, Min(0.05f)] private float _firstBurstInterval = 1f;
    [SerializeField, Min(0.05f)] private float _lastBurstInterval = 0.55f;
    [SerializeField, Min(1)] private int _shotsPerBurst = 22;
    [SerializeField, Min(1f)] private float _range = 25f;
    [SerializeField] private ShotSettings _shot = new();

    [Header("조준탄")]
    // 사방 탄과 함께 플레이어를 노리는 빠른 부채꼴.
    [SerializeField, Min(0)] private int _aimedShotCount = 5;
    [SerializeField, Range(0f, 180f)] private float _aimedSpread = 30f;
    [SerializeField] private ShotSettings _aimedShot = new();

    public override void Prewarm()
    {
        if (null != _darknessPrefab)
        {
            GameManager.Pool.Prewarm(_darknessPrefab, 1);
        }
    }

    public override async UniTask<bool> PlayAsync(BattleUnit boss, CancellationToken token)
    {
        if (null == _darknessPrefab)
        {
            Debug.LogError($"암흑 : {name}에 어둠 프리팹이 지정되지 않았습니다.");
            return true;
        }

        AutoAttacker attacker = boss.Attacker;
        Transform bossTransform = boss.transform;
        Vector3 origin = bossTransform.position;
        Color clear = new Color(_darkColor.r, _darkColor.g, _darkColor.b, 0f);
        SpriteRenderer darkness = PooledSprite.Spawn(_darknessPrefab, attacker.Target.FollowTransform.position, 0f, new Vector2(_coverSize, _coverSize), clear);

        try
        {
            if (null != attacker.Hands)
            {
                attacker.Hands.PoseAsync(HandGesture.BothRaise, attacker.Target.GetAimPoint(Vector3.zero), _fadeDuration, token).Forget();
            }

            FadeDarkness(darkness, _darkColor.a);

            await UniTask.Delay(TimeSpan.FromSeconds(_fadeDuration), cancellationToken: token);

            float elapsed = 0f;
            float nextBurst = 0f;
            bool isCueing = false;

            // 게임 시간으로 돌아 히트 스톱 동안에는 함께 멈춘다. 구멍은 매 프레임 플레이어를 따라가며 점점 좁아진다.
            while (elapsed < _duration && false == attacker.Target.IsDefeated)
            {
                float progress = elapsed / _duration;
                float size = _coverSize * Mathf.Lerp(1f, _endScale, progress);

                darkness.transform.position = attacker.Target.FollowTransform.position;
                darkness.transform.localScale = new Vector3(size, size, 1f);

                if (false == isCueing && elapsed >= nextBurst)
                {
                    isCueing = true;
                    Reposition(boss);
                    ImpactPlayer.Play(_cue, boss.Body.position);
                }

                if (isCueing && elapsed >= nextBurst + _cueDuration)
                {
                    isCueing = false;
                    FireBurst(attacker, token);
                    nextBurst += _cueDuration + Mathf.Lerp(_firstBurstInterval, _lastBurstInterval, progress);
                }

                await UniTask.Yield(PlayerLoopTiming.Update, token);
                elapsed += Time.deltaTime;
            }

            FadeDarkness(darkness, 0f);

            await UniTask.Delay(TimeSpan.FromSeconds(_fadeDuration), cancellationToken: token);

            return true;
        }
        finally
        {
            // 어둠이 걷히기 전에 원래 자리로 돌아가, 걷힌 뒤에는 보스가 처음 자리에 있다.
            bossTransform.position = origin;

            PooledSprite.Despawn(darkness);

            if (null != attacker.Hands)
            {
                attacker.Hands.ReturnToRest();
            }
        }
    }

    // 어둠 속에서 보스가 플레이어 둘레 아무 데로나 옮긴다. 어둠이 유닛을 덮어 옮기는 모습은 보이지 않는다.
    // 플레이어가 구석에 있으면 바깥쪽 자리가 이동 범위로 잘려 코앞이 되므로, 잘린 뒤에도 최소 거리가 남는 방향을 다시 뽑는다.
    private void Reposition(BattleUnit boss)
    {
        Vector3 center = boss.Attacker.Target.FollowTransform.position;
        Vector3 best = boss.transform.position;
        float bestDistance = -1f;

        for (int i = 0; i < REPOSITION_TRY_COUNT; i++)
        {
            Vector3 position = PickRepositionPoint(boss, center);
            float distance = Vector2.Distance(position, center);

            if (distance > bestDistance)
            {
                best = position;
                bestDistance = distance;
            }

            if (distance >= _repositionDistance.Min)
            {
                break;
            }
        }

        boss.transform.position = best;
    }

    private Vector3 PickRepositionPoint(BattleUnit boss, Vector3 center)
    {
        Vector3 position = center + Geometry2D.ToDirection(UnityEngine.Random.Range(0f, 360f)) * _repositionDistance.PickRandom();
        position.z = boss.transform.position.z;

        if (null != boss.Mover)
        {
            position = boss.Mover.ClampPosition(position);
        }

        return position;
    }

    private void FadeDarkness(SpriteRenderer darkness, float alpha)
    {
        darkness.DOKill();
        darkness.DOFade(alpha, _fadeDuration);
    }

    // 보스 자리에서 사방으로 한 바퀴 퍼뜨리고, 플레이어 쪽으로 빠른 부채꼴을 함께 쏜다.
    private void FireBurst(AutoAttacker attacker, CancellationToken token)
    {
        Vector3 origin = attacker.Owner.Body.position;
        float startAngle = UnityEngine.Random.Range(0f, 360f);
        float step = 360f / _shotsPerBurst;

        for (int i = 0; i < _shotsPerBurst; i++)
        {
            attacker.FireShotToward(null, _shot, origin, Geometry2D.ToDirection(startAngle + step * i), _range, 0f, token);
        }

        if (0 == _aimedShotCount)
        {
            return;
        }

        Vector3 toTarget = attacker.Target.GetAimPoint(Vector3.zero) - origin;
        float centerAngle = Geometry2D.ToAngle(toTarget);
        float aimedStep = 1 < _aimedShotCount ? _aimedSpread / (_aimedShotCount - 1) : 0f;
        float aimedStart = 1 < _aimedShotCount ? centerAngle - _aimedSpread * 0.5f : centerAngle;

        for (int i = 0; i < _aimedShotCount; i++)
        {
            attacker.FireShotToward(null, _aimedShot, origin, Geometry2D.ToDirection(aimedStart + aimedStep * i), _range, 0f, token);
        }
    }
}
