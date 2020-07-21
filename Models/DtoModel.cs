namespace ApiTools.Models
{
    public interface IDtoModel<TId>
    {
        TId Id { get; set; }
    }

    public class DtoModel<TId> : IDtoModel<TId>
    {
        public TId Id { get; set; }
    }
}