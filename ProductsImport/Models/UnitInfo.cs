namespace ProductsImport.Models;

/// <summary>A row from cls2 where cls2002 = 100 (unit of measure classifier).</summary>
public class UnitInfo
{
    /// <summary>cls2001 - primary key, stored as ass1005.</summary>
    public long Id { get; set; }

    /// <summary>cls2004 - unit name (e.g. "шт", "кг").</summary>
    public string Name { get; set; } = string.Empty;

    public override string ToString() => Name;
}
