using Core.Specifications.Base;

namespace Core.Specifications
{
    public class ProductSpecParams : IPagingSpecParams
    {
        private const int MaxPageSize = 50;
        public int PageIndex {get; set;} = 1;

        private int _pageSize = 6;
        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = (value > MaxPageSize) ? MaxPageSize : value;
        }

        public int? BrandId { get; set; }
        public int? TypeId { get; set; }
        /// <summary>
        ///  priceAsc / priceDesc
        /// </summary>
        public string Sort { get; set; }
        private string _search;
        public string Search 
        { 
            get => _search;
            set => _search = value.ToLower();
        }
    }
}