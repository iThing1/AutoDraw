using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

public class BattleManager : MonoBehaviour
{
    [Header("스폰 위치")]
    [SerializeField] private Transform _playerSpawnPoint;
    [SerializeField] private Transform _bossSpawnPoint;

    [Header("유닛 프리팹")]
    [SerializeField] private BattleUnit _playerPrefab;
    [SerializeField] private BattleUnit _bossPrefab;

    [Header("스테이지 영역")]
    // 바닥 종이 영역. 비워 두면 카메라와 유닛 이동을 제한하지 않는다.
    [SerializeField] private StageArea _stageArea;

    [Header("접근")]
    [SerializeField] private BossIntroTrigger _introTrigger;

    [Header("보스 등장")]
    [SerializeField] private BossIntroCutscene _introCutscene;

    [Header("전투")]
    [SerializeField] private PaintCanvas _paintCanvas;
    [SerializeField] private BattleHudView _hudPrefab;

    [Header("환경 패턴")]
    // 비워 두면 환경 탄막 없이 보스 공격만 나온다.
    [SerializeField] private EnvironmentHazard _environmentHazard;

    [Header("결과")]
    [SerializeField] private EndingCutscene _endingCutscene;
    [SerializeField] private DefeatCutscene _defeatCutscene;
    [SerializeField] private ResultUI _resultPrefab;

    [Header("사운드")]
    // 보스가 나올 때 틀고, 승패가 나면 줄여 끈다.
    [SerializeField] private AudioClip _bgm;
    [SerializeField, Min(0f)] private float _bgmFadeDuration = 1f;

    [Header("풀")]
    // 첫 공격 때 투사체가 한꺼번에 생성되며 끊기지 않도록 유닛마다 미리 만들어 둘 개수.
    [SerializeField, Min(0)] private int _projectilePrewarmCount = 20;

    private BattleUnit _player;
    private BattleUnit _boss;
    private BattleHudView _hud;
    private BattleHudViewModel _hudViewModel;
    private ResultUI _result;

    // 결과 화면에서 고른 것. 다시 하기·갤러리 이동은 스테이지를 만든 쪽이 처리한다.
    public event Action<ResultChoice> OnResultChosen;
    // 결과 화면보다 먼저 알려, 버튼을 누르기 전에 게임을 꺼도 그림이 남게 한다. 텍스처는 받는 쪽이 지운다.
    public event Action<Texture2D> OnStageCleared;

    private void Start()
    {
        if (false == HasRequiredReferences())
        {
            return;
        }

        SpawnUnits();
        PrewarmPools();

        if (null != _stageArea)
        {
            GameManager.CameraRig.SetCameraBounds(_stageArea.Collider);
        }

        GameManager.CameraRig.FollowPlayer(_player.transform);

        RunAsync(destroyCancellationToken).Forget();
    }

    // 카메라 리그와 UI는 스테이지보다 오래 남아, 이 스테이지가 쓰던 경계와 화면을 정리한다.
    // 게임을 끌 때는 게임 매니저가 먼저 사라질 수 있다.
    private void OnDestroy()
    {
        if (null == GameManager.Instance)
        {
            return;
        }

        if (null != _stageArea && null != GameManager.CameraRig)
        {
            GameManager.CameraRig.ClearCameraBounds();
        }

        ReleaseHud();
        GameManager.UI.Close(_result);
    }

    private bool HasRequiredReferences()
    {
        // 앞에서부터 확인해 처음 비어 있는 참조에서 멈춘다.
        // nameof는 컴파일할 때 필드 이름을 문자열로 바꿔 줘서, 필드 이름을 바꿔도 로그가 함께 바뀐다.
        bool hasAllReferences = IsAssigned(_playerSpawnPoint, nameof(_playerSpawnPoint))
            && IsAssigned(_bossSpawnPoint, nameof(_bossSpawnPoint))
            && IsAssigned(_playerPrefab, nameof(_playerPrefab))
            && IsAssigned(_bossPrefab, nameof(_bossPrefab))
            && IsAssigned(_introTrigger, nameof(_introTrigger))
            && IsAssigned(_introCutscene, nameof(_introCutscene))
            && IsAssigned(_paintCanvas, nameof(_paintCanvas))
            && IsAssigned(_hudPrefab, nameof(_hudPrefab))
            && IsAssigned(_endingCutscene, nameof(_endingCutscene))
            && IsAssigned(_defeatCutscene, nameof(_defeatCutscene))
            && IsAssigned(_resultPrefab, nameof(_resultPrefab))
            && IsAssigned(GameManager.CameraRig, nameof(GameManager.CameraRig))
            && IsAssigned(GameManager.UI.Cinematic, nameof(GameManager.UI.Cinematic));

        if (false == hasAllReferences)
        {
            return false;
        }

        // 플레이어가 움직여야 등장 트리거에 닿아 다음 단계로 넘어간다.
        if (null == _playerPrefab.GetComponent<UnitMover>())
        {
            Debug.LogError("전투 매니저 : 플레이어 프리팹에 이동 컴포넌트(UnitMover)가 붙어 있지 않습니다.");
            return false;
        }

        // 하위 컴포넌트는 빠진 항목을 직접 로그로 남기므로 결과만 확인한다.
        return _introCutscene.HasRequiredReferences()
            && GameManager.CameraRig.HasRequiredReferences()
            && _paintCanvas.HasRequiredReferences()
            && _endingCutscene.HasRequiredReferences()
            && _defeatCutscene.HasRequiredReferences();
    }

    // System.Object와 이름이 겹쳐 UnityEngine.Object로 적는다. Unity 오브젝트 타입으로 받아야 파괴된 오브젝트도 null로 본다.
    private bool IsAssigned(UnityEngine.Object target, string label)
    {
        if (null != target)
        {
            return true;
        }

        Debug.LogError($"전투 매니저 : {label} 참조가 비어 있습니다.");
        return false;
    }

    private void SpawnUnits()
    {
        _player = Instantiate(_playerPrefab, _playerSpawnPoint.position, Quaternion.identity, transform);
        _player.name = _playerPrefab.name;

        _boss = Instantiate(_bossPrefab, _bossSpawnPoint.position, Quaternion.identity, transform);
        _boss.name = _bossPrefab.name;

        // 카메라가 종이 안만 비추므로, 보스도 종이 밖으로 나가면 화면에서 잘린다.
        if (null != _stageArea)
        {
            _player.Mover.SetMoveArea(_stageArea.GetMoveArea(_player));

            if (null != _boss.Mover)
            {
                _boss.Mover.SetMoveArea(_stageArea.GetMoveArea(_boss));
            }
        }

        _boss.gameObject.SetActive(false);
    }

    private void PrewarmPools()
    {
        PrewarmUnit(_player);
        PrewarmUnit(_boss);

        if (null != _environmentHazard && null != _boss.Data)
        {
            _environmentHazard.Prewarm(_boss.Data.ProjectilePrefab);
        }

        _defeatCutscene.Prewarm();
    }

    private void PrewarmUnit(BattleUnit unit)
    {
        BattleUnitData data = unit.Data;

        if (null == data)
        {
            return;
        }

        if (null != data.ProjectilePrefab)
        {
            GameManager.Pool.Prewarm(data.ProjectilePrefab, _projectilePrewarmCount);
        }

        if (null != data.AttackStyle)
        {
            data.AttackStyle.Prewarm();
        }
    }

    private async UniTaskVoid RunAsync(CancellationToken token)
    {
        try
        {
            await PlayApproachAsync(token);
            await PlayIntroAsync(token);
            bool isBossDefeated = await PlayBattleAsync(token);
            GameManager.Sound.StopBgm(_bgmFadeDuration);
            ResultTitleSettings resultTitle;

            if (isBossDefeated)
            {
                Texture2D painting = await PlayEndingAsync(token);
                OnStageCleared?.Invoke(painting);
                resultTitle = _endingCutscene.ResultTitle;
            }
            else
            {
                await PlayDefeatAsync(token);
                resultTitle = _defeatCutscene.ResultTitle;
            }

            _result = GameManager.UI.Open(_resultPrefab, UILayer.Overlay);
            ResultChoice choice = await _result.PlayAsync(resultTitle, token);

            OnResultChosen?.Invoke(choice);
        }
        catch (OperationCanceledException)
        {
            // 씬을 벗어나 연출이 중간에 끊긴 경우라 따로 처리하지 않는다.
        }
    }

    // 플레이어 이동은 여기서 한 번 시작해 스테이지가 끝날 때까지 돌고, 컷신 동안에는 상태(Idle)로 멈춘다.
    private async UniTask PlayApproachAsync(CancellationToken token)
    {
        _player.ChangeState(UnitState.Move);
        _player.Mover.RunAsync(token).Forget();

        await _introTrigger.WaitForEnterAsync(token);

        _player.ChangeState(UnitState.Idle);
    }

    private async UniTask PlayIntroAsync(CancellationToken token)
    {
        GameManager.Sound.PlayBgm(_bgm, _bgmFadeDuration);
        GameManager.CameraRig.PrepareBattleFraming(_player, _boss);

        await _introCutscene.PlayAsync(_boss, _bossSpawnPoint, token);
    }

    private async UniTask<bool> PlayBattleAsync(CancellationToken token)
    {
        // 전투 HUD는 전투에서만 쓰므로 전투 매니저가 직접 열고, 엔딩이 시작되면 닫는다.
        _hud = GameManager.UI.Open(_hudPrefab, UILayer.HUD);
        _hudViewModel = new BattleHudViewModel(_player, _boss);
        _hud.Bind(_hudViewModel);
        _hud.Show();

        // 보스가 쓰러지면 전투 토큰을 취소해, 남은 공격 루프와 날아가던 투사체를 함께 정리한다.
        using (CancellationTokenSource battleSource = CancellationTokenSource.CreateLinkedTokenSource(token))
        {
            StartUnit(_player, _boss, battleSource.Token);
            StartUnit(_boss, _player, battleSource.Token);

            if (null != _boss.Mover)
            {
                _boss.Mover.RunAsync(battleSource.Token).Forget();
            }

            // 환경 탄막은 보스 자리에서 나오지만 보스 공격 순서와 따로 돈다.
            if (null != _environmentHazard)
            {
                _environmentHazard.RunAsync(_boss.Attacker, battleSource.Token).Forget();
            }

            // WhenAny는 먼저 끝난 작업의 순서 번호를 돌려준다. 0은 보스, 1은 플레이어가 먼저 쓰러진 것이다.
            int defeatedIndex = await UniTask.WhenAny(
                _boss.Health.WaitForDeathAsync(token),
                _player.Health.WaitForDeathAsync(token));

            battleSource.Cancel();

            // 쓰러진 쪽은 Dead라 그대로 남는다.
            _player.ChangeState(UnitState.Idle);
            _boss.ChangeState(UnitState.Idle);

            return 0 == defeatedIndex;
        }
    }

    private void StartUnit(BattleUnit unit, BattleUnit target, CancellationToken token)
    {
        unit.ChangeState(UnitState.Move);
        unit.Attacker.RunAsync(target, _paintCanvas, token).Forget();
    }

    private void DrawMoveAreaGizmo(BattleUnit unitPrefab, Color color)
    {
        if (null == unitPrefab)
        {
            return;
        }

        Rect area = _stageArea.GetMoveArea(unitPrefab);

        Gizmos.color = color;
        Gizmos.DrawWireCube(area.center, new Vector3(area.width, area.height, 0f));
    }

    private async UniTask<Texture2D> PlayEndingAsync(CancellationToken token)
    {
        UniTask closeHud = CloseHudAsync(token);
        Texture2D painting = await _endingCutscene.PlayAsync(_boss, _paintCanvas, token);

        await closeHud;

        return painting;
    }

    private async UniTask PlayDefeatAsync(CancellationToken token)
    {
        await UniTask.WhenAll(
            CloseHudAsync(token),
            _defeatCutscene.PlayAsync(_player, _paintCanvas, token));
    }

    private async UniTask CloseHudAsync(CancellationToken token)
    {
        await _hud.HideAsync(token);

        ReleaseHud();
    }

    // View가 먼저 ViewModel 구독을 풀고, 그다음 ViewModel이 게임 컴포넌트에 건 구독을 푼다. ViewModel은 만든 쪽인 여기서 푼다.
    private void ReleaseHud()
    {
        if (null != _hud)
        {
            _hud.UnBind();
            GameManager.UI.Close(_hud);
            _hud = null;
        }

        if (null != _hudViewModel)
        {
            _hudViewModel.UnBind();
            _hudViewModel = null;
        }
    }

    // 플레이어는 자동 회복 때문에 거의 쓰러지지 않아, 플레이 중 컴포넌트 우클릭 메뉴로 결과 흐름을 바로 확인한다.
    [ContextMenu("테스트/플레이어 처치")]
    private void KillPlayerForTest()
    {
        KillForTest(_player);
    }

    [ContextMenu("테스트/보스 처치")]
    private void KillBossForTest()
    {
        KillForTest(_boss);
    }

    private void KillForTest(BattleUnit unit)
    {
        if (null == unit || unit.Health.IsDead)
        {
            return;
        }

        unit.Health.TakeDamage(new DamageInfo { Amount = unit.Health.CurrentHp, HitPoint = unit.Body.position });
    }

    // 여백을 바꿀 때 유닛별 이동 범위를 씬 뷰에서 바로 확인하기 위해 그린다. 하늘색은 플레이어, 빨간색은 보스다.
    private void OnDrawGizmosSelected()
    {
        if (null != _stageArea)
        {
            DrawMoveAreaGizmo(_playerPrefab, Color.cyan);
            DrawMoveAreaGizmo(_bossPrefab, Color.red);
        }

        if (null == _bossSpawnPoint || null == _bossPrefab || null == _bossPrefab.Data)
        {
            return;
        }

        WanderSettings wander = _bossPrefab.Data.Wander;

        if (false == wander.UseWander)
        {
            return;
        }

        Vector2 halfSize = wander.AreaHalfSize;

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireCube(_bossSpawnPoint.position, new Vector3(halfSize.x * 2f, halfSize.y * 2f, 0f));
    }
}
