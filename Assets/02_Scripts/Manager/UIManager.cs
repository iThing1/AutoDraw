using UnityEngine;

// UI를 언제 열고 닫을지는 각 화면을 가진 쪽(전투 매니저 등)이 정하고, 이 클래스는 생성·레이어 정렬·파괴만 맡는다.
public class UIManager
{
    private const int LAYER_SORTING_STEP = 10;

    private Transform _root;

    private CinematicUI _cinematic;

    // 레터박스·배너는 등장 컷신과 엔딩이 함께 쓰므로 공용으로 미리 열어 둔다.
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

    public void Close(Component ui)
    {
        if (null == ui)
        {
            return;
        }

        Object.Destroy(ui.gameObject);
    }
}
