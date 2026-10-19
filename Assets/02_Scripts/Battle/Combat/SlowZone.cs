using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

// 들어온 플레이어와 탄을 느리게 하는 원형 구역. 풀에서 꺼내 한 번 재생하고 스스로 돌아간다.
// 켜진 구역 목록도 여기서 들고, 어떤 자리가 얼마나 느려지는지 답한다. 보스는 묻지 않아 영향을 받지 않는다.
[RequireComponent(typeof(SpriteRenderer))]
public class SlowZone : MonoBehaviour
{
    private static readonly List<SlowZone> ACTIVE_ZONES = new();

    private SpriteRenderer _renderer;
    private SlowZoneSettings _settings;
    // 자라는 동안에는 겉모습과 함께 판정 반경도 커진다.
    private float _radius;

    public float MoveScale { get { return _settings.MoveScale; } }
    public float ProjectileScale { get { return _settings.ProjectileScale; } }

    // 여러 구역이 겹치면 가장 느린 쪽을 따른다.
    public static float GetMoveScale(Vector3 position)
    {
        float scale = 1f;

        for (int i = 0; i < ACTIVE_ZONES.Count; i++)
        {
            if (ACTIVE_ZONES[i].Contains(position))
            {
                scale = Mathf.Min(scale, ACTIVE_ZONES[i].MoveScale);
            }
        }

        return scale;
    }

    public static float GetProjectileScale(Vector3 position)
    {
        float scale = 1f;

        for (int i = 0; i < ACTIVE_ZONES.Count; i++)
        {
            if (ACTIVE_ZONES[i].Contains(position))
            {
                scale = Mathf.Min(scale, ACTIVE_ZONES[i].ProjectileScale);
            }
        }

        return scale;
    }

    // 에디터에서 Reload Domain을 꺼도 지난 플레이의 구역이 남지 않게 한다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ClearActiveZones()
    {
        ACTIVE_ZONES.Clear();
    }

    private void Awake()
    {
        _renderer = GetComponent<SpriteRenderer>();
    }

    // 풀로 돌아가거나 스테이지와 함께 사라질 때 더 이상 느리게 하지 않는다.
    private void OnDisable()
    {
        ACTIVE_ZONES.Remove(this);
    }

    public bool Contains(Vector3 position)
    {
        return Vector2.Distance(position, transform.position) <= _radius;
    }

    // 게임 시간으로 돌아 히트 스톱 동안에는 구역도 함께 멈춘다.
    public async UniTask PlayAsync(SlowZoneSettings settings, CancellationToken token)
    {
        _settings = settings;
        _radius = 0f;
        _renderer.color = settings.Color;
        transform.localScale = Vector3.zero;

        if (false == ACTIVE_ZONES.Contains(this))
        {
            ACTIVE_ZONES.Add(this);
        }

        try
        {
            float diameter = settings.Radius * 2f;

            await DOVirtual.Float(0f, settings.Radius, settings.GrowDuration, SetRadius)
                .SetEase(Ease.OutQuad)
                .SetTarget(transform)
                .ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, token);

            transform.localScale = new Vector3(diameter, diameter, 1f);

            await UniTask.Delay(TimeSpan.FromSeconds(settings.Duration), cancellationToken: token);

            // 깨지는 순간부터 느려지지 않게 먼저 빼고, 겉모습만 튀었다 사라진다.
            ACTIVE_ZONES.Remove(this);

            await DOTween.Sequence()
                .Join(transform.DOPunchScale(transform.localScale * settings.EndPunch, settings.EndDuration, PunchTween.VIBRATO, PunchTween.ELASTICITY))
                .Join(_renderer.DOFade(0f, settings.EndDuration))
                .SetTarget(transform)
                .ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, token);
        }
        finally
        {
            transform.DOKill();
            _renderer.DOKill();
            GameManager.Pool.Despawn(gameObject);
        }
    }

    private void SetRadius(float radius)
    {
        _radius = radius;

        float diameter = radius * 2f;
        transform.localScale = new Vector3(diameter, diameter, 1f);
    }
}
