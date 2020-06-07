using System.Text.Json.Serialization;

namespace ApiTools.Models
{
    public class AccountDbEntity<TAccountIdType> : DbEntity<TAccountIdType> where TAccountIdType : new()
    {
        [JsonIgnore] public string Password { get; set; }
    }
}