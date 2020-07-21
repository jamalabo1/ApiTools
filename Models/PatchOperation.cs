using Microsoft.AspNetCore.JsonPatch;

namespace ApiTools.Models
{
    public class PatchOperation<TModel, TModelKeyId> where TModel : class, IDtoModel<TModelKeyId>
    {
        public TModelKeyId Id { get; set; }
        public JsonPatchDocument<TModel> JsonPatchDocument { get; set; }
    }
}