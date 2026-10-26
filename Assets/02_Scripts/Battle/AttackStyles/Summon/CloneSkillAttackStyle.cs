using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

// 복제. 정해진 시간 동안 플레이어 곁에 반투명한 복제체를 불러, 플레이어가 지금 고른 무기를 그대로 쏘게 한다.
// 복제체는 무기 공격만 하는 작은 유닛이라, 무기가 쓰는 위치·보정·물감 색을 플레이어와 똑같이 갖고 보스는 복제체를 노리지 않는다.
// 플레이어가 무기를 바꾸면 복제체도 바꾸고, 연출·기믹이 플레이어 공격을 멈추면 복제체도 함께 멈춘다. 복제체가 낸 피해는 스킬 게이지를 채우지 않는다.
[CreateAssetMenu(fileName = "CloneSkillAttackStyle", menuName = "Scriptable Objects/AttackStyle/CloneSkill")]
public class CloneSkillAttackStyle : AttackStyle
{
    [Header("복제체")]
    // BattleUnit · Health · AutoAttacker · UnitView만 가진 플레이어 모양 유닛(03_Prefabs/Units/PlayerClone). 데이터는 플레이어 데이터를 쓴다.
    [SerializeField] private BattleUnit _clonePrefab;
    [SerializeField, Min(0.1f)] private float _duration = 10f;
    // 플레이어 자리에서 이만큼 떨어진 곳을 따라다닌다.
    [SerializeField] private Vector2 _followOffset = new Vector2(-1.4f, 0.8f);
    // 클수록 플레이어를 빨리 따라붙는다.
    [SerializeField, Min(0.1f)] private float _followSharpness = 10f;
    [SerializeField] private Color _tint = new Color(0.75f, 0.85f, 1f, 0.7f);
    [SerializeField, Min(0f)] private float _fadeDuration = 0.3f;
    [SerializeField] private ImpactSettings _summonImpact = new();

    public override async UniTask FireAsync(AttackContext context, float gaugeGain, CancellationToken token)
    {
        if (null == _clonePrefab)
        {
            Debug.LogError($"복제 : {name}에 복제체 프리팹이 지정되지 않았습니다.");
            return;
        }

        BattleUnit player = context.Owner;
        BattleUnit clone = Instantiate(_clonePrefab, GetFollowPoint(player), Quaternion.identity);

        // 복제체의 공격은 따로 끊어, 시간이 다 되면 복제체만 멈춘다.
        using (CancellationTokenSource attackSource = CancellationTokenSource.CreateLinkedTokenSource(token))
        {
            try
            {
                clone.SetStats(player.Stats);
                clone.OverrideAttackStyle(player.AttackStyle, player.WeaponModifier);
                clone.ChangeState(UnitState.Move);
                CopyLook(player, clone, 1f);

                ImpactPlayer.Play(_summonImpact, clone.Body.position);
                clone.Attacker.RunAsync(context.Target.Unit, context.PaintCanvas, attackSource.Token).Forget();

                await FollowAsync(context, player, clone, token);

                attackSource.Cancel();
                await FadeOutAsync(player, clone, token);
            }
            finally
            {
                attackSource.Cancel();

                if (null != clone)
                {
                    Destroy(clone.gameObject);
                }
            }
        }
    }

    // 시간이 다 되거나 어느 한쪽이 쓰러질 때까지 플레이어 곁을 따라다니며 무기와 멈춤을 맞춘다.
    // 몸 그림은 애니메이션이 움직인 뒤에 베껴, 복제체 자신의 애니메이션에 덮이지 않게 한다.
    private async UniTask FollowAsync(AttackContext context, BattleUnit player, BattleUnit clone, CancellationToken token)
    {
        float elapsed = 0f;

        while (elapsed < _duration && false == player.Health.IsDead && false == context.Target.IsDefeated)
        {
            float follow = 1f - Mathf.Exp(-_followSharpness * Time.deltaTime);
            clone.transform.position = Vector3.Lerp(clone.transform.position, GetFollowPoint(player), follow);

            if (clone.AttackStyle != player.AttackStyle)
            {
                clone.OverrideAttackStyle(player.AttackStyle, player.WeaponModifier);
            }

            SyncInterrupt(player.Attacker, clone.Attacker);
            CopyLook(player, clone, 1f);

            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate, token);
            elapsed += Time.deltaTime;
        }
    }

    private async UniTask FadeOutAsync(BattleUnit player, BattleUnit clone, CancellationToken token)
    {
        float elapsed = 0f;

        while (elapsed < _fadeDuration)
        {
            CopyLook(player, clone, 1f - elapsed / _fadeDuration);

            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate, token);
            elapsed += Time.deltaTime;
        }
    }

    private Vector3 GetFollowPoint(BattleUnit player)
    {
        return player.transform.position + (Vector3)_followOffset;
    }

    private static void SyncInterrupt(AutoAttacker player, AutoAttacker clone)
    {
        if (player.IsInterrupted == clone.IsInterrupted)
        {
            return;
        }

        if (player.IsInterrupted)
        {
            clone.Interrupt();
        }
        else
        {
            clone.Resume();
        }
    }

    // 플레이어 몸 그림(애니메이션으로 기울고 늘어난 모습 포함)을 복제체에 옮기고 반투명한 색을 입힌다.
    private void CopyLook(BattleUnit player, BattleUnit clone, float alphaRatio)
    {
        SpriteRenderer source = player.BodyRenderer;
        SpriteRenderer target = clone.BodyRenderer;

        if (null == source || null == target)
        {
            return;
        }

        target.sprite = source.sprite;
        target.flipX = source.flipX;
        target.transform.localPosition = source.transform.localPosition;
        target.transform.localRotation = source.transform.localRotation;
        target.transform.localScale = source.transform.localScale;
        target.color = new Color(_tint.r, _tint.g, _tint.b, _tint.a * alphaRatio);
    }
}
