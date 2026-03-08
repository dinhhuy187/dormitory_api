using System;
using System.Collections.Generic;
using System.Text;

namespace Shared
{
    public record ApiResponse<T>(T Data, PaginationMetadata? Meta = null);

    public record PaginationMetadata(int TotalItems, int PageSize, int CurrentPage)
    {
        public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);
        public bool HasPrevious => CurrentPage > 1;
        public bool HasNext => CurrentPage < TotalPages;
    }
}
