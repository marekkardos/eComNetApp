namespace Api.Helpers
{
    public interface IPictureUrlResolver
    {
        string GetAbsolutePath(string pictureRelativePath);
    }
}