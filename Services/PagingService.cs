﻿using System.Linq;
using System.Threading.Tasks;
using ApiTools.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace ApiTools.Services
{
    public interface IPagingService
    {
        Task<PagingServiceResponse<T>> Apply<T>(IQueryable<T> set);
    }

    public class PagingService : IPagingService
    {
        private readonly IHttpContextAccessor _accessor;
        private int _limit = 100;
        private int _page = 1;
        private int _size;

        public PagingService(IHttpContextAccessor accessor)
        {
            _accessor = accessor;
        }

        /// <summary>
        ///     Applies the paging query parameters from the URL to the query.
        /// </summary>
        /// <param name="set">The Query object</param>
        /// <typeparam name="T">Type of entities the query holds</typeparam>
        /// <returns>The set of data paged in paging response that contains the information required.</returns>
        public async Task<PagingServiceResponse<T>> Apply<T>(IQueryable<T> set)
        {
            var httpQuery = _accessor.HttpContext.Request.Query;
            var pageValue = httpQuery.LastOrDefault(x => x.Key == "page").Value;
            if (pageValue.Count > 0) int.TryParse(pageValue, out _page);
            var limitValue = httpQuery.LastOrDefault(x => x.Key == "limit").Value;
            if (limitValue.Count > 0) int.TryParse(limitValue, out _limit);
            if (_limit > 1000 || _limit <= 0) _limit = 1000;
            if (_page <= 0) _page = 1;
            _size = await set.CountAsync();
            if (_page > 1) set = set.Skip((_page - 1) * _limit);
            set = set.Take(_limit);

            var currentSize = await set.CountAsync();
            return new PagingServiceResponse<T>
            {
                Data = await set.ToListAsync(),
                Size = _size,
                CurrentSize = currentSize,
                Page = _page,
                PerPage = _limit
            };
        }
    }
}