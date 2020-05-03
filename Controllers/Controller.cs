using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApiTools.Models;
using ApiTools.Services;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ApiTools.Controllers
{
    public abstract class SuperController<TModel, TModelKeyId, TService> : Controller
        where TModel : DbEntity<TModelKeyId>
        where TService : IService<TModel, TModelKeyId>
        where TModelKeyId : new()
    {
        protected abstract TService Service { get; }

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
            return GenerateResult(response);
        }

        [HttpGet]
        [Route("{id}")]
        public virtual async Task<IActionResult> GetResource([FromRoute] TModelKeyId id)
        {
            var rolesResponse = CheckUserRoles(GetResource_Roles());
            if (rolesResponse != null) return rolesResponse;


            var response = await Service.Read(id);
            return GenerateResult(response);
        }


        [HttpDelete]
        [Route("{id}")]
        public virtual async Task<IActionResult> DeleteResource([FromRoute] TModelKeyId id)
        {
            var rolesResponse = CheckUserRoles(DeleteResource_Roles());
            if (rolesResponse != null) return rolesResponse;

            var response = await Service.Delete(id);
            return GenerateResult(response);
        }

        [HttpDelete]
        [Route("/bulk")]
        public virtual async Task<IActionResult> DeleteResources([FromRoute] IEnumerable<TModelKeyId> ids)
        {
            var rolesResponse = CheckUserRoles(DeleteResources_Roles());
            if (rolesResponse != null) return rolesResponse;


            var response = await Service.Delete(ids);
            return GenerateResult(response);
        }


        public IActionResult GenerateResult(ServiceResponse response)
        {
            return StatusCode(response.StatusCode);
        }

        public IActionResult GenerateResult(ServiceResponse<TModel> response)
        {
            return StatusCode(response.StatusCode, response.Response);
        }

        public IActionResult GenerateResult(ServiceResponse<PagingServiceResponse<TModel>> response)
        {
            return StatusCode(response.StatusCode, response.Response);
        }

        public virtual RouteRules CreateResource_Roles()
        {
            return null;
        }

        public virtual RouteRules CreateResources_Roles()
        {
            return null;
        }

        public virtual RouteRules GetResource_Roles()
        {
            return null;
        }

        public virtual RouteRules GetResources_Roles()
        {
            return null;
        }

        public virtual RouteRules UpdateResource_Roles()
        {
            return null;
        }

        public virtual RouteRules UpdateResources_Roles()
        {
            return null;
        }

        public virtual RouteRules DeleteResource_Roles()
        {
            return null;
        }

        public virtual RouteRules DeleteResources_Roles()
        {
            return null;
        }
    }

    public abstract class
        SuperController<TModel, TModelKeyId, TService, TModelData> : SuperController<TModel, TModelKeyId, TService>
        where TModel : DbEntity<TModelKeyId>
        where TService : IService<TModel, TModelKeyId, TModelData>
        where TModelKeyId : new()
    {
        [HttpPost]
        [Route("")]
        public virtual async Task<IActionResult> CreateResource([FromBody] TModelData data)
        {
            var rolesResponse = CheckUserRoles(CreateResource_Roles());
            if (rolesResponse != null) return rolesResponse;


            var response = await Service.Create(data);
            return GenerateResult(response);
        }

        [HttpPost]
        [Route("/bulk")]
        public virtual async Task<IActionResult> CreateResources([FromBody] IEnumerable<TModelData> data)
        {
            var rolesResponse = CheckUserRoles(CreateResources_Roles());
            if (rolesResponse != null) return rolesResponse;


            var response = await Service.Create(data);
            return GenerateResult(response);
        }

        [HttpPut]
        [Route("{id}")]
        public virtual async Task<IActionResult> UpdateResource([FromRoute] TModelKeyId id, [FromBody] TModelData data)
        {
            var rolesResponse = CheckUserRoles(UpdateResource_Roles());
            if (rolesResponse != null) return rolesResponse;


            var response = await Service.Update(id, data);
            return GenerateResult(response);
        }

        [HttpPut]
        [Route("/bulk")]
        public virtual async Task<IActionResult> UpdateResources(
            [FromBody] List<BulkUpdateModel<TModelData, TModelKeyId>> bulkUpdateModels)
        {
            var rolesResponse = CheckUserRoles(UpdateResources_Roles());
            if (rolesResponse != null) return rolesResponse;


            var response = await Service.Update(bulkUpdateModels);
            return GenerateResult(response);
        }
    }
}