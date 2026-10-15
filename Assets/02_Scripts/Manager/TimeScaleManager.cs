using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

// 게임 시간 배율을 정하는 곳. 히트 스톱과 슬로 모션이 Time.timeScale을 각자 바꾸면 서로 덮어쓰므로 여기서만 바꾼다.
//
// [슬로 모션 사용 규칙]
// 1. BeginSlowMotion은 반드시 using 문으로 감싼다. 블록을 벗어나면 정상 종료·예외·취소 모두 자동으로 풀린다.
//        using (GameManager.TimeScale.BeginSlowMotion(_slowTimeScale))
//        {
//            await SwingAsync(attacker, gaugeGain, token);
//        }
// 2. 돌려받은 값을 using 없이 버리면 풀 방법이 없어 슬로 모션이 계속 남는다.
// 3. 여러 슬로 모션이 겹치면 나중에 건 쪽이 이긴다. 먼저 건 쪽의 블록이 나중에 끝나도 나중 것은 풀리지 않는다.
// 4. Time.timeScale은 이 클래스 밖에서 바꾸지 않는다.
public class TimeScaleManager
{
    // using 블록이 끝나면 Dispose가 불려 슬로 모션을 푼다. 푸는 길을 이것 하나로 두려고 ClearSlowMotion은 private이다.
    // struct라서 걸 때마다 메모리를 새로 잡지 않는다.
    public readonly struct SlowMotionScope : IDisposable
    {
        private readonly TimeScaleManager _manager;
        private readonly int _ticket;

        internal SlowMotionScope(TimeScaleManager manager, int ticket)
        {
            _manager = manager;
            _ticket = ticket;
        }

        public void Dispose()
        {
            // new 없이 만든 빈 값(default)은 건 슬로 모션이 없으므로 아무것도 하지 않는다.
            if (null == _manager)
            {
                return;
            }

            _manager.ClearSlowMotion(_ticket);
        }
    }

    private CancellationToken _token;

    private bool _isHitStopping;
    private float _hitStopEndTime;
    private float _hitStopTimeScale = 1f;

    private float _slowTimeScale = 1f;
    // 지금 걸려 있는 슬로 모션의 번호표. 0이면 걸린 것이 없다.
    private int _activeSlowMotionTicket;
    private int _lastSlowMotionTicket;

    public void Init(CancellationToken token)
    {
        _token = token;
    }

    // 여러 타격이 겹치면 가장 늦게 끝나는 시간과 가장 느린 배율을 따른다.
    public void PlayHitStop(float duration, float timeScale)
    {
        if (0f >= duration)
        {
            return;
        }

        float endTime = Time.realtimeSinceStartup + duration;

        if (_isHitStopping)
        {
            _hitStopEndTime = Mathf.Max(_hitStopEndTime, endTime);
            _hitStopTimeScale = Mathf.Min(_hitStopTimeScale, timeScale);
            ApplyTimeScale();
            return;
        }

        _hitStopEndTime = endTime;
        _hitStopTimeScale = timeScale;

        RunHitStopAsync().Forget();
    }

    public SlowMotionScope BeginSlowMotion(float timeScale)
    {
        _lastSlowMotionTicket++;
        _activeSlowMotionTicket = _lastSlowMotionTicket;
        _slowTimeScale = timeScale;

        ApplyTimeScale();

        return new SlowMotionScope(this, _activeSlowMotionTicket);
    }

    // 이전 슬로 모션의 using 블록이 나중에 끝나도 번호표가 0이라 다시 건드리지 않는다.
    public void ResetToNormal()
    {
        _hitStopEndTime = 0f;
        _isHitStopping = false;
        _activeSlowMotionTicket = 0;
        _slowTimeScale = 1f;

        ApplyTimeScale();
    }

    // 번호표가 지금 것과 다르면 이미 다른 연출이 덮어쓴 것이라, 그 연출의 슬로 모션이 풀리지 않게 무시한다.
    private void ClearSlowMotion(int ticket)
    {
        if (ticket != _activeSlowMotionTicket)
        {
            return;
        }

        _activeSlowMotionTicket = 0;
        _slowTimeScale = 1f;

        ApplyTimeScale();
    }

    // 게임 시간이 멈춘 동안에도 끝을 재야 하므로 실제 시간 기준으로 기다린다.
    private async UniTaskVoid RunHitStopAsync()
    {
        _isHitStopping = true;
        ApplyTimeScale();

        try
        {
            while (Time.realtimeSinceStartup < _hitStopEndTime)
            {
                await UniTask.Yield(PlayerLoopTiming.Update, _token);
            }
        }
        catch (OperationCanceledException)
        {
            // GameManager가 사라진 경우라도 시간 배율은 아래에서 되돌린다.
        }
        finally
        {
            _isHitStopping = false;
            ApplyTimeScale();
        }
    }

    // 슬로 모션 중에 맞으면 히트 스톱 배율이 슬로 모션보다 빠를 수 있어, 둘 중 느린 쪽을 쓴다.
    private void ApplyTimeScale()
    {
        if (_isHitStopping)
        {
            Time.timeScale = Mathf.Min(_hitStopTimeScale, _slowTimeScale);
        }
        else
        {
            Time.timeScale = _slowTimeScale;
        }
    }
}
