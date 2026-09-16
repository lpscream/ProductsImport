namespace ProductsImport.Models;

/// <summary>Maps spreadsheet column indices (0-based) to database target fields.</summary>
public class ColumnMapping
{
    private readonly Dictionary<int, TargetField> _map = new();

    public void Set(int columnIndex, TargetField field) => _map[columnIndex] = field;

    public TargetField Get(int columnIndex) => _map.TryGetValue(columnIndex, out var field) ? field : TargetField.None;

    public int? GetColumn(TargetField field)
    {
        foreach (var kvp in _map)
        {
            if (kvp.Value == field)
            {
                return kvp.Key;
            }
        }

        return null;
    }

    public bool IsMapped(TargetField field) => GetColumn(field).HasValue;

    public string? GetValue(string[] row, TargetField field)
    {
        var col = GetColumn(field);
        if (col is null || col.Value >= row.Length)
        {
            return null;
        }

        var value = row[col.Value]?.Trim();
        return string.IsNullOrEmpty(value) ? null : value;
    }
}
