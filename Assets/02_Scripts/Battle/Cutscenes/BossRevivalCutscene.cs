using Cysharp.Threading.Tasks;
using DG.Tweening;
using System.Threading;
using UnityEngine;

// 보스가 페이즈 끝에서 쓰러졌다가 다시 일어나는 연출. CollapseAsync와 RiseAsync 사이에 부르는 쪽이 다음 페이즈로 넘긴다.
public class BossRevivalCutscene
{
    private readonly BattleUnit _boss;
    private readonly BossHands _hands;
    private readonly RevivalSettings _settings;
    private readonly Vector3 _startScale;
    private SpriteRenderer _beam;

    // 손 달린 보스가 아니면 hands는 null이다.
    public BossRevivalCutscene(BattleUnit boss, BossHands hands, RevivalSettings settings)
    {
        _boss = boss;
        _hands = hands;
        _settings = settings;
        _startScale = boss.transform.localScale;
    }

    public async UniTask CollapseAsync(CancellationToken token)
    {
        using (GameManager.TimeScale.BeginSlowMotion(_settings.SlowTimeScale))
        {
            await RealtimeDelay.WaitAsync(_settings.SlowDuration, token);
        }

        await CinematicPlayer.EnterAsync(_boss.Body, _settings.Cinematic, token);

        SetHandsGroggy(true);

        Vector2 collapse = _settings.CollapseScale;

        await _boss.transform.DOScale(new Vector3(_startScale.x * collapse.x, _startScale.y * collapse.y, _startScale.z), _settings.CollapseDuration)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true)
            .ToUniTaskKillOnCancel(token);

        await RealtimeDelay.WaitAsync(_settings.HoldDuration, token);

        _beam = SpawnBeam();
        await DropBeamAsync(token);

        ImpactPlayer.Play(_settings.BeamImpact, _boss.Body.position);
    }

    public async UniTask RiseAsync(BossPhaseData nextPhase, CancellationToken token)
    {
        SetHandsGroggy(false);

        await _boss.transform.DOScale(_startScale, _settings.RiseDuration)
            .SetEase(_settings.RiseEase)
            .SetUpdate(true)
            .ToUniTaskKillOnCancel(token);

        ImpactPlayer.Play(_settings.RiseImpact, _boss.Body.position);

        if (null != _beam)
        {
            await _beam.DOFade(0f, _settings.BeamFadeDuration)
                .SetUpdate(true)
                .ToUniTaskKillOnCancel(token);
        }

        await PlayBannerAsync(nextPhase, token);
        await CinematicPlayer.ExitAsync(_settings.Cinematic, token);
    }

    // 끊겨도 쓰러진 크기로 남거나 빛기둥이 떠 있지 않게 되돌린다.
    public void Cleanup()
    {
        _boss.transform.localScale = _startScale;
        PooledSprite.Despawn(_beam);
        _beam = null;
    }

    private SpriteRenderer SpawnBeam()
    {
        if (null == _settings.BeamPrefab)
        {
            return null;
        }

        return PooledSprite.Spawn(_settings.BeamPrefab, _boss.Body.position, 0f, new Vector2(_settings.BeamWidth, 0f), _settings.BeamColor);
    }

    // 윗끝은 화면 위에 고정한 채 아랫끝이 보스까지 내려온다.
    private async UniTask DropBeamAsync(CancellationToken token)
    {
        if (null == _beam)
        {
            return;
        }

        Transform beamTransform = _beam.transform;
        Vector3 bottom = _boss.Body.position;
        Vector3 top = bottom + Vector3.up * _settings.BeamHeight;
        float width = _settings.BeamWidth;

        await DOVirtual.Float(0f, _settings.BeamHeight, _settings.BeamDropDuration, height =>
            {
                beamTransform.position = top + Vector3.down * (height * 0.5f);
                beamTransform.localScale = new Vector3(width, height, 1f);
            })
            .SetEase(Ease.InQuad)
            .SetUpdate(true)
            .SetTarget(this)
            .ToUniTaskKillOnCancel(token);
    }

    private async UniTask PlayBannerAsync(BossPhaseData phase, CancellationToken token)
    {
        if (string.IsNullOrEmpty(phase.BannerTitle))
        {
            return;
        }

        GameManager.Sound.PlaySfx(_settings.Banner.Sound);

        await GameManager.UI.Cinematic.BossBanner.PlayAsync(phase.BannerTitle, phase.BannerSubtitle, _settings.Banner, token);
    }

    private void SetHandsGroggy(bool isGroggy)
    {
        if (null != _hands)
        {
            _hands.SetGroggy(isGroggy);
        }
    }
}
