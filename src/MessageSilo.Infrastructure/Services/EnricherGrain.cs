﻿using MessageSilo.Application.DTOs;
using MessageSilo.Domain.Entities;
using MessageSilo.Domain.Enums;
using MessageSilo.Domain.Helpers;
using MessageSilo.Domain.Interfaces;
using MessageSilo.Infrastructure.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RestSharp;

namespace MessageSilo.Infrastructure.Services
{
    public class EnricherGrain : Grain, IEnricherGrain
    {
        private readonly ILogger<EnricherGrain> logger;

        private readonly IGrainFactory grainFactory;

        private readonly IConfiguration configuration;

        private IEnricher enricher { get; set; }

        public EnricherGrain(ILogger<EnricherGrain> logger, IConfiguration configuration, IGrainFactory grainFactory)
        {
            this.logger = logger;
            this.configuration = configuration;
            this.grainFactory = grainFactory;
        }

        public override async Task OnActivateAsync(CancellationToken cancellationToken)
        {
            await Init();

            await base.OnActivateAsync(cancellationToken);
        }

        public async Task<Message?> Enrich(Message message)
        {
            try
            {
                message.Body = await enricher.TransformMessage(message.Body);
                return message;
            }
            catch (Exception ex)
            {
                var (userId, name, scaleSet) = this.GetPrimaryKeyString().Explode();
                logger.LogError(ex, $"[Enricher][{name}][#{scaleSet}] Cannot enrich message [{message?.Id}]");
                throw;
            }
        }

        public async Task Init()
        {
            var (userId, name, scaleSet) = this.GetPrimaryKeyString().Explode();

            try
            {
                var em = grainFactory.GetGrain<IEntityManagerGrain>(userId);
                var settings = await em.GetEnricherSettings(name);

                if (settings == null)
                    return;

                enricher = getEnricher(settings);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"[Enricher][{name}][#{scaleSet}] Initialization error");
            }
        }

        private IEnricher getEnricher(EnricherDTO dto)
        {
            return dto.Type switch
            {
                EnricherType.Inline => new InlineEnricher(dto.Function),
                EnricherType.API => new APIEnricher(dto.Url, dto.Method ?? Method.Post, dto.Retry ?? new()),
                EnricherType.AI => new AIEnricher(
                    new AIService(
                        dto.ApiKey ?? configuration["AI_API_KEY"],
                        dto.Model ?? configuration["AI_MODEL"]
                        ),
                    dto.Command),
                _ => throw new NotSupportedException(),
            };
        }
    }
}
