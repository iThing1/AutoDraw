using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Threading;
using UnityEngine;

// 갤러리 맵. 1인칭으로 걸어 다니다 액자를 골라 그 그림 속 스테이지로 들어간다.
// 맵 사이 이동(암전·생성)은 MapSpawner가 맡고, 여기는 갤러리 안의 일만 한다.
public class GalleryStage : MonoBehaviour
{
    [Header("플레이어")]
    [SerializeField] private GalleryPlayer _playerPrefab;
    [SerializeField] private Transform _playerSpawnPoint;

    [Header("액자")]
    [SerializeField] private GalleryFrame[] _frames;
    [SerializeField] private GalleryHudUI _hudPrefab;

    [Header("진입 연출")]
    [SerializeField, Min(0f)] private float _approachDuration = 0.8f;
    [SerializeField] private Ease _approachEase = Ease.InOutSine;

    private GalleryPlayer _player;
    private GalleryHudUI _hud;

    // 들어갈 스테이지를 고르면 알린다. 화면을 가리고 스테이지를 만드는 것은 받는 쪽이 한다.
    public event Action<StageData> OnStageChosen;

    // 갤러리를 숨기면 함께 숨도록 플레이어를 갤러리 아래에 만든다.
    private void Awake()
    {
        _player = Instantiate(_playerPrefab, _playerSpawnPoint.position, _playerSpawnPoint.rotation, transform);
        _player.name = _playerPrefab.name;

        _player.OnFocusChanged += HandleFocusChanged;
        _player.OnInteract += HandleInteract;
        _player.SavePose();
    }

    private void OnDestroy()
    {
        _player.OnFocusChanged -= HandleFocusChanged;
        _player.OnInteract -= HandleInteract;
    }

    // 마지막으로 서 있던 자리에서 다시 걸어 다닌다. 마우스로 둘러보도록 커서를 숨겨 잠근다.
    public void Show()
    {
        gameObject.SetActive(true);

        for (int i = 0; i < _frames.Length; i++)
        {
            _frames[i].Refresh();
        }

        _player.RestorePose();
        _player.SetControlEnabled(true);

        _hud = GameManager.UI.Open(_hudPrefab, UILayer.HUD);
        _hud.Setup(_player.InteractKeyLabel);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // 전투의 결과 버튼을 누를 수 있게 커서를 다시 보인다.
    public void Hide()
    {
        _player.SetControlEnabled(false);

        GameManager.UI.Close(_hud);
        _hud = null;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        gameObject.SetActive(false);
    }

    private void HandleFocusChanged(GalleryFrame frame)
    {
        if (null != _hud)
        {
            _hud.ShowFocus(frame);
        }
    }

    private void HandleInteract(GalleryFrame frame)
    {
        if (frame.IsLocked)
        {
            frame.PlayLocked();
            return;
        }

        EnterFrameAsync(frame, destroyCancellationToken).Forget();
    }

    // 돌아왔을 때 그 자리에서 다시 걷도록, 다가가기 전에 서 있던 자리를 기억해 둔다.
    private async UniTaskVoid EnterFrameAsync(GalleryFrame frame, CancellationToken token)
    {
        _player.SavePose();
        _player.SetControlEnabled(false);
        _hud.SetCrosshairVisible(false);

        await _player.MoveCameraToAsync(frame.ViewPoint, _approachDuration, _approachEase, token);

        OnStageChosen?.Invoke(frame.Stage);
    }
}
