using DG.Tweening;
using System;
using UnityEngine;

[Serializable]
public class ResultTitleSettings
{
    [SerializeField] private string _text = "STAGE CLEAR";
    [SerializeField] private PaintPalette _letterPalette;
    // 공백 글자는 TMP가 폭을 0으로 계산해 레이아웃에서 사라지므로, 띄어쓰기 폭을 따로 정한다.
    [SerializeField, Min(0f)] private float _spaceWidth = 70f;
    [SerializeField, Min(0f)] private float _delayBefore = 0.3f;
    [SerializeField, Min(0f)] private float _letterInterval = 0.08f;
    [SerializeField, Min(0f)] private float _letterPopDuration = 0.3f;
    [SerializeField] private Ease _letterEase = Ease.OutBack;
    [SerializeField] private AudioClip _letterSound;
    [SerializeField] private AudioClip _finishSound;
    [SerializeField, Min(0f)] private float _finishPunch = 0.15f;
    // 문구와 버튼 뒤에 까는 종이색 띠. 검은 잉크 위에서도 글자가 보이게 한다. 투명도가 0이면 띠를 깔지 않는다.
    [SerializeField] private Color _backdropColor = new Color(0.96f, 0.94f, 0.9f, 0f);

    public string Text { get { return _text; } }
    public PaintPalette LetterPalette { get { return _letterPalette; } }
    public float SpaceWidth { get { return _spaceWidth; } }
    public float DelayBefore { get { return _delayBefore; } }
    public float LetterInterval { get { return _letterInterval; } }
    public float LetterPopDuration { get { return _letterPopDuration; } }
    public Ease LetterEase { get { return _letterEase; } }
    public AudioClip LetterSound { get { return _letterSound; } }
    public AudioClip FinishSound { get { return _finishSound; } }
    public float FinishPunch { get { return _finishPunch; } }
    public Color BackdropColor { get { return _backdropColor; } }
}
