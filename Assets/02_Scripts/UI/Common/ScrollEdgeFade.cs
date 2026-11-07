using UnityEngine;
using UnityEngine.UI;

// 세로 스크롤 영역의 위·아래 끝에, 그쪽으로 더 볼 것이 남았을 때만 흐린 띠를 깐다. 띠는 바탕색에서 투명으로 옅어져 목록이 창 속으로 스며드는 것처럼 보인다.
// 띠 그림은 처음 쓸 때 한 번 만들어 모든 스크롤 영역이 함께 쓴다. 화면 표시라 게임 시간 배율과 상관없이 실제 시간으로 옅어진다.
[RequireComponent(typeof(ScrollRect))]
public class ScrollEdgeFade : MonoBehaviour
{
    private const int GRADIENT_HEIGHT = 32;
    // 끝에서 이만큼(정규화 위치) 안쪽이면 끝에 닿은 것으로 본다.
    private const float EDGE_THRESHOLD = 0.001f;

    private static Sprite s_gradient;

    // 위 띠는 그대로, 아래 띠는 위아래를 뒤집어(스케일 y = -1) 둔다. 둘 다 클릭을 가로막지 않게 Raycast Target을 끈다.
    [SerializeField] private Image _topFade;
    [SerializeField] private Image _bottomFade;
    [SerializeField] private Color _color = new Color(0.1f, 0.1f, 0.12f, 1f);
    [SerializeField, Min(0.1f)] private float _fadeSpeed = 8f;

    private ScrollRect _scrollRect;

    private void Awake()
    {
        _scrollRect = GetComponent<ScrollRect>();

        if (null == s_gradient)
        {
            s_gradient = CreateGradient();
        }

        Setup(_topFade);
        Setup(_bottomFade);
    }

    // 목록이 영역보다 길 때만 흐린 띠를 보인다. 맨 위에 있으면 위 띠를, 맨 아래에 있으면 아래 띠를 걷는다.
    private void LateUpdate()
    {
        RectTransform content = _scrollRect.content;
        RectTransform viewport = null != _scrollRect.viewport ? _scrollRect.viewport : (RectTransform)transform;
        bool canScroll = null != content && content.rect.height > viewport.rect.height;
        float position = _scrollRect.verticalNormalizedPosition;

        Fade(_topFade, canScroll && position < 1f - EDGE_THRESHOLD);
        Fade(_bottomFade, canScroll && position > EDGE_THRESHOLD);
    }

    private void Setup(Image fade)
    {
        if (null == fade)
        {
            return;
        }

        fade.sprite = s_gradient;
        fade.raycastTarget = false;
        fade.color = new Color(_color.r, _color.g, _color.b, 0f);
    }

    private void Fade(Image fade, bool isVisible)
    {
        if (null == fade)
        {
            return;
        }

        Color color = fade.color;
        float alpha = Mathf.MoveTowards(color.a, isVisible ? _color.a : 0f, _fadeSpeed * Time.unscaledDeltaTime);

        if (false == Mathf.Approximately(alpha, color.a))
        {
            fade.color = new Color(_color.r, _color.g, _color.b, alpha);
        }
    }

    // 위가 진하고 아래로 갈수록 투명해지는 세로 띠. 색은 Image 색으로 입히므로 흰색으로 만든다.
    private static Sprite CreateGradient()
    {
        Texture2D texture = new Texture2D(1, GRADIENT_HEIGHT, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        for (int y = 0; y < GRADIENT_HEIGHT; y++)
        {
            float alpha = y / (float)(GRADIENT_HEIGHT - 1);
            texture.SetPixel(0, y, new Color(1f, 1f, 1f, alpha * alpha));
        }

        texture.Apply();

        return Sprite.Create(texture, new Rect(0f, 0f, 1f, GRADIENT_HEIGHT), new Vector2(0.5f, 0.5f));
    }
}
