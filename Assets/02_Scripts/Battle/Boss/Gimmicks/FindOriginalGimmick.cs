using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

// 원본 찾기. 플레이어 둘레에 액자 네 개가 걸리고, 보스가 그중 하나로 빨려 들어간 뒤 액자들이 뒤섞인다.
// 섞인 뒤 정해진 시간 안에 보스가 든 액자 안에 서 있으면 그로기, 다른 액자에 있거나 어느 액자에도 없으면 가짜 액자들이 반격한다.
// 진짜 액자 구석에는 위작가의 서명이 이따금 반짝여, 섞는 걸 놓쳐도 찾을 단서가 된다.
// 쏘는 탄이 보스 자리를 알려 주지 않도록, 기믹 동안 플레이어 공격을 멈춘다(PausesTargetAttack).
// 숨은 보스를 함께 담으려는 전투 카메라도 답을 알려 주므로, 기믹 동안 카메라를 네 액자의 가운데에 고정한다.
[CreateAssetMenu(fileName = "FindOriginalGimmick", menuName = "Scriptable Objects/Boss/Gimmick/FindOriginal")]
public class FindOriginalGimmick : PunishingBossGimmick
{
    private const int FRAME_COUNT = 4;

    [Header("액자")]
    // 테두리와 안쪽 면 그림. 1x1 네모 그림을 늘려 쓴다.
    [SerializeField] private GameObject _framePrefab;
    [SerializeField] private Vector2 _frameSize = new Vector2(3f, 3.8f);
    [SerializeField, Min(0.01f)] private float _edgeWidth = 0.25f;
    // 플레이어 자리에서 네 액자 가운데까지의 가로·세로 거리. 네 액자가 플레이어를 둘러싸 어느 쪽이든 같은 거리에서 시작한다.
    [SerializeField] private Vector2 _slotOffset = new Vector2(5f, 3.5f);
    [SerializeField] private Color _edgeColor = new Color(0.1f, 0.1f, 0.12f, 1f);
    [SerializeField] private Color _fillColor = new Color(0.1f, 0.1f, 0.12f, 0.18f);
    [SerializeField, Min(0f)] private float _appearDuration = 0.4f;

    [Header("카메라")]
    // 네 액자 바깥으로 더 비출 여백.
    [SerializeField, Min(0f)] private float _cameraPadding = 1.5f;
    [SerializeField, Min(0f)] private float _cameraBlendDuration = 0.5f;

    [Header("숨기")]
    // 보스가 진짜 액자로 날아가며 오그라드는 시간. 플레이어가 어느 액자인지 똑똑히 보게 한다.
    [SerializeField, Min(0.01f)] private float _hideDuration = 0.8f;
    [SerializeField, Min(0f)] private float _shuffleDelay = 0.6f;

    [Header("섞기")]
    [SerializeField, Min(1)] private int _swapCount = 6;
    [SerializeField, Min(0.05f)] private float _firstSwapDuration = 0.55f;
    [SerializeField, Min(0.05f)] private float _lastSwapDuration = 0.3f;
    // 이 차례부터는 두 쌍을 한꺼번에 바꾼다(0부터 센다). 섞는 횟수보다 크면 끝까지 한 쌍씩 바꾼다.
    [SerializeField, Min(0)] private int _doubleSwapFrom = 3;
    // 서로 자리를 바꾸는 두 액자가 겹치지 않게, 바꾸는 길을 옆으로 휘는 폭.
    [SerializeField, Min(0f)] private float _swapArc = 1.5f;
    [SerializeField] private ImpactSettings _swapImpact = new();

    [Header("서명 반짝임")]
    [SerializeField] private GameObject _glintPrefab;
    [SerializeField] private Color _glintColor = new Color(1f, 0.82f, 0.3f, 1f);
    [SerializeField, Min(0.05f)] private float _glintSize = 0.4f;
    [SerializeField, Min(0f)] private float _glintInset = 0.5f;
    [SerializeField, Min(0.05f)] private float _glintInterval = 1.2f;
    [SerializeField, Min(0.01f)] private float _glintDuration = 0.25f;

    [Header("고르기")]
    // 이 시간 동안 액자 안쪽이 차오르고, 다 차는 순간 서 있는 액자로 정한다.
    [SerializeField, Min(0.1f)] private float _chooseDuration = 3f;
    [SerializeField] private Color _selectColor = new Color(0.95f, 0.72f, 0.2f, 1f);

    [Header("결과")]
    [SerializeField, Min(0.01f)] private float _revealDuration = 0.35f;
    [SerializeField] private ImpactSettings _revealImpact = new();
    // 고른 액자가 가짜면 가짜 액자마다 터진다.
    [SerializeField] private ImpactSettings _counterImpact = new();
    [SerializeField, Min(0f)] private float _resultHold = 0.6f;
    [SerializeField, Min(0f)] private float _fadeDuration = 0.4f;

    public override bool PausesTargetAttack { get { return true; } }

    public override void Prewarm()
    {
        if (null != _framePrefab)
        {
            GameManager.Pool.Prewarm(_framePrefab, FRAME_COUNT * GimmickFrame.SPRITE_COUNT);
        }

        if (null != _glintPrefab)
        {
            GameManager.Pool.Prewarm(_glintPrefab, 1);
        }
    }

    public override async UniTask<bool> PlayAsync(AttackContext context, CancellationToken token)
    {
        if (null == _framePrefab)
        {
            Debug.LogError($"원본 찾기 : {name}에 액자 그림이 지정되지 않았습니다.");
            return true;
        }

        Transform root = context.Owner.transform;
        Transform body = context.Owner.Body;
        Vector3 bodyScale = body.localScale;
        List<GimmickFrame> frames = new List<GimmickFrame>(FRAME_COUNT);
        SpriteRenderer glint = null;

        try
        {
            List<Vector3> slots = BuildSlots(context.Target);
            Color hidden = new Color(_edgeColor.r, _edgeColor.g, _edgeColor.b, 0f);

            GameManager.CameraRig.ShowArea(GetSlotArea(slots), _cameraPadding, _cameraBlendDuration);

            for (int i = 0; i < FRAME_COUNT; i++)
            {
                GimmickFrame frame = new GimmickFrame(_framePrefab, _frameSize, _edgeWidth, hidden, _fillColor);
                frame.Place(slots[i]);
                frame.SetFill(0f);
                frames.Add(frame);
            }

            List<UniTask> fades = new List<UniTask>(FRAME_COUNT * GimmickFrame.SPRITE_COUNT);

            for (int i = 0; i < frames.Count; i++)
            {
                frames[i].AddFades(_edgeColor.a, _fillColor.a, _appearDuration, fades, token);
            }

            await UniTask.WhenAll(fades);

            int real = UnityEngine.Random.Range(0, FRAME_COUNT);

            if (null != _glintPrefab)
            {
                glint = PooledSprite.Spawn(_glintPrefab, frames[real].GetSignaturePoint(_glintInset), 0f, Vector2.zero, _glintColor);
            }

            await HideBossAsync(root, body, frames[real], token);
            await UniTask.Delay(TimeSpan.FromSeconds(_shuffleDelay), cancellationToken: token);

            float clock = await ShuffleAsync(frames, slots, real, glint, token);

            int chosen = await ChooseAsync(context.Target, frames, real, glint, clock, token);
            bool isFound = chosen == real;

            await RevealAsync(context, root, body, bodyScale, frames, real, isFound, token);

            await UniTask.Delay(TimeSpan.FromSeconds(_resultHold), cancellationToken: token);

            fades.Clear();

            for (int i = 0; i < frames.Count; i++)
            {
                frames[i].AddFades(0f, 0f, _fadeDuration, fades, token);
            }

            await UniTask.WhenAll(fades);

            return isFound;
        }
        finally
        {
            GameManager.CameraRig.EndShowArea(_cameraBlendDuration);
            body.localScale = bodyScale;

            if (null != context.Owner.Mover)
            {
                root.position = context.Owner.Mover.ClampPosition(root.position);
            }

            for (int i = 0; i < frames.Count; i++)
            {
                frames[i].Despawn();
            }

            PooledSprite.Despawn(glint);
        }
    }

    // 플레이어를 가운데 둔 네 귀퉁이. 판 가장자리에서는 플레이어 이동 범위 안으로 당긴다.
    private List<Vector3> BuildSlots(AttackTarget target)
    {
        Vector3 center = target.FollowTransform.position;
        UnitMover mover = target.Unit.Mover;
        List<Vector3> slots = new List<Vector3>(FRAME_COUNT);

        for (int i = 0; i < FRAME_COUNT; i++)
        {
            float x = 0 == i % 2 ? -_slotOffset.x : _slotOffset.x;
            float y = 2 > i ? _slotOffset.y : -_slotOffset.y;
            Vector3 slot = center + new Vector3(x, y, 0f);

            slots.Add(null != mover ? mover.ClampPosition(slot) : slot);
        }

        return slots;
    }

    // 네 액자를 다 담는 영역. 섞을 때 옆으로 휘어 바깥쪽으로 나가는 폭까지 넣는다.
    private Bounds GetSlotArea(List<Vector3> slots)
    {
        Bounds area = new Bounds(slots[0], Vector3.zero);

        for (int i = 1; i < slots.Count; i++)
        {
            area.Encapsulate(slots[i]);
        }

        area.Expand(new Vector3(_frameSize.x + _swapArc * 2f, _frameSize.y + _swapArc * 2f, 0f));

        return area;
    }

    private async UniTask HideBossAsync(Transform root, Transform body, GimmickFrame frame, CancellationToken token)
    {
        Vector3 target = new Vector3(frame.Center.x, frame.Center.y, root.position.z);

        UniTask move = root.DOMove(target, _hideDuration)
            .SetEase(Ease.InOutQuad)
            .ToUniTaskKillOnCancel(token);

        UniTask shrink = body.DOScale(Vector3.zero, _hideDuration)
            .SetEase(Ease.InBack)
            .ToUniTaskKillOnCancel(token);

        await UniTask.WhenAll(move, shrink);
    }

    // 매번 겹치지 않는 액자 쌍을 골라 자리를 바꾼다. 갈수록 빨라지고, 정해진 차례부터는 두 쌍을 한꺼번에 바꾼다.
    // 섞은 시간을 돌려줘 반짝임 박자를 이어 가게 한다. 소리·흔들림은 네 자리의 가운데에서 내 진짜 자리를 알려 주지 않는다.
    // 숨은 보스는 진짜 액자를 따라 옮기지 않는다. 전투 카메라가 보스를 따라 움직여 답을 알려 주기 때문이다.
    private async UniTask<float> ShuffleAsync(List<GimmickFrame> frames, List<Vector3> slots, int real, SpriteRenderer glint, CancellationToken token)
    {
        int[] slotOfFrame = { 0, 1, 2, 3 };
        int[] order = { 0, 1, 2, 3 };
        Vector3[] from = new Vector3[FRAME_COUNT];
        Vector3 middle = (slots[0] + slots[1] + slots[2] + slots[3]) * 0.25f;
        float clock = 0f;

        for (int swap = 0; swap < _swapCount; swap++)
        {
            Shuffle(order);

            bool isDouble = swap >= _doubleSwapFrom;
            int movingCount = isDouble ? FRAME_COUNT : 2;
            float duration = Mathf.Lerp(_firstSwapDuration, _lastSwapDuration, 1 < _swapCount ? swap / (float)(_swapCount - 1) : 1f);

            for (int i = 0; i < movingCount; i += 2)
            {
                int a = order[i];
                int b = order[i + 1];

                (slotOfFrame[a], slotOfFrame[b]) = (slotOfFrame[b], slotOfFrame[a]);
            }

            for (int i = 0; i < FRAME_COUNT; i++)
            {
                from[i] = frames[i].Center;
            }

            ImpactPlayer.Play(_swapImpact, middle);

            float elapsed = 0f;

            while (true)
            {
                float t = DOVirtual.EasedValue(0f, 1f, elapsed / duration, Ease.InOutQuad);

                for (int i = 0; i < movingCount; i++)
                {
                    int index = order[i];
                    Vector3 to = slots[slotOfFrame[index]];
                    Vector3 path = to - from[index];
                    // 자기 길의 왼쪽으로 휜다. 짝을 이룬 두 액자는 서로 반대로 가므로 왼쪽도 반대가 되어 서로 비켜 간다.
                    Vector3 bend = new Vector3(-path.y, path.x, 0f).normalized * (Mathf.Sin(t * Mathf.PI) * _swapArc);

                    frames[index].Place(Vector3.Lerp(from[index], to, t) + bend);
                }

                UpdateGlint(glint, frames[real], clock);

                if (elapsed >= duration)
                {
                    break;
                }

                await UniTask.Yield(PlayerLoopTiming.Update, token);
                elapsed = Mathf.Min(elapsed + Time.deltaTime, duration);
                clock += Time.deltaTime;
            }
        }

        return clock;
    }

