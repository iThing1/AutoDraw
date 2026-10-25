using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Threading;
using UnityEngine;
using UnityEngine.InputSystem;

// 갤러리 맵. 맵 사이 이동(암전·생성)은 MapSpawner가 맡고, 여기는 갤러리 안의 일만 한다.
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

    [Header("특성")]
    [SerializeField] private PerkTreeView _perkTreeViewPrefab;

    [Header("사운드")]
    [SerializeField] private AudioClip _bgm;
    [SerializeField, Min(0f)] private float _bgmFadeDuration = 1f;

    private GalleryPlayer _player;
    private GalleryHudUI _hud;
    private PauseMenuUI _pauseMenu;
    // 상호작용으로 연 창(장비, 특성 트리). 한 번에 하나만 연다.
    private Component _screen;
    private GalleryScreenViewModel _screenViewModel;
    private GalleryPalettes _palettes;
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

        _palettes = new GalleryPalettes(GetComponentsInChildren<PaletteItem>(true));

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

        if (null != _screen)
        {
            HandleScreenClosed();
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

    public void Show()
    {
        gameObject.SetActive(true);

        for (int i = 0; i < _frames.Length; i++)
        {
            _frames[i].Refresh();
        }

        _palettes.Refresh(GameManager.Player.Progress);
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
        CloseScreen();
        _player.SetControlEnabled(false);

        GameManager.UI.Close(_hud);
        _hud = null;

        SetCursorLocked(false);

        gameObject.SetActive(false);
    }

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

    // 아래는 상호작용한 물건(GalleryInteractable.Interact)이 부른다.
    public void EnterFrame(GalleryFrame frame)
    {
        EnterFrameAsync(frame, destroyCancellationToken).Forget();
    }

    public void OpenLoadout()
    {
        OpenScreen(_loadoutViewPrefab, new LoadoutViewModel(GameManager.Player, GameManager.Tables, GameManager.Assets));
    }

    public void OpenPerkTree()
    {
        OpenScreen(_perkTreeViewPrefab, new PerkTreeViewModel(GameManager.Player, GameManager.Tables, GameManager.Assets));
    }

    // 갤러리를 떠나기 전에 게임이 꺼져도 남도록 바로 저장한다.
    public void CollectPalette(PaletteItem palette)
    {
        PlayerModel player = GameManager.Player;

        if (false == player.CollectPalette(palette.PaletteId))
        {
            return;
        }

        palette.gameObject.SetActive(false);
        GameManager.SavePlayer();
        _hud.ShowPaletteFound(player.AvailablePalettes);
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
        target.Interact(this);
    }

    private async UniTaskVoid EnterFrameAsync(GalleryFrame frame, CancellationToken token)
    {
        _enteredFrame = frame;
        _player.SavePose();
        SetControlEnabled(false);
        GameManager.Sound.StopBgm(_bgmFadeDuration);

        await _player.MoveCameraToAsync(frame.ViewPoint, _approachDuration, _approachEase, token);

        OnStageChosen?.Invoke(frame.Stage);
    }

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

    // 창이 이미 열려 있거나 열리지 않으면 만든 ViewModel의 구독을 바로 푼다.
    private void OpenScreen<TView, TViewModel>(TView prefab, TViewModel viewModel)
        where TView : ViewBase<TViewModel>
        where TViewModel : GalleryScreenViewModel
    {
        if (null != _screen)
        {
            viewModel.UnBind();
            return;
        }

        TView view = GameManager.UI.Open(prefab, UILayer.Content);

        if (null == view)
        {
            viewModel.UnBind();
            return;
        }

        _screen = view;
        _screenViewModel = viewModel;
        _screenViewModel.OnCloseRequested += HandleScreenClosed;
        view.Bind(viewModel);

        SetControlEnabled(false);
        SetCursorLocked(false);
    }

    private void HandleScreenClosed()
    {
        CloseScreen();

        SetControlEnabled(true);
        SetCursorLocked(true);
    }

    // 어떻게 닫든(닫기 버튼, Esc, 갤러리를 떠날 때) 창에서 바꾼 것(장비, 특성)을 저장한다.
    private void CloseScreen()
    {
        if (null == _screen)
        {
            return;
        }

        _screenViewModel.OnCloseRequested -= HandleScreenClosed;
        _screenViewModel.UnBind();
        _screenViewModel = null;

        GameManager.SavePlayer();

        GameManager.UI.Close(_screen);
        _screen = null;
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
