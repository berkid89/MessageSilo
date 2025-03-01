﻿using MessageSilo.Application.DTOs;
using MessageSilo.Domain.Entities;
using MessageSilo.Domain.Enums;
using MessageSilo.Domain.Helpers;
using MessageSilo.Domain.Interfaces;
using MessageSilo.Infrastructure.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MessageSilo.Infrastructure.Services
{
    public class TargetGrain : Grain, ITargetGrain
    {
        private readonly ILogger<TargetGrain> logger;

        private readonly IGrainFactory grainFactory;

        private readonly IConfiguration configuration;

        private ITarget target;

        public TargetGrain(ILogger<TargetGrain> logger, IGrainFactory grainFactory, IConfiguration configuration)
        {
            this.logger = logger;
            this.grainFactory = grainFactory;
            this.configuration = configuration;
        }

        public override async Task OnActivateAsync(CancellationToken cancellationToken)
        {
            await Init();

            await base.OnActivateAsync(cancellationToken);
        }

        public async Task Send(Message message)
        {
            try
            {
                await target.Send(message);
            }
            catch (Exception ex)
            {
                var (userId, name, scaleSet) = this.GetPrimaryKeyString().Explode();
                logger.LogError(ex, $"[Target][{name}][#{scaleSet}] Cannot send message [{message?.Id}]");
                throw;
            }
        }

        public async Task Init()
        {
            var (userId, name, scaleSet) = this.GetPrimaryKeyString().Explode();

            try
            {
                var em = grainFactory.GetGrain<IEntityManagerGrain>(userId);
                var settings = await em.GetTargetSettings(name);

                if (settings == null)
                    return;

                target = getTarget(settings);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"[Target][{name}][#{scaleSet}] Initialization error");
            }
        }

        private ITarget getTarget(TargetDTO dto)
        {
            return dto.Type switch
            {
                TargetType.API => new APITarget(dto.Url, dto.Retry ?? new()),
                TargetType.Azure_EventGrid => new AzureEventGridTarget(dto.Endpoint, dto.AccessKey),
                TargetType.AI_Router => new AIRouter(dto.UserId, new AIService(
                        dto.ApiKey ?? configuration["AI_API_KEY"],
                        dto.Model ?? configuration["AI_MODEL"]
                        ), dto.Rules, grainFactory),
                _ => throw new NotSupportedException(),
            };
        }
    }
}
