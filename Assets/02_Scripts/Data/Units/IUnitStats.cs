// 보스와 플레이어가 함께 쓰는 전투 스탯. 보스는 SO(BossData)에, 플레이어는 데이터 표(PlayerStatData)에 값이 있다.
public interface IUnitStats
{
    int MaxHp { get; }
    // 떨어진 자리가 몸 중심에서 이 거리 안이면 맞는다.
    float HitRadius { get; }
}
