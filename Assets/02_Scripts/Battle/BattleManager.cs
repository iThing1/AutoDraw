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

        GameManager.CameraRig.FollowPlayer(_player.transform);

        WarnIfAutoHealCannotSave();

        RunAsync(destroyCancellationToken).Forget();
    }

    private bool HasRequiredReferences()
    {
        if (null == _playerSpawnPoint || null == _bossSpawnPoint)
        {
            Debug.LogError("전투 매니저 : 스폰 포인트가 지정되지 않았습니다.");
            return false;
        }

        if (null == _playerPrefab || null == _playerPrefab.GetComponent<PlayerAutoMover>())
        {
            Debug.LogError("전투 매니저 : 플레이어 프리팹이 없거나 PlayerAutoMover가 붙어 있지 않습니다.");
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

        if (null == _introCutscene)
        {
            Debug.LogError("전투 매니저 : BossIntroCutscene이 지정되지 않았습니다.");
            return false;
        }

        if (null == GameManager.CameraRig)
        {
            Debug.LogError("전투 매니저 : GameManager에 카메라 리그가 지정되지 않았습니다.");
            return false;
        }

        if (null == _paintCanvas)
        {
            Debug.LogError("전투 매니저 : PaintCanvas가 지정되지 않았습니다.");
            return false;
        }

        if (null == _hudPrefab)
        {
            Debug.LogError("전투 매니저 : 전투 HUD 프리팹이 지정되지 않았습니다.");
            return false;
        }

        if (null == _endingCutscene)
        {
            Debug.LogError("전투 매니저 : EndingCutscene이 지정되지 않았습니다.");
            return false;
        }

        // 하위 컴포넌트는 빠진 항목을 직접 로그로 남기므로 결과만 확인한다.
        if (false == _introCutscene.HasRequiredReferences() || false == GameManager.CameraRig.HasRequiredReferences()
            || false == _paintCanvas.HasRequiredReferences() || false == _endingCutscene.HasRequiredReferences())
        {
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
        _playerMover = _player.GetComponent<PlayerAutoMover>();

        _boss = Instantiate(_bossPrefab, _bossSpawnPoint.position, Quaternion.identity, transform);
        _boss.name = _bossPrefab.name;
        _bossWander = _boss.GetComponent<WanderMover>();

        _boss.gameObject.SetActive(false);
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

        ShotSettings bossShot = bossData.AttackStyle.Shot;

        int thresholdHp = Mathf.RoundToInt(playerData.MaxHp * playerData.AutoHealThresholdRatio);
        int bossMaxHit = Mathf.RoundToInt(bossShot.Damage.Max * bossShot.CriticalMultiplier);

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
        SetPhase(BattlePhase.Approach);

        _playerMover.StartMoving();

        await _introTrigger.WaitForEnterAsync(token);

        _playerMover.Stop();
    }

    private async UniTask PlayIntroAsync(CancellationToken token)
    {
        SetPhase(BattlePhase.Intro);

        GameManager.CameraRig.PrepareBattleFraming(_player, _boss);

        await _introCutscene.PlayAsync(_boss, _bossSpawnPoint, token);
    }

    private async UniTask<bool> PlayBattleAsync(CancellationToken token)
    {
        SetPhase(BattlePhase.Battle);

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
        SetPhase(BattlePhase.Ending);

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

    private void SetPhase(BattlePhase phase)
    {
        Debug.Log($"전투 단계 : {phase}");
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
