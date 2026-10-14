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

    private CinemachineCamera[] _confinedCameras;
    private CinemachineConfiner2D[] _confiners;
    private float[] _lastOrthoSizes;

    public Camera OutputCamera { get { return _brain.OutputCamera; } }

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

    // 경계는 스테이지마다 달라 스테이지가 넘겨준다. 모든 카메라가 이 영역 밖을 비추지 않는다.
    public void SetCameraBounds(Collider2D bounds)
    {
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

    // 다음 스테이지에 이전 경계가 남지 않게 끈다.
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

        _confinedCameras = null;
        _confiners = null;
        _lastOrthoSizes = null;
    }

    public void FollowPlayer(Transform player)
    {
        _playerCamera.Follow = player;

        // 생성 위치에서 플레이어 위치까지 감쇠로 미끄러져 오지 않도록 첫 프레임은 바로 맞춘다.
        _playerCamera.PreviousStateIsValid = false;

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

        _playerCamera.Priority = target == _playerCamera ? ACTIVE_PRIORITY : INACTIVE_PRIORITY;
        _cinematicCamera.Priority = target == _cinematicCamera ? ACTIVE_PRIORITY : INACTIVE_PRIORITY;
        _battleCamera.Priority = target == _battleCamera ? ACTIVE_PRIORITY : INACTIVE_PRIORITY;
    }
}
