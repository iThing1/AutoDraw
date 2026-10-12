using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class BossIntroTrigger : MonoBehaviour
{
    [SerializeField] private string _playerTag = "Player";

    private UniTaskCompletionSource _enterSource = new();
    private bool _isTriggered;

    public UniTask WaitForEnterAsync(CancellationToken token)
    {
        return _enterSource.Task.AttachExternalCancellation(token);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_isTriggered)
        {
            return;
        }

        if (false == other.CompareTag(_playerTag))
        {
            return;
        }

        _isTriggered = true;
        _enterSource.TrySetResult();
    }
}
