using System.ComponentModel.DataAnnotations;

namespace Tresh.Api.Dtos.Requests
{
    public class UserLoginRequest
    {
        [Required]
        [EmailAddress]
        public string Email {  get; set; }
        [Required]
        public string Password { get; set; }
    }
}
