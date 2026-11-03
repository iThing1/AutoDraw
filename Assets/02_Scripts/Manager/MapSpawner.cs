using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;
using UnityEngine.InputSystem;

// 타이틀을 띄우고, 맵(갤러리·전투 스테이지)을 만들고 지우며, 맵을 바꿀 때 화면을 가리고 지난 맵이 남긴 공용 상태를 정리한다.
// 갤러리는 한 번 만들어 두고 숨겼다 다시 보이고, 전투 스테이지는 들어갈 때마다 새로 만든다.
// 일시정지는 맵마다 두지 않고 여기 하나를 두어, 지금 보이는 맵을 대상으로 바꿔 준다.
public class MapSpawner : MonoBehaviour
{
    [SerializeField] private GalleryStage _galleryPrefab;

    [Header("일시정지")]
    [SerializeField] private InputActionReference _pauseAction;
    [SerializeField] private PauseMenuUI _pauseMenuPrefab;

    [Header("타이틀")]
    // 비워 두면 타이틀 없이 갤러리에서 바로 시작한다.
    [SerializeField] private TitleUI _titlePrefab;
    [SerializeField] private AudioClip _titleBgm;
    [SerializeField, Min(0f)] private float _titleBgmFadeDuration = 1f;

    [Header("테스트")]
    // 켜면 갤러리를 건너뛰고 아래 스테이지로 바로 시작한다. 전투만 고칠 때 쓴다.
    [SerializeField] private bool _startInStage;
    [SerializeField] private StageData _testStage;

    private GalleryStage _gallery;
    private BattleStage _battle;
    private StageData _currentStage;
    // 입력이나 메뉴가 지정되지 않았으면 비워 두고 일시정지 없이 진행한다.
    private PauseController _pause;
    // 맵을 바꾸는 동안 들어온 입력(액자 선택·결과 버튼)이 겹쳐 이동을 두 번 시작하지 않게 막는다.
    private bool _isMoving;

    // 공용 매니저(UI·풀·시간 배율)를 쓰므로 GameManager가 그것들을 준비한 뒤 부른다.
    public void Init()
    {
        if (null == _galleryPrefab)
        {
            Debug.LogError("맵 스포너 : 갤러리 프리팹이 지정되지 않았습니다.");
            return;
        }

        ScreenFader fader = GameManager.UI.Cinematic.ScreenFader;
        fader.ShowImmediately();

        if (null != _pauseAction && null != _pauseMenuPrefab)
        {
            _pause = new PauseController(_pauseMenuPrefab, _pauseAction.action);
            _pause.OnLeaveChosen += HandleResultChosen;
        }
        else
        {
            Debug.LogError("맵 스포너 : 일시정지 입력이나 메뉴 프리팹이 지정되지 않아 일시정지 없이 진행합니다.");
        }

        _gallery = Instantiate(_galleryPrefab);
        _gallery.name = _galleryPrefab.name;
        _gallery.OnStageChosen += HandleStageChosen;

        if (_startInStage && null != _testStage)
        {
            HideGallery();
            SpawnStage(_testStage);
            fader.FadeInAsync(destroyCancellationToken).Forget();
        }
        else if (null != _titlePrefab)
        {
            ShowTitleAsync(destroyCancellationToken).Forget();
        }
        else
        {
            ShowGallery();
            fader.FadeInAsync(destroyCancellationToken).Forget();
        }
    }

    private void Update()
    {
        if (null != _pause)
        {
            _pause.Tick();
        }
    }

    // 타이틀은 맵이 아니라 화면이라, 갤러리를 숨겨 둔 채 띄우고 시작을 누르면 갤러리로 넘어간다.
    // 암전이 타이틀을 덮도록 시네마틱 UI(Content)보다 아래인 HUD 레이어에 연다.
    private async UniTaskVoid ShowTitleAsync(CancellationToken token)
    {
        ScreenFader fader = GameManager.UI.Cinematic.ScreenFader;

        HideGallery();

        TitleUI title = GameManager.UI.Open(_titlePrefab, UILayer.HUD);
        GameManager.Sound.PlayBgm(_titleBgm, _titleBgmFadeDuration);

        await fader.FadeInAsync(token);
        await title.WaitForStartAsync(token);
        await fader.FadeOutAsync(token);

        GameManager.UI.Close(title);
        ShowGallery();

        await fader.FadeInAsync(token);
    }

    private void HandleStageChosen(StageData stage)
    {
        if (false == _isMoving)
        {
            EnterStageAsync(stage, destroyCancellationToken).Forget();
        }
    }

