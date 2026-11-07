using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace OrtakRota.Web.Services;

public static class EnumExtensions
{
    // Enum değerinin [Display(Name = "...")] ile verilen Türkçe adını döndürür
    public static string GetDisplayName(this Enum value)
    {
        var member = value.GetType().GetMember(value.ToString()).FirstOrDefault();
        return member?.GetCustomAttribute<DisplayAttribute>()?.GetName() ?? value.ToString();
    }
}
