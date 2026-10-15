using Cysharp.Threading.Tasks;
using DG.Tweening;
using System.Threading;
using UnityEngine;

public class GameManager : SingletonBehaviour<GameManager>
{
    // 환경 탄막은 탄 수십 발이 각자 트윈으로 날고, 빗나가면 이어 날아가는 트윈(시퀀스)이 더 붙는다.
    // 기본 용량(트윈 200, 시퀀스 50)을 넘기면 실행 중에 늘리며 끊길 수 있어 처음부터 넉넉히 잡는다.
    private const int TWEENERS_CAPACITY = 800;
    private const int SEQUENCES_CAPACITY = 200;

    public static BattleManager Battle { get { return Instance._battle; } }
    public static BattleCameraRig CameraRig { get { return Instance._cameraRig; } }
    public static UIManager UI { get { return Instance._uiManager; } }
    public static SoundManager Sound { get { return Instance._soundManager; } }
    public static PoolManager Pool { get { return Instance._poolManager; } }
    public static TimeScaleManager TimeScale { get { return Instance._timeScaleManager; } }

    [Header("씬")]
    [SerializeField] private BattleCameraRig _cameraRig;

    [Header("프리팹")]
    [SerializeField] private CinematicUI _cinematicUIPrefab;
    [SerializeField] private BattleManager _battleStagePrefab;

    private SoundManager _soundManager = new();
    private UIManager _uiManager = new();
    private PoolManager _poolManager = new();
    private TimeScaleManager _timeScaleManager = new();

    private BattleManager _battle;

    protected override void Init()
    {
        base.Init();

        // 중복으로 생성된 GameManager는 base.Init에서 파괴되므로 이후 초기화를 건너뛴다.
        if (this != Instance)
        {
            return;
        }

        DOTween.SetTweensCapacity(TWEENERS_CAPACITY, SEQUENCES_CAPACITY);

        _soundManager.Init(gameObject);
        _poolManager.Init();
        _timeScaleManager.Init(destroyCancellationToken);

        // 스테이지가 생성되자마자 등장 컷신이 시네마틱 UI를 쓰므로 UI를 먼저 만든다.
        _uiManager.Init(_cinematicUIPrefab);

        ScreenFader fader = _uiManager.Cinematic.ScreenFader;
        fader.ShowImmediately();

        SpawnBattleStage();

        fader.FadeInAsync(destroyCancellationToken).Forget();
    }

    private void SpawnBattleStage()
    {
        if (null == _battleStagePrefab)
        {
            Debug.LogError("게임 매니저 : BattleStage 프리팹이 지정되지 않았습니다.");
            return;
        }

        _battle = Instantiate(_battleStagePrefab);
        _battle.name = _battleStagePrefab.name;
        _battle.OnResultChosen += HandleResultChosen;
    }

    // 갤러리는 아직 없어 버튼을 꺼 두었다.
    private void HandleResultChosen(ResultChoice choice)
    {
        if (ResultChoice.Retry == choice)
        {
            RestartStageAsync(destroyCancellationToken).Forget();
        }
    }

    // 스테이지를 통째로 새로 만들어 지난 판의 상태가 남지 않게 한다.
    private async UniTaskVoid RestartStageAsync(CancellationToken token)
    {
        ScreenFader fader = _uiManager.Cinematic.ScreenFader;

        await fader.FadeOutAsync(token);

        _battle.OnResultChosen -= HandleResultChosen;
        Destroy(_battle.gameObject);
        _battle = null;

        // Destroy는 프레임 끝에 처리되어, 이전 스테이지가 정리된 뒤에 새로 만든다.
        await UniTask.Yield(PlayerLoopTiming.Update, token);

        _timeScaleManager.ResetToNormal();
        _poolManager.RemoveDestroyed();
        _uiManager.Cinematic.Letterbox.HideImmediately();

        SpawnBattleStage();

        await fader.FadeInAsync(token);
    }
}
