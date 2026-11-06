using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Pool;

// 앞 보스들의 패턴을 골라, 보스 머리 위에 그 보스 색의 액자를 걸고 그대로 쓴다. 여러 개를 꺼내면 액자를 나란히 걸고 함께 쓴다.
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
    // 서로 다른 소장품을 이만큼 함께 꺼낸다. 소장품이 모자라면 있는 만큼만 꺼낸다.
    [SerializeField, Min(1)] private int _exhibitCount = 1;

    [Header("액자")]
    [SerializeField] private GameObject _framePrefab;
    [SerializeField] private Vector2 _frameSize = new Vector2(2.6f, 3.2f);
    [SerializeField] private Vector3 _frameOffset = new Vector3(0f, 3.4f, 0f);
    [SerializeField, Min(0f)] private float _frameSpacing = 3.2f;
    [SerializeField, Min(0.01f)] private float _edgeWidth = 0.18f;
    [SerializeField] private Color _edgeColor = new Color(0.78f, 0.62f, 0.25f, 1f);
    [SerializeField, Min(0f)] private float _revealDuration = 0.8f;
    [SerializeField, Min(0f)] private float _fadeDuration = 0.3f;

    public override void Prewarm()
    {
        if (null != _framePrefab)
        {
            GameManager.Pool.Prewarm(_framePrefab, _exhibitCount * GimmickFrame.SPRITE_COUNT);
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
        List<Entry> entries = ListPool<Entry>.Get();
        List<GimmickFrame> frames = ListPool<GimmickFrame>.Get();
        List<UniTask> exhibits = ListPool<UniTask>.Get();

        try
        {
            PickEntries(entries);

            if (null != _framePrefab)
            {
                HangFrames(context.Owner.Body.position + _frameOffset, entries, frames);
                await RevealAsync(frames, token);
            }

            for (int i = 0; i < entries.Count; i++)
            {
                exhibits.Add(entries[i].Style.PickStyle(null).FireAsync(context, gaugeGain, token));
            }

            await UniTask.WhenAll(exhibits);

            if (0 < frames.Count)
            {
                await FadeOutAsync(frames, token);
            }
        }
        finally
        {
            for (int i = 0; i < frames.Count; i++)
            {
                frames[i].Despawn();
            }

            ListPool<Entry>.Release(entries);
            ListPool<GimmickFrame>.Release(frames);
            ListPool<UniTask>.Release(exhibits);
        }
    }

    // 쓸 수 있는 소장품을 모은 뒤 앞자리부터 남은 것 중 하나씩 뽑아, 같은 소장품이 겹치지 않게 고른다.
    private void PickEntries(List<Entry> picked)
    {
        if (null == _exhibits || 0 == _exhibits.Length)
        {
            Debug.LogError($"소장품 패턴 : {name}에 소장품이 없습니다.", this);
            return;
        }

        for (int i = 0; i < _exhibits.Length; i++)
        {
            if (IsValid(_exhibits[i]))
            {
                picked.Add(_exhibits[i]);
            }
        }

        int count = Mathf.Min(_exhibitCount, picked.Count);

        for (int i = 0; i < count; i++)
        {
            int j = UnityEngine.Random.Range(i, picked.Count);
            (picked[i], picked[j]) = (picked[j], picked[i]);
        }

        picked.RemoveRange(count, picked.Count - count);
    }

    // 자기 자신을 넣은 항목은 고를 때마다 자기를 다시 골라 끝나지 않으므로 뺀다.
    private bool IsValid(Entry entry)
    {
        return null != entry && null != entry.Style && this != entry.Style;
    }

    private void HangFrames(Vector3 center, List<Entry> entries, List<GimmickFrame> frames)
    {
        float firstOffset = -_frameSpacing * (entries.Count - 1) * 0.5f;

        for (int i = 0; i < entries.Count; i++)
        {
            GimmickFrame frame = new GimmickFrame(_framePrefab, _frameSize, _edgeWidth, _edgeColor, entries[i].Color);

            frame.Place(center + new Vector3(firstOffset + _frameSpacing * i, 0f, 0f));
            frames.Add(frame);
        }
    }

    private async UniTask RevealAsync(List<GimmickFrame> frames, CancellationToken token)
    {
        float elapsed = 0f;

        while (true)
        {
            float progress = 0f < _revealDuration ? elapsed / _revealDuration : 1f;

            for (int i = 0; i < frames.Count; i++)
            {
                frames[i].SetFill(progress);
            }

            if (elapsed >= _revealDuration)
            {
                break;
            }

            await UniTask.Yield(PlayerLoopTiming.Update, token);
            elapsed += Time.deltaTime;
        }
    }

    private async UniTask FadeOutAsync(List<GimmickFrame> frames, CancellationToken token)
    {
        List<UniTask> fades = ListPool<UniTask>.Get();

        try
        {
            for (int i = 0; i < frames.Count; i++)
            {
                frames[i].AddFades(0f, 0f, _fadeDuration, fades, token);
            }

            await UniTask.WhenAll(fades);
        }
        finally
        {
            ListPool<UniTask>.Release(fades);
        }
    }
}
