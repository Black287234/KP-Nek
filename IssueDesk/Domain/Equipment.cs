namespace IssueDesk.Domain;

/// <summary>Единица оборудования, выдаваемая сотрудникам (ноутбук, проектор, фотоаппарат и т. п.).</summary>
public class Equipment
{
    public int Id { get; set; }

    /// <summary>Инвентарный номер вида ИН-000123; уникален.</summary>
    public string InventoryNumber { get; set; } = "";

    public string Name { get; set; } = "";
    public EquipmentKind Kind { get; set; }
    public string? SerialNumber { get; set; }

    /// <summary>Текущее техническое состояние; обновляется при возврате.</summary>
    public Condition Condition { get; set; } = Condition.Good;

    /// <summary>Списание вместо удаления: история выдач сохраняется.</summary>
    public DateOnly? WrittenOffOn { get; set; }
    public string? WriteOffReason { get; set; }
    public bool IsWrittenOff => WrittenOffOn != null;
}

public enum EquipmentKind { Laptop, Projector, Camera, Tablet, Audio, Measuring, Other }

/// <summary>Техническое состояние. Неисправное оборудование не выдаётся.</summary>
public enum Condition { Good, Worn, NeedsRepair, Broken }
