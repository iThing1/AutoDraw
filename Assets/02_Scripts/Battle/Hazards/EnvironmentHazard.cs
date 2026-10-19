using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

// 보스 공격과 따로, 전투 중 일정 주기로 환경 탄막을 일으킨다.
// 탄은 보스 자리에서 나오지만 보스 패턴 순서와 상관없이 돌아, 보스의 다른 공격과 겹쳐 나올 수 있다.
public class EnvironmentHazard : MonoBehaviour
{
    [SerializeField] private HazardPattern[] _patterns;
    // 전투가 시작되자마자 겹치면 보스 공격을 익힐 틈이 없어, 처음 한 번은 늦게 시작한다.
    [SerializeField, Min(0f)] private float _firstDelay = 8f;
    // 한 패턴이 끝난 뒤 다음 패턴까지의 간격.
    [SerializeField] private FloatRange _interval = new FloatRange(12f, 18f);

    private int _lastPatternIndex = -1;

    // Prewarm은 최소 개수를 맞추므로, 같은 프리팹을 여러 패턴이 써도 중복으로 만들지 않는다.
    public void Prewarm(GameObject defaultProjectile)
    {
        if (null == _patterns)
        {
            return;
        }

        for (int i = 0; i < _patterns.Length; i++)
        {
            if (null != _patterns[i])
            {
                _patterns[i].Prewarm(defaultProjectile);
            }
        }
    }

    // 게임 시간으로 기다려, 히트 스톱과 슬로 모션 동안에는 다음 패턴도 함께 늦춘다.
    public async UniTask RunAsync(AutoAttacker shooter, CancellationToken token)
    {
        if (null == _patterns || 0 == _patterns.Length)
        {
            return;
        }

        await UniTask.Delay(TimeSpan.FromSeconds(_firstDelay), cancellationToken: token);

        while (false == shooter.Owner.Health.IsDead && false == shooter.Target.IsDefeated)
        {
            // 보스가 기믹·부활로 멈춘 동안에는 새 탄막을 꺼내지 않아, 기믹과 탄막이 겹치지 않게 한다.
            await shooter.WaitUntilResumedAsync(token);

            HazardPattern pattern = PickPattern();

            if (null != pattern)
            {
                await pattern.PlayAsync(shooter, token);
            }

            await UniTask.Delay(TimeSpan.FromSeconds(_interval.PickRandom()), cancellationToken: token);
        }
    }

    // 같은 패턴이 연달아 나오면 단조로워, 둘 이상이면 직전 패턴은 빼고 고른다.
    private HazardPattern PickPattern()
    {
        if (1 == _patterns.Length || 0 > _lastPatternIndex)
        {
            _lastPatternIndex = UnityEngine.Random.Range(0, _patterns.Length);
            return _patterns[_lastPatternIndex];
        }

        // 직전 칸을 뺀 나머지 중에서 고르고, 직전 칸 이후는 한 칸씩 밀어 빈자리를 건너뛴다.
        int index = UnityEngine.Random.Range(0, _patterns.Length - 1);

        if (index >= _lastPatternIndex)
        {
            index++;
        }

        _lastPatternIndex = index;
        return _patterns[index];
    }
}
