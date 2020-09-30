using System;
using System.Linq;
using ApiTools.Models;
using AutoMapper;

namespace ApiTools.AutoMapper
{
    public class DtoProfile : Profile
    {
        protected virtual void GuidIdOptions<TData, TEntity>(IMemberConfigurationExpression<TData, TEntity, Guid> option)
            where TData : IDtoEntity<Guid>
            where TEntity : IBaseDbEntity<Guid>
        {
            option.MapFrom((source, dest) =>
                IgnoreVirtualExtensions.PropValue(dest, nameof(dest.Id)) ??
                IgnoreVirtualExtensions.PropValue(source, nameof(source.Id))
            );
        }


        protected virtual IMappingExpression<TDto, T> CreateEntityMap<TDto, T>()
            where TDto : IDtoEntity<Guid>
            where T : IBaseDbEntity<Guid>
        {
            return CreateMap<TDto, T>()
                .ForMember(d => d.Id, GuidIdOptions)
                .IgnoreEmptyGuid();
            // .IgnoreAllVirtual();
        }

        protected virtual IMappingExpression<T, TDto> CreateEntityMapReversed<TDto, T>()
            where TDto : IDtoEntity<Guid>
            where T : IBaseDbEntity<Guid>
        {
            return CreateEntityMap<TDto, T>()
                .ReverseMap()
                .IgnorePassword();
        }
    }

    public static class IgnoreVirtualExtensions
    {
        public static IMappingExpression<TSource, TDestination>
            IgnoreAllVirtual<TSource, TDestination>(
                this IMappingExpression<TSource, TDestination> expression)
        {
            var desType = typeof(TDestination);
            foreach (var property in desType.GetProperties().Where(p =>
                p.GetGetMethod().IsVirtual))
                expression.ForMember(property.Name, opt => opt.Ignore());

            return expression;
        }

        public static IMappingExpression<TSource, TDestination>
            IgnoreEmptyGuid<TSource, TDestination>(
                this IMappingExpression<TSource, TDestination> expression) where TSource : IDtoEntity<Guid>
            where TDestination : IBaseDbEntity<Guid>
        {
            var desType = typeof(TDestination);
            foreach (var property in desType.GetProperties().Where(p => p.PropertyType == typeof(Guid)))
                expression.ForMember(property.Name, opt =>
                {
                    opt.MapFrom((source, dest) =>
                        PropValue(dest, property.Name) ?? PropValue(source, property.Name)
                    );
                    // opt.MapFrom((source, dest) => source.Id == Guid.Empty ? dest.GetType().getp : source.Id);
                });

            return expression;
        }

        public static Guid? PropValue(object source, string propertyName)
        {
            var value = source.GetType().GetProperty(propertyName)?.GetValue(source);
            var val = (Guid?) value;
            if (val == Guid.Empty) return null;
            return val;
        }

        public static IMappingExpression<TSource, TDestination>
            IgnorePassword<TSource, TDestination>(
                this IMappingExpression<TSource, TDestination> expression)
        {
            // var desType = typeof(TDestination);
            // foreach (var property in desType.GetProperties().Where(p => p.Name == nameof(IAccountDtoEntity.Password)))
            // expression.ForMember(property.Name, opt => opt.Ignore());

            return expression;
        }

        public static IMappingExpression<TDest, TSource>
            HashPassword<TSource, TDest>(
                this IMappingExpression<TSource, TDest> expression) where TSource : IAccountDtoEntity<Guid>
            where TDest : IAccountDbEntity<Guid, DateTimeOffset, DateTimeOffset>
        {
            return expression.ForMember(nameof(IAccountDbEntity<Guid, DateTimeOffset, DateTimeOffset>.Password),
                        opt => opt.MapFrom<PasswordMapper<TSource, TDest>>())
                    .ReverseMap()
                    .ForMember(x => x.Password, cb => cb.MapFrom(e => string.Empty))
                ;
        }

        public static IMappingExpression<TSource, TDest>
            GuidId<TSource, TDest>(
                this IMappingExpression<TSource, TDest> expression) where TDest : IBaseDbEntity<Guid>
        {
            expression.ForMember(nameof(IBaseDbEntity<Guid>.Id), x => Guid.NewGuid());

            return expression;
        }
    }
}