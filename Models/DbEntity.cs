﻿using System;
using System.Collections.Generic;

namespace ApiTools.Models
{
    public interface IBaseDbEntity<T> : IContextEntity<T> where T : new()
    {
    }
    public interface IDbEntity<T, TCreationTime, TModificationTime> : IBaseDbEntity<T> where T : new()
    {
        public TCreationTime CreationTime { get; set; }
        public TModificationTime ModificationTime { get; set; }
    }
    public abstract class DbEntity<T, TCreationTime, TModificationTime> : ContextEntity<T>, IBaseDbEntity<T> where T : new()
    {
        public TCreationTime CreationTime { get; set; }
        public TModificationTime ModificationTime { get; set; }
    }
}