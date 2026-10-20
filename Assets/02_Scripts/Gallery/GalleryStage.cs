using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Threading;
using UnityEngine;
using UnityEngine.InputSystem;

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

    [Header("돌아오기 연출")]
    [SerializeField, Min(0f)] private float _returnDuration = 1.2f;
    [SerializeField] private Ease _returnEase = Ease.InOutSine;

    [Header("일시정지")]
    [SerializeField] private InputActionReference _pauseAction;
    [SerializeField] private PauseMenuUI _pauseMenuPrefab;

    [Header("장비")]
    [SerializeField] private LoadoutView _loadoutViewPrefab;

    [Header("사운드")]
    [SerializeField] private AudioClip _bgm;
    [SerializeField, Min(0f)] private float _bgmFadeDuration = 1f;

    private GalleryPlayer _player;
    private GalleryHudUI _hud;
    private PauseMenuUI _pauseMenu;
    private LoadoutView _loadoutView;
    private LoadoutViewModel _loadoutViewModel;
    // 돌아왔을 때 이 액자 앞에서 물러나며 시작하려고 기억해 둔다.
    private GalleryFrame _enteredFrame;

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

        if (null == _pauseAction)
        {
            Debug.LogError("갤러리 : 일시정지 입력(InputActionReference)이 지정되지 않았습니다.");
        }
    }

    private void OnEnable()
    {
        _pauseAction.Enable();
    }

    private void OnDestroy()
    {
        _player.OnFocusChanged -= HandleFocusChanged;
        _player.OnInteract -= HandleInteract;
    }

    // Esc는 열린 창부터 닫고, 걸어 다닐 수 있을 때만 멈춘다. 액자로 다가가거나 물러나는 중에는 무시한다.
    private void Update()
    {
        if (false == _pauseAction.WasPressedThisFrame())
        {
            return;
        }

        if (null != _loadoutView)
        {
            HandleLoadoutClosed();
        }
        else if (null != _pauseMenu)
        {
            Resume();
        }
        else if (_player.IsControlEnabled)
        {
            Pause();
        }
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

        _hud = GameManager.UI.Open(_hudPrefab, UILayer.HUD);
        _hud.Setup(_player.InteractKeyLabel);

        SetCursorLocked(true);
        GameManager.Sound.PlayBgm(_bgm, _bgmFadeDuration);

        if (null == _enteredFrame)
        {
            SetControlEnabled(true);
            return;
        }

        ReturnFromFrameAsync(_enteredFrame, destroyCancellationToken).Forget();
        _enteredFrame = null;
    }

    // 전투의 결과 버튼을 누를 수 있게 커서를 다시 보인다.
    public void Hide()
    {
        ClosePauseMenu();
        CloseLoadout();
        _player.SetControlEnabled(false);

        GameManager.UI.Close(_hud);
        _hud = null;

        SetCursorLocked(false);

        gameObject.SetActive(false);
    }

    // 방금 깬 스테이지의 액자만 다음 Show에서 그림을 다시 읽게 한다.
    public void MarkPaintingChanged(StageData stage)
    {
        for (int i = 0; i < _frames.Length; i++)
        {
            if (stage == _frames[i].Stage)
            {
                _frames[i].MarkPaintingChanged();
            }
        }
    }

    private void HandleFocusChanged(GalleryInteractable target)
    {
        if (null != _hud)
        {
            _hud.ShowFocus(target);
        }
    }

    private void HandleInteract(GalleryInteractable target)
    {
        if (target is GalleryFrame frame)
        {
            InteractFrame(frame);
        }
        else if (target is LoadoutStand)
        {
            OpenLoadout();
        }
    }

    private void InteractFrame(GalleryFrame frame)
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
        _enteredFrame = frame;
        _player.SavePose();
        SetControlEnabled(false);
        GameManager.Sound.StopBgm(_bgmFadeDuration);

        await _player.MoveCameraToAsync(frame.ViewPoint, _approachDuration, _approachEase, token);

        OnStageChosen?.Invoke(frame.Stage);
    }

    // 들어갔던 그림 앞에서 시작해, 걸린 그림을 보며 서 있던 자리로 물러난다.
    private async UniTaskVoid ReturnFromFrameAsync(GalleryFrame frame, CancellationToken token)
    {
        SetControlEnabled(false);
        _player.PlaceCameraAt(frame.ViewPoint);

        await _player.MoveCameraHomeAsync(_returnDuration, _returnEase, token);

        SetControlEnabled(true);
    }

    // 갤러리에는 시간에 따라 움직이는 것이 플레이어뿐이라, 시간 배율은 두고 조작만 멈춘다.
    private void Pause()
    {
        _pauseMenu = GameManager.UI.Open(_pauseMenuPrefab, UILayer.Overlay);

        if (null == _pauseMenu)
        {
            return;
        }

        _pauseMenu.OnResume += Resume;

        SetControlEnabled(false);
        SetCursorLocked(false);
    }

    private void Resume()
    {
        ClosePauseMenu();

        SetControlEnabled(true);
        SetCursorLocked(true);
    }

    private void ClosePauseMenu()
    {
        if (null == _pauseMenu)
        {
            return;
        }

        _pauseMenu.OnResume -= Resume;
        GameManager.UI.Close(_pauseMenu);
        _pauseMenu = null;
    }

    private void OpenLoadout()
    {
        _loadoutView = GameManager.UI.Open(_loadoutViewPrefab, UILayer.Content);

        if (null == _loadoutView)
        {
            return;
        }

        _loadoutViewModel = new LoadoutViewModel(GameManager.Player.Loadout, GameManager.Tables, GameManager.Assets);
        _loadoutViewModel.OnCloseRequested += HandleLoadoutClosed;
        _loadoutView.Bind(_loadoutViewModel);

        SetControlEnabled(false);
        SetCursorLocked(false);
    }

    private void HandleLoadoutClosed()
    {
        CloseLoadout();

        SetControlEnabled(true);
        SetCursorLocked(true);
    }

    // 어떻게 닫든(닫기 버튼, Esc, 갤러리를 떠날 때) 바꾼 장비를 저장한다.
    private void CloseLoadout()
    {
        if (null == _loadoutView)
        {
            return;
        }

        _loadoutViewModel.OnCloseRequested -= HandleLoadoutClosed;
        _loadoutViewModel.UnBind();
        _loadoutViewModel = null;

        GameManager.SavePlayer();

        GameManager.UI.Close(_loadoutView);
        _loadoutView = null;
    }

    private void SetControlEnabled(bool isEnabled)
    {
        _player.SetControlEnabled(isEnabled);
        _hud.SetCrosshairVisible(isEnabled);
    }

    private void SetCursorLocked(bool isLocked)
    {
        Cursor.lockState = isLocked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = false == isLocked;
    }
}
