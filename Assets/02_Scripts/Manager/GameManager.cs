using DG.Tweening;
using UnityEngine;

public class GameManager : SingletonBehaviour<GameManager>
{
    // 환경 탄막은 탄 수십 발이 각자 트윈으로 날고, 빗나가면 이어 날아가는 트윈(시퀀스)이 더 붙는다.
    // 기본 용량(트윈 200, 시퀀스 50)을 넘기면 실행 중에 늘리며 끊길 수 있어 처음부터 넉넉히 잡는다.
    private const int TWEENERS_CAPACITY = 800;
    private const int SEQUENCES_CAPACITY = 200;

    public static BattleCameraRig CameraRig { get { return Instance._cameraRig; } }
    public static MapSpawner Map { get { return Instance._mapSpawner; } }
    public static UIManager UI { get { return Instance._uiManager; } }
    public static SoundManager Sound { get { return Instance._soundManager; } }
    public static PoolManager Pool { get { return Instance._poolManager; } }
    public static TimeScaleManager TimeScale { get { return Instance._timeScaleManager; } }

    [Header("씬")]
    [SerializeField] private BattleCameraRig _cameraRig;
    [SerializeField] private MapSpawner _mapSpawner;

    [Header("프리팹")]
    [SerializeField] private CinematicUI _cinematicUIPrefab;

    private SoundManager _soundManager = new();
    private UIManager _uiManager = new();
    private PoolManager _poolManager = new();
    private TimeScaleManager _timeScaleManager = new();

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

        if (null == _mapSpawner)
        {
            Debug.LogError("게임 매니저 : MapSpawner가 지정되지 않았습니다.");
            return;
        }

        _mapSpawner.Init();
    }
}