    // 액자 안쪽이 다 차는 순간 플레이어가 서 있는 액자 번호를 돌려준다. 어느 액자에도 없으면 -1이다.
    // 지금 서 있는 액자의 테두리는 다른 색으로 칠해, 어느 액자를 고른 셈인지 보이게 한다.
    private async UniTask<int> ChooseAsync(AttackTarget target, List<GimmickFrame> frames, int real, SpriteRenderer glint, float clock,
        CancellationToken token)
    {
        float elapsed = 0f;
        int chosen = -1;

        while (true)
        {
            chosen = FindFrame(frames, target.FollowTransform.position);

            for (int i = 0; i < frames.Count; i++)
            {
                frames[i].SetEdgeColor(i == chosen ? _selectColor : _edgeColor);
                frames[i].SetFill(elapsed / _chooseDuration);
            }

            UpdateGlint(glint, frames[real], clock);

            if (elapsed >= _chooseDuration)
            {
                return chosen;
            }

            await UniTask.Yield(PlayerLoopTiming.Update, token);
            elapsed = Mathf.Min(elapsed + Time.deltaTime, _chooseDuration);
            clock += Time.deltaTime;
        }
    }

    // 진짜 액자에서 보스가 다시 커져 나온다. 가짜를 골랐으면 가짜 액자들이 터지며 반격한다.
    private async UniTask RevealAsync(AttackContext context, Transform root, Transform body, Vector3 bodyScale, List<GimmickFrame> frames, int real,
        bool isFound, CancellationToken token)
    {
        FollowFrame(root, frames[real]);
        ImpactPlayer.Play(_revealImpact, frames[real].Center);

        if (false == isFound)
        {
            for (int i = 0; i < frames.Count; i++)
            {
                if (i != real)
                {
                    ImpactPlayer.Play(_counterImpact, frames[i].Center);
                }
            }

            PunishTarget(context);
        }

        await body.DOScale(bodyScale, _revealDuration)
            .SetEase(Ease.OutBack)
            .ToUniTaskKillOnCancel(token);
    }

    private static void FollowFrame(Transform root, GimmickFrame frame)
    {
        root.position = new Vector3(frame.Center.x, frame.Center.y, root.position.z);
    }

    private static int FindFrame(List<GimmickFrame> frames, Vector3 position)
    {
        for (int i = 0; i < frames.Count; i++)
        {
            if (frames[i].Contains(position))
            {
                return i;
            }
        }

        return -1;
    }

    // 정해진 간격마다 잠깐 커졌다 작아지며 반짝인다.
    private void UpdateGlint(SpriteRenderer glint, GimmickFrame frame, float clock)
    {
        if (null == glint)
        {
            return;
        }

        float phase = clock % _glintInterval;
        float pulse = phase < _glintDuration ? Mathf.Sin(phase / _glintDuration * Mathf.PI) : 0f;
        float size = _glintSize * pulse;

        glint.transform.position = frame.GetSignaturePoint(_glintInset);
        glint.transform.localScale = new Vector3(size, size, 1f);
    }

    private static void Shuffle(int[] values)
    {
        for (int i = values.Length - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);

            (values[i], values[j]) = (values[j], values[i]);
        }
    }
}
