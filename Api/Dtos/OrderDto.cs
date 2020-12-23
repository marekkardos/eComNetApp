using System.ComponentModel.DataAnnotations;

namespace Api.Dtos
{
    public class OrderDto
    {
        [Required]
        public string BasketId { get; set; }
        
        [Required]
        [Range(1, 4)]
        public int DeliveryMethodId { get; set; }

        [Required]
        public AddressDto ShipToAddress { get; set; }
    }
}