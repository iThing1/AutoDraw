using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

public class HitStopManager
{
    private CancellationToken _token;

    private bool _isRunning;
    private float _endTime;
    private float _timeScale = 1f;
    private float _baseTimeScale = 1f;

    // 히트 스톱이 끝나면 돌아갈 시간 배율. 엔딩의 슬로 모션처럼 기본 속도 자체를 바꿀 때 쓴다.
    public float BaseTimeScale
    {
        get
        {
            return _baseTimeScale;
        }
        set
        {
            _baseTimeScale = value;

            if (false == _isRunning)
            {
                Time.timeScale = value;
            }
        }
    }

    public void Init(CancellationToken token)
    {
        _token = token;
    }

    // 여러 타격이 겹치면 가장 늦게 끝나는 시간과 가장 느린 배율을 따른다.
    public void Play(float duration, float timeScale)
    {
        if (0f >= duration)
        {
            return;
        }

        float endTime = Time.realtimeSinceStartup + duration;

        if (_isRunning)
        {
            _endTime = Mathf.Max(_endTime, endTime);
            _timeScale = Mathf.Min(_timeScale, timeScale);
            Time.timeScale = _timeScale;
            return;
        }

        _endTime = endTime;
        _timeScale = timeScale;

        RunAsync().Forget();
    }

    // 게임 시간이 멈춘 동안에도 끝을 재야 하므로 실제 시간 기준으로 기다린다.
    private async UniTaskVoid RunAsync()
    {
        _isRunning = true;
        Time.timeScale = _timeScale;

        try
        {
            while (Time.realtimeSinceStartup < _endTime)
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
            _isRunning = false;
            Time.timeScale = _baseTimeScale;
        }
    }
}
