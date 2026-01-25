using System.ComponentModel.DataAnnotations;

namespace Api.Dtos
{
    public class ExternalLoginDto
    {
        [Required]
        public string Provider { get; set; }

        public string ReturnUrl { get; set; }
    }
}
