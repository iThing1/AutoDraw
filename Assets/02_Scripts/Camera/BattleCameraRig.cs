using Unity.Cinemachine;
using UnityEngine;

public class BattleCameraRig : MonoBehaviour
{
    private const int ACTIVE_PRIORITY = 20;
    private const int INACTIVE_PRIORITY = 10;
    // 줌이 이만큼 바뀔 때만 경계를 다시 계산한다. 매 프레임 계산은 비싸다.
    private const float CONFINER_RECALCULATE_ORTHO_DELTA = 0.05f;

    [SerializeField] private CinemachineBrain _brain;
    [SerializeField] private CinemachineCamera _playerCamera;
    [SerializeField] private CinemachineCamera _cinematicCamera;
    [SerializeField] private CinemachineCamera _battleCamera;
    [SerializeField] private CinemachineTargetGroup _battleTargetGroup;
    [SerializeField] private CinemachineImpulseSource _impulseSource;

    [Header("흔들림")]
    [SerializeField, Min(0f)] private float _shakeScale = 1f;
    [SerializeField, Min(0f)] private float _shakeMergeWindow = 0.2f;

    // 첫 흔들림은 겹침 검사에 걸리지 않도록 아주 먼 과거로 시작한다.
    private float _lastShakeTime = float.NegativeInfinity;
    private float _lastShakeForce;

    // 컷신이 끝나고 돌아갈 카메라. 접근 중에는 플레이어 카메라, 전투가 준비되면 전투 카메라가 된다.
    private CinemachineCamera _gameplayCamera;

    private Collider2D _cameraBounds;
    private CinemachineCamera[] _confinedCameras;
    private CinemachineConfiner2D[] _confiners;
    private float[] _lastOrthoSizes;

    // 판 전체(엔딩)나 일부 영역(기믹)을 고정해 비추는 카메라. 가끔만 쓰여 씬에 두지 않고 처음 쓸 때 만든다.
    private CinemachineCamera _fixedCamera;
    private CinemachineCamera _activeCamera;
    // 브레인과 같은 오브젝트의 카메라. 처음 찾을 때 한 번만 찾는다.
    private Camera _renderCamera;

    public Camera OutputCamera { get { return _brain.OutputCamera; } }
    // 브레인을 거치지 않고 직접 켜고 끄거나 그림을 찍을 때 쓰는 카메라.
    // 브레인의 OutputCamera는 게임 시작 직후 비어 있을 수 있어 같은 오브젝트의 카메라를 직접 찾는다.
    public Camera RenderCamera
    {
        get
        {
            if (null == _renderCamera)
            {
                _renderCamera = _brain.GetComponent<Camera>();
            }

            return _renderCamera;
        }
    }
    // 스테이지가 넘겨준 카메라 경계. 경계가 없는 스테이지면 null이다.
    public Collider2D CameraBounds { get { return _cameraBounds; } }

    public void SetOutputEnabled(bool isEnabled)
    {
        RenderCamera.enabled = isEnabled;
    }

    // Confiner2D는 효율 때문에 줌 변화를 스스로 반영하지 않는다. 전투 카메라는 두 유닛을 담으려고 줌이 계속 바뀌므로 직접 알린다.
    private void LateUpdate()
    {
        if (null == _confiners)
        {
            return;
        }

        for (int i = 0; i < _confiners.Length; i++)
        {
            float orthoSize = _confinedCameras[i].State.Lens.OrthographicSize;

            if (Mathf.Abs(orthoSize - _lastOrthoSizes[i]) < CONFINER_RECALCULATE_ORTHO_DELTA)
            {
                continue;
            }

            _lastOrthoSizes[i] = orthoSize;
            _confiners[i].InvalidateLensCache();
        }
    }

    public bool HasRequiredReferences()
    {
        if (null == _brain || null == _playerCamera || null == _cinematicCamera || null == _battleCamera
            || null == _battleTargetGroup || null == _impulseSource)
        {
            Debug.LogError("카메라 : 브레인, 플레이어·시네마틱·전투 카메라, 전투 타깃 그룹, 임펄스 소스 중 지정되지 않은 것이 있습니다.");
            return false;
        }

        return true;
    }

