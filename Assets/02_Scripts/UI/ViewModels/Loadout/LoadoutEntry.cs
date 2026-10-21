using UnityEngine;
using UnityEngine.Video;

// 장비 화면 버튼 하나에 그릴 값. 표의 한 줄(또는 막기·없음처럼 표에 없는 항목)을 화면용으로 옮겨 담는다.
public class LoadoutEntry
{
    private readonly string _id;
    private readonly string _label;
    private readonly string _description;
    private readonly Sprite _icon;
    private readonly Color _iconColor;
    private readonly VideoClip _preview;

    // 칸마다 뜻이 다르다. 무기·스킬·소모품은 표 Id, 유틸리티는 종류 이름, 소모품 없음은 빈 값이다.
    public string Id { get { return _id; } }
    public string Label { get { return _label; } }
    public string Description { get { return _description; } }
    // 아이콘 그림이 아직 없으면 null이고, 화면은 색 타일로 대신 그린다.
    public Sprite Icon { get { return _icon; } }
    public Color IconColor { get { return _iconColor; } }
    // 공격 예시 영상. 아직 녹화하지 않았으면 null이다.
    public VideoClip Preview { get { return _preview; } }

    public LoadoutEntry(string id, string label, string description, Sprite icon, Color iconColor, VideoClip preview)
    {
        _id = id;
        _label = label;
        _description = description;
        _icon = icon;
        _iconColor = iconColor;
        _preview = preview;
    }
}
