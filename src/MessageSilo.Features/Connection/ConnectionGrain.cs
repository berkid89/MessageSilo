﻿using MessageSilo.Domain.Entities;
using MessageSilo.Domain.Enums;
using MessageSilo.Domain.Helpers;
using MessageSilo.Features.AWS;
using MessageSilo.Features.Azure;
using MessageSilo.Features.Enricher;
using MessageSilo.Features.EntityManager;
using MessageSilo.Features.RabbitMQ;
using MessageSilo.Features.Target;
using MessageSilo.Shared.Models;
using Microsoft.Extensions.Logging;

namespace MessageSilo.Features.Connection
{
    public class ConnectionGrain : Grain, IConnectionGrain
    {
        private readonly ILogger<ConnectionGrain> logger;

        private readonly IGrainFactory grainFactory;

        private string? targetId { get; set; }

        private EntityKind? targetKind { get; set; }

        private MessagePlatformType? messagePlatformType { get; set; }

        private List<string> enrichers = new List<string>();

        public ConnectionGrain(ILogger<ConnectionGrain> logger, IGrainFactory grainFactory)
        {
            this.logger = logger;
            this.grainFactory = grainFactory;
        }

        public async Task Init(bool force = false)
        {
            if (!force && messagePlatformType is not null)
                return;

            var (userId, name, scaleSet) = this.GetPrimaryKeyString().Explode();

            try
            {
                var em = grainFactory.GetGrain<IEntityManagerGrain>(userId);
                var settings = await em.GetConnectionSettings(name);

                if (settings == null)
                    return;

                if (settings.TargetId is not null)
                {
                    targetId = $"{settings.TargetId}#{scaleSet}";
                    targetKind = settings.TargetKind;
                }

                messagePlatformType = settings.Type.Value;
                enrichers = settings.Enrichers.ToList();
                var messagePlatformConnection = getMessagePlatformConnection();
                await messagePlatformConnection.Init(settings);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"[Connection][{name}][#{scaleSet}] Initialization error");
            }
        }

        public async Task Delete()
        {
            var messagePlatformConnection = getMessagePlatformConnection();
            await messagePlatformConnection.DisposeAsync();
        }

        public async Task Send(Message message)
        {
            try
            {
                await Init();

                var messagePlatformConnection = getMessagePlatformConnection();
                await messagePlatformConnection.Enqueue(message);
            }
            catch (Exception ex)
            {
                var (userId, name, scaleSet) = this.GetPrimaryKeyString().Explode();
                logger.LogError(ex, $"[Connection][{name}][#{scaleSet}] Cannot enqueue message [{message?.Id}]");
                throw;
            }
        }

        public async Task<bool> TransformAndSend(Message message)
        {
            try
            {
                await Init();

                var (userId, name, scaleSet) = this.GetPrimaryKeyString().Explode();

                foreach (var enricherName in enrichers)
                {
                    if (message is null)
                        return false;

                    var enricherGrain = grainFactory.GetGrain<IEnricherGrain>($"{userId}|{enricherName}#{scaleSet}");

                    message = await enricherGrain.Enrich(message);
                }

                if (message is null)
                    return false;

                if (targetId is not null)
                    await getTarget().Send(message);

                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task Health()
        {

        }

        private IMessageSenderGrain getTarget()
        {
            return targetKind switch
            {
                EntityKind.Connection => grainFactory.GetGrain<IConnectionGrain>(targetId),
                EntityKind.Target => grainFactory.GetGrain<ITargetGrain>(targetId),
                _ => throw new NotSupportedException(),
            };
        }

        private IMessagePlatformConnectionGrain getMessagePlatformConnection()
        {
            return messagePlatformType switch
            {
                MessagePlatformType.Azure_Queue => grainFactory.GetGrain<IAzureServiceBusConnectionGrain>(this.GetPrimaryKeyString()),
                MessagePlatformType.Azure_Topic => grainFactory.GetGrain<IAzureServiceBusConnectionGrain>(this.GetPrimaryKeyString()),
                MessagePlatformType.RabbitMQ => grainFactory.GetGrain<IRabbitMQConnectionGrain>(this.GetPrimaryKeyString()),
                MessagePlatformType.AWS_SQS => grainFactory.GetGrain<IAWSSQSConnectionGrain>(this.GetPrimaryKeyString()),
                _ => throw new NotSupportedException(),
            };
        }
    }
}
