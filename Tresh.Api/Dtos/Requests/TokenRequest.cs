using System.ComponentModel.DataAnnotations;

namespace Tresh.Api.Dtos.Requests
{
    public class TokenRequest
    {
        [Required]
        public string Token {  get; set; }
        [Required]
        public string RefreshToken { get; set; }
    }
}
