namespace ApiTools.Models
{
    public class BulkUpdateModel<TModelDto, TModelKeyId> where TModelDto : IDtoModel<TModelKeyId>
    {
        public TModelDto Entity { get; set; }
    }
}