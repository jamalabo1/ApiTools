﻿namespace ApiTools.Models
{
    public class ContextOptions
    {
        public static readonly ContextOptions AllowTrack = new ContextOptions {Track = true};
        public static readonly ContextOptions DisableTrack = new ContextOptions {Track = false};
        public static readonly ContextOptions DisableQuery = new ContextOptions {Query = false};
        public static readonly ContextOptions EnableAllExist = new ContextOptions {AllExist = true};

        /// <summary>
        ///     Call GetQueryProvider on the entities,
        ///     this is typically used to apply what entities the current Authenticated user can access
        ///     <example>
        ///         if the authenticated user wants to access messages, and some where in the server the method Read was called,
        ///         and if the query is set to <c>true</c> then read will call <c>GetQueryProvider</c> method,
        ///         which will return an <c>IQueryable</c> that says give me all messages that belong to this user
        ///         <code>
        /// var userId = User.GetUserId();
        /// return set.Where(x => x.UserId == userId);
        /// </code>
        ///     </example>
        /// </summary>
        public bool Query { get; set; } = true;

        /// <summary>
        ///     Track entities after Find (Read), is <c>false</c> then Entity framework will not track the changes for it.
        ///     if true then it will track, (read more about it in entity framework docs)
        /// </summary>
        public bool Track { get; set; }

        /// <summary>
        ///     Order entities by their creation time (applies only to <c>DbEntity</c>)
        /// </summary>
        public bool Order { get; set; } = true;

        /// <summary>
        ///     When Performing an <c>Exist</c> Operation, ensure that all entities are present in the database (exist)
        ///     if set to <c>false</c> then the operation will return <c>true</c> if at least 1 entity id exist.
        /// </summary>
        public bool AllExist { get; set; }

        /// <summary>
        ///     When supplied multiple expressions and the database gets <c>AND</c> operation, if this set to false then the
        ///     database gets a <c>OR</c> operation
        /// </summary>
        public bool UseAndInMultipleExpressions { get; set; } = true;

        public bool Upsert { get; set; }

        public bool DetachAfterSave { get; set; }

    }
}