namespace ApiTools.Models
{
    public interface IDtoEntity<TId>
    {
        TId Id { get; set; }
    }

    public class DtoEntity<TId> : IDtoEntity<TId>
    {
        public TId Id { get; set; }
    }
}