using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace EquipmentIssue.Models;

/// <summary>Русские названия значений перечислений из атрибута [Display].</summary>
public static class EnumExtensions
{
    public static string DisplayName(this Enum value)
    {
        var member = value.GetType().GetMember(value.ToString()).FirstOrDefault();
        var attribute = member?.GetCustomAttribute<DisplayAttribute>();
        return attribute?.Name ?? value.ToString();
    }
}
