using Cysharp.Threading.Tasks;
using DG.Tweening;
using System.Threading;
using UnityEngine;

// o0o 보스의 두 손. 평소에는 몸통 옆에 떠 있고, 패턴 직전에는 정해진 손동작으로 무엇이 올지 알린다.
// 손 위치는 보스 기준 로컬 좌표로 움직이며, 게임 시간으로 돌아 히트 스톱 동안에는 함께 멈춘다.
public class BossHands : MonoBehaviour
{
    // 원을 그리는 손동작이 지나는 점 수. 많을수록 둥글다.
    private const int CIRCLE_POINT_COUNT = 8;

    [SerializeField] private Transform _leftHand;
    [SerializeField] private Transform _rightHand;

    [Header("떠 있기")]
    [SerializeField, Min(0f)] private float _bobHeight = 0.12f;
    [SerializeField, Min(0f)] private float _bobSpeed = 2.5f;

    [Header("자세")]
    // 손을 뻗거나 젖힐 때 쉬는 자리에서 벗어나는 거리.
    [SerializeField, Min(0f)] private float _reachDistance = 2.2f;
    [SerializeField, Min(0f)] private float _raiseHeight = 2.2f;
    [SerializeField, Min(0f)] private float _circleRadius = 0.6f;
    // 손뼉 칠 때 두 손 사이를 반쯤 띄워 맞닿는 순간이 보이게 한다.
    [SerializeField, Min(0f)] private float _clapGap = 0.3f;
    [SerializeField] private Ease _poseEase = Ease.OutCubic;
    // 예고 시간 중 손이 자세를 잡는 비율. 나머지 시간에는 손이 부풀었다 돌아와 '곧 온다'를 알린다.
    [SerializeField, Range(0.1f, 1f)] private float _poseRatio = 0.75f;
    [SerializeField, Min(0f)] private float _readyPunch = 0.3f;

    [Header("복귀")]
    [SerializeField, Min(0f)] private float _returnDuration = 0.25f;
    [SerializeField] private Ease _returnEase = Ease.OutBack;

    [Header("그로기")]
    [SerializeField] private Vector2 _groggyOffset = new Vector2(0.3f, -1.1f);
    [SerializeField, Min(0f)] private float _groggyDuration = 0.4f;

    [Header("예고 빛")]
    // 예고하는 손 뒤에 깔리는 빛. 흰 종이 위에서도 보이게 손마다 색을 정한다(왼손 설치형, 오른손 공격형).
    // 손 그림의 자식이면 엔딩에서 손이 조각날 때 함께 복제되므로, 보스 바로 아래에 두고 Position Constraint(소스 = 손)로 따라가게 한다.
    [SerializeField] private SpriteRenderer _leftGlow;
    [SerializeField] private SpriteRenderer _rightGlow;
    [SerializeField] private Color _leftTellColor = new Color(0.2f, 0.45f, 0.95f, 0.85f);
    [SerializeField] private Color _rightTellColor = new Color(0.9f, 0.2f, 0.2f, 0.85f);
    [SerializeField, Min(0f)] private float _glowFadeDuration = 0.12f;

    private Vector3 _leftRest;
    private Vector3 _rightRest;
    private Vector3 _leftScale;
    private Vector3 _rightScale;
    // 자세를 잡았거나 그로기인 동안에는 떠 있는 흔들림을 멈춰, 트윈이 정한 자리를 덮어쓰지 않게 한다.
    private bool _isHolding;

    public Transform LeftHand { get { return _leftHand; } }
    public Transform RightHand { get { return _rightHand; } }

    private void Awake()
    {
        _leftRest = _leftHand.localPosition;
        _rightRest = _rightHand.localPosition;
        _leftScale = _leftHand.localScale;
        _rightScale = _rightHand.localScale;

        HideGlow(_leftGlow);
        HideGlow(_rightGlow);
    }

    // 두 손이 엇갈려 오르내리도록 오른손은 반 박자 늦춘다.
    private void Update()
    {
        if (_isHolding)
        {
            return;
        }

        float phase = Time.time * _bobSpeed;

        _leftHand.localPosition = _leftRest + Vector3.up * (Mathf.Sin(phase) * _bobHeight);
        _rightHand.localPosition = _rightRest + Vector3.up * (Mathf.Sin(phase + Mathf.PI) * _bobHeight);
    }

