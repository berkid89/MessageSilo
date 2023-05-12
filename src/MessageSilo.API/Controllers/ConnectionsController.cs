﻿using FluentValidation;
using MessageSilo.Features.Connection;
using MessageSilo.Shared.DataAccess;
using MessageSilo.Shared.Enums;
using MessageSilo.Shared.Models;
using Orleans;

namespace MessageSilo.API.Controllers
{
    public class ConnectionsController : CRUDController<ConnectionSettingsDTO, ConnectionState, IConnectionGrain>
    {
        protected override EntityKind GetKind() => EntityKind.Connection;

        public ConnectionsController(ILogger<CRUDController<ConnectionSettingsDTO, ConnectionState, IConnectionGrain>> logger, IClusterClient client, IHttpContextAccessor httpContextAccessor, IEntityRepository repo, IValidator<ConnectionSettingsDTO> validator) : base(logger, client, httpContextAccessor, repo, validator)
        {
        }
    }
}
