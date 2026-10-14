using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

// 여러 공격 스타일을 가중치로 섞어 쓰는 묶음. 보스처럼 패턴을 번갈아 쓰는 유닛에 연결한다.
[CreateAssetMenu(fileName = "AttackStyleSet", menuName = "Scriptable Objects/AttackStyle/Set")]
public class AttackStyleSet : AttackStyle
{
    [Serializable]
    public class Entry
    {
        [SerializeField] private AttackStyle _style;
        [SerializeField, Min(0f)] private float _weight = 1f;

        public AttackStyle Style { get { return _style; } }
        public float Weight { get { return _weight; } }
    }

    [Header("패턴")]
    [SerializeField] private Entry[] _entries;

    public override int MaxHitDamage
    {
        get
        {
            int maxHitDamage = 0;

            if (null == _entries)
            {
                return maxHitDamage;
            }

            for (int i = 0; i < _entries.Length; i++)
            {
                if (IsValid(_entries[i]))
                {
                    maxHitDamage = Mathf.Max(maxHitDamage, _entries[i].Style.MaxHitDamage);
                }
            }

            return maxHitDamage;
        }
    }

    public override AttackStyle PickStyle()
    {
        Entry entry = PickEntry();

        if (null == entry)
        {
            Debug.LogError($"공격 스타일 묶음 : {name}에 고를 수 있는 공격 스타일이 없습니다.");
            return this;
        }

        return entry.Style.PickStyle();
    }

    // 자동 공격은 PickStyle로 고른 스타일을 직접 쏘므로, 이 묶음이 그대로 넘어온 경우에만 여기서 하나를 골라 쏜다.
    public override UniTask FireAsync(AutoAttacker attacker, float gaugeGain, CancellationToken token)
    {
        AttackStyle style = PickStyle();

        if (this == style)
        {
            return UniTask.CompletedTask;
        }

        return style.FireAsync(attacker, gaugeGain, token);
    }

    private Entry PickEntry()
    {
        if (null == _entries)
        {
            return null;
        }

        float totalWeight = 0f;

        for (int i = 0; i < _entries.Length; i++)
        {
            if (IsValid(_entries[i]))
            {
                totalWeight += _entries[i].Weight;
            }
        }

        if (0f >= totalWeight)
        {
            return null;
        }

        float remainingWeight = UnityEngine.Random.value * totalWeight;

        for (int i = 0; i < _entries.Length; i++)
        {
            if (false == IsValid(_entries[i]))
            {
                continue;
            }

            remainingWeight -= _entries[i].Weight;

            if (0f >= remainingWeight)
            {
                return _entries[i];
            }
        }

        return null;
    }

    // 자기 자신을 넣으면 고를 때마다 자기를 다시 골라 끝나지 않으므로 뺀다.
    private bool IsValid(Entry entry)
    {
        return null != entry && null != entry.Style && this != entry.Style && 0f < entry.Weight;
    }
}