    private void HandleResultChosen(ResultChoice choice)
    {
        if (_isMoving)
        {
            return;
        }

        if (ResultChoice.Retry == choice)
        {
            RestartStageAsync(destroyCancellationToken).Forget();
        }
        else
        {
            ReturnToGalleryAsync(destroyCancellationToken).Forget();
        }
    }

    // 보상은 그림을 찍지 못했어도 받고, 결과 화면에서 꺼도 남도록 바로 저장한다.
    // 갤러리로 돌아가면 그림이 액자에 걸린다. 텍스처는 파일로 남겼으니 바로 지운다.
    private void HandleStageCleared(Texture2D painting)
    {
        GameManager.Player.ClaimFirstClear(_currentStage);
        GameManager.SavePlayer();

        if (null == painting)
        {
            return;
        }

        PaintingStorage.Save(_currentStage, painting);
        Destroy(painting);
        _gallery.MarkPaintingChanged(_currentStage);
    }

    private async UniTaskVoid EnterStageAsync(StageData stage, CancellationToken token)
    {
        _isMoving = true;

        await GameManager.UI.Cinematic.ScreenFader.FadeOutAsync(token);

        HideGallery();
        SpawnStage(stage);

        await FinishMoveAsync(token);
    }

    private async UniTaskVoid RestartStageAsync(CancellationToken token)
    {
        _isMoving = true;

        await GameManager.UI.Cinematic.ScreenFader.FadeOutAsync(token);
        await DestroyStageAsync(token);

        SpawnStage(_currentStage);

        await FinishMoveAsync(token);
    }

    private async UniTaskVoid ReturnToGalleryAsync(CancellationToken token)
    {
        _isMoving = true;

        await GameManager.UI.Cinematic.ScreenFader.FadeOutAsync(token);
        await DestroyStageAsync(token);

        ShowGallery();

        await FinishMoveAsync(token);
    }

    private async UniTask FinishMoveAsync(CancellationToken token)
    {
        await GameManager.UI.Cinematic.ScreenFader.FadeInAsync(token);

        _isMoving = false;
    }

    private void SpawnStage(StageData stage)
    {
        if (null == stage || null == stage.StagePrefab)
        {
            Debug.LogError("맵 스포너 : 들어갈 스테이지나 그 전투 스테이지 프리팹이 비어 있습니다.");
            return;
        }

        _currentStage = stage;

        _battle = Instantiate(stage.StagePrefab);
        _battle.name = stage.StagePrefab.name;
        _battle.OnResultChosen += HandleResultChosen;
        _battle.OnStageCleared += HandleStageCleared;
        SetPauseTarget(_battle);
    }

    // Destroy는 프레임 끝에 처리되어, 이전 스테이지가 카메라 경계와 UI를 정리한 뒤에 다음 맵을 꺼낸다.
    // 일시정지 메뉴에서 떠났으면 스테이지가 사라질 때까지 멈춰 두었다가 그 뒤에 푼다.
    private async UniTask DestroyStageAsync(CancellationToken token)
    {
        _battle.OnResultChosen -= HandleResultChosen;
        _battle.OnStageCleared -= HandleStageCleared;
        Destroy(_battle.gameObject);
        _battle = null;

        await UniTask.Yield(PlayerLoopTiming.Update, token);

        SetPauseTarget(null);
        ResetSharedState();
    }

    // 갤러리는 자기 카메라로 그리므로 전투 카메라를 끄고, 전투로 들어갈 때 다시 켠다.
    private void ShowGallery()
    {
        GameManager.CameraRig.SetOutputEnabled(false);
        _gallery.Show();
        SetPauseTarget(_gallery);
    }

    private void HideGallery()
    {
        SetPauseTarget(null);
        _gallery.Hide();
        GameManager.CameraRig.SetOutputEnabled(true);
    }

    private void SetPauseTarget(IPausable target)
    {
        if (null != _pause)
        {
            _pause.SetTarget(target);
        }
    }

    // 지난 맵의 연출이 걸어 둔 히트 스톱·슬로 모션·일시정지, 풀에 남은 파괴된 오브젝트, 내려온 레터박스를 걷어 낸다. 느려지는 구역은 꺼질 때 스스로 빠진다.
    private void ResetSharedState()
    {
        GameManager.TimeScale.ResetToNormal();
        GameManager.Pool.RemoveDestroyed();
        GameManager.UI.Cinematic.Letterbox.HideImmediately();
    }
}
