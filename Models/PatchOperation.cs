using Microsoft.AspNetCore.JsonPatch;

namespace ApiTools.Models
{
    public class PatchOperation<TModel, TModelKeyId> where TModel : class, IDtoEntity<TModelKeyId>
    {
        public TModelKeyId Id { get; set; }
        public JsonPatchDocument<TModel> JsonPatchDocument { get; set; }
    }
}