namespace ApiTools.Models
{
    public class BulkUpdateModel<TModelDto, TModelKeyId> where TModelDto : IDtoEntity<TModelKeyId>
    {
        public TModelDto Entity { get; set; }
    }
}