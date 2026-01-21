namespace SeedData
{
    public class SeedDataException : Exception
    {
        public SeedDataException()
        {
        }
        public SeedDataException(string? message) : base(message)
        {
        }
        public SeedDataException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}
