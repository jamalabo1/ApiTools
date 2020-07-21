using System.Collections.Generic;
using ApiTools.Models;
using AutoMapper;

namespace ApiTools.Services
{
    public interface IMapperHelper
    {
        IServiceResponse<TDto> MapDto<TDto, T>(IServiceResponse<T> response);
        IServiceResponse<IEnumerable<TDto>> MapDto<TDto, T>(IServiceResponse<IEnumerable<T>> response);
        TDo MapDto<TDo, T>(T data);
        IEnumerable<TDo> MapDto<TDo, T>(IEnumerable<T> data);
    }

    public class MapperHelper : IMapperHelper
    {
        private readonly IMapper _mapper;

        public MapperHelper(IMapper mapper)
        {
            _mapper = mapper;
        }

        public virtual IServiceResponse<TDto> MapDto<TDto, T>(IServiceResponse<T> response)
        {
            return response.ToOtherResponse(MapDto<TDto, T>(response.Response));
        }

        public virtual IServiceResponse<IEnumerable<TDto>> MapDto<TDto, T>(IServiceResponse<IEnumerable<T>> response)
        {
            return response.ToOtherResponse(MapDto<IEnumerable<TDto>, IEnumerable<T>>(response.Response));
        }

        public TDo MapDto<TDo, T>(T data)
        {
            return _mapper.Map<TDo>(data);
        }

        public IEnumerable<TDo> MapDto<TDo, T>(IEnumerable<T> data)
        {
            return MapDto<IEnumerable<TDo>, IEnumerable<T>>(data);
        }
    }
}