﻿using System;
using System.Collections.Generic;

namespace ApiTools.Models
{
    public interface IDbEntity<T> : IContextEntity<T> where T : new()
    {
        public DateTimeOffset CreationTime { get; set; }
        public DateTimeOffset ModificationTime { get; set; }
    }
    public abstract class  DbEntity<T> : ContextEntity<T> where T : new()
    {
        public DateTimeOffset CreationTime { get; set; }
        public DateTimeOffset ModificationTime { get; set; }
    }
}