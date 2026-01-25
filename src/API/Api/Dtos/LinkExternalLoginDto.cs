using System.ComponentModel.DataAnnotations;

namespace Api.Dtos
{
    public class LinkExternalLoginDto
    {
        [Required]
        public string Provider { get; set; }

        public string ReturnUrl { get; set; }
    }
}
