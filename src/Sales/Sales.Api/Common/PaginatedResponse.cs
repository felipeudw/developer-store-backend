using System;
using System.Collections.Generic;
using AutoMapper;

namespace Sales.Api.Common
{
    public sealed class PaginatedResponse<T>
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();
    }

    public static class PaginationExtensions
    {
        public static PaginatedResponse<TDest> ToPaginatedResponse<TSource, TDest>(
            this IEnumerable<TSource> source,
            int page,
            int pageSize,
            IMapper mapper)
        {
            if (mapper is null) throw new ArgumentNullException(nameof(mapper));

            var p = page <= 0 ? 1 : page;
            var ps = pageSize <= 0 ? 20 : pageSize;

            var mapped = mapper.Map<IReadOnlyList<TDest>>(source);

            return new PaginatedResponse<TDest>
            {
                Page = p,
                PageSize = ps,
                Items = mapped
            };
        }
    }
}