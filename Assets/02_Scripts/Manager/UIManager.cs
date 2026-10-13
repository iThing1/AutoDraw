using UnityEngine;

public class UIManager
{
    private const int LAYER_SORTING_STEP = 10;

    private Transform _root;

    private CinematicUI _cinematic;

    public CinematicUI Cinematic { get { return _cinematic; } }

    public void Init(CinematicUI cinematicPrefab)
    {
        _root = new GameObject("UIRoot").transform;

        _cinematic = Open(cinematicPrefab, UILayer.Content);
    }

    // UI 프리팹은 각자 Canvas를 가지고, 정렬 순서는 프리팹 값 대신 레이어 기준으로 덮어쓴다.
    public T Open<T>(T prefab, UILayer layer) where T : Component
    {
        if (null == prefab)
        {
            Debug.LogError($"UI 매니저 : {layer} 레이어에 열 프리팹이 지정되지 않았습니다.");
            return null;
        }

        T ui = Object.Instantiate(prefab, _root);
        ui.name = prefab.name;

        Canvas canvas = ui.GetComponent<Canvas>();

        if (null != canvas)
        {
            canvas.sortingOrder = (int)layer * LAYER_SORTING_STEP;
        }

        return ui;
    }
}
