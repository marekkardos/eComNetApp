namespace Api.Dtos
{
    public class ExternalLoginInfoDto
    {
        public string LoginProvider { get; set; }
        public string ProviderKey { get; set; }
        public string ProviderDisplayName { get; set; }
    }
}
