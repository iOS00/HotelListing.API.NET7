namespace HotelListing.API.Core.DTOs
{
    public class PagedResult<T>
    {
        public int TotalCount { get; set;}  // number of recolds
        public int PageNumber { get; set; }  // index of page
        public int RecordNumber { get; set; }  // index of record
        public List<T> Items { get; set; }  // records themselves
    }
}