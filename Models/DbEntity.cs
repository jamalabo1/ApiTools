﻿using System;
using System.Collections.Generic;

namespace ApiTools.Models
{
    public abstract class  DbEntity<T> : ContextEntity<T> where T : new()
    {
        public DateTimeOffset CreationTime { get; set; }
        public DateTimeOffset ModificationTime { get; set; }
    }
}