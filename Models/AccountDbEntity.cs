using System.Text.Json.Serialization;
using ApiTools.Context;

namespace ApiTools.Models
{
    public interface IBaseAccountDbEntity<TAccountIdType> : IContextEntity<TAccountIdType> where TAccountIdType : new()
    {
        [JsonIgnore] public string Password { get; set; }
    }
    public interface IAccountDbEntity<TAccountIdType, TCreationTime, TModificationTime> : IDbEntity<TAccountIdType, TCreationTime, TModificationTime>, IBaseAccountDbEntity<TAccountIdType> where TAccountIdType : new()
    {
    }

    public class AccountDbEntity<TAccountIdType, TCreationTime, TModificationTime> : DbEntity<TAccountIdType, TCreationTime, TModificationTime>, IBaseAccountDbEntity<TAccountIdType> where TAccountIdType : new()
    {
        [JsonIgnore]
        public string Password { get; set; }
    }
}