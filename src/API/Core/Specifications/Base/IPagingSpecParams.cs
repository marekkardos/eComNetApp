namespace Core.Specifications.Base
{
    public interface IPagingSpecParams
    {
        int PageSize { get; }

        int PageIndex { get; }
    }
}