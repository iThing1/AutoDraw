using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Threading;
using UnityEngine;

// 갤러리 맵. 맵 사이 이동(암전·생성)은 MapSpawner가 맡고, 여기는 갤러리 안의 일만 한다.
public class GalleryStage : MonoBehaviour, IPausable
{
    [Header("플레이어")]
    [SerializeField] private GalleryPlayer _playerPrefab;
    [SerializeField] private Transform _playerSpawnPoint;

    [Header("액자")]
    [SerializeField] private GalleryFrame[] _frames;
    [SerializeField] private GalleryHudUI _hudPrefab;

    [Header("전시실")]
    [SerializeField] private RoomData _room;
    [SerializeField] private GalleryDoor _door;
    [SerializeField] private RoomCompleteSettings _roomComplete = new();

    [Header("액자 열림 연출")]
    [SerializeField, Min(0f)] private float _frameOpenedMoveDuration = 1f;
    [SerializeField, Min(0f)] private float _frameOpenedHoldDuration = 0.6f;
    [SerializeField] private AudioClip _frameOpenedSound;

    [Header("진입 연출")]
    [SerializeField, Min(0f)] private float _approachDuration = 0.8f;
    [SerializeField] private Ease _approachEase = Ease.InOutSine;

    [Header("돌아오기 연출")]
    [SerializeField, Min(0f)] private float _returnDuration = 1.2f;
    [SerializeField] private Ease _returnEase = Ease.InOutSine;

    [Header("장비")]
    [SerializeField] private LoadoutView _loadoutViewPrefab;

    [Header("특성")]
    [SerializeField] private PerkTreeView _perkTreeViewPrefab;

    [Header("사운드")]
    [SerializeField] private AudioClip _bgm;
    [SerializeField, Min(0f)] private float _bgmFadeDuration = 1f;

    private GalleryPlayer _player;
    private GalleryHudUI _hud;
    // 상호작용으로 연 창(장비, 특성 트리). 한 번에 하나만 연다.
    private Component _screen;
    private GalleryScreenViewModel _screenViewModel;
    private GalleryPalettes _palettes;
    // 돌아왔을 때 이 액자 앞에서 물러나며 시작하려고 기억해 둔다.
    private GalleryFrame _enteredFrame;
    private RoomCompleteCutscene _roomCompleteCutscene;

    // 들어갈 스테이지를 고르면 알린다. 화면을 가리고 스테이지를 만드는 것은 받는 쪽이 한다.
    public event Action<StageData> OnStageChosen;

    // 걸어 다닐 수 있을 때만 멈춘다. 액자로 다가가거나 물러나는 중이나 창이 열린 동안은 조작이 꺼져 있다.
    public bool CanPause { get { return _player.IsControlEnabled; } }
    public bool CanLeave { get { return false; } }

    // 갤러리를 숨기면 함께 숨도록 플레이어를 갤러리 아래에 만든다.
    private void Awake()
    {
        _player = Instantiate(_playerPrefab, _playerSpawnPoint.position, _playerSpawnPoint.rotation, transform);
        _player.name = _playerPrefab.name;

        _player.OnFocusChanged += HandleFocusChanged;
        _player.OnInteract += HandleInteract;
        _player.SavePose();

        _palettes = new GalleryPalettes(GetComponentsInChildren<PaletteItem>(true));
        _roomCompleteCutscene = new RoomCompleteCutscene(_player, _roomComplete);

        if (null == _room)
        {
            Debug.LogError("갤러리 : 전시실 데이터가 지정되지 않아 순서 잠금과 완성 연출 없이 진행합니다.");
        }
    }

    private void OnDestroy()
    {
        _player.OnFocusChanged -= HandleFocusChanged;
        _player.OnInteract -= HandleInteract;
    }

    public void Show()
    {
        gameObject.SetActive(true);

        PlayerProgress progress = GameManager.Player.Progress;
        GalleryFrame returnedFrame = _enteredFrame;
        GalleryFrame openedFrame = RefreshFrames(progress);

        _enteredFrame = null;

        // 액자에서 돌아왔을 때만 새로 열린 것이다. 처음 들어올 때는 모든 액자가 잠긴 상태에서 시작한다.
        if (null == returnedFrame)
        {
            openedFrame = null;
        }
        else if (null != openedFrame)
        {
            openedFrame.KeepClothForUnveil();
        }

        RefreshDoor(progress);
        _palettes.Refresh(progress);
        _player.RestorePose();

        _hud = GameManager.UI.Open(_hudPrefab, UILayer.HUD);
        _hud.Setup(_player.InteractKeyLabel);

        SetCursorLocked(true);
        GameManager.Sound.PlayBgm(_bgm, _bgmFadeDuration);

        ArriveAsync(returnedFrame, openedFrame, destroyCancellationToken).Forget();
    }

