using Unity.Cinemachine;
using UnityEngine;

public class BattleCameraRig : MonoBehaviour
{
    private const int ACTIVE_PRIORITY = 20;
    private const int INACTIVE_PRIORITY = 10;

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

    public Camera OutputCamera { get { return _brain.OutputCamera; } }

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
        _battleTargetGroup.Targets.Clear();
        _battleTargetGroup.AddMember(player.Body, 1f, player.Data.FramingRadius);
        _battleTargetGroup.AddMember(boss.Body, 1f, boss.Data.FramingRadius);

        _battleCamera.Follow = _battleTargetGroup.transform;

        _gameplayCamera = _battleCamera;
    }

    public void FocusTarget(Transform target, float blendDuration)
    {
        _cinematicCamera.Follow = target;

        SetActiveCamera(_cinematicCamera, blendDuration);
    }

    public void ReturnToGameplay(float blendDuration)
    {
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
