using EquipmentIssue.Data;
using EquipmentIssue.Models;

namespace EquipmentIssue.Services;

/// <summary>Работа со справочником оборудования (этап 1: данные в памяти).</summary>
public class EquipmentService
{
    public List<Equipment> GetAll() => EquipmentStore.Items.OrderBy(e => e.InventoryNumber).ToList();

    public Equipment? GetById(int id) => EquipmentStore.Items.FirstOrDefault(e => e.Id == id);
}
