﻿using FluentValidation;
using FluentValidation.Results;
using MessageSilo.Features.EntityManager;
using MessageSilo.Shared.DataAccess;
using MessageSilo.Shared.Enums;
using MessageSilo.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Orleans;

namespace MessageSilo.API.Controllers
{
    [Route("api/v1/[controller]")]
    public abstract class CRUDController<DTO, STATE, GRAIN> : MessageSiloControllerBase
        where DTO : Entity
        where STATE : class
        where GRAIN : IEntityGrain<DTO, STATE>
    {
        protected readonly IEntityRepository repo;

        protected abstract EntityKind GetKind();

        public CRUDController(
            ILogger<CRUDController<DTO, STATE, GRAIN>> logger,
            IClusterClient client, IHttpContextAccessor httpContextAccessor,
            IEntityRepository repo)
            : base(logger, httpContextAccessor, client)
        {
            this.repo = repo;
        }

        [HttpGet()]
        public async Task<ApiContract<IEnumerable<STATE>>> Index()
        {
            var result = new List<STATE>();

            var entities = await repo.Query(GetKind(), loggedInUserId);

            foreach (var c in entities)
            {
                var entity = client!.GetGrain<GRAIN>(c.Id);
                result.Add(await entity.GetState());
            }

            return await Task.FromResult(new ApiContract<IEnumerable<STATE>>(httpContextAccessor, StatusCodes.Status200OK, data: result));
        }

        [HttpDelete(template: "{name}")]
        public async Task<ApiContract<STATE>> Delete(string name)
        {
            var id = $"{loggedInUserId}|{name}";
            var entity = client!.GetGrain<GRAIN>(id);

            var validationResults = await entityManagerGrain.Delete(name);

            if (validationResults is not null)
                return await Task.FromResult(new ApiContract<STATE>(httpContextAccessor, StatusCodes.Status400BadRequest, errors: validationResults));

            await entity.Delete();
            await repo.Delete(loggedInUserId, name);

            return await Task.FromResult(new ApiContract<STATE>(httpContextAccessor, StatusCodes.Status200OK));
        }

        [HttpGet(template: "{name}")]
        public async Task<ApiContract<STATE>> Show(string name)
        {
            var id = $"{loggedInUserId}|{name}";
            var entities = await repo.Query(GetKind(), loggedInUserId);

            if (!entities.Any(p => p.Id == id))
            {
                return await Task.FromResult(new ApiContract<STATE>(httpContextAccessor, StatusCodes.Status404NotFound));
            }

            var entity = client!.GetGrain<GRAIN>(id);

            return await Task.FromResult(new ApiContract<STATE>(httpContextAccessor, StatusCodes.Status200OK, data: await entity.GetState()));
        }

        [HttpPut(template: "{name}")]
        public async Task<ApiContract<STATE>> Update(string name, [FromBody] DTO dto)
        {
            dto.PartitionKey = loggedInUserId;

            var validationResults = await entityManagerGrain.Upsert(dto);

            if (validationResults is not null)
                return await Task.FromResult(new ApiContract<STATE>(httpContextAccessor, StatusCodes.Status400BadRequest, errors: validationResults));

            await repo.Upsert(
                new Entity()
                {
                    PartitionKey = loggedInUserId,
                    RowKey = name,
                    Kind = GetKind()
                }
            );

            var entity = client!.GetGrain<GRAIN>(dto.Id);
            await entity.Update(dto);

            return await Task.FromResult(new ApiContract<STATE>(httpContextAccessor, StatusCodes.Status200OK, data: await entity.GetState()));
        }
    }
}
