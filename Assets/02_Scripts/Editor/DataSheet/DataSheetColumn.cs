using System;

public readonly struct DataSheetColumn
{
    public readonly string Field;
    public readonly int Index;
    public readonly DataValueKind Kind;
    public readonly bool IsArray;
    public readonly Type EnumType;

    public DataSheetColumn(string field, int index, DataValueKind kind, bool isArray, Type enumType)
    {
        Field = field;
        Index = index;
        Kind = kind;
        IsArray = isArray;
        EnumType = enumType;
    }
}
