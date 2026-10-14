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

    [Header("접근")]
    [SerializeField] private BossIntroTrigger _introTrigger;

    [Header("보스 등장")]
    [SerializeField] private BossIntroCutscene _introCutscene;

    [Header("전투")]
    [SerializeField] private PaintCanvas _paintCanvas;
    [SerializeField] private BattleHudUI _hudPrefab;

    [Header("엔딩")]
    [SerializeField] private EndingCutscene _endingCutscene;

    [Header("풀")]
    // 첫 공격 때 투사체가 한꺼번에 생성되며 끊기지 않도록 유닛마다 미리 만들어 둘 개수.
    [SerializeField, Min(0)] private int _projectilePrewarmCount = 20;

    private BattleUnit _player;
    private PlayerAutoMover _playerMover;
    private BattleUnit _boss;
    private WanderMover _bossWander;
    private BattleHudUI _hud;

    private void Start()
    {
        if (false == HasRequiredReferences())
        {
            return;
        }

        SpawnUnits();
        PrewarmPools();

        GameManager.CameraRig.FollowPlayer(_player.transform);

        WarnIfAutoHealCannotSave();

        RunAsync(destroyCancellationToken).Forget();
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
            && IsAssigned(GameManager.CameraRig, nameof(GameManager.CameraRig))
            && IsAssigned(GameManager.UI.Cinematic, nameof(GameManager.UI.Cinematic));

        if (false == hasAllReferences)
        {
            return false;
        }

        if (null == _playerPrefab.GetComponent<PlayerAutoMover>())
        {
            Debug.LogError("전투 매니저 : 플레이어 프리팹에 PlayerAutoMover가 붙어 있지 않습니다.");
            return false;
        }

        // 하위 컴포넌트는 빠진 항목을 직접 로그로 남기므로 결과만 확인한다.
        return _introCutscene.HasRequiredReferences()
            && GameManager.CameraRig.HasRequiredReferences()
            && _paintCanvas.HasRequiredReferences()
            && _endingCutscene.HasRequiredReferences();
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
        _playerMover = _player.GetComponent<PlayerAutoMover>();

        _boss = Instantiate(_bossPrefab, _bossSpawnPoint.position, Quaternion.identity, transform);
        _boss.name = _bossPrefab.name;
        _bossWander = _boss.GetComponent<WanderMover>();

        _boss.gameObject.SetActive(false);
    }

    private void PrewarmPools()
    {
        PrewarmUnit(_player);
        PrewarmUnit(_boss);
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

    // 자동 회복은 맞은 직후 기준선 아래인지로 시작하므로, 기준선 체력이 보스의 최대 한 방보다 낮으면 회복 전에 쓰러질 수 있다.
    private void WarnIfAutoHealCannotSave()
    {
        BattleUnitData playerData = _player.Data;
        BattleUnitData bossData = _boss.Data;

        if (null == playerData || null == bossData || null == bossData.AttackStyle || false == playerData.UseAutoHeal)
        {
            return;
        }

        int thresholdHp = Mathf.RoundToInt(playerData.MaxHp * playerData.AutoHealThresholdRatio);
        int bossMaxHit = bossData.AttackStyle.MaxHitDamage;

        if (thresholdHp <= bossMaxHit)
        {
            Debug.LogWarning($"전투 매니저 : 자동 회복 기준 체력({thresholdHp})이 보스의 최대 한 방({bossMaxHit}) 이하라 회복 전에 쓰러질 수 있습니다.");
        }
    }

    private async UniTaskVoid RunAsync(CancellationToken token)
    {
        try
        {
            await PlayApproachAsync(token);
            await PlayIntroAsync(token);
            bool isBossDefeated = await PlayBattleAsync(token);

            if (isBossDefeated)
            {
                await PlayEndingAsync(token);
            }
        }
        catch (OperationCanceledException)
        {
            // 씬을 벗어나 연출이 중간에 끊긴 경우라 따로 처리하지 않는다.
        }
    }

    private async UniTask PlayApproachAsync(CancellationToken token)
    {
        _playerMover.StartMoving();

        await _introTrigger.WaitForEnterAsync(token);

        _playerMover.Stop();
    }

    private async UniTask PlayIntroAsync(CancellationToken token)
    {
        GameManager.CameraRig.PrepareBattleFraming(_player, _boss);

        await _introCutscene.PlayAsync(_boss, _bossSpawnPoint, token);
    }

    private async UniTask<bool> PlayBattleAsync(CancellationToken token)
    {
        // 전투 HUD는 전투에서만 쓰므로 전투 매니저가 직접 열고, 엔딩이 시작되면 닫는다.
        _hud = GameManager.UI.Open(_hudPrefab, UILayer.HUD);
        _hud.Bind(_player, _boss);
        _hud.Show();

        // 보스가 쓰러지면 전투 토큰을 취소해, 남은 공격 루프와 날아가던 투사체를 함께 정리한다.
        using (CancellationTokenSource battleSource = CancellationTokenSource.CreateLinkedTokenSource(token))
        {
            _player.Attacker.RunAsync(_boss, _paintCanvas, battleSource.Token).Forget();
            _boss.Attacker.RunAsync(_player, _paintCanvas, battleSource.Token).Forget();

            if (null != _bossWander)
            {
                _bossWander.RunAsync(_bossSpawnPoint.position, battleSource.Token).Forget();
            }

            // WhenAny는 먼저 끝난 작업의 순서 번호를 돌려준다. 0은 보스, 1은 플레이어가 먼저 쓰러진 것이다.
            int defeatedIndex = await UniTask.WhenAny(
                _boss.Health.WaitForDeathAsync(token),
                _player.Health.WaitForDeathAsync(token));

            battleSource.Cancel();

            if (1 == defeatedIndex)
            {
                Debug.LogWarning("전투 매니저 : 플레이어가 먼저 쓰러졌습니다. PlayerData의 자동 회복 설정을 확인해 주세요.");
                return false;
            }

            return true;
        }
    }

    private async UniTask PlayEndingAsync(CancellationToken token)
    {
        await UniTask.WhenAll(
            CloseHudAsync(token),
            _endingCutscene.PlayAsync(_boss, _paintCanvas, token));
    }

    private async UniTask CloseHudAsync(CancellationToken token)
    {
        await _hud.HideAsync(token);

        GameManager.UI.Close(_hud);
        _hud = null;
    }

    // 배회 범위를 데이터로 바꿀 때 화면 안에 들어오는지 씬 뷰에서 바로 확인하기 위해 그린다.
    private void OnDrawGizmosSelected()
    {
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
