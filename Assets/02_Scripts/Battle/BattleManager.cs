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
    // M3에서 보스 전용 컴포넌트가 생기면 그 타입으로 바꾼다.
    [SerializeField] private GameObject _bossPrefab;

    [Header("Approach")]
    [SerializeField] private BossIntroTrigger _introTrigger;

    public event Action<BattlePhase> OnPhaseChanged;

    private PlayerAutoMover _player;
    private GameObject _boss;

    private BattlePhase _phase = BattlePhase.None;

    public PlayerAutoMover Player { get { return _player; } }
    public GameObject Boss { get { return _boss; } }
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

        if (null == GameManager.CameraRig)
        {
            Debug.LogError("전투 매니저 : GameManager에 카메라 리그가 지정되지 않았습니다.");
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

    // M2에서 보스 등장 컷신으로 채운다.
    private async UniTask PlayIntroAsync(CancellationToken token)
    {
        SetPhase(BattlePhase.Intro);

        await UniTask.Yield(token);
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