    public void SetCameraBounds(Collider2D bounds)
    {
        _cameraBounds = bounds;
        _confinedCameras = new CinemachineCamera[] { _playerCamera, _cinematicCamera, _battleCamera };
        _confiners = new CinemachineConfiner2D[_confinedCameras.Length];
        _lastOrthoSizes = new float[_confinedCameras.Length];

        for (int i = 0; i < _confinedCameras.Length; i++)
        {
            CinemachineConfiner2D confiner = _confinedCameras[i].gameObject.GetOrAddComponent<CinemachineConfiner2D>();
            confiner.BoundingShape2D = bounds;
            confiner.InvalidateBoundingShapeCache();
            confiner.enabled = true;

            _confiners[i] = confiner;
        }
    }

    public void ClearCameraBounds()
    {
        if (null != _confiners)
        {
            for (int i = 0; i < _confiners.Length; i++)
            {
                if (null != _confiners[i])
                {
                    _confiners[i].BoundingShape2D = null;
                    _confiners[i].enabled = false;
                }
            }
        }

        _cameraBounds = null;
        _confinedCameras = null;
        _confiners = null;
        _lastOrthoSizes = null;
    }

    // 엔딩 슬로 모션 중에도 정해진 시간에 끝나도록 시간 배율을 무시한다.
    public bool TryShowWholeStage(float padding, float blendDuration)
    {
        if (null == _cameraBounds)
        {
            return false;
        }

        Bounds bounds = _cameraBounds.bounds;

        _brain.IgnoreTimeScale = true;
        ShowFixedView(bounds.center, GetOrthoSizeToFit(bounds, padding), blendDuration);

        return true;
    }

    // 기믹이 판의 한 영역을 다 보여 줘야 할 때 그 가운데에 카메라를 고정한다. 화면이 카메라 경계 밖을 비추지 않게 가운데를 안쪽으로 당긴다.
    public void ShowArea(Bounds area, float padding, float blendDuration)
    {
        float orthoSize = GetOrthoSizeToFit(area, padding);
        Vector3 center = area.center;

        if (null != _cameraBounds)
        {
            center = ClampViewCenter(center, orthoSize, _cameraBounds.bounds);
        }

        _brain.IgnoreTimeScale = false;
        ShowFixedView(center, orthoSize, blendDuration);
    }

    // ShowArea로 비추는 중일 때만 전투 카메라로 돌아간다. 그사이 다른 연출이 카메라를 가져갔으면 그대로 둔다.
    public void EndShowArea(float blendDuration)
    {
        if (null != _fixedCamera && _fixedCamera == _activeCamera)
        {
            ReturnToGameplay(blendDuration);
        }
    }

    public void FollowPlayer(Transform player)
    {
        _playerCamera.Follow = player;

        // 생성 위치에서 플레이어 위치까지 감쇠로 미끄러져 오지 않도록 첫 프레임은 바로 맞춘다.
        _playerCamera.PreviousStateIsValid = false;

        // 엔딩·패배 연출이 켜 둔 채로 다시 시작할 수 있다.
        _brain.IgnoreTimeScale = false;

        _gameplayCamera = _playerCamera;
        SetActiveCamera(_playerCamera, 0f);
    }

    // 플레이어와 보스를 한 화면에 담는 전투 카메라를 준비만 하고, 실제 전환은 ReturnToGameplay에서 한다.
    public void PrepareBattleFraming(BattleUnit player, BattleUnit boss)
    {
        // 넉백은 자식 Body만 흔들므로, 루트를 따라가야 맞을 때마다 카메라가 함께 흔들리지 않는다.
        _battleTargetGroup.Targets.Clear();
        _battleTargetGroup.AddMember(player.transform, 1f, player.Data.FramingRadius);
        _battleTargetGroup.AddMember(boss.transform, 1f, boss.Data.FramingRadius);

        _battleCamera.Follow = _battleTargetGroup.transform;

        _gameplayCamera = _battleCamera;
    }

