using UnityEngine;

// 잉크에서 기어 나와 대상을 움켜쥐는 그림자 팔 하나. 선 모양은 프리팹에서 정하고 여기서는 점 위치만 계산한다.
public class InkArm : MonoBehaviour
{
    private const int FINGER_POINT_COUNT = 3;
    private const float MIN_VISIBLE_PROGRESS = 0.01f;
    // 손가락 첫 마디가 차지하는 비율. 나머지가 굽는 끝마디다.
    private const float FINGER_FIRST_JOINT_RATIO = 0.55f;
    // 펴진 손가락 끝마디가 바깥으로 살짝 젖혀지는 각도.
    private const float OPEN_FINGER_CURL = 15f;
    private const float GRABBED_FINGER_SPREAD_RATIO = 0.4f;

    [Header("선")]
    [SerializeField] private LineRenderer _body;
    // 테두리는 검은 팔이 검은 잉크 위에서 묻히지 않게 깐다. 비워 두면 두르지 않는다.
    [SerializeField] private LineRenderer _bodyOutline;
    [SerializeField] private LineRenderer[] _fingers;
    [SerializeField] private LineRenderer[] _fingerOutlines;
    [SerializeField, Min(2)] private int _segments = 24;

    [Header("손가락")]
    [SerializeField, Min(0f)] private float _fingerLength = 0.24f;
    [SerializeField, Range(0f, 90f)] private float _fingerSpreadAngle = 32f;
    // 움켜쥐면 끝마디가 이 각도만큼 안쪽으로 말려 갈고리처럼 된다.
    [SerializeField, Range(0f, 150f)] private float _fingerCurlAngle = 80f;

    private readonly Vector3[] _fingerPoints = new Vector3[FINGER_POINT_COUNT];
    private Vector3[] _points;

    private InkSwallowSettings _settings;
    private Vector3 _root;
    private Vector3 _grabPoint;
    private float _startTime;
    private float _crawlDuration;
    private float _slitherPhase;
    private bool _isSinkArm;
    private bool _isVisible;

    public float ArrivalTime { get { return _startTime + _crawlDuration; } }

    private void Awake()
    {
        _points = new Vector3[_segments + 1];

        SetPointCount(_body, _points.Length);
        SetPointCount(_bodyOutline, _points.Length);

        for (int i = 0; i < _fingers.Length; i++)
        {
            SetPointCount(_fingers[i], FINGER_POINT_COUNT);
            SetPointCount(GetFingerOutline(i), FINGER_POINT_COUNT);
        }
    }

    // 풀에서 꺼낼 때마다 기어 나올 차례 전까지 숨겨 둔다.
    private void OnEnable()
    {
        ApplyVisible(false);
    }

    // 가라앉을 잉크의 팔(isSinkArm)은 대상을 쥔 채 따라가고, 나머지는 끌고 가는 동안 손을 놓고 되감긴다.
    public void Setup(Vector3 root, Vector3 grabPoint, float startTime, bool isSinkArm, InkSwallowSettings settings)
    {
        _root = root;
        _grabPoint = grabPoint;
        _startTime = startTime;
        _isSinkArm = isSinkArm;
        _settings = settings;
        _crawlDuration = Vector2.Distance(root, grabPoint) / settings.CrawlSpeed;
        _slitherPhase = Random.Range(0f, 2f * Mathf.PI);
    }

    // 기어 오는 도중 붙잡을 자리를 옮겨, 움직이는 대상을 쫓아가게 한다. 닿는 시간은 처음 정한 그대로다.
    public void SetGrabPoint(Vector3 grabPoint)
    {
        _grabPoint = grabPoint;
    }

    public void Tick(SwallowFrame frame)
    {
        float headProgress = SwallowFrame.GetProgress(frame.Elapsed, _startTime, _crawlDuration);
        float grab = frame.Grab;
        float widthScale = 1f;
        Vector3 end = _grabPoint;

        if (_isSinkArm)
        {
            end = frame.TargetPosition + (_grabPoint - frame.Center) * frame.Remain;
            widthScale = frame.Remain;
        }
        else
        {
            float release = Mathf.Clamp01(frame.PullProgress / _settings.ReleaseRatio);
            headProgress *= 1f - release;
            grab *= 1f - release;
        }

        bool isVisible = MIN_VISIBLE_PROGRESS < headProgress && 0f < widthScale;
        SetVisible(isVisible);

        if (false == isVisible)
        {
            return;
        }

        int lastIndex = _points.Length - 1;
        Vector3 along = end - _root;
        Vector3 side = new Vector3(-along.y, along.x, 0f).normalized * _settings.SlitherAmplitude;

        for (int i = 0; i <= lastIndex; i++)
        {
            _points[i] = GetPathPoint(along, side, headProgress * i / lastIndex);
        }

        SetPositions(_body, _bodyOutline, _points, widthScale);
        UpdateFingers(_points[lastIndex], _points[lastIndex] - _points[lastIndex - 1], grab, widthScale);
    }

