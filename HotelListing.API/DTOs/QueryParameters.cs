namespace HotelListing.API.DTOs
{
    public class QueryParameters
    {
        private int _pageSize = 15;  // default page size
        public int StartIndex { get; set; }
        public int PageNumber { get; set; }
        public int PageSize  // customized page size
        {
            get 
            {
                return _pageSize; 
            }
            set 
            {
                _pageSize = value; 
            }
        }
    }
}