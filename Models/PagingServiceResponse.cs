﻿using System.Collections.Generic;

namespace ApiTools.Models
{
    public class PagingServiceResponse<T>
    {
        public IEnumerable<T> Data { get; set; }
        public int Size { get; set; }
        public int CurrentSize { get; set; }
        public int Page { get; set; }
        public int PerPage { get; set; }
    }
} 