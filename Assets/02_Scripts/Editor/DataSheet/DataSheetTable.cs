using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

// 1줄 설명, 2줄 형식, 3줄 필드 이름, 4줄부터 값. 필드 이름이 빈 열은 메모로 보고 건너뛴다.
// 2줄이 기본 형식이 아니면 enum 이름으로 보고, 칸의 이름을 번호로 바꿔 쓴다(JsonUtility가 enum을 번호로 읽는다).
public class DataSheetTable
{
    public const string ID_FIELD = "Id";

    private const int TYPE_LINE = 1;
    private const int FIELD_LINE = 2;
    private const int FIRST_VALUE_LINE = 3;
    private const char ARRAY_SEPARATOR = '|';
    private const string ARRAY_SUFFIX = "[]";

    private readonly List<DataSheetColumn> _columns = new();
    private readonly List<string[]> _rows = new();
    private readonly List<int> _lineNumbers = new();
    private readonly HashSet<string> _ids = new();
    private readonly string _name;

    public string Name { get { return _name; } }
    public int RowCount { get { return _rows.Count; } }

    private DataSheetTable(string name)
    {
        _name = name;
    }

    public static DataSheetTable Parse(string name, List<string[]> lines, Func<string, Type> findEnum, List<string> errors)
    {
        if (lines.Count < FIRST_VALUE_LINE)
        {
            errors.Add($"{name} : 머리 줄 3개(설명·형식·필드 이름)가 없습니다.");
            return null;
        }

        DataSheetTable table = new(name);

        if (false == table.ReadColumns(lines[TYPE_LINE], lines[FIELD_LINE], findEnum, errors))
        {
            return null;
        }

        for (int i = FIRST_VALUE_LINE; i < lines.Count; i++)
        {
            table.ReadRow(lines[i], i + 1, errors);
        }

        return table;
    }

    public bool HasField(string field)
    {
        return 0 <= FindColumn(field);
    }

    public bool ContainsId(string id)
    {
        return _ids.Contains(id);
    }

    public int GetLineNumber(int row)
    {
        return _lineNumbers[row];
    }

    public string GetId(int row)
    {
        return GetCell(row, ID_FIELD);
    }

    public string GetCell(int row, string field)
    {
        int column = FindColumn(field);

        if (column < 0)
        {
            return string.Empty;
        }

        return GetCellText(_rows[row], _columns[column].Index);
    }

    public string[] GetArray(int row, string field)
    {
        return SplitArray(GetCell(row, field));
    }

    // JsonUtility로 바로 읽도록 {"_items":[...]} 모양으로, 필드는 private 이름(MaxHp → _maxHp)으로 쓴다.
    public string ToJson(List<string> errors)
    {
        StringBuilder json = new();
        json.Append("{\"_items\":[");

        for (int row = 0; row < _rows.Count; row++)
        {
            json.Append(0 == row ? "\n{" : ",\n{");

            for (int i = 0; i < _columns.Count; i++)
            {
                DataSheetColumn column = _columns[i];

                if (0 < i)
                {
                    json.Append(',');
                }

                json.Append('"').Append(ToFieldName(column.Field)).Append("\":");
                AppendValue(json, column, row, errors);
            }

            json.Append('}');
        }

        json.Append("\n]}\n");
        return json.ToString();
    }

    private bool ReadColumns(string[] types, string[] fields, Func<string, Type> findEnum, List<string> errors)
    {
        int errorCount = errors.Count;

        for (int i = 0; i < fields.Length; i++)
        {
            string field = fields[i].Trim();

            if (0 == field.Length)
            {
                continue;
            }

            if (0 <= FindColumn(field))
            {
                errors.Add($"{Name} : 필드 이름 {field}이(가) 두 번 있습니다.");
                continue;
            }

            string type = GetCellText(types, i);
            bool isArray = type.EndsWith(ARRAY_SUFFIX, StringComparison.Ordinal);
            string kindName = isArray ? type.Substring(0, type.Length - ARRAY_SUFFIX.Length) : type;

            Type enumType = null;

            if (false == TryParseKind(kindName, out DataValueKind kind))
            {
                enumType = findEnum(kindName);

                if (null == enumType)
                {
                    errors.Add($"{Name} : {field} 열의 형식 '{type}'을(를) 모릅니다. string·int·float·bool, 게임 코드의 enum 이름과 그 배열([])만 됩니다.");
                    continue;
                }

                kind = DataValueKind.Enum;
            }

            _columns.Add(new DataSheetColumn(field, i, kind, isArray, enumType));
        }

        if (0 == _columns.Count || ID_FIELD != _columns[0].Field)
        {
            errors.Add($"{Name} : 첫 열의 필드 이름이 {ID_FIELD}가 아닙니다.");
        }

        return errorCount == errors.Count;
    }

    private void ReadRow(string[] cells, int lineNumber, List<string> errors)
    {
        if (IsEmptyRow(cells))
        {
            return;
        }

        string id = GetCellText(cells, _columns[0].Index);

        if (0 == id.Length)
        {
            errors.Add($"{Name} {lineNumber}줄 : Id가 비어 있습니다.");
            return;
        }

        if (false == _ids.Add(id))
        {
            errors.Add($"{Name} {lineNumber}줄 : Id {id}이(가) 겹칩니다.");
            return;
        }

        _rows.Add(cells);
        _lineNumbers.Add(lineNumber);
    }

    private void AppendValue(StringBuilder json, DataSheetColumn column, int row, List<string> errors)
    {
        string text = GetCellText(_rows[row], column.Index);

        if (false == column.IsArray)
        {
            AppendScalar(json, column, text, row, errors);
            return;
        }

        string[] items = SplitArray(text);
        json.Append('[');

        for (int i = 0; i < items.Length; i++)
        {
            if (0 < i)
            {
                json.Append(',');
            }

            AppendScalar(json, column, items[i], row, errors);
        }

        json.Append(']');
    }

    // 빈 숫자 칸은 0, 빈 bool 칸은 false다.
    private void AppendScalar(StringBuilder json, DataSheetColumn column, string text, int row, List<string> errors)
    {
        switch (column.Kind)
        {
            case DataValueKind.String:
                AppendString(json, text);
                return;

            case DataValueKind.Int:
                if (0 == text.Length)
                {
                    json.Append('0');
                }
                else if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int intValue))
                {
                    json.Append(intValue.ToString(CultureInfo.InvariantCulture));
                }
                else
                {
                    AddValueError(column, text, row, errors);
                    json.Append('0');
                }
                return;

            case DataValueKind.Float:
                if (0 == text.Length)
                {
                    json.Append('0');
                }
                else if (float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float floatValue) && float.IsFinite(floatValue))
                {
                    json.Append(floatValue.ToString("R", CultureInfo.InvariantCulture));
                }
                else
                {
                    AddValueError(column, text, row, errors);
                    json.Append('0');
                }
                return;

            case DataValueKind.Bool:
                if (0 == text.Length)
                {
                    json.Append("false");
                }
                else if (bool.TryParse(text, out bool boolValue))
                {
                    json.Append(boolValue ? "true" : "false");
                }
                else
                {
                    AddValueError(column, text, row, errors);
                    json.Append("false");
                }
                return;

            // 빈 칸과 번호로 적은 칸은 받지 않는다.
            case DataValueKind.Enum:
                if (0 <= Array.IndexOf(Enum.GetNames(column.EnumType), text))
                {
                    json.Append(Convert.ToInt32(Enum.Parse(column.EnumType, text), CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture));
                }
                else
                {
                    AddValueError(column, text, row, errors);
                    json.Append('0');
                }
                return;
        }
    }

    private void AddValueError(DataSheetColumn column, string text, int row, List<string> errors)
    {
        string kindName = DataValueKind.Enum == column.Kind ? column.EnumType.Name : column.Kind.ToString().ToLowerInvariant();
        errors.Add($"{Name} {_lineNumbers[row]}줄 {column.Field} : '{text}'은(는) {kindName}이(가) 아닙니다.");
    }

    private int FindColumn(string field)
    {
        for (int i = 0; i < _columns.Count; i++)
        {
            if (field == _columns[i].Field)
            {
                return i;
            }
        }

        return -1;
    }

    private static bool TryParseKind(string kindName, out DataValueKind kind)
    {
        return Enum.TryParse(kindName, true, out kind) && Enum.IsDefined(typeof(DataValueKind), kind) && DataValueKind.Enum != kind;
    }

    private static string ToFieldName(string field)
    {
        return "_" + char.ToLowerInvariant(field[0]) + field.Substring(1);
    }

    private static void AppendString(StringBuilder json, string text)
    {
        json.Append('"');

        foreach (char c in text)
        {
            switch (c)
            {
                case '"':
                    json.Append("\\\"");
                    break;

                case '\\':
                    json.Append("\\\\");
                    break;

                case '\n':
                    json.Append("\\n");
                    break;

                case '\t':
                    json.Append("\\t");
                    break;

                default:
                    if (c < ' ')
                    {
                        json.Append("\\u").Append(((int)c).ToString("x4"));
                    }
                    else
                    {
                        json.Append(c);
                    }
                    break;
            }
        }

        json.Append('"');
    }

    private static string[] SplitArray(string text)
    {
        if (0 == text.Length)
        {
            return Array.Empty<string>();
        }

        string[] items = text.Split(ARRAY_SEPARATOR);

        for (int i = 0; i < items.Length; i++)
        {
            items[i] = items[i].Trim();
        }

        return items;
    }

    private static string GetCellText(string[] cells, int index)
    {
        return index < cells.Length ? cells[index].Trim() : string.Empty;
    }

    private static bool IsEmptyRow(string[] cells)
    {
        foreach (string cell in cells)
        {
            if (false == string.IsNullOrWhiteSpace(cell))
            {
                return false;
            }
        }

        return true;
    }
}
