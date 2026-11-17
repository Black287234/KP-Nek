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

    /// <summary>Техническое состояние, фиксируется при выдаче и возврате.</summary>
    public Condition Condition { get; set; } = Condition.Good;
}

public enum EquipmentKind { Laptop, Projector, Camera, Tablet, Audio, Measuring, Other }

public enum Condition { Good, Worn, NeedsRepair, Broken }
