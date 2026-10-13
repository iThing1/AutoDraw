using Cysharp.Threading.Tasks;
using System;
using UnityEngine;

public class PooledEffect : MonoBehaviour
{
    private const string EFFECT_SORTING_LAYER = "Effect";

    // Cartoon FX는 git에서 제외된 서드파티라 타입을 직접 참조하면 저장소만 받은 쪽에서 컴파일이 깨진다.
    // 이 컴포넌트는 재생이 끝나면 오브젝트를 파괴하므로, 이름으로 찾아 끄고 수명은 풀이 관리한다.
    private const string AUTO_DESTROY_COMPONENT_NAME = "CFXR_Effect";

    private ParticleSystem[] _particles;

    private void Awake()
    {
        _particles = GetComponentsInChildren<ParticleSystem>(true);

        for (int i = 0; i < _particles.Length; i++)
        {
            ParticleSystem.MainModule main = _particles[i].main;

            // 기본값(Local)은 부모 크기를 무시하므로, 루트 크기가 하위 파티클까지 적용되게 바꾼다.
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;

            ParticleSystemRenderer particleRenderer = _particles[i].GetComponent<ParticleSystemRenderer>();

            if (null != particleRenderer)
            {
                particleRenderer.sortingLayerName = EFFECT_SORTING_LAYER;
            }
        }

        Behaviour autoDestroy = GetComponent(AUTO_DESTROY_COMPONENT_NAME) as Behaviour;

        if (null != autoDestroy)
        {
            autoDestroy.enabled = false;
        }
    }

    public void Play(Color color, float scale, float lifetime)
    {
        transform.localScale = Vector3.one * scale;

        for (int i = 0; i < _particles.Length; i++)
        {
            ParticleSystem.MainModule main = _particles[i].main;
            main.startColor = color;

            _particles[i].Clear(false);
            _particles[i].Play(false);
        }

        ReturnAfterAsync(lifetime).Forget();
    }

    // 히트 스톱 중에는 파티클도 멈추므로, 반환 타이머도 게임 시간으로 재야 이펙트가 중간에 잘리지 않는다.
    private async UniTaskVoid ReturnAfterAsync(float lifetime)
    {
        bool isCanceled = await UniTask.Delay(TimeSpan.FromSeconds(lifetime), cancellationToken: destroyCancellationToken)
            .SuppressCancellationThrow();

        if (isCanceled)
        {
            return;
        }

        GameManager.Pool.Despawn(gameObject);
    }
}
