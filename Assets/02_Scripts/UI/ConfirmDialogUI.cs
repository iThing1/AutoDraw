using Cysharp.Threading.Tasks;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 되돌릴 수 없는 선택 전에 묻는 창. 닫는 것은 연 쪽이 한다.
public class ConfirmDialogUI : MonoBehaviour
{
    [SerializeField] private TMP_Text _message;
    [SerializeField] private Button _confirmButton;
    [SerializeField] private Button _cancelButton;

    public async UniTask<bool> AskAsync(string message, CancellationToken token)
    {
        _message.text = message;

        // 먼저 눌린 버튼의 순서 번호다. 0은 확인, 1은 취소다.
        int choiceIndex = await UniTask.WhenAny(_confirmButton.OnClickAsync(token), _cancelButton.OnClickAsync(token));

        return 0 == choiceIndex;
    }
}