    // 처치 슬로 모션 중에도 정해진 시간 안에 보스에게 도착하도록, 시네마틱 카메라 이동은 시간 배율을 무시한다.
    public void FocusTarget(Transform target, float blendDuration)
    {
        _brain.IgnoreTimeScale = true;
        _cinematicCamera.Follow = target;

        SetActiveCamera(_cinematicCamera, blendDuration);
    }

    // 전투 중에는 히트 스톱에 맞춰 카메라 추적도 함께 멈춰야 타격이 묵직하게 보인다.
    public void ReturnToGameplay(float blendDuration)
    {
        _brain.IgnoreTimeScale = false;
        SetActiveCamera(_gameplayCamera, blendDuration);
    }

    // 연사·파편처럼 짧은 간격으로 여러 번 맞아도 흔들림이 쌓이지 않도록, 직전 흔들림이 끝나기 전에 온 약한 흔들림은 건너뛴다.
    // 히트 스톱 중에도 간격이 흘러가야 하므로 실제 시간으로 잰다.
    public void Shake(float force)
    {
        float scaledForce = force * _shakeScale;

        if (0f >= scaledForce)
        {
            return;
        }

        bool isOverlapping = Time.unscaledTime - _lastShakeTime < _shakeMergeWindow;

        if (isOverlapping && scaledForce <= _lastShakeForce)
        {
            return;
        }

        _lastShakeTime = Time.unscaledTime;
        _lastShakeForce = scaledForce;

        _impulseSource.GenerateImpulseWithForce(scaledForce);
    }

    private void SetActiveCamera(CinemachineCamera target, float blendDuration)
    {
        _brain.DefaultBlend = 0f < blendDuration
            ? new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, blendDuration)
            : new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.Cut, 0f);

        _activeCamera = target;
        _playerCamera.Priority = target == _playerCamera ? ACTIVE_PRIORITY : INACTIVE_PRIORITY;
        _cinematicCamera.Priority = target == _cinematicCamera ? ACTIVE_PRIORITY : INACTIVE_PRIORITY;
        _battleCamera.Priority = target == _battleCamera ? ACTIVE_PRIORITY : INACTIVE_PRIORITY;

        if (null != _fixedCamera)
        {
            _fixedCamera.Priority = target == _fixedCamera ? ACTIVE_PRIORITY : INACTIVE_PRIORITY;
        }
    }

    private void ShowFixedView(Vector3 center, float orthoSize, float blendDuration)
    {
        if (null == _fixedCamera)
        {
            _fixedCamera = CreateFixedCamera();
        }

        LensSettings lens = _battleCamera.Lens;
        lens.OrthographicSize = orthoSize;
        _fixedCamera.Lens = lens;

        float cameraZ = _battleCamera.transform.position.z;
        _fixedCamera.transform.position = new Vector3(center.x, center.y, cameraZ);

        SetActiveCamera(_fixedCamera, blendDuration);
    }

    // 직교 크기는 화면 높이의 절반이라, 가로는 화면 비율로 나눠 높이 기준으로 바꾼다.
    private float GetOrthoSizeToFit(Bounds area, float padding)
    {
        return Mathf.Max(area.extents.y + padding, (area.extents.x + padding) / OutputCamera.aspect);
    }

    private Vector3 ClampViewCenter(Vector3 center, float orthoSize, Bounds bounds)
    {
        center.x = ClampViewAxis(center.x, orthoSize * OutputCamera.aspect, bounds.min.x, bounds.max.x);
        center.y = ClampViewAxis(center.y, orthoSize, bounds.min.y, bounds.max.y);

        return center;
    }

    // 화면이 경계보다 넓으면 경계 가운데에 둔다.
    private static float ClampViewAxis(float value, float halfView, float min, float max)
    {
        if (max - min <= halfView * 2f)
        {
            return (min + max) * 0.5f;
        }

        return Mathf.Clamp(value, min + halfView, max - halfView);
    }

    private CinemachineCamera CreateFixedCamera()
    {
        GameObject cameraObject = new GameObject("FixedCamera");
        cameraObject.transform.SetParent(transform, false);

        CinemachineCamera fixedCamera = cameraObject.AddComponent<CinemachineCamera>();
        fixedCamera.Priority = INACTIVE_PRIORITY;

        return fixedCamera;
    }
}
