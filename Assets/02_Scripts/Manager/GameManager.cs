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
    private const string PLAYER_STAT_ID = "player";

    public static BattleCameraRig CameraRig { get { return Instance._cameraRig; } }
    public static UIManager UI { get { return Instance._uiManager; } }
    public static SoundManager Sound { get { return Instance._soundManager; } }
    public static PoolManager Pool { get { return Instance._poolManager; } }
    public static TimeScaleManager TimeScale { get { return Instance._timeScaleManager; } }
    public static DataTableManager Tables { get { return Instance._dataTableManager; } }
    public static AssetManager Assets { get { return Instance._assetManager; } }
    // 표를 읽은 뒤에 만들어지고, 그 전에는 null이다.
    public static PlayerModel Player { get { return Instance._player; } }

    [Header("씬")]
    [SerializeField] private BattleCameraRig _cameraRig;
    [SerializeField] private MapSpawner _mapSpawner;

    [Header("프리팹")]
    [SerializeField] private CinematicUI _cinematicUIPrefab;

    [Header("데이터")]
    [SerializeField] private AssetRegistry _assetRegistry;

    [Header("사운드")]
    // 효과음(1)보다 낮게 깔아 타격음·경고음이 묻히지 않게 한다.
    [SerializeField, Range(0f, 1f)] private float _bgmVolume = 0.5f;

    private SoundManager _soundManager = new();
    private UIManager _uiManager = new();
    private PoolManager _poolManager = new();
    private TimeScaleManager _timeScaleManager = new();
    private DataTableManager _dataTableManager = new();
    private AssetManager _assetManager = new();
    private PlayerModel _player;

    protected override void Init()
    {
        base.Init();

        // 중복으로 생성된 GameManager는 base.Init에서 파괴되므로 이후 초기화를 건너뛴다.
        if (this != Instance)
        {
            return;
        }

        DOTween.SetTweensCapacity(TWEENERS_CAPACITY, SEQUENCES_CAPACITY);

        _soundManager.Init(gameObject, _bgmVolume);
        _poolManager.Init();
        _timeScaleManager.Init(destroyCancellationToken);

        // 스테이지가 생성되자마자 등장 컷신이 시네마틱 UI를 쓰므로 UI를 먼저 만든다.
        _uiManager.Init(_cinematicUIPrefab);

        if (null == _mapSpawner)
        {
            Debug.LogError("게임 매니저 : MapSpawner가 지정되지 않았습니다.");
            return;
        }

        InitAsync(destroyCancellationToken).Forget();
    }

    // 맵이 만들어지자마자 스탯을 읽으므로 플레이어를 만든 뒤에 맵을 만든다.
    private async UniTaskVoid InitAsync(CancellationToken token)
    {
        // 표를 읽는 동안 빈 씬이 비치지 않게 먼저 가린다.
        _uiManager.Cinematic.ScreenFader.ShowImmediately();

        await UniTask.WhenAll(
            _dataTableManager.LoadAsync(new ResourcesTableLoader(), token),
            _assetManager.PreloadAsync(_assetRegistry, token));

        if (false == CreatePlayer())
        {
            return;
        }

        _mapSpawner.Init();
    }

    private bool CreatePlayer()
    {
        PlayerStatData baseStats = _dataTableManager.PlayerStats.Get(PLAYER_STAT_ID);

        if (null == baseStats)
        {
            Debug.LogError($"게임 매니저 : 플레이어 기본 스탯({PLAYER_STAT_ID})이 없어 게임을 시작하지 않습니다.");
            return false;
        }

        _player = PlayerSaveStorage.TryLoad(out PlayerSaveData save)
            ? PlayerModel.FromSave(save, baseStats, _dataTableManager)
            : PlayerModel.CreateNew(baseStats, _dataTableManager);

        return true;
    }
}
