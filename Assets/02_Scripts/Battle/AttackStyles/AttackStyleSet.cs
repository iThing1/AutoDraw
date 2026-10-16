using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

// 여러 공격 스타일을 가중치로 섞어 쓰는 묶음. 보스처럼 패턴을 번갈아 쓰는 유닛에 연결한다.
// 묶음 안에 묶음을 넣을 수 있다. 서로를 넣어 끝없이 도는 구성은 에디터에서 바로 알리고, 실행 중에는 깊이로 끊는다.
[CreateAssetMenu(fileName = "AttackStyleSet", menuName = "Scriptable Objects/AttackStyle/Set")]
public class AttackStyleSet : AttackStyle
{
    // 정상적인 구성이라면 이보다 깊게 묶을 일이 없으므로, 넘어가면 순환으로 본다.
    private const int MAX_NESTING_DEPTH = 8;

    [Serializable]
    public class Entry
    {
        [SerializeField] private AttackStyle _style;
        [SerializeField, Min(0f)] private float _weight = 1f;
        // 목표와의 거리가 이 범위 안일 때만 고른다. 최대 0은 제한 없음이다.
        [SerializeField, Min(0f)] private float _minDistance;
        [SerializeField, Min(0f)] private float _maxDistance;
        // 한 번 쓰면 이 시간(초) 동안은 다시 고르지 않는다.
        [SerializeField, Min(0f)] private float _cooldown;

        public AttackStyle Style { get { return _style; } }
        public float Weight { get { return _weight; } }

        // 쏘는 쪽을 모르면(null) 조건 없이 고를 수 있다고 본다.
        public bool IsAvailable(AutoAttacker picker)
        {
            if (null == picker || null == picker.Target)
            {
                return true;
            }

            float distance = Vector2.Distance(picker.Owner.transform.position, picker.Target.Unit.transform.position);

            if (distance < _minDistance || (0f < _maxDistance && distance > _maxDistance))
            {
                return false;
            }

            return false == picker.IsOnCooldown(_style, _cooldown);
        }
    }

    [Header("패턴")]
    [SerializeField] private Entry[] _entries;

    public override AttackStyle PickStyle()
    {
        return PickStyle(null);
    }

    // 안쪽 묶음을 따라 내려가며 실제 공격 하나를 고른다. 재귀 대신 반복문이라 순환이 있어도 깊이 제한에서 멈춘다.
    // 거리·쿨타임 조건에 맞는 항목이 하나도 없으면 조건 없이 골라, 보스가 공격을 멈추지 않게 한다.
    public override AttackStyle PickStyle(AutoAttacker picker)
    {
        AttackStyleSet currentSet = this;

        for (int depth = 0; depth < MAX_NESTING_DEPTH; depth++)
        {
            Entry entry = currentSet.PickEntry(picker);

            if (null == entry)
            {
                entry = currentSet.PickEntry(null);
            }

            if (null == entry)
            {
                Debug.LogError($"공격 스타일 묶음 : {currentSet.name}에 고를 수 있는 공격 스타일이 없습니다.");
                return this;
            }

            AttackStyleSet nestedSet = entry.Style as AttackStyleSet;

            if (null == nestedSet)
            {
                return entry.Style;
            }

            currentSet = nestedSet;
        }

        Debug.LogError($"공격 스타일 묶음 : {name}의 묶음이 너무 깊거나 서로를 포함하고 있습니다.");
        return this;
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

    public override void Prewarm()
    {
        PrewarmEntries(0);
    }

    // 인스펙터에서 값을 바꿀 때마다 불려, 순환 구성을 실행하기 전에 바로 알려 준다.
    private void OnValidate()
    {
        if (ContainsSet(this, 0))
        {
            Debug.LogError($"공격 스타일 묶음 : {name}이 다른 묶음을 거쳐 자기 자신을 다시 포함합니다. 패턴 목록을 확인해 주세요.", this);
        }
    }

    private void PrewarmEntries(int depth)
    {
        if (null == _entries || depth >= MAX_NESTING_DEPTH)
        {
            return;
        }

        for (int i = 0; i < _entries.Length; i++)
        {
            if (false == IsValid(_entries[i]))
            {
                continue;
            }

            AttackStyleSet nestedSet = _entries[i].Style as AttackStyleSet;

            if (null != nestedSet)
            {
                nestedSet.PrewarmEntries(depth + 1);
            }
            else
            {
                _entries[i].Style.Prewarm();
            }
        }
    }

    // target이 이 묶음 아래 어딘가에 다시 나타나는지 찾는다. 깊이 제한을 넘으면 순환으로 본다.
    private bool ContainsSet(AttackStyleSet target, int depth)
    {
        if (null == _entries)
        {
            return false;
        }

        if (depth >= MAX_NESTING_DEPTH)
        {
            return true;
        }

        for (int i = 0; i < _entries.Length; i++)
        {
            AttackStyleSet nestedSet = null != _entries[i] ? _entries[i].Style as AttackStyleSet : null;

            if (null == nestedSet)
            {
                continue;
            }

            if (target == nestedSet || nestedSet.ContainsSet(target, depth + 1))
            {
                return true;
            }
        }

        return false;
    }

    private Entry PickEntry(AutoAttacker picker)
    {
        if (null == _entries)
        {
            return null;
        }

        float totalWeight = 0f;

        for (int i = 0; i < _entries.Length; i++)
        {
            if (IsPickable(_entries[i], picker))
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
            if (false == IsPickable(_entries[i], picker))
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

    private bool IsPickable(Entry entry, AutoAttacker picker)
    {
        return IsValid(entry) && entry.IsAvailable(picker);
    }

    // 자기 자신을 바로 넣은 항목은 고를 때마다 자기를 다시 골라 끝나지 않으므로 뺀다.
    private bool IsValid(Entry entry)
    {
        return null != entry && null != entry.Style && this != entry.Style && 0f < entry.Weight;
    }
}
