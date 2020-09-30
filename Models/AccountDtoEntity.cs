using System;

namespace ApiTools.Models
{
    public interface IAccountDtoEntity<T> : IDtoEntity<T>
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class AccountDtoEntity<T>:  DtoEntity<T>, IAccountDtoEntity<T>

    {
        public string Username { get; set; }
        public string Password { get; set; }
    }
}