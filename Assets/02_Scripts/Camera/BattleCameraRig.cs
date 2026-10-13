using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Serialization;

public class BattleCameraRig : MonoBehaviour
{
    private const int ACTIVE_PRIORITY = 20;
    private const int INACTIVE_PRIORITY = 10;

    [SerializeField] private CinemachineBrain _brain;
    [SerializeField] private CinemachineCamera _playerCamera;
    [SerializeField, FormerlySerializedAs("_bossCamera")] private CinemachineCamera _cinematicCamera;
    [SerializeField] private CinemachineImpulseSource _impulseSource;

    public bool HasRequiredReferences()
    {
        if (null == _brain || null == _playerCamera || null == _cinematicCamera || null == _impulseSource)
        {
            Debug.LogError("카메라 : 브레인, 플레이어·시네마틱 카메라, 임펄스 소스 중 지정되지 않은 것이 있습니다.");
            return false;
        }

        return true;
    }

    public void FollowPlayer(Transform player)
    {
        _playerCamera.Follow = player;

        // 생성 위치에서 플레이어 위치까지 감쇠로 미끄러져 오지 않도록 첫 프레임은 바로 맞춘다.
        _playerCamera.PreviousStateIsValid = false;

        SetActiveCamera(_playerCamera, 0f);
    }

    public void FocusTarget(Transform target, float blendDuration)
    {
        _cinematicCamera.Follow = target;

        SetActiveCamera(_cinematicCamera, blendDuration);
    }

    public void FocusPlayer(float blendDuration)
    {
        SetActiveCamera(_playerCamera, blendDuration);
    }

    public void Shake(float force)
    {
        _impulseSource.GenerateImpulseWithForce(force);
    }

    private void SetActiveCamera(CinemachineCamera target, float blendDuration)
    {
        _brain.DefaultBlend = 0f < blendDuration
            ? new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, blendDuration)
            : new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.Cut, 0f);

        _playerCamera.Priority = target == _playerCamera ? ACTIVE_PRIORITY : INACTIVE_PRIORITY;
        _cinematicCamera.Priority = target == _cinematicCamera ? ACTIVE_PRIORITY : INACTIVE_PRIORITY;
    }
}
