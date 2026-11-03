using UnityEngine;

// 유틸리티 칸에 끼우는 동작(막기·대시)의 공통 부모. 플레이어에는 종류마다 하나씩 붙어 있고, 끼운 종류만 켜진다.
// 끼우지 않은 쪽은 꺼 두어 입력도 받지 않고, IsEquipped가 false라 HUD도 그 칸을 그리지 않는다.
public abstract class PlayerUtility : MonoBehaviour
{
    private Sprite _icon;
    // 이 전투에서 실제로 쓴 횟수. 보스가 이 유틸리티를 자주 쓰는 플레이어에게 반응하는 데 쓴다.
    private int _useCount;

    public abstract UtilityType Type { get; }
    // 끼우지 않았거나 프리셋·입력이 비어 준비하지 못했으면 false다.
    public abstract bool IsEquipped { get; }
    public abstract string KeyLabel { get; }

    public Sprite Icon { get { return _icon; } }
    public int UseCount { get { return _useCount; } }

    public void Setup(UtilityType utility, PlayerStats stats, AssetManager assets)
    {
        if (false == enabled || Type != utility)
        {
            enabled = false;
            return;
        }

        _icon = assets.Get<Sprite>(utility.GetIconKey());
        Equip(stats);
    }

    // 끼운 종류일 때만 Setup이 부른다.
    protected abstract void Equip(PlayerStats stats);

    protected void CountUse()
    {
        _useCount++;
    }
}
