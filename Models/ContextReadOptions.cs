﻿namespace ApiTools.Models
{
    public class ContextReadOptions
    {
        public static readonly ContextReadOptions AllowTrack = new ContextReadOptions {Track = true};
        public static readonly ContextReadOptions DisableTrack = new ContextReadOptions {Track = false};
        public static readonly ContextReadOptions DisableQuery = new ContextReadOptions {Query = false};
        public static readonly ContextReadOptions EnableAllExist = new ContextReadOptions {AllExist = true};

        /// <summary>
        /// Call GetQueryProvider on the entities,
        /// this is typically used to apply what entities the current Authenticated user can access
        /// <example>
        /// if the authenticated user wants to access messages, and some where in the server the method Read was called, and if the query is set to <c>true</c> then read will call <c>GetQueryProvider</c> method,
        /// which will return an <c>IQueryable</c> that says give me all messages that belong to this user
        /// <code>
        /// var userId = User.GetUserId();
        /// return set.Where(x => x.UserId == userId);
        /// </code> 
        /// </example>
        /// </summary>
        public bool Query { get; set; } = true;
        /// <summary>
        /// Track entities after Find (Read), is <c>false</c> then Entity framework will not track the changes for it.
        /// if true then it will track, (read more about it in entity framework docs)
        /// </summary>
        public bool Track { get; set; } = false;
        /// <summary>
        /// Order entities by their creation time (applies only to <c>DbEntity</c>)
        /// </summary>
        public bool Order { get; set; } = true;
        /// <summary>
        /// When Performing an <c>Exist</c> Operation, ensure that all entities are present in the database (exist)
        /// if set to <c>false</c> then the operation will return <c>true</c> if at least 1 entity id exist.
        /// </summary>
        public bool AllExist { get; set; } = false;
    }
} 