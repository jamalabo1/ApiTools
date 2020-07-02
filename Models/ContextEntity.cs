﻿namespace ApiTools.Models
{
    public interface IContextEntity<T> where T: new()
    {
        public T Id { get; set; }
    }
    public abstract class ContextEntity<T>: IContextEntity<T> where T: new()
    {
        public T Id { get; set; }
    }
} 