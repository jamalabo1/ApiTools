namespace ApiTools.Models
{
    public class BulkUpdateModel<TModel, TModelKeyId>
    {
        public TModel Entity { get; set; }
        public TModelKeyId Id { get; set; }
    }
}