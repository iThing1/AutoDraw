using UnityEngine;

// 몸 그림(Visual) 한 장을 매 프레임 늘이고 누르고 기울여 숨쉬기·걸음·공격·피격·대시를 보여 준다.
// Visual만 움직이므로 판정·넉백·조준(Body)과 컷신(루트 크기)과 겹치지 않는다. 게임 시간을 따라 히트 스톱 때 함께 멈춘다.
public class UnitAnimation
{
    // 이보다 느리게 옆으로 움직이면 좌우를 뒤집지 않아, 멈출 때 방향이 떨리지 않게 한다.
    private const float FLIP_SPEED = 0.5f;
    private const float MIN_DASH_SPEED = 0.01f;

    private readonly UnitAnimationSettings _settings;
    private readonly Transform _root;
    private readonly Transform _visual;
    private readonly SpriteRenderer _renderer;
    private readonly Vector3 _baseScale;
    private readonly Vector3 _basePosition;

    private Vector3 _lastRootPosition;
    private Vector3 _velocity;
    private float _breathePhase;
    private float _stepPhase;

    private float _reactionAmount;
    private float _reactionTime;
    private bool _isAttackReaction;

    public UnitAnimation(UnitAnimationSettings settings, Transform root, SpriteRenderer renderer)
    {
        _settings = settings;
        _root = root;
        _renderer = renderer;
        _visual = renderer.transform;
        _baseScale = _visual.localScale;
        _basePosition = _visual.localPosition;
        _lastRootPosition = root.position;
        _reactionTime = settings.ReactionDuration;
    }

    public void PlayAttack()
    {
        StartReaction(_settings.AttackSquash, true);
    }

    public void PlayHit()
    {
        StartReaction(_settings.HitSquash, false);
    }

    public void PlayGuard()
    {
        StartReaction(_settings.GuardSquash, false);
    }

    // 죽음처럼 컷신이 그림을 넘겨받을 때 처음 모습으로 돌려놓는다.
    public void ResetPose()
    {
        _visual.localScale = _baseScale;
        _visual.localPosition = _basePosition;
        _visual.localRotation = Quaternion.identity;
    }

    public void Tick(float deltaTime, bool isDashing)
    {
        if (0f >= deltaTime)
        {
            return;
        }

        // 이동 방식(물리·트윈·배회)이 유닛마다 달라, 루트가 실제로 움직인 거리로 속도를 잰다.
        Vector3 rootPosition = _root.position;
        Vector3 rawVelocity = (rootPosition - _lastRootPosition) / deltaTime;
        _lastRootPosition = rootPosition;
        _velocity = Vector3.Lerp(_velocity, rawVelocity, 1f - Mathf.Exp(-_settings.FollowSharpness * deltaTime));

        float speed = _velocity.magnitude;
        float moveRatio = Mathf.Clamp01(speed / _settings.FullSpeed);

        _breathePhase = Mathf.Repeat(_breathePhase + deltaTime * _settings.BreatheSpeed, 1f);
        float breathe = Mathf.Sin(_breathePhase * Mathf.PI * 2f) * _settings.BreatheAmount * (1f - moveRatio);

        // 걸음 한 번(0→1)에 위로 떴다 내려앉고, 내려앉는 순간 눌린다.
        _stepPhase = Mathf.Repeat(_stepPhase + speed * deltaTime / _settings.StepLength, 1f);
        float lift = Mathf.Sin(_stepPhase * Mathf.PI);
        float bob = lift * _settings.BobHeight * moveRatio;
        float land = (1f - lift) * _settings.LandSquash * moveRatio;

        // 양수면 납작하게, 음수면 길쭉하게.
        float squash = land - breathe + TickReaction(deltaTime);
        Vector2 stretch = new Vector2(1f + squash, 1f - squash);
        float angle = -Mathf.Clamp(_velocity.x / _settings.FullSpeed, -1f, 1f) * _settings.LeanAngle;

        // 대시는 가는 방향으로 길게 늘인다. 왼쪽으로 갈 때 그림이 뒤집혀 보이지 않게 각도를 -90~90도 안에 둔다.
        if (isDashing && MIN_DASH_SPEED < speed)
        {
            Vector3 direction = 0f > _velocity.x ? -_velocity : _velocity;
            angle = Geometry2D.ToAngle(direction);
            stretch = new Vector2(1f + _settings.DashStretch, 1f - _settings.DashStretch * 0.5f);
        }

        _visual.localScale = new Vector3(_baseScale.x * stretch.x, _baseScale.y * stretch.y, _baseScale.z);
        _visual.localPosition = _basePosition + new Vector3(0f, bob, 0f);
        _visual.localRotation = Quaternion.Euler(0f, 0f, angle);

        if (_settings.FlipByDirection && FLIP_SPEED < Mathf.Abs(_velocity.x))
        {
            _renderer.flipX = 0f > _velocity.x;
        }
    }

    private void StartReaction(float amount, bool isAttack)
    {
        _reactionAmount = amount;
        _reactionTime = 0f;
        _isAttackReaction = isAttack;
    }

    // 피격·막기는 맞는 순간 가장 납작했다가 풀리고, 공격은 움츠렸다(납작) 튀어 나간다(길쭉).
    private float TickReaction(float deltaTime)
    {
        float duration = _settings.ReactionDuration;

        if (_reactionTime >= duration)
        {
            return 0f;
        }

        _reactionTime += deltaTime;
        float progress = Mathf.Clamp01(_reactionTime / duration);
        float fade = 1f - progress;

        if (_isAttackReaction)
        {
            return _reactionAmount * Mathf.Sin(progress * Mathf.PI * 2f) * fade;
        }

        return _reactionAmount * fade * fade;
    }
}
