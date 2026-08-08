using System.Collections.Generic;

namespace ABP.Application.Common.DTOs.Common
{
    public class PagedResultDto<T>
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalRecords { get; set; }
        public int TotalPages { get; set; }
        public IReadOnlyList<T> Data { get; set; } = new List<T>();
    }
}
