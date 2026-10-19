using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

// Resources/JsonOutput에서 데이터 표를 읽는다. 가져오기 도구(DataSheetConfig)의 출력 폴더와 맞춰야 한다.
public class ResourcesTableLoader : IDataTableLoader
{
    private const string FOLDER = "JsonOutput/";

    public async UniTask<string> LoadTextAsync(string tableName, CancellationToken token)
    {
        TextAsset asset = await Resources.LoadAsync<TextAsset>(FOLDER + tableName).WithCancellation(token) as TextAsset;

        if (null == asset)
        {
            return null;
        }

        string text = asset.text;
        Resources.UnloadAsset(asset);

        return text;
    }
}
