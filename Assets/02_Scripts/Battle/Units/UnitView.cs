using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;

// 유닛의 겉모습 연출(피격 플래시, 넉백, 맞을수록 물드는 바탕색, 몸 그림 애니메이션)을 맡는다.
public class UnitView : MonoBehaviour
{
    // 피격 때 번쩍일 렌더러. 그림자·이펙트처럼 번쩍이면 안 되는 자식이 섞이지 않도록 프리팹에서 직접 지정한다.
    [SerializeField] private SpriteRenderer[] _renderers;

    private Color[] _originalColors;
    private BattleUnit _unit;
    // 데이터에서 꺼 두었거나 몸 그림이 없으면 null이다.
    private UnitAnimation _animation;

    public IReadOnlyList<SpriteRenderer> Renderers { get { return _renderers; } }

    private void Awake()
    {
        _originalColors = new Color[_renderers.Length];

        for (int i = 0; i < _renderers.Length; i++)
        {
            _originalColors[i] = _renderers[i].color;
        }
    }

    // BattleUnit이 Awake에서 체력을 찾은 뒤에 구독한다.
    private void Start()
    {
        if (false == TryGetComponent(out _unit) || null == _unit.Data || null == _unit.BodyRenderer || false == _unit.Data.Animation.UseAnimation)
        {
            return;
        }

        _animation = new UnitAnimation(_unit.Data.Animation, transform, _unit.BodyRenderer);

        _unit.OnStateChanged += HandleStateChanged;
        _unit.OnBlocked += HandleBlocked;
        _unit.Health.OnDamaged += HandleDamaged;
    }

    private void LateUpdate()
    {
        if (null != _animation)
        {
            _animation.Tick(Time.deltaTime, _unit.IsDodging);
        }
    }

    private void OnDestroy()
    {
        if (null == _animation)
        {
            return;
        }

        _unit.OnStateChanged -= HandleStateChanged;
        _unit.OnBlocked -= HandleBlocked;
        _unit.Health.OnDamaged -= HandleDamaged;
    }

    // 맞을수록 물드는 보스처럼 바탕색이 바뀌는 유닛용. 피격 플래시는 맞은 뒤에 시작해 바뀐 색으로 돌아온다.
    // 플래시가 도는 중이면 그 플래시가 끝날 때 맞추도록 색은 바로 칠하지 않는다.
    public void SetBaseColor(Color color)
    {
        for (int i = 0; i < _renderers.Length; i++)
        {
            _originalColors[i] = color;

            if (false == DOTween.IsTweening(_renderers[i]))
            {
                _renderers[i].color = color;
            }
        }
    }

    // 히트 스톱으로 게임 시간이 멈춘 동안에도 보여야 하므로 실제 시간으로 재생한다.
    public void Flash(Color flashColor, float duration)
    {
        for (int i = 0; i < _renderers.Length; i++)
        {
            SpriteRenderer spriteRenderer = _renderers[i];

            spriteRenderer.DOKill();
            spriteRenderer.color = flashColor;
            spriteRenderer.DOColor(_originalColors[i], duration).SetUpdate(true);
        }
    }

    public void Knockback(Transform body, Vector3 direction, float distance, float duration)
    {
        if (0f >= distance || 0f >= duration)
        {
            return;
        }

        body.DOComplete();
        body.DOPunchPosition(direction.normalized * distance, duration, PunchTween.VIBRATO, PunchTween.ELASTICITY).SetUpdate(true);
    }

    // 쓰러지면 죽음 연출이 그림을 넘겨받으므로 처음 모습으로 돌려놓고 멈춘다.
    private void HandleStateChanged(UnitState state)
    {
        if (UnitState.Action == state)
        {
            _animation.PlayAttack();
        }
        else if (UnitState.Dead == state)
        {
            _animation.ResetPose();
            enabled = false;
        }
    }

    private void HandleBlocked(Vector3 point)
    {
        _animation.PlayGuard();
    }

    private void HandleDamaged(DamageInfo damage)
    {
        _animation.PlayHit();
    }
}