    // 이번에 잠김이 풀린 액자를 돌려준다.
    private GalleryFrame RefreshFrames(PlayerProgress progress)
    {
        GalleryFrame openedFrame = null;

        for (int i = 0; i < _frames.Length; i++)
        {
            GalleryFrame frame = _frames[i];
            bool wasLocked = frame.IsLocked;

            frame.Refresh(null == _room || _room.IsOpen(frame.Stage, progress));

            if (wasLocked && false == frame.IsLocked)
            {
                openedFrame = frame;
            }
        }

        return openedFrame;
    }

    private void RefreshDoor(PlayerProgress progress)
    {
        bool isComplete = null != _room && _room.IsComplete(progress);

        _door.Refresh(isComplete, isComplete && false == progress.IsDoorVisited(_room.SaveKey));
    }

    // 전투의 결과 버튼을 누를 수 있게 커서를 다시 보인다.
    public void Hide()
    {
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

    // 다음 전시실이 생기면 여기서 옮겨 간다. 지금은 안내만 띄운다. 문은 전시실을 모두 채워야 열려 _room이 늘 있다.
    public void UseDoor(GalleryDoor door)
    {
        GameManager.Player.Progress.MarkDoorVisited(_room.SaveKey);
        GameManager.SavePlayer();

        door.Refresh(true, false);
        _hud.ShowNotice(_room.NextRoomNotice);
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

    // 처음 들어오면 전시실 이름을 알리고, 액자에서 돌아오면 물러난 뒤 조작을 돌려준다.
    private async UniTaskVoid ArriveAsync(GalleryFrame returnedFrame, GalleryFrame openedFrame, CancellationToken token)
    {
        SetControlEnabled(false);

        if (null != returnedFrame)
        {
            _player.PlaceCameraAt(returnedFrame.ViewPoint);
            await _player.MoveCameraHomeAsync(_returnDuration, _returnEase, token);

            if (null != openedFrame)
            {
                await PlayFrameOpenedAsync(openedFrame, token);
            }

            await PlayRoomCompleteIfNeededAsync(token);
        }
        else if (null != _room)
        {
            _hud.ShowNotice(_room.DisplayName);
        }

        SetControlEnabled(true);
    }

    // 새로 열린 액자를 비춰 천을 걷어 내고 제자리로 돌아온다.
    private async UniTask PlayFrameOpenedAsync(GalleryFrame frame, CancellationToken token)
    {
        await _player.MoveCameraToAsync(frame.ViewPoint, _frameOpenedMoveDuration, _returnEase, token);

        GameManager.Sound.PlaySfx(_frameOpenedSound);
        _hud.ShowFrameOpened(frame.Label);

        await frame.PlayUnveilAsync(token);
        await GameManager.TimeScale.WaitRealtimeAsync(_frameOpenedHoldDuration, token);
        await _player.MoveCameraHomeAsync(_frameOpenedMoveDuration, _returnEase, token);
    }

    // 완성 기록이 없을 때만 튼다. 끝까지 본 뒤에 기록해, 연출 도중 게임을 끄면 다음에 돌아올 때 다시 보여 준다.
    private async UniTask PlayRoomCompleteIfNeededAsync(CancellationToken token)
    {
        PlayerProgress progress = GameManager.Player.Progress;

        if (null == _room || false == _room.IsComplete(progress) || progress.IsRoomCompleted(_room.SaveKey))
        {
            return;
        }

        await _roomCompleteCutscene.PlayAsync(_room, _frames, token);

        progress.MarkRoomCompleted(_room.SaveKey);
        GameManager.SavePlayer();
    }

    // Esc는 열린 창(장비, 특성 트리)부터 닫는다.
    public bool TryCloseScreen()
    {
        if (null == _screen)
        {
            return false;
        }

        HandleScreenClosed();
        return true;
    }

    // 메뉴를 누를 수 있게 조작을 끄고 커서를 보인다.
    public void OnPauseChanged(bool isPaused)
    {
        SetControlEnabled(false == isPaused);
        SetCursorLocked(false == isPaused);
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

        GameManager.Sound.PlayUi(UISoundKind.Open);
        SetControlEnabled(false);
        SetCursorLocked(false);
    }

    // 닫기 버튼과 Esc가 모두 여기로 와서, 닫는 소리도 여기서 낸다.
    private void HandleScreenClosed()
    {
        GameManager.Sound.PlayUi(UISoundKind.Close);
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
