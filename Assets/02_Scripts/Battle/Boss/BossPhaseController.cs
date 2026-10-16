using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

// 보스의 페이즈를 맡는다. 맞을수록 색이 물들고, 체력 줄에 닿으면 그 줄에서 멈춰 기믹을 쓰며,
// 마지막이 아닌 페이즈의 바닥에 닿으면 쓰러졌다가 다음 페이즈의 체력으로 다시 일어난다.
// 진짜로 쓰러지는 것은 마지막 페이즈뿐이라, 전투 매니저는 지금처럼 보스가 죽을 때까지만 기다리면 된다.
[RequireComponent(typeof(BattleUnit))]
public class BossPhaseController : MonoBehaviour
{
    // 마지막이 아닌 페이즈는 바닥 대신 이 체력에서 멈춰 쓰러지지 않게 한다.
    private const int REVIVAL_FLOOR_HP = 1;

    [SerializeField] private BossPhaseData[] _phases;

    [Header("기믹 뒤 물듦")]
    [SerializeField, Min(0f)] private float _toneShiftDuration = 1f;
    [SerializeField] private ImpactSettings _toneShiftImpact = new();

    [Header("다시 공격")]
    // 기믹이 끝난 뒤 보스 공격과 환경 탄막이 다시 시작하기까지 숨 돌릴 틈. 기다리던 탄막이 끝나자마자 터지지 않게 한다.
    [SerializeField, Min(0f)] private float _resumeDelay = 1f;

    [Header("그로기")]
    [SerializeField, Min(0f)] private float _groggyDuration = 4f;
    [SerializeField, Min(1f)] private float _groggyDamageMultiplier = 1.5f;

    [Header("부활")]
    [SerializeField] private RevivalSettings _revival = new();

    private readonly List<float> _gimmickRatios = new();

    private BattleUnit _unit;
    private int _phaseIndex;
    // 다음에 닿을 기믹 줄이자, 지금 색 구간의 번호.
    private int _nextLineIndex;
    private Color _currentColor;
    // 기믹·부활 중에 닿은 줄은 끝난 뒤에 처리한다.
    private bool _isBusy;
    // 한 단계 물드는 동안에는 맞아도 색을 체력에 맞춰 덮어쓰지 않는다.
    private bool _isColorShifting;
    private bool _hasPendingFloor;
    // 기믹·부활을 끊는 소스. 보스가 사라지거나 전투가 끝나면(플레이어가 쓰러짐) 함께 끊겨, 패배 연출 위로 기믹이 계속 돌지 않는다.
    private CancellationTokenSource _flowSource;

    // 페이즈가 바뀌면 알려, 체력바가 기믹 눈금과 남은 페이즈 수를 다시 그리게 한다.
    public event Action OnPhaseChanged;
    // 기믹이 시작하고 끝날 때 알려, 체력바 색으로 지금은 체력이 줄지 않는 시간임을 보여 주게 한다.
    public event Action<bool> OnGimmickActiveChanged;
    // 기믹 결과(해냈으면 true)를 알려, 화면에 성공·실패를 띄우게 한다.
    public event Action<bool> OnGimmickFinished;

    public int RemainingPhaseCount { get { return _phases.Length - _phaseIndex; } }
    // 지금 페이즈에서 아직 남은 기믹 줄의 체력 비율.
    public IReadOnlyList<float> GimmickRatios { get { return _gimmickRatios; } }

    private void Awake()
    {
        _unit = GetComponent<BattleUnit>();

        _unit.Health.OnDamaged += HandleDamaged;
        _unit.Health.OnFloorReached += HandleFloorReached;
    }

    private void OnDestroy()
    {
        if (null == _unit)
        {
            return;
        }

        _unit.Health.OnDamaged -= HandleDamaged;
        _unit.Health.OnFloorReached -= HandleFloorReached;

        _flowSource?.Dispose();
    }

    // 모든 Awake(체력 초기화 포함)가 끝난 뒤, 보스가 화면에 나오기 전에 첫 페이즈를 건다.
    private void Start()
    {
        if (null == _phases || 0 == _phases.Length)
        {
            Debug.LogError($"보스 페이즈 : {name}에 페이즈 데이터가 없습니다.");
            enabled = false;
            return;
        }

        _unit.Health.Init(GetPhaseMaxHp(0));
        ApplyPhase(0);
        PrewarmGimmicks();
    }

    private void HandleDamaged(DamageInfo damage)
    {
        if (_isColorShifting)
        {
            return;
        }

        ApplyColor(GetCurrentColor());
    }

