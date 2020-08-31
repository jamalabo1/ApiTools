using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApiTools.Models;
using ApiTools.Services;
using AutoMapper;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;

namespace ApiTools.Controllers
{
    public static class SuperController
    {
        public static IActionResult GenerateResult<T>(IApiResponse<T> response)
        {
            if (response.StatusCode == StatusCodes.Status204NoContent ||
                !response.Success && response.Response == null && response.Messages.Count == 0)
                return new StatusCodeResult(response.StatusCode);
            var result = new ObjectResult(response)
            {
                StatusCode = response.StatusCode
            };
            return result;
        }

        public static IActionResult GenerateResult(IApiResponse response)
        {
            if (response.StatusCode == StatusCodes.Status204NoContent ||
                !response.Success && response.Messages.Count == 0)
                return new StatusCodeResult(response.StatusCode);
            var result = new ObjectResult(response)
            {
                StatusCode = response.StatusCode
            };
            return result;
        }
    }

    [ApiController]
    public class
        SuperController<TModel, TModelKeyId, TService, TModelDto> : Controller
        where TModel : IBaseDbEntity<TModelKeyId>
        where TService : IService<TModel, TModelKeyId, TModelDto>
        where TModelKeyId : new()
        where TModelDto : class, IDtoModel<TModelKeyId>
    {
        public SuperController(IServiceHelper<TModel, TModelKeyId, TModelDto> serviceHelper)
        {
            Service = (TService) serviceHelper.Service;
            Mapper = serviceHelper.Mapper;
            MapperHelper = serviceHelper.MapperHelper;
        }

        protected virtual TService Service { get; }
        protected IMapper Mapper { get; set; }
        protected IMapperHelper MapperHelper { get; set; }

        protected IActionResult? CheckUserRoles(RouteRules routeRules, bool allowAnonymous = false)
        { 
            if (routeRules == null) return StatusCode(StatusCodes.Status404NotFound);
            if (routeRules.AllowAnonymous || allowAnonymous) return null;
            if (User == null) return StatusCode(StatusCodes.Status403Forbidden);
            return routeRules.Roles.Any(role => User.IsInRole(role))
                ? null
                : StatusCode(StatusCodes.Status403Forbidden);
        }

        [HttpGet]
        [Route("")]
        public virtual async Task<IActionResult> GetResources()
        {
            var rolesResponse = CheckUserRoles(GetResources_Roles());
            if (rolesResponse != null) return rolesResponse;

            var response = await Service.Read();
            return GenerateActionResult(response);
        }

        protected virtual IServiceResponse<IEnumerable<TModelDto>> MapDto(
            IServiceResponse<IEnumerable<TModel>> response)
        {
            return response.ToOtherServiceResponse(MapDto(response.Response));
        }

        private IServiceResponse<PagingServiceResponse<TModelDto>> MapDto(
            IServiceResponse<PagingServiceResponse<TModel>> response)
        {
            return response.ToOtherServiceResponse(response.Response.ToOtherResponse(MapDto(response.Response.Data)));
        }


        protected TModelDto MapDto(TModel data)
        {
            return MapperHelper.MapDto<TModelDto, TModel>(data);
        }

        protected IEnumerable<TModelDto> MapDto(IEnumerable<TModel> data)
        {
            return MapperHelper.MapDto<TModelDto, TModel>(data);
        }


        // [HttpGet]
        // [Route("{field}")]
        // public virtual async Task<IActionResult> GetResourcesField([FromRoute] string field)
        // {
        //     var rolesResponse = CheckUserRoles(GetResourceField_Roles());
        //     if (rolesResponse != null) return rolesResponse;
        //
        //     var response = await Service.Read(field);
        //     return GenerateActionResult(response);
        // }
        //
        // [HttpGet]
        // [Route("{id}/{field}")]
        // public virtual async Task<IActionResult> GetResourceField([FromRoute] TModelKeyId id, [FromRoute] string field)
        // {
        //     var rolesResponse = CheckUserRoles(GetResourceField_Roles());
        //     if (rolesResponse != null) return rolesResponse;
        //
        //     var response = await Service.Read(id, field);
        //     return GenerateActionResult(response);
        // }

        [HttpGet]
        [Route("{id:guid}")]
        public virtual async Task<IActionResult> GetResource([FromRoute] TModelKeyId id)
        {
            var rolesResponse = CheckUserRoles(GetResource_Roles());
            if (rolesResponse != null) return rolesResponse;


            var response = await Service.Read(id);
            return GenerateActionResult(response);
        }

        [HttpDelete]
        [Route("{id}")]
        public virtual async Task<IActionResult> DeleteResource([FromRoute] TModelKeyId id)
        {
            var rolesResponse = CheckUserRoles(DeleteResource_Roles());
            if (rolesResponse != null) return rolesResponse;

            var response = await Service.Delete(id);
            return GenerateActionResult(response);
        }

        [HttpDelete]
        [Route("bulk")]
        public virtual async Task<IActionResult> DeleteResources([FromRoute] IEnumerable<TModelKeyId> ids)
        {
            var rolesResponse = CheckUserRoles(DeleteResources_Roles());
            if (rolesResponse != null) return rolesResponse;


            var response = await Service.Delete(ids);
            return GenerateActionResult(response);
        }


        // [HttpGet]
        // [Route("{id}/{field}/{fieldId}")]
        // public virtual async Task<IActionResult> GetResourceArrayFieldItem([FromRoute] TModelKeyId id,
        //     [FromRoute] string field, [FromRoute] string fieldId)
        // {
        //     var rolesResponse = CheckUserRoles(GetResourceField_Roles());
        //     if (rolesResponse != null) return rolesResponse;
        //
        //     var response = await Service.Read(id, field, fieldId);
        //     if (response.Response is IContextEntity<TModelKeyId>)
        //     {
        //     }
        //
        //     return GenerateActionResult(response);
        // }


        protected virtual IActionResult GenerateActionResult(IServiceResponse response)
        {
            return SuperController.GenerateResult(response);
        }

        protected virtual IActionResult GenerateActionResult<T>(IServiceResponse<T> response)
        {
            return SuperController.GenerateResult(response);
        }

        protected virtual IActionResult GenerateMappedActionResult<TDto, T>(IServiceResponse<T> response)
        {
            return SuperController.GenerateResult(MapperHelper.MapDto<TDto, T>(response));
        }

        protected virtual IActionResult GenerateMappedActionResult<TDto, T>(IServiceResponse<IEnumerable<T>> response)
        {
            return SuperController.GenerateResult(MapperHelper.MapDto<TDto, T>(response));
        }


        protected virtual IActionResult GenerateActionResult(IServiceResponse<PagingServiceResponse<TModel>> response)
        {
            return GenerateActionResult(response.StatusCode, response);
        }

        protected virtual IActionResult GenerateActionResult(int statusCode, object value)
        {
            return StatusCode(statusCode, value);
        }

        protected virtual RouteRules CreateResource_Roles()
        {
            return DefaultShared_Roles();
        }

        protected virtual RouteRules GetResourceField_Roles()
        {
            return DefaultShared_Roles();
        }

        protected virtual RouteRules CreateResources_Roles()
        {
            return CreateResource_Roles();
        }

        protected virtual RouteRules GetResource_Roles()
        {
            return DefaultShared_Roles();
        }

        protected virtual RouteRules GetResources_Roles()
        {
            return GetResource_Roles();
        }

        protected virtual RouteRules UpsetResourcesField_Roles()
        {
            return UpdateResources_Roles();
        }

        protected virtual RouteRules UpdateResource_Roles()
        {
            return DefaultShared_Roles();
        }

        protected virtual RouteRules UpdateResources_Roles()
        {
            return UpdateResource_Roles();
        }

        protected virtual RouteRules DeleteResource_Roles()
        {
            return DefaultShared_Roles();
        }

        protected virtual RouteRules DeleteResources_Roles()
        {
            return DeleteResource_Roles();
        }


        protected virtual RouteRules UpdatePartialResource_Roles()
        {
            return UpdateResource_Roles();
        }

        protected virtual RouteRules DefaultShared_Roles()
        {
            return null;
        }

        [HttpPost]
        [Route("")]
        public virtual async Task<IActionResult> CreateResource([FromBody] TModelDto data)
        {
            var rolesResponse = CheckUserRoles(CreateResource_Roles());
            if (rolesResponse != null) return rolesResponse;


            var response = await Service.Create(data);
            return GenerateActionResult(response);
        }

        [HttpPost]
        [Route("bulk")]
        public virtual async Task<IActionResult> CreateResources([FromBody] IEnumerable<TModelDto> data)
        {
            var rolesResponse = CheckUserRoles(CreateResources_Roles());
            if (rolesResponse != null) return rolesResponse;


            var response = await Service.Create(data);
            return GenerateActionResult(response);
        }

        [HttpPut]
        [Route("{id}")]
        public virtual async Task<IActionResult> UpdateResource([FromRoute] TModelKeyId id, [FromBody] TModelDto data)
        {
            var rolesResponse = CheckUserRoles(UpdateResource_Roles());
            if (rolesResponse != null) return rolesResponse;


            var response = await Service.Update(id, data);
            return GenerateActionResult(response);
        }

        [HttpPut]
        [Route("bulk")]
        public virtual async Task<IActionResult> UpdateResources(
            [FromBody] List<TModelDto> bulkUpdateModels)
        {
            var rolesResponse = CheckUserRoles(UpdateResources_Roles());
            if (rolesResponse != null) return rolesResponse;


            var response = await Service.Update(bulkUpdateModels);
            return GenerateActionResult(response);
        }

        // [HttpPut]
        // [Route("upsert")]
        // public virtual async Task<IActionResult> UpsetResourcesField([FromBody] IEnumerable<TModelData> entities)
        // {
        //     var rolesResponse = CheckUserRoles(UpsetResourcesField_Roles());
        //     if (rolesResponse != null) return rolesResponse;
        //
        //
        //     var response = await Service.CreateOrUpdate(entities);
        //     return GenerateActionResult(response);
        // }

        [HttpPatch("{id}")]
        public virtual async Task<IActionResult> Patch(
            [FromRoute] TModelKeyId id,
            [FromBody] JsonPatchDocument<TModelDto> patchDoc
        )
        {
            if (Mapper == null) return GenerateActionResult(404, null);
            var rolesResponse = CheckUserRoles(UpdatePartialResource_Roles());
            if (rolesResponse != null) return rolesResponse;

            if (!ModelState.IsValid) return new BadRequestObjectResult(ModelState);

            var response = await Service.Patch(id, patchDoc);
            return GenerateActionResult(response);
        }

        [HttpPatch("bulk")]
        public virtual async Task<IActionResult> Patch(
            [FromBody] IList<PatchOperation<TModelDto, TModelKeyId>> patchDocs)
        {
            if (Mapper == null) return GenerateActionResult(404, null);
            var rolesResponse = CheckUserRoles(UpdatePartialResource_Roles());
            if (rolesResponse != null) return rolesResponse;

            if (!ModelState.IsValid) return new BadRequestObjectResult(ModelState);

            var response = await Service.Patch(patchDocs);
            return GenerateActionResult(response);
        }
    }
}