using UnityEngine;

// 전투를 시작하는 쪽이 플레이어를 만든 직후 부르는 입구. 끼운 장비와 스탯을 각 부품에 필요한 만큼만 나눠 주어,
// 부품들이 GameManager를 직접 읽지 않게 한다. 같은 프레임 안(Start 전)에 불러야 첫 무기 선택과 미리 생성이 장비를 보고 이뤄진다.
[RequireComponent(typeof(PlayerAttack), typeof(PlayerGuard), typeof(PlayerConsumable))]
[RequireComponent(typeof(PlayerInputMover), typeof(BattleUnit))]
public class PlayerSetup : MonoBehaviour
{
    // 스킬 게이지를 막기가 쓰므로, 게이지를 채우는 공격을 먼저 준비한다.
    public void Setup(PlayerModel player, DataTableManager tables, AssetManager assets)
    {
        PlayerLoadout loadout = player.Loadout;
        PlayerStats stats = player.Stats;

        GetComponent<BattleUnit>().SetStats(stats);
        GetComponent<PlayerAttack>().Setup(loadout, stats, tables, assets);
        GetComponent<PlayerGuard>().Setup(loadout.Utility, stats);
        GetComponent<PlayerConsumable>().Setup(loadout.ConsumableId, stats, tables, assets);
        GetComponent<PlayerInputMover>().Setup(stats);
    }
}
