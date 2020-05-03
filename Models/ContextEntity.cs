﻿namespace ApiTools.Models
{
    public abstract class ContextEntity<T> where T: new()
    {
        public T Id { get; set; }
    }
} 