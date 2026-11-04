using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Pool;

// 앞 보스들의 패턴 하나를 골라, 보스 머리 위에 그 보스 색의 액자를 걸고 그대로 쓴다.
[CreateAssetMenu(fileName = "ExhibitAttackStyle", menuName = "Scriptable Objects/AttackStyle/Exhibit")]
public class ExhibitAttackStyle : AttackStyle
{
    [Serializable]
    public class Entry
    {
        // 묶음을 넣으면 그 안에서 하나를 고른다.
        [SerializeField] private AttackStyle _style;
        [SerializeField] private Color _color = Color.white;

        public AttackStyle Style { get { return _style; } }
        public Color Color { get { return _color; } }
    }

    [Header("소장품")]
    [SerializeField] private Entry[] _exhibits;

    [Header("액자")]
    [SerializeField] private GameObject _framePrefab;
    [SerializeField] private Vector2 _frameSize = new Vector2(2.6f, 3.2f);
    [SerializeField] private Vector3 _frameOffset = new Vector3(0f, 3.4f, 0f);
    [SerializeField, Min(0.01f)] private float _edgeWidth = 0.18f;
    [SerializeField] private Color _edgeColor = new Color(0.78f, 0.62f, 0.25f, 1f);
    [SerializeField, Min(0f)] private float _revealDuration = 0.8f;
    [SerializeField, Min(0f)] private float _fadeDuration = 0.3f;

    public override void Prewarm()
    {
        if (null != _framePrefab)
        {
            GameManager.Pool.Prewarm(_framePrefab, GimmickFrame.SPRITE_COUNT);
        }

        if (null == _exhibits)
        {
            return;
        }

        for (int i = 0; i < _exhibits.Length; i++)
        {
            if (IsValid(_exhibits[i]))
            {
                _exhibits[i].Style.Prewarm();
            }
        }
    }

    public override async UniTask FireAsync(AttackContext context, float gaugeGain, CancellationToken token)
    {
        Entry entry = PickEntry();

        if (null == entry)
        {
            return;
        }

        AttackStyle exhibit = entry.Style.PickStyle(null);

        if (null == _framePrefab)
        {
            await exhibit.FireAsync(context, gaugeGain, token);
            return;
        }

        GimmickFrame frame = new GimmickFrame(_framePrefab, _frameSize, _edgeWidth, _edgeColor, entry.Color);

        try
        {
            frame.Place(context.Owner.Body.position + _frameOffset);

            await RevealAsync(frame, token);
            await exhibit.FireAsync(context, gaugeGain, token);
            await FadeOutAsync(frame, token);
        }
        finally
        {
            frame.Despawn();
        }
    }

    private Entry PickEntry()
    {
        if (null == _exhibits || 0 == _exhibits.Length)
        {
            Debug.LogError($"소장품 패턴 : {name}에 소장품이 없습니다.", this);
            return null;
        }

        Entry entry = _exhibits[UnityEngine.Random.Range(0, _exhibits.Length)];

        return IsValid(entry) ? entry : null;
    }

    // 자기 자신을 넣은 항목은 고를 때마다 자기를 다시 골라 끝나지 않으므로 뺀다.
    private bool IsValid(Entry entry)
    {
        return null != entry && null != entry.Style && this != entry.Style;
    }

    private async UniTask RevealAsync(GimmickFrame frame, CancellationToken token)
    {
        float elapsed = 0f;

        while (true)
        {
            frame.SetFill(0f < _revealDuration ? elapsed / _revealDuration : 1f);

            if (elapsed >= _revealDuration)
            {
                break;
            }

            await UniTask.Yield(PlayerLoopTiming.Update, token);
            elapsed += Time.deltaTime;
        }
    }

    private async UniTask FadeOutAsync(GimmickFrame frame, CancellationToken token)
    {
        List<UniTask> fades = ListPool<UniTask>.Get();

        try
        {
            frame.AddFades(0f, 0f, _fadeDuration, fades, token);

            await UniTask.WhenAll(fades);
        }
        finally
        {
            ListPool<UniTask>.Release(fades);
        }
    }
}
