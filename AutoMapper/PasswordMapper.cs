using System;
using ApiTools.Models;
using ApiTools.Services;
using AutoMapper;

namespace ApiTools.AutoMapper
{
    public class PasswordMapper<TSource, TDest> : IValueResolver<TSource, TDest, object>
        where TSource : IAccountDtoEntity<Guid> where TDest : IAccountDbEntity<Guid, DateTimeOffset, DateTimeOffset>
    {
        private readonly IPasswordService _passwordService;

        public PasswordMapper(IPasswordService passwordService)
        {
            _passwordService = passwordService;
        }

        public object Resolve(TSource source, TDest destination, object destMember,
            ResolutionContext context)
        {
            if (destination.Password != null) return destination.Password;
            return _passwordService.HashPassword(source.Password);
        }
    }
}