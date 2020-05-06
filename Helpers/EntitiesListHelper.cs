using System;
using System.Collections.Generic;
using System.Linq;
using ApiTools.Models;

namespace ApiTools.Helpers
{
    public interface IEntitiesListHelper
    {
    }


    public class EntitiesListHelper : IEntitiesListHelper
    {
        public static EntitiesListHelperResponse<T> FilterData<T, T2>(IEnumerable<T> dbEntities,
            IEnumerable<T2> dataEntities, EntitiesListHelperRequirements<T, T2> requirements) where T: new()
        {
            if (requirements == null) return null;

            var dbEntitiesList = dbEntities.ToList();
            var dataEntitiesList = dataEntities.ToList();

            var toDelete = (from dbEntity in dbEntitiesList
                let pass = dataEntitiesList.Any(dataEntity => requirements.MatchFunc(dbEntity, dataEntity))
                where pass == false
                select dbEntity).ToList();
            
            var toCreate = (from dataEntity in dataEntitiesList
                let doesNotExist = !dbEntitiesList.Any(dbEntity => requirements.MatchFunc(dbEntity, dataEntity))
                where doesNotExist
                select requirements.MapModel(dataEntity, new T())).ToList();
            
            var toUpdate = (from dbEntity in dbEntitiesList
                let dataEntity = dataEntitiesList.FirstOrDefault(x => requirements.MatchFunc(dbEntity, x))
                where dataEntity != null
                where requirements.ModifiedMatchFunc(dbEntity, dataEntity)
                select requirements.MapModel(dataEntity, dbEntity)).ToList();

            return new EntitiesListHelperResponse<T>
            {
                Create = toCreate,
                Update = toUpdate,
                Delete = toDelete
            };
        }
    }
}