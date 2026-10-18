// 체력이 어떻게 바뀌었는지. 체력바가 바뀐 방식마다 다른 연출(잔상, 바로 채움, 처음부터 차오름)을 고른다.
public enum HealthBarChange
{
    Damaged,
    Healed,
    Refilled,
}