    public async UniTask PoseAsync(HandGesture gesture, Vector3 aimPoint, float duration, CancellationToken token)
    {
        if (HandGesture.None == gesture)
        {
            return;
        }

        KillTweens();
        _isHolding = true;

        if (IsLeftGesture(gesture))
        {
            ShowGlow(_leftGlow, _leftTellColor);
        }

        if (IsRightGesture(gesture))
        {
            ShowGlow(_rightGlow, _rightTellColor);
        }

        await BuildPose(gesture, aimPoint, duration).ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, token);
    }

    private Sequence BuildPose(HandGesture gesture, Vector3 aimPoint, float duration)
    {
        Vector3 aim = transform.InverseTransformDirection(aimPoint - transform.position);
        aim.z = 0f;
        aim = 0f < aim.sqrMagnitude ? aim.normalized : Vector3.right;

        float poseDuration = duration * _poseRatio;
        float readyDuration = duration - poseDuration;

        // 스테이지가 파괴되는 프레임에 끊긴 패턴이 손을 거두면, 손이 사라진 뒤 트윈이 시작하며 경고가 난다. 보스와 함께 정리되게 묶는다.
        Sequence pose = DOTween.Sequence()
            .SetTarget(this)
            .SetLink(gameObject);

        switch (gesture)
        {
            case HandGesture.LeftRaise:
                pose.Join(MoveHand(_leftHand, new Vector3(_leftRest.x * 0.5f, _raiseHeight, 0f), poseDuration));
                break;

            case HandGesture.LeftReach:
                pose.Join(MoveHand(_leftHand, _leftRest + aim * _reachDistance, poseDuration));
                break;

            case HandGesture.LeftCircle:
                pose.Join(_leftHand.DOLocalPath(BuildCircle(_leftRest + Vector3.up * _circleRadius, _circleRadius, -90f), poseDuration, PathType.CatmullRom)
                    .SetEase(Ease.Linear));
                break;

            case HandGesture.RightWindUp:
                pose.Join(MoveHand(_rightHand, _rightRest - aim * (_reachDistance * 0.5f) + Vector3.up * (_reachDistance * 0.2f), poseDuration));
                break;

            case HandGesture.RightCoil:
                pose.Join(MoveHand(_rightHand, new Vector3(_rightRest.x * 0.3f, _rightRest.y - _reachDistance * 0.35f, 0f), poseDuration));
                break;

            case HandGesture.BothPress:
                pose.Join(MoveHand(_leftHand, _leftRest + Vector3.down * (_reachDistance * 0.55f), poseDuration));
                pose.Join(MoveHand(_rightHand, _rightRest + Vector3.down * (_reachDistance * 0.55f), poseDuration));
                break;

            case HandGesture.BothRaise:
                pose.Join(MoveHand(_leftHand, new Vector3(_leftRest.x * 0.6f, _raiseHeight, 0f), poseDuration));
                pose.Join(MoveHand(_rightHand, new Vector3(_rightRest.x * 0.6f, _raiseHeight, 0f), poseDuration));
                break;

            case HandGesture.Clap:
                pose.Join(MoveHand(_leftHand, new Vector3(-_clapGap, _raiseHeight * 0.3f, 0f), poseDuration));
                pose.Join(MoveHand(_rightHand, new Vector3(_clapGap, _raiseHeight * 0.3f, 0f), poseDuration));
                break;

            case HandGesture.Orbit:
                pose.Join(_leftHand.DOLocalPath(BuildCircle(Vector3.zero, _leftRest.magnitude, Geometry2D.ToAngle(_leftRest)), poseDuration, PathType.CatmullRom).SetEase(Ease.Linear));
                pose.Join(_rightHand.DOLocalPath(BuildCircle(Vector3.zero, _rightRest.magnitude, Geometry2D.ToAngle(_rightRest)), poseDuration, PathType.CatmullRom).SetEase(Ease.Linear));
                break;
        }

        if (0f < readyDuration)
        {
            pose.Append(PunchHands(gesture, readyDuration));
        }

        return pose;
    }

    // 패턴을 쓰는 동안 잡고 있던 자세를 풀고 쉬는 자리로 돌아간다. 돌아간 뒤에 다시 떠 있기 시작한다.
    public void ReturnToRest()
    {
        KillTweens();

        DOTween.Sequence()
            .Join(_leftHand.DOLocalMove(_leftRest, _returnDuration).SetEase(_returnEase))
            .Join(_rightHand.DOLocalMove(_rightRest, _returnDuration).SetEase(_returnEase))
            .SetTarget(this)
            .SetLink(gameObject)
            .OnComplete(StopHolding);
    }

    public void SetGroggy(bool isGroggy)
    {
        if (false == isGroggy)
        {
            ReturnToRest();
            return;
        }

        KillTweens();
        _isHolding = true;

        Vector3 leftDrop = new Vector3(_leftRest.x + _groggyOffset.x, _leftRest.y + _groggyOffset.y, 0f);
        Vector3 rightDrop = new Vector3(_rightRest.x - _groggyOffset.x, _rightRest.y + _groggyOffset.y, 0f);

        DOTween.Sequence()
            .Join(_leftHand.DOLocalMove(leftDrop, _groggyDuration).SetEase(Ease.OutBounce))
            .Join(_rightHand.DOLocalMove(rightDrop, _groggyDuration).SetEase(Ease.OutBounce))
            .SetTarget(this)
            .SetLink(gameObject);
    }

    private Tween MoveHand(Transform hand, Vector3 localPosition, float duration)
    {
        return hand.DOLocalMove(localPosition, duration).SetEase(_poseEase);
    }

    private Tween PunchHands(HandGesture gesture, float duration)
    {
        Sequence punch = DOTween.Sequence();

        if (IsLeftGesture(gesture))
        {
            punch.Join(_leftHand.DOPunchScale(_leftScale * _readyPunch, duration, PunchTween.VIBRATO, PunchTween.ELASTICITY));
        }

        if (IsRightGesture(gesture))
        {
            punch.Join(_rightHand.DOPunchScale(_rightScale * _readyPunch, duration, PunchTween.VIBRATO, PunchTween.ELASTICITY));
        }

        return punch;
    }

    private bool IsLeftGesture(HandGesture gesture)
    {
        return HandGesture.LeftRaise == gesture || HandGesture.LeftReach == gesture || HandGesture.LeftCircle == gesture
            || IsBothGesture(gesture);
    }

    private bool IsRightGesture(HandGesture gesture)
    {
        return HandGesture.RightWindUp == gesture || HandGesture.RightCoil == gesture
            || IsBothGesture(gesture);
    }

    private bool IsBothGesture(HandGesture gesture)
    {
        return HandGesture.BothPress == gesture || HandGesture.BothRaise == gesture || HandGesture.Clap == gesture || HandGesture.Orbit == gesture;
    }

    // 원 위의 점 여러 개를 지나 한 바퀴 돈다. 경로의 시작점은 손의 현재 위치라 첫 점부터 넣는다. startAngle(도)에서 시작해 반시계로 돈다.
    private Vector3[] BuildCircle(Vector3 center, float radius, float startAngle)
    {
        Vector3[] points = new Vector3[CIRCLE_POINT_COUNT + 1];

        for (int i = 0; i <= CIRCLE_POINT_COUNT; i++)
        {
            points[i] = center + Geometry2D.ToDirection(i / (float)CIRCLE_POINT_COUNT * 360f + startAngle) * radius;
        }

        return points;
    }

    private void KillTweens()
    {
        HideGlow(_leftGlow);
        HideGlow(_rightGlow);
        DOTween.Kill(this);
        _leftHand.DOKill();
        _rightHand.DOKill();
        _leftHand.localScale = _leftScale;
        _rightHand.localScale = _rightScale;
    }

    private void ShowGlow(SpriteRenderer glow, Color color)
    {
        if (null == glow)
        {
            return;
        }

        glow.DOKill();
        glow.enabled = true;
        glow.color = new Color(color.r, color.g, color.b, 0f);
        glow.DOFade(color.a, _glowFadeDuration).SetLink(gameObject);
    }

    private void HideGlow(SpriteRenderer glow)
    {
        if (null == glow)
        {
            return;
        }

        glow.DOKill();
        glow.enabled = false;
    }

    private void StopHolding()
    {
        _isHolding = false;
    }
}
