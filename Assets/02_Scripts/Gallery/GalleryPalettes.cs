using UnityEngine;

// 갤러리에 놓인 팔레트들. 주운 것은 숨긴다.
public class GalleryPalettes
{
    private readonly PaletteItem[] _palettes;

    public GalleryPalettes(PaletteItem[] palettes)
    {
        _palettes = palettes;
        CheckIds();
    }

    public void Refresh(PlayerProgress progress)
    {
        for (int i = 0; i < _palettes.Length; i++)
        {
            _palettes[i].gameObject.SetActive(false == progress.HasPalette(_palettes[i].PaletteId));
        }
    }

    // ID가 비면 주워도 기록되지 않고, 겹치면 하나를 주울 때 함께 사라진다.
    private void CheckIds()
    {
        for (int i = 0; i < _palettes.Length; i++)
        {
            string paletteId = _palettes[i].PaletteId;

            if (string.IsNullOrEmpty(paletteId))
            {
                Debug.LogError($"갤러리 : 팔레트 {_palettes[i].name}에 ID가 없습니다.");
                continue;
            }

            for (int j = 0; j < i; j++)
            {
                if (paletteId == _palettes[j].PaletteId)
                {
                    Debug.LogError($"갤러리 : 팔레트 ID {paletteId}이(가) 겹칩니다.");
                }
            }
        }
    }
}
