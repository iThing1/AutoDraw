using Unity.Cinemachine;
using UnityEngine;

public class BattleCameraRig : MonoBehaviour
{
    [SerializeField] private CinemachineCamera _playerCamera;

    public void FollowPlayer(Transform player)
    {
        if (null == _playerCamera)
        {
            Debug.LogError("카메라 : 플레이어 카메라가 지정되지 않았습니다.");
            return;
        }

        _playerCamera.Follow = player;

        // 생성 위치에서 플레이어 위치까지 감쇠로 미끄러져 오지 않도록 첫 프레임은 바로 맞춘다.
        _playerCamera.PreviousStateIsValid = false;
    }
}
