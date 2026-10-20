using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.Video;

// 갤러리 보관대에서 여는 장비 화면. 연결할 때 칸 줄을 한 번 만들고, 그 뒤로는 바뀐 버튼과 설명·예시 영상만 다시 그린다.
public class LoadoutView : ViewBase<LoadoutViewModel>
{
    [SerializeField] private LoadoutSlotView _slotPrefab;
    [SerializeField] private Transform _slotRoot;
    // 칸과 같은 순서로 넣는다(Basic, Heavy, Skill, Utility, Item). 키가 없는 칸은 비워 둔다.
    [SerializeField] private InputActionReference[] _slotKeys;
    [SerializeField] private TMP_Text _description;
    [SerializeField] private Button _closeButton;

    [Header("예시 영상")]
    [SerializeField] private RawImage _preview;
    [SerializeField] private Vector2Int _previewSize = new Vector2Int(384, 216);

    private readonly List<LoadoutSlotView> _slotViews = new();
    private VideoPlayer _videoPlayer;
    private RenderTexture _previewTexture;

    private void Awake()
    {
        _closeButton.onClick.AddListener(HandleCloseClicked);

        _previewTexture = new RenderTexture(_previewSize.x, _previewSize.y, 0);
        _preview.texture = _previewTexture;

        _videoPlayer = _preview.gameObject.GetOrAddComponent<VideoPlayer>();
        _videoPlayer.playOnAwake = false;
        _videoPlayer.isLooping = true;
        _videoPlayer.audioOutputMode = VideoAudioOutputMode.None;
        _videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        _videoPlayer.targetTexture = _previewTexture;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        _previewTexture.Release();
        Destroy(_previewTexture);
    }

    // 키 이름은 화면에만 쓰는 값이라 ViewModel 대신 여기서 입력 에셋을 읽는다.
    protected override void OnBind()
    {
        IReadOnlyList<LoadoutSlotViewModel> slots = ViewModel.Slots;

        for (int i = 0; i < slots.Count; i++)
        {
            LoadoutSlotView slotView = Instantiate(_slotPrefab, _slotRoot);
            slotView.SetKeyLabel(GetKeyLabel(i));
            slotView.Bind(slots[i]);
            _slotViews.Add(slotView);
        }
    }

    // 다른 ViewModel을 다시 꽂을 때 옛 줄이 남지 않게 지운다. 화면째 지워질 때는 줄이 먼저 지워졌을 수 있다.
    protected override void OnUnbind()
    {
        for (int i = 0; i < _slotViews.Count; i++)
        {
            if (null != _slotViews[i])
            {
                Destroy(_slotViews[i].gameObject);
            }
        }

        _slotViews.Clear();
    }

    protected override void OnPropertyChanged(string propertyName)
    {
        switch (propertyName)
        {
            case nameof(LoadoutViewModel.Description):
                DrawDescription();
                break;

            case nameof(LoadoutViewModel.Preview):
                DrawPreview();
                break;
        }
    }

    protected override void RefreshAll()
    {
        DrawDescription();
        DrawPreview();
    }

    private string GetKeyLabel(int index)
    {
        return null != _slotKeys && index < _slotKeys.Length ? _slotKeys[index].GetKeyLabel() : string.Empty;
    }

    private void DrawDescription()
    {
        _description.text = ViewModel.Description;
    }

    // 영상이 없는 장비는 미리보기 칸을 숨긴다.
    private void DrawPreview()
    {
        VideoClip clip = ViewModel.Preview;

        if (null == clip)
        {
            _videoPlayer.Stop();
            _preview.gameObject.SetActive(false);
            return;
        }

        _preview.gameObject.SetActive(true);
        _videoPlayer.clip = clip;
        _videoPlayer.Play();
    }

    private void HandleCloseClicked()
    {
        if (null != ViewModel)
        {
            ViewModel.RequestClose();
        }
    }
}
