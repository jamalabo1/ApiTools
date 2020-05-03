﻿namespace ApiTools.Models
{
    public class ContextReadOptions
    {
        public static readonly ContextReadOptions AllowTrack = new ContextReadOptions {Track = true};
        public static readonly ContextReadOptions DisableTrack = new ContextReadOptions {Track = false};
        public static readonly ContextReadOptions DisableQuery = new ContextReadOptions {Query = false};
        public bool Query { get; set; } = true;
        public bool Track { get; set; } = false;
        public bool Order { get; set; } = true;
    }
} 