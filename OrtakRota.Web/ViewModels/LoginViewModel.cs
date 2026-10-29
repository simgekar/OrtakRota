using System.ComponentModel.DataAnnotations;

namespace OrtakRota.Web.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "E-posta zorunludur.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi girin.")]
    [Display(Name = "E-posta")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Şifre zorunludur.")]
    [DataType(DataType.Password)]
    [Display(Name = "Şifre")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Beni hatırla")]
    public bool RememberMe { get; set; }
}
