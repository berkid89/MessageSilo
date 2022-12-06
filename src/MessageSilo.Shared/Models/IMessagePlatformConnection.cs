﻿using MessageSilo.Shared.Enums;

namespace MessageSilo.Shared.Models
{
    public interface IMessagePlatformConnection
    {
        string Name { get; }

        string ConnectionString { get; }

        MessagePlatformType Type { get; }

        void InitDeadLetterCorrector();

        Task<IEnumerable<Message>> GetDeadLetterMessagesAsync();
    }
}
