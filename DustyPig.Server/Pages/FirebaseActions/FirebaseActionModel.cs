using System.ComponentModel.DataAnnotations;

namespace DustyPig.Server.Pages.FirebaseActions
{
    public class FirebaseActionModel
    {
        public string Code { get; set; }

        public string Title { get; set; }

        public string Message { get; set; }

        public string ErrorMessage { get; set; }

        public bool ShowPasswordReset { get; set; }

        [Required]
        [StringLength(1000, ErrorMessage = "Password must be at least {2} characters long", MinimumLength = 1)]
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        public string NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare(nameof(NewPassword), ErrorMessage = "The passwords do not match.")]
        public string ConfirmPassword { get; set; }
    }
}
