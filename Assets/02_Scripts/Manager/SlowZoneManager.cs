using System.Collections.Generic;
using UnityEngine;

// 지금 켜져 있는 느려지는 구역(시간 왜곡, 먹물 웅덩이)을 모아 두고, 어떤 자리가 얼마나 느려지는지 알려 준다.
// 게임 전체 시간(TimeScaleManager)과 달리 구역 안에 있는 것만 느려진다. 보스는 묻지 않아 영향을 받지 않는다.
public class SlowZoneManager
{
    private readonly List<SlowZone> _zones = new();

    public void Register(SlowZone zone)
    {
        if (false == _zones.Contains(zone))
        {
            _zones.Add(zone);
        }
    }

    public void Unregister(SlowZone zone)
    {
        _zones.Remove(zone);
    }

    // 스테이지를 바꿀 때 남은 구역이 다음 맵에 영향을 주지 않게 비운다.
    public void Clear()
    {
        _zones.Clear();
    }

    // 여러 구역이 겹치면 가장 느린 쪽을 따른다.
    public float GetMoveScale(Vector3 position)
    {
        float scale = 1f;

        for (int i = 0; i < _zones.Count; i++)
        {
            if (_zones[i].Contains(position))
            {
                scale = Mathf.Min(scale, _zones[i].MoveScale);
            }
        }

        return scale;
    }

    public float GetProjectileScale(Vector3 position)
    {
        float scale = 1f;

        for (int i = 0; i < _zones.Count; i++)
        {
            if (_zones[i].Contains(position))
            {
                scale = Mathf.Min(scale, _zones[i].ProjectileScale);
            }
        }

        return scale;
    }
}
