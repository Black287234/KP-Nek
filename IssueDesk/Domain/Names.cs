namespace IssueDesk.Domain;

/// <summary>Русские названия значений перечислений для интерфейса.</summary>
public static class Names
{
    public static string Of(EquipmentKind k) => k switch
    {
        EquipmentKind.Laptop => "Ноутбук",
        EquipmentKind.Projector => "Проектор",
        EquipmentKind.Camera => "Фото- и видеотехника",
        EquipmentKind.Tablet => "Планшет",
        EquipmentKind.Audio => "Звуковое оборудование",
        EquipmentKind.Measuring => "Измерительный прибор",
        _ => "Прочее"
    };

    public static string Of(Condition c) => c switch
    {
        Condition.Good => "исправно",
        Condition.Worn => "исправно, есть износ",
        Condition.NeedsRepair => "требует ремонта",
        _ => "неисправно"
    };

    public static IEnumerable<EquipmentKind> Kinds => Enum.GetValues<EquipmentKind>();
    public static IEnumerable<Condition> Conditions => Enum.GetValues<Condition>();
}
