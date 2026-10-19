using UnityEngine;

// 공격이 목표에 닿았을 때의 처리를 맡는다. 피해를 굴리고, 명중 반경·방패로 맞았는지 가린 뒤, 체력·타격 연출·물감 자국·게이지까지 한 번에 처리한다.
// 언제 쏠지는 AutoAttacker가 정하고, 맞히는 규칙을 바꿀 때는 여기만 고친다. 전투마다 목표와 캔버스를 받아 새로 만든다.
public class DamageDealer
{
    private readonly BattleUnit _owner;
    private readonly SkillGauge _skillGauge;
    private readonly AttackTarget _target;
    private readonly PaintCanvas _paintCanvas;
    private readonly bool _appliesWeaponModifier;
    private int _nextColorIndex;

    // 게이지는 플레이어만 있어 null일 수 있다.
    private DamageDealer(BattleUnit owner, SkillGauge skillGauge, AttackTarget target, PaintCanvas paintCanvas, bool appliesWeaponModifier)
    {
        _owner = owner;
        _skillGauge = skillGauge;
        _target = target;
        _paintCanvas = paintCanvas;
        _appliesWeaponModifier = appliesWeaponModifier;
    }

    public static DamageDealer ForWeapon(BattleUnit owner, SkillGauge skillGauge, AttackTarget target, PaintCanvas paintCanvas)
    {
        return new DamageDealer(owner, skillGauge, target, paintCanvas, true);
    }

    // 스킬은 무기 보정을 받지 않는다.
    public static DamageDealer ForSkill(BattleUnit owner, SkillGauge skillGauge, AttackTarget target, PaintCanvas paintCanvas)
    {
        return new DamageDealer(owner, skillGauge, target, paintCanvas, false);
    }

    // 날아가는 과정 없이 바로 맞힌다. 근접 공격처럼 투사체가 없는 공격에 쓴다.
    public void Strike(HitSettings hit, Vector3 hitPoint, float gaugeGain)
    {
        ApplyHit(hit, RollDamage(hit), hitPoint, gaugeGain);
    }

    // 기믹 실패처럼 막을 수 없는 공격. 명중 반경·방패를 보지 않고 바로 맞힌다.
    public void StrikeUnblockable(HitSettings hit, Vector3 hitPoint)
    {
        if (_target.IsDefeated)
        {
            return;
        }

        DamageInfo damage = RollDamage(hit);
        damage.HitPoint = hitPoint;

        _target.Unit.Health.TakeDamage(damage);

        HitFeedbackPlayer.Play(hit.GetHitFeedback(damage.IsCritical), _target.Unit, damage, _paintCanvas);
    }

    // 범위 공격. 폭발 반경 안에 목표가 있으면 목표 자리에 맞고, 방패를 들고 있으면 막힌다.
    public void Blast(HitSettings hit, Vector3 center, float radius, float gaugeGain)
    {
        if (false == _target.IsInBlast(center, radius))
        {
            return;
        }

        ApplyHit(hit, RollDamage(hit), _target.GetAimPoint(Vector3.zero), gaugeGain);
    }

    // 막혔으면 피해·물감 자국·게이지 없이 끝난다. 막기 연출은 막은 쪽이 맡는다.
    // 투사체는 비행이 이미 닿았다고 본 뒤에 불러, 명중 반경으로 다시 거르지 않는다(큰 탄은 반경 밖에서 몸 가장자리에 닿는다).
    public void DealHit(HitSettings hit, DamageInfo damage, Vector3 hitPoint, float gaugeGain)
    {
        if (_target.IsDefeated || _target.TryBlockHit(hitPoint))
        {
            return;
        }

        damage.HitPoint = hitPoint;

        _target.Unit.Health.TakeDamage(damage);

        HitFeedbackPlayer.Play(hit.GetHitFeedback(damage.IsCritical), _target.Unit, damage, _paintCanvas);

        if (null != _skillGauge && 0f < gaugeGain)
        {
            _skillGauge.Add(gaugeGain);
        }
    }

    // 피해량과 물감 색은 쏘는 순간 정해, 날아가는 투사체의 색과 맞은 자리의 물감 색이 같게 한다.
    public DamageInfo RollDamage(HitSettings hit)
    {
        int damageBonus = _appliesWeaponModifier ? _owner.WeaponModifier.DamageBonus : 0;
        int amount = hit.Damage.PickRandom(damageBonus);
        bool isCritical = Random.value < hit.CriticalChance;

        if (isCritical)
        {
            amount = Mathf.RoundToInt(amount * hit.CriticalMultiplier);
        }

        PaintPalette palette = _owner.Data.Palette;

        return new DamageInfo
        {
            Amount = amount,
            IsCritical = isCritical,
            PaintColor = PickColor(),
            IsInk = null != palette && palette.IsInk,
            Attacker = _owner,
        };
    }

    // 팔레트 색을 차례로 돌려 써, 맞은 자리마다 색이 바뀐다.
    public Color PickColor()
    {
        PaintPalette palette = _owner.Data.Palette;

        if (null == palette)
        {
            return Color.white;
        }

        Color color = palette.Pick(_nextColorIndex);
        _nextColorIndex++;

        return color;
    }

    // 방패가 명중 반경보다 넓어서, 명중 반경 밖이어도 방패 안이면 막기 판정으로 넘긴다.
    private void ApplyHit(HitSettings hit, DamageInfo damage, Vector3 hitPoint, float gaugeGain)
    {
        if (false == _target.IsHit(hitPoint) && false == _target.IsBlockedAt(hitPoint, 0f))
        {
            return;
        }

        DealHit(hit, damage, hitPoint, gaugeGain);
    }
}
