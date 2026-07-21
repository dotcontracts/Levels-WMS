using System.Collections.Generic;

namespace wms_android.shared.Models
{
    public class PaginatedResponse<T>
    {
        public IEnumerable<T> Data { get; set; } = new List<T>();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public PaginatedResponse()
        {
        }

        /// <summary>
        /// Constructor with data
        /// </summary>
        public PaginatedResponse(IEnumerable<T> data, int page, int pageSize, int totalCount)
        {
            Data = data;
            Page = page;
            PageSize = pageSize;
            TotalCount = totalCount;
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            HasNextPage = page < TotalPages;
            HasPreviousPage = page > 1;
        }

        /// <summary>
        /// Creates an empty paginated response
        /// </summary>
        public static PaginatedResponse<T> Empty(int page = 1, int pageSize = 50)
        {
            return new PaginatedResponse<T>(new List<T>(), page, pageSize, 0);
        }
    }

    public class ParcelQueryParams
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
        public string? Destination { get; set; }
        public ParcelStatus? Status { get; set; }
        public List<ParcelStatus>? Statuses { get; set; } // Support multiple statuses
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? CreatedByUsername { get; set; }
        public string? SearchTerm { get; set; }
        public string? PaymentMethod { get; set; }

        /// <summary>
        /// Validates the pagination parameters
        /// </summary>
        public void Validate()
        {
            Page = Math.Max(1, Page);
            PageSize = Math.Clamp(PageSize, 1, 500);

            if (FromDate.HasValue && ToDate.HasValue && FromDate > ToDate)
            {
                // Swap dates if FromDate is after ToDate
                var temp = FromDate;
                FromDate = ToDate;
                ToDate = temp;
            }
        }
    }
}