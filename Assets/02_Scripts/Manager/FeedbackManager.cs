using UnityEngine;

// 타격·발사·폭발처럼 "한 번 터지는" 순간의 연출(흔들림, 소리, 파티클, 히트 스톱, 플래시, 넉백, 물감 자국)을 재생한다.
// 쓰는 매니저는 Init에서 넘겨받아, 어디에 기대는지 이 클래스만 봐도 알 수 있게 한다.
public class FeedbackManager
{
    private BattleCameraRig _cameraRig;
    private SoundManager _sound;
    private PoolManager _pool;
    private TimeScaleManager _timeScale;

    public void Init(BattleCameraRig cameraRig, SoundManager sound, PoolManager pool, TimeScaleManager timeScale)
    {
        _cameraRig = cameraRig;
        _sound = sound;
        _pool = pool;
        _timeScale = timeScale;
    }

    public void PlayImpact(ImpactSettings settings, Vector3 position)
    {
        PlayImpact(settings, position, settings.EffectColor);
    }

    // 타격처럼 공격마다 물감 색이 달라지는 경우, 설정의 기본 색 대신 넘겨받은 색으로 파티클을 칠한다.
    public void PlayImpact(ImpactSettings settings, Vector3 position, Color effectColor)
    {
        _cameraRig.Shake(settings.ShakeForce);
        _sound.PlaySfx(settings.PickSound(), settings.PickPitch());

        SpawnEffect(settings, position, effectColor);
    }

    // 프리셋 하나로 히트 스톱, 플래시, 넉백, 임팩트, 물감 자국을 한 번에 재생한다.
    public void PlayHit(HitFeedbackPreset preset, BattleUnit target, DamageInfo damage, PaintCanvas canvas)
    {
        if (null == preset)
        {
            return;
        }

        _timeScale.PlayHitStop(preset.HitStopDuration, preset.HitStopTimeScale);

        UnitView view = target.View;

        if (null != view)
        {
            view.Flash(preset.FlashColor, preset.FlashDuration);

            // 공격자에게서 멀어지는 쪽으로 밀어낸다. 공격자를 모르면 오른쪽으로 민다.
            Vector3 direction = Vector3.right;

            if (null != damage.Attacker)
            {
                direction = target.Body.position - damage.Attacker.Body.position;
            }

            view.Knockback(target.Body, direction, preset.KnockbackDistance, preset.KnockbackDuration);
        }

        PlayImpact(preset.Impact, damage.HitPoint, damage.PaintColor);

        if (null != canvas)
        {
            canvas.LeaveSplats(damage.HitPoint, damage.PaintColor, damage.IsInk, preset);
        }
    }

    // 공격이 목표에 닿기 전, 쏘는 순간의 연출. 자동 공격도 "내가 쏜다"는 느낌이 나게 소리와 몸 반응을 함께 낸다.
    public void PlayFire(FireFeedbackSettings settings, BattleUnit owner, Vector3 position)
    {
        PlayFire(settings, owner, position, settings.Impact.EffectColor);
    }

    // 구슬처럼 쏘는 것마다 물감 색이 다르면, 설정의 기본 색 대신 그 색으로 파티클을 칠한다.
    public void PlayFire(FireFeedbackSettings settings, BattleUnit owner, Vector3 position, Color effectColor)
    {
        if (false == settings.Enabled)
        {
            return;
        }

        PlayImpact(settings.Impact, position, effectColor);

        if (settings.Recoils && null != owner.View)
        {
            owner.View.PlayAttack();
        }
    }

    private void SpawnEffect(ImpactSettings settings, Vector3 position, Color effectColor)
    {
        if (null == settings.EffectPrefab)
        {
            return;
        }

        GameObject instance = _pool.Spawn(settings.EffectPrefab, position, Quaternion.identity);

        instance.GetOrAddComponent<PooledEffect>().Play(effectColor, settings.EffectScale, settings.EffectLifetime);
    }
}
