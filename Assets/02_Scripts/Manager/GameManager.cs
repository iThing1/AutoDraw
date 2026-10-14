using UnityEngine;

public class GameManager : SingletonBehaviour<GameManager>
{
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

        _soundManager.Init(gameObject);
        _poolManager.Init();
        _timeScaleManager.Init(destroyCancellationToken);

        // 스테이지가 생성되자마자 등장 컷신이 시네마틱 UI를 쓰므로 UI를 먼저 만든다.
        _uiManager.Init(_cinematicUIPrefab);

        SpawnBattleStage();
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
    }
}