    private void HandleFloorReached()
    {
        if (_isBusy)
        {
            _hasPendingFloor = true;
            return;
        }

        BossPhaseData phase = _phases[_phaseIndex];

        if (_nextLineIndex < phase.GimmickLineCount)
        {
            PlayGimmickAsync(phase.GetGimmickLine(_nextLineIndex), BeginFlow()).Forget();
        }
        else if (_phaseIndex < _phases.Length - 1)
        {
            PlayRevivalAsync(BeginFlow()).Forget();
        }
    }

    // 줄에서 멈춰 기믹을 쓰고, 한 단계 물든 뒤 다음 줄까지 체력을 연다. 성공했으면 그로기 동안 더 아프게 맞는다.
    private async UniTaskVoid PlayGimmickAsync(BossPhaseData.GimmickLine line, CancellationToken token)
    {
        _isBusy = true;

        try
        {
            await StopBossAsync(token);

            OnGimmickActiveChanged?.Invoke(true);

            bool isSucceeded = await PlayGimmickWithGuardAsync(line.Gimmick, token);

            OnGimmickActiveChanged?.Invoke(false);
            OnGimmickFinished?.Invoke(isSucceeded);

            _nextLineIndex++;
            UpdateGimmickRatios();
            OnPhaseChanged?.Invoke();

            // 기믹이 끝나면 잉크가 번지듯 한 단계 크게 물든다.
            ImpactPlayer.Play(_toneShiftImpact, _unit.Body.position);
            await TweenColorAsync(_currentColor, GetSegmentStartColor(_nextLineIndex), _toneShiftDuration, false, token);

            UpdateFloor();

            if (isSucceeded)
            {
                await PlayGroggyAsync(token);
            }

            await ResumeBossAsync(token);
        }
        catch (OperationCanceledException)
        {
            // 스테이지가 사라져 기믹이 끊긴 경우라 따로 처리하지 않는다.
        }
        finally
        {
            FinishBusy();
        }
    }

    private async UniTask<bool> PlayGimmickWithGuardAsync(BossGimmick gimmick, CancellationToken token)
    {
        if (null == gimmick)
        {
            Debug.LogError($"보스 페이즈 : {name}의 기믹 줄에 기믹이 지정되지 않았습니다.");
            return false;
        }

        using (_unit.Health.BeginInvulnerable())
        {
            return await gimmick.PlayAsync(_unit, token);
        }
    }

    // 납작하게 쓰러졌다가, 위에서 내려온 검은 빛기둥을 받아 다음 페이즈의 색과 체력으로 다시 일어난다.
    private async UniTaskVoid PlayRevivalAsync(CancellationToken token)
    {
        _isBusy = true;

        Transform boss = _unit.transform;
        Vector3 startScale = boss.localScale;
        SpriteRenderer beam = null;

        try
        {
            using (_unit.Health.BeginInvulnerable())
            {
                await StopBossAsync(token);
                SetPlayerPaused(true);

                using (GameManager.TimeScale.BeginSlowMotion(_revival.SlowTimeScale))
                {
                    await RealtimeDelay.WaitAsync(_revival.SlowDuration, token);
                }

                await CinematicPlayer.EnterAsync(_unit.Body, _revival.Cinematic, token);

                SetHandsGroggy(true);

                Vector2 collapse = _revival.CollapseScale;

                await boss.DOScale(new Vector3(startScale.x * collapse.x, startScale.y * collapse.y, startScale.z), _revival.CollapseDuration)
                    .SetEase(Ease.OutQuad)
                    .SetUpdate(true)
                    .ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, token);

                await RealtimeDelay.WaitAsync(_revival.HoldDuration, token);

                beam = SpawnBeam();
                await DropBeamAsync(beam, token);

                ImpactPlayer.Play(_revival.BeamImpact, _unit.Body.position);

                // 빛기둥 안에서 다음 페이즈로 넘어가며, 체력은 다시 차고 몸은 지금 색에서 다음 페이즈 색으로 물든다.
                Color before = _currentColor;

                _phaseIndex++;
                _unit.Health.Refill(GetPhaseMaxHp(_phaseIndex));
                ApplyPhase(_phaseIndex);
                // 새 페이즈 색이 한 프레임 먼저 칠해지지 않게, 물들기 전까지는 이전 색으로 둔다.
                ApplyColor(before);

                await TweenColorAsync(before, GetCurrentColor(), _revival.BeamHoldDuration, true, token);

                SetHandsGroggy(false);

                await boss.DOScale(startScale, _revival.RiseDuration)
                    .SetEase(_revival.RiseEase)
                    .SetUpdate(true)
                    .ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, token);

                ImpactPlayer.Play(_revival.RiseImpact, _unit.Body.position);

                if (null != beam)
                {
                    await beam.DOFade(0f, _revival.BeamFadeDuration)
                        .SetUpdate(true)
                        .ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, token);
                }

                await PlayBannerAsync(_phases[_phaseIndex], token);
                await CinematicPlayer.ExitAsync(_revival.Cinematic, token);
            }

            SetPlayerPaused(false);
            ResumeBoss();
        }
        catch (OperationCanceledException)
        {
            // 스테이지가 사라져 연출이 끊긴 경우라 따로 처리하지 않는다.
        }
        finally
        {
            boss.localScale = startScale;
            PooledSprite.Despawn(beam);
            FinishBusy();
        }
    }

    private async UniTask PlayBannerAsync(BossPhaseData phase, CancellationToken token)
    {
        if (string.IsNullOrEmpty(phase.BannerTitle))
        {
            return;
        }

        GameManager.Sound.PlaySfx(_revival.Banner.Sound);

        await GameManager.UI.Cinematic.BossBanner.PlayAsync(phase.BannerTitle, phase.BannerSubtitle, _revival.Banner, token);
    }

    // 그로기 동안은 공격도 이동도 하지 않고, 받는 피해가 늘어난다.
    private async UniTask PlayGroggyAsync(CancellationToken token)
    {
        SetHandsGroggy(true);
        _unit.Health.SetDamageMultiplier(_groggyDamageMultiplier);

        try
        {
            await UniTask.Delay(TimeSpan.FromSeconds(_groggyDuration), cancellationToken: token);
        }
        finally
        {
            _unit.Health.SetDamageMultiplier(1f);
            SetHandsGroggy(false);
        }
    }

    // 진행 중인 패턴을 끊고 그 자리에 세운다. 끊긴 패턴이 손을 거두고 Action을 풀 때까지 기다린 뒤 멈춰야,
    // 기믹이 잡은 손동작이나 상태가 끊긴 패턴의 정리에 덮어써지지 않는다.
    private async UniTask StopBossAsync(CancellationToken token)
    {
        _unit.Attacker.Interrupt();

        await UniTask.NextFrame(token);

        while (UnitState.Action == _unit.State)
        {
            await UniTask.Yield(PlayerLoopTiming.Update, token);
        }

        _unit.ChangeState(UnitState.Idle);
    }

    // 보스는 바로 움직이되, 공격은 잠깐 쉬었다가 시작한다. 공격이 멈춘 동안에는 환경 탄막도 새로 나오지 않는다.
    private async UniTask ResumeBossAsync(CancellationToken token)
    {
        _unit.ChangeState(UnitState.Move);

        await UniTask.Delay(TimeSpan.FromSeconds(_resumeDelay), cancellationToken: token);

        _unit.Attacker.Resume();
    }

    // 쓰러졌다 일어나는 동안에는 플레이어도 공격·이동·막기를 멈춘다. 연출 중인 보스에게 계속 쏘아 대면 어색하다.
    private void SetPlayerPaused(bool isPaused)
    {
        AttackTarget target = _unit.Attacker.Target;

        if (null == target || target.IsDefeated)
        {
            return;
        }

        BattleUnit player = target.Unit;

        if (isPaused)
        {
            player.Attacker.Interrupt();
            player.ChangeState(UnitState.Idle);
        }
        else
        {
            player.ChangeState(UnitState.Move);
            player.Attacker.Resume();
        }
    }

    private void ResumeBoss()
    {
        _unit.ChangeState(UnitState.Move);
        _unit.Attacker.Resume();
    }

    private CancellationToken BeginFlow()
    {
        _flowSource?.Dispose();
        _flowSource = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken, _unit.Attacker.BattleToken);

        return _flowSource.Token;
    }

    // 기믹·그로기 중에 다음 줄까지 깎였으면, 끝난 뒤 바로 그 줄을 처리한다.
    private void FinishBusy()
    {
        _isBusy = false;

        _flowSource?.Dispose();
        _flowSource = null;

        if (_hasPendingFloor)
        {
            _hasPendingFloor = false;
            HandleFloorReached();
        }
    }

    private void ApplyPhase(int index)
    {
        _phaseIndex = index;
        _nextLineIndex = 0;

        _unit.OverrideAttackStyle(_phases[index].AttackStyle);

        UpdateFloor();
        UpdateGimmickRatios();
        ApplyColor(GetCurrentColor());

        OnPhaseChanged?.Invoke();
    }

    // 다음 기믹 줄에서 멈추고, 줄이 남지 않았으면 마지막 페이즈만 쓰러질 수 있게 연다.
    private void UpdateFloor()
    {
        BossPhaseData phase = _phases[_phaseIndex];
        Health health = _unit.Health;
        int floor = 0;

        if (_nextLineIndex < phase.GimmickLineCount)
        {
            floor = Mathf.CeilToInt(health.MaxHp * phase.GetGimmickLine(_nextLineIndex).HpRatio);
        }
        else if (_phaseIndex < _phases.Length - 1)
        {
            floor = REVIVAL_FLOOR_HP;
        }

        health.SetFloor(floor);
    }

    private void UpdateGimmickRatios()
    {
        _gimmickRatios.Clear();

        BossPhaseData phase = _phases[_phaseIndex];

        for (int i = _nextLineIndex; i < phase.GimmickLineCount; i++)
        {
            _gimmickRatios.Add(phase.GetGimmickLine(i).HpRatio);
        }
    }

    // 지금 구간 안에서 체력이 깎인 만큼 구간의 시작 색에서 끝 색으로 물든다.
    private Color GetCurrentColor()
    {
        BossPhaseData phase = _phases[_phaseIndex];
        BossPhaseData.ColorSegment segment = phase.GetColorSegment(_nextLineIndex);

        if (null == segment)
        {
            return _currentColor;
        }

        float top = phase.GetSegmentTop(_nextLineIndex);
        float bottom = phase.GetSegmentBottom(_nextLineIndex);

        return segment.Evaluate(Mathf.InverseLerp(top, bottom, _unit.Health.Ratio));
    }

    private Color GetSegmentStartColor(int segmentIndex)
    {
        BossPhaseData.ColorSegment segment = _phases[_phaseIndex].GetColorSegment(segmentIndex);

        return null != segment ? segment.From : _currentColor;
    }

    private void ApplyColor(Color color)
    {
        _currentColor = color;
        _unit.View.SetBaseColor(color);
    }

    // 위에서 내려올 검은 빛기둥. 처음에는 높이 0으로 숨겨 둔다.
    private SpriteRenderer SpawnBeam()
    {
        if (null == _revival.BeamPrefab)
        {
            return null;
        }

        return PooledSprite.Spawn(_revival.BeamPrefab, _unit.Body.position, 0f, new Vector2(_revival.BeamWidth, 0f), _revival.BeamColor);
    }

    // 윗끝은 화면 위에 고정한 채 아랫끝이 보스까지 내려온다. 연출이라 실제 시간으로 돈다.
    private async UniTask DropBeamAsync(SpriteRenderer beam, CancellationToken token)
    {
        if (null == beam)
        {
            return;
        }

        Transform beamTransform = beam.transform;
        Vector3 bottom = _unit.Body.position;
        Vector3 top = bottom + Vector3.up * _revival.BeamHeight;
        float width = _revival.BeamWidth;

        await DOVirtual.Float(0f, _revival.BeamHeight, _revival.BeamDropDuration, height =>
            {
                beamTransform.position = top + Vector3.down * (height * 0.5f);
                beamTransform.localScale = new Vector3(width, height, 1f);
            })
            .SetEase(Ease.InQuad)
            .SetUpdate(true)
            .SetTarget(this)
            .ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, token);
    }

    // 물드는 동안에는 맞아도 체력에 맞춘 색으로 덮어쓰지 않는다. 기믹 뒤에는 게임 시간, 부활 연출은 실제 시간으로 돈다.
    private async UniTask TweenColorAsync(Color from, Color to, float duration, bool isRealtime, CancellationToken token)
    {
        _isColorShifting = true;

        try
        {
            await DOVirtual.Color(from, to, duration, ApplyColor)
                .SetUpdate(isRealtime)
                .SetTarget(this)
                .ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, token);
        }
        finally
        {
            _isColorShifting = false;
        }
    }

    private void SetHandsGroggy(bool isGroggy)
    {
        if (null != _unit.Attacker.Hands)
        {
            _unit.Attacker.Hands.SetGroggy(isGroggy);
        }
    }

    private int GetPhaseMaxHp(int index)
    {
        return Mathf.Max(1, Mathf.RoundToInt(_unit.Data.MaxHp * _phases[index].HpRatio));
    }

    private void PrewarmGimmicks()
    {
        for (int phase = 0; phase < _phases.Length; phase++)
        {
            for (int line = 0; line < _phases[phase].GimmickLineCount; line++)
            {
                BossGimmick gimmick = _phases[phase].GetGimmickLine(line).Gimmick;

                if (null != gimmick)
                {
                    gimmick.Prewarm();
                }
            }
        }
    }
}
