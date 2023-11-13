﻿using MessageSilo.Shared.Enums;
using MessageSilo.Shared.Models;
using Microsoft.Extensions.Logging;
using Orleans.Runtime;

namespace MessageSilo.Features.Target
{
    public class TargetGrain : Grain, ITargetGrain
    {
        private readonly ILogger<TargetGrain> logger;

        private readonly IGrainFactory grainFactory;

        private IPersistentState<TargetDTO> persistence { get; set; }

        private ITarget target;

        public TargetGrain([PersistentState("TargetState")] IPersistentState<TargetDTO> state, ILogger<TargetGrain> logger, IGrainFactory grainFactory)
        {
            persistence = state;
            this.logger = logger;
            this.grainFactory = grainFactory;
        }

        public override async Task OnActivateAsync(CancellationToken cancellationToken)
        {
            if (persistence.RecordExists)
                reInit();

            await base.OnActivateAsync(cancellationToken);
        }

        public async Task Send(Message message)
        {
            await target.Send(message);
        }

        public async Task Update(TargetDTO t)
        {
            persistence.State = t;
            await persistence.WriteStateAsync();
            reInit();
        }

        public async Task<TargetDTO> GetState()
        {
            return await Task.FromResult(persistence.State);
        }

        public async Task Delete()
        {
            await this.persistence.ClearStateAsync();
        }

        private void reInit()
        {
            var settings = persistence.State;

            switch (settings.Type)
            {
                case TargetType.API:
                    target = new APITarget(settings.Url);
                    break;
                case TargetType.Azure_EventGrid:
                    target = new AzureEventGridTarget(settings.Endpoint, settings.AccessKey);
                    break;
            }
        }
    }
}
