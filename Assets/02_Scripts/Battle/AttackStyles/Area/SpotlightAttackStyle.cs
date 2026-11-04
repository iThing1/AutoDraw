using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Pool;

// 조명들이 보스에게서 빛줄기를 뻗어 목표를 쫓다가 하나씩 멈춰, 비춘 자리를 터뜨린다.
[CreateAssetMenu(fileName = "SpotlightAttackStyle", menuName = "Scriptable Objects/AttackStyle/Spotlight")]
public class SpotlightAttackStyle : AttackStyle
{
    [Header("조명")]
    [SerializeField, Min(1)] private int _lightCount = 3;
    [SerializeField, Min(0.1f)] private float _radius = 1.6f;
    [SerializeField] private FloatRange _chaseSpeed = new FloatRange(5f, 8f);
    [SerializeField, Min(0f)] private float _chaseDuration = 1.2f;
    // 조명마다 이만큼씩 늦게 멈춘다.
    [SerializeField, Min(0f)] private float _lockInterval = 0.45f;

    [Header("그림")]
    [SerializeField] private GameObject _lightPrefab;
    [SerializeField] private GameObject _beamPrefab;
    [SerializeField] private Color _lightColor = new Color(1f, 0.92f, 0.6f, 0.3f);
    [SerializeField, Min(0.01f)] private float _beamWidth = 0.12f;

    [Header("예고")]
    [SerializeField] private GameObject _telegraphPrefab;
    [SerializeField] private Color _telegraphColor = new Color(1f, 0.82f, 0.35f, 0.45f);
    [SerializeField, Min(0f)] private float _warnDuration = 0.6f;

    [Header("터짐")]
    [SerializeField] private HitSettings _hit = new();
    [SerializeField] private ImpactSettings _impact = new();
    [SerializeField] private HitFeedbackPreset _groundSplat;

    public override void Prewarm()
    {
        if (null != _lightPrefab)
        {
            GameManager.Pool.Prewarm(_lightPrefab, _lightCount);
        }

        if (null != _beamPrefab)
        {
            GameManager.Pool.Prewarm(_beamPrefab, _lightCount);
        }

        if (null != _telegraphPrefab)
        {
            GameManager.Pool.Prewarm(_telegraphPrefab, _lightCount * 2);
        }
    }

    public override async UniTask FireAsync(AttackContext context, float gaugeGain, CancellationToken token)
    {
        List<UniTask> lights = ListPool<UniTask>.Get();

        try
        {
            for (int i = 0; i < _lightCount; i++)
            {
                lights.Add(LightAsync(context, _chaseDuration + _lockInterval * i, gaugeGain, token));
            }

            await UniTask.WhenAll(lights);
        }
        finally
        {
            ListPool<UniTask>.Release(lights);
        }
    }

    private async UniTask LightAsync(AttackContext context, float chaseDuration, float gaugeGain, CancellationToken token)
    {
        if (null == _lightPrefab || null == _beamPrefab)
        {
            return;
        }

        Transform origin = context.Owner.Body;
        float speed = _chaseSpeed.PickRandom();
        float diameter = _radius * 2f;
        SpriteRenderer light = GameManager.Pool.SpawnSprite(_lightPrefab, origin.position, 0f, new Vector2(diameter, diameter), _lightColor);
        SpriteRenderer beam = GameManager.Pool.SpawnSprite(_beamPrefab, origin.position, 0f, Vector2.zero, _lightColor);

        try
        {
            float elapsed = 0f;

            while (elapsed < chaseDuration)
            {
                Vector3 aimPoint = context.Target.GetAimPoint(Vector3.zero);

                light.transform.position = Vector3.MoveTowards(light.transform.position, aimPoint, speed * Time.deltaTime);
                PlaceBeam(beam, origin.position, light.transform.position);

                await UniTask.Yield(PlayerLoopTiming.Update, token);
                elapsed += Time.deltaTime;
            }

            Vector3 point = light.transform.position;

            await Telegraph.PlayCircleAsync(GameManager.Pool, _telegraphPrefab, point, _radius, _telegraphColor, _warnDuration, token);

            GameManager.Feedback.PlayImpact(_impact, point);
            context.Damage.Blast(_hit, point, _radius, gaugeGain);

            if (null != _groundSplat && null != context.PaintCanvas)
            {
                context.PaintCanvas.LeaveSplats(point, context.Damage.PickColor(), false, _groundSplat);
            }
        }
        finally
        {
            GameManager.Pool.DespawnSprite(light);
            GameManager.Pool.DespawnSprite(beam);
        }
    }

    private void PlaceBeam(SpriteRenderer beam, Vector3 from, Vector3 to)
    {
        Vector3 delta = to - from;

        beam.transform.SetPositionAndRotation((from + to) * 0.5f, Quaternion.Euler(0f, 0f, Geometry2D.ToAngle(delta)));
        beam.transform.localScale = new Vector3(delta.magnitude, _beamWidth, 1f);
    }
}
