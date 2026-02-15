using System.ComponentModel.DataAnnotations;

namespace DustyPig.Server.Pages.DeleteAccount;

public class DeleteAccountModel
{
    public enum States
    {
        None,
        Error,
        Deleted
    }

    [Required]
    [DataType(DataType.EmailAddress)]
    [Display(Name = "Email")]
    public string Email { get; set; }

    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; }

    public States State { get; set; }

    public string Error { get; set; }
}
