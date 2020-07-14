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
        public static IActionResult GenerateResult<T>(ServiceResponse<T> response)
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
    }

    [ApiController]
    public abstract class
        SuperController<TModel, TModelKeyId, TService, TModelDto> : Controller
        where TModel : IBaseDbEntity<TModelKeyId>
        where TService : IService<TModel, TModelKeyId, TModelDto>
        where TModelKeyId : new()
        where TModelDto : class
    {
        public SuperController(IServiceHelper<TModel, TModelKeyId, TModelDto> serviceHelper)
        {
            Service = (TService) serviceHelper.Service;
            Mapper = serviceHelper.Mapper;
        }

        protected TService Service { get; }
        protected IMapper Mapper { get; set; }

        [CanBeNull]
        protected IActionResult CheckUserRoles(RouteRules routeRules)
        {
            if (routeRules == null) return StatusCode(StatusCodes.Status404NotFound);
            if (routeRules.AllowAnonymous) return null;
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
            return GenerateActionResult(MapDto(response));
        }


        private ServiceResponse<TModelDto> MapDto(ServiceResponse<TModel> response)
        {
            return ServiceResponse<TModelDto>.FromOtherResponse(response, MapDto(response.Response));
        }

        private ServiceResponse<IEnumerable<TModelDto>> MapDto(ServiceResponse<IEnumerable<TModel>> response)
        {
            return ServiceResponse<IEnumerable<TModelDto>>.FromOtherResponse(response, MapDto(response.Response));
        }

        private ServiceResponse<PagingServiceResponse<TModelDto>> MapDto(
            ServiceResponse<PagingServiceResponse<TModel>> response)
        {
            return ServiceResponse<PagingServiceResponse<TModelDto>>.FromOtherResponse(response,
                response.Response.ToOtherResponse(MapDto(response.Response.Data)
                )
            );
        }

        private TModelDto MapDto(TModel data)
        {
            return Mapper.Map<TModelDto>(data);
        }

        private IEnumerable<TModelDto> MapDto(IEnumerable<TModel> data)
        {
            return Mapper.Map<IEnumerable<TModelDto>>(data);
        }

        [HttpGet]
        [Route("{field}")]
        public virtual async Task<IActionResult> GetResourcesField([FromRoute] string field)
        {
            var rolesResponse = CheckUserRoles(GetResourceField_Roles());
            if (rolesResponse != null) return rolesResponse;

            var response = await Service.Read(field);
            return GenerateActionResult(response);
        }

        [HttpGet]
        [Route("{id}/{field}")]
        public virtual async Task<IActionResult> GetResourceField([FromRoute] TModelKeyId id, [FromRoute] string field)
        {
            var rolesResponse = CheckUserRoles(GetResourceField_Roles());
            if (rolesResponse != null) return rolesResponse;

            var response = await Service.Read(id, field);
            return GenerateActionResult(response);
        }

        [HttpGet]
        [Route("{id:guid}")]
        public virtual async Task<IActionResult> GetResource([FromRoute] TModelKeyId id)
        {
            var rolesResponse = CheckUserRoles(GetResource_Roles());
            if (rolesResponse != null) return rolesResponse;


            var response = await Service.Read(id);
            return GenerateActionResult(MapDto(response));
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


        [HttpGet]
        [Route("{id}/{field}/{fieldId}")]
        public virtual async Task<IActionResult> GetResourceArrayFieldItem([FromRoute] TModelKeyId id,
            [FromRoute] string field, [FromRoute] long fieldId)
        {
            var rolesResponse = CheckUserRoles(GetResourceField_Roles());
            if (rolesResponse != null) return rolesResponse;

            var response = await Service.Read(id, field, fieldId);
            return GenerateActionResult(response);
        }


        protected virtual IActionResult GenerateActionResult(ServiceResponse response)
        {
            if (!response.Success && response.Messages.Count == 0 ||
                response.StatusCode == StatusCodes.Status204NoContent) return StatusCode(response.StatusCode);
            return StatusCode(response.StatusCode, response);
        }

        protected virtual IActionResult GenerateActionResult<T>(ServiceResponse<T> response)
        {
            return SuperController.GenerateResult(response);
        }


        protected virtual IActionResult GenerateActionResult(ServiceResponse<PagingServiceResponse<TModel>> response)
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
            return GenerateActionResult(MapDto(response));
        }

        [HttpPost]
        [Route("bulk")]
        public virtual async Task<IActionResult> CreateResources([FromBody] IEnumerable<TModelDto> data)
        {
            var rolesResponse = CheckUserRoles(CreateResources_Roles());
            if (rolesResponse != null) return rolesResponse;


            var response = await Service.Create(data);
            return GenerateActionResult(MapDto(response));
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
            [FromBody] List<BulkUpdateModel<TModelDto, TModelKeyId>> bulkUpdateModels)
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
        public virtual async Task<IActionResult> Patch([FromRoute] TModelKeyId id,
            [FromBody] JsonPatchDocument<TModelDto> dataPatch)
        {
            if (Mapper == null) return GenerateActionResult(404, null);
            var rolesResponse = CheckUserRoles(UpdatePartialResource_Roles());
            if (rolesResponse != null) return rolesResponse;

            var response = await Service.Read(id);
            if (!response.Success) return GenerateActionResult(response);
            var model = response.Response;


            var modelDto = Mapper.Map<TModelDto>(model);

            dataPatch.ApplyTo(modelDto);


            if (!ModelState.IsValid) return new BadRequestObjectResult(ModelState);

            Mapper.Map(modelDto, model);

            var updateResponse = await Service.Update(model);

            return GenerateActionResult(updateResponse);
        }
    }
}