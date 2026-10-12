using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

public class BattleManager : MonoBehaviour
{
    [Header("Spawn Point")]
    [SerializeField] private Transform _playerSpawnPoint;
    [SerializeField] private Transform _bossSpawnPoint;

    [Header("Unit Prefab")]
    [SerializeField] private PlayerAutoMover _playerPrefab;
    [SerializeField] private BossUnit _bossPrefab;

    [Header("Approach")]
    [SerializeField] private BossIntroTrigger _introTrigger;

    [Header("Intro")]
    [SerializeField] private BossIntroCutscene _introCutscene;

    public event Action<BattlePhase> OnPhaseChanged;

    private PlayerAutoMover _player;
    private BossUnit _boss;

    private BattlePhase _phase = BattlePhase.None;

    public PlayerAutoMover Player { get { return _player; } }
    public BossUnit Boss { get { return _boss; } }
    public BattlePhase Phase { get { return _phase; } }

    private void Start()
    {
        if (false == HasRequiredReferences())
        {
            return;
        }

        SpawnUnits();

        GameManager.CameraRig.FollowPlayer(_player.transform);

        RunAsync(destroyCancellationToken).Forget();
    }

    private bool HasRequiredReferences()
    {
        if (null == _playerSpawnPoint || null == _bossSpawnPoint)
        {
            Debug.LogError("전투 매니저 : 스폰 포인트가 지정되지 않았습니다.");
            return false;
        }

        if (null == _playerPrefab)
        {
            Debug.LogError("전투 매니저 : 플레이어 프리팹이 지정되지 않았습니다.");
            return false;
        }

        if (null == _bossPrefab)
        {
            Debug.LogError("전투 매니저 : 보스 프리팹이 지정되지 않았습니다.");
            return false;
        }

        if (null == _introTrigger)
        {
            Debug.LogError("전투 매니저 : BossIntroTrigger가 지정되지 않았습니다.");
            return false;
        }

        if (null == _introCutscene || false == _introCutscene.HasRequiredReferences())
        {
            Debug.LogError("전투 매니저 : BossIntroCutscene 설정을 확인해 주세요.");
            return false;
        }

        if (null == GameManager.CameraRig || false == GameManager.CameraRig.HasRequiredReferences())
        {
            Debug.LogError("전투 매니저 : GameManager의 카메라 리그 설정을 확인해 주세요.");
            return false;
        }

        if (null == GameManager.UI.Cinematic)
        {
            Debug.LogError("전투 매니저 : 시네마틱 UI가 생성되지 않았습니다.");
            return false;
        }

        return true;
    }

    private void SpawnUnits()
    {
        _player = Instantiate(_playerPrefab, _playerSpawnPoint.position, Quaternion.identity, transform);
        _player.name = _playerPrefab.name;

        _boss = Instantiate(_bossPrefab, _bossSpawnPoint.position, Quaternion.identity, transform);
        _boss.name = _bossPrefab.name;

        _boss.gameObject.SetActive(false);
    }

    private async UniTaskVoid RunAsync(CancellationToken token)
    {
        try
        {
            await PlayApproachAsync(token);
            await PlayIntroAsync(token);
            await PlayBattleAsync(token);
            await PlayEndingAsync(token);
        }
        catch (OperationCanceledException)
        {
            // 씬을 벗어나 연출이 중간에 끊긴 경우라 따로 처리하지 않는다.
        }
    }

    private async UniTask PlayApproachAsync(CancellationToken token)
    {
        SetPhase(BattlePhase.Approach);

        _player.StartMoving();

        await _introTrigger.WaitForEnterAsync(token);

        _player.Stop();
    }

    private async UniTask PlayIntroAsync(CancellationToken token)
    {
        SetPhase(BattlePhase.Intro);

        await _introCutscene.PlayAsync(_boss, _bossSpawnPoint, token);
    }

    // M3에서 자동 전투로 채운다.
    private async UniTask PlayBattleAsync(CancellationToken token)
    {
        SetPhase(BattlePhase.Battle);

        await UniTask.Yield(token);
    }

    // M4에서 처치와 엔딩으로 채운다.
    private async UniTask PlayEndingAsync(CancellationToken token)
    {
        SetPhase(BattlePhase.Ending);

        await UniTask.Yield(token);
    }

    private void SetPhase(BattlePhase phase)
    {
        _phase = phase;

        Debug.Log($"전투 단계 : {phase}");

        OnPhaseChanged?.Invoke(phase);
    }
}
