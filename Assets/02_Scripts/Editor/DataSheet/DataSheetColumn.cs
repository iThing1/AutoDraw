// 시트의 한 열. 3줄의 필드 이름과 2줄의 형식을 묶는다.
public readonly struct DataSheetColumn
{
    public readonly string Field;
    public readonly int Index;
    public readonly DataValueKind Kind;
    public readonly bool IsArray;

    public DataSheetColumn(string field, int index, DataValueKind kind, bool isArray)
    {
        Field = field;
        Index = index;
        Kind = kind;
        IsArray = isArray;
    }
}