    // 뿌리와 붙잡을 자리를 잇는 직선에 옆으로 구불거림을 더한다. 양 끝은 구불거림을 줄여 뿌리와 손이 제자리에 붙어 있게 한다.
    private Vector3 GetPathPoint(Vector3 along, Vector3 side, float progress)
    {
        float envelope = 4f * progress * (1f - progress);
        float slither = Mathf.Sin(2f * Mathf.PI * _settings.SlitherWaves * progress + _slitherPhase);

        return _root + along * progress + side * (slither * envelope);
    }

    // 손가락은 두 마디로, 펴졌을 땐 부채꼴로 벌어져 끝이 살짝 젖혀지고, 움켜쥐면 모이며 끝마디가 안쪽으로 말린다.
    private void UpdateFingers(Vector3 tip, Vector3 direction, float grab, float widthScale)
    {
        Vector3 forward = direction.normalized;
        float spread = _fingerSpreadAngle * Mathf.Lerp(1f, GRABBED_FINGER_SPREAD_RATIO, grab);
        float curl = Mathf.Lerp(-OPEN_FINGER_CURL, _fingerCurlAngle, grab);
        float length = _fingerLength * widthScale;
        float middle = (_fingers.Length - 1) * 0.5f;

        for (int i = 0; i < _fingers.Length; i++)
        {
            float offset = i - middle;
            // 가운데 손가락은 한쪽으로, 양옆 손가락은 손바닥 가운데 쪽으로 말린다.
            float curlSign = 0f < offset ? -1f : 1f;
            float firstAngle = offset * spread;

            Vector3 firstDirection = Quaternion.Euler(0f, 0f, firstAngle) * forward;
            Vector3 secondDirection = Quaternion.Euler(0f, 0f, firstAngle + curlSign * curl) * forward;
            Vector3 joint = tip + firstDirection * (length * FINGER_FIRST_JOINT_RATIO);

            _fingerPoints[0] = tip;
            _fingerPoints[1] = joint;
            _fingerPoints[2] = joint + secondDirection * (length * (1f - FINGER_FIRST_JOINT_RATIO));

            SetPositions(_fingers[i], GetFingerOutline(i), _fingerPoints, widthScale);
        }
    }

    // 매 프레임 불리므로, 바뀔 때만 렌더러를 켜고 끈다.
    private void SetVisible(bool isVisible)
    {
        if (isVisible != _isVisible)
        {
            ApplyVisible(isVisible);
        }
    }

    private void ApplyVisible(bool isVisible)
    {
        _isVisible = isVisible;

        SetEnabled(_body, isVisible);
        SetEnabled(_bodyOutline, isVisible);

        for (int i = 0; i < _fingers.Length; i++)
        {
            SetEnabled(_fingers[i], isVisible);
            SetEnabled(GetFingerOutline(i), isVisible);
        }
    }

    private LineRenderer GetFingerOutline(int index)
    {
        if (null == _fingerOutlines || index >= _fingerOutlines.Length)
        {
            return null;
        }

        return _fingerOutlines[index];
    }

    private static void SetPositions(LineRenderer line, LineRenderer outline, Vector3[] points, float widthScale)
    {
        line.SetPositions(points);
        line.widthMultiplier = widthScale;

        if (null != outline)
        {
            outline.SetPositions(points);
            outline.widthMultiplier = widthScale;
        }
    }

    private static void SetPointCount(LineRenderer line, int count)
    {
        if (null != line)
        {
            line.positionCount = count;
        }
    }

    private static void SetEnabled(LineRenderer line, bool isEnabled)
    {
        if (null != line)
        {
            line.enabled = isEnabled;
        }
    }
}
