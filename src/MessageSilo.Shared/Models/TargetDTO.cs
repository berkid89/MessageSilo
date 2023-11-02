﻿using MessageSilo.Shared.Enums;
using MessageSilo.Shared.Serialization;

namespace MessageSilo.Shared.Models
{
    [GenerateSerializer]
    public class TargetDTO : Entity
    {
        //Common
        [Id(0)]
        public TargetType? Type { get; set; }

        //API
        [Id(1)]
        public string Url { get; set; }

        //Azure_EventGrid
        [Id(2)]
        public string Endpoint { get; set; }

        [Id(3)]
        public string AccessKey { get; set; }

        public TargetDTO()
        {
            Kind = EntityKind.Target;
        }

        public async Task Encrypt(string password)
        {
            if (AccessKey is not null)
                AccessKey = await encryptAsync(AccessKey, password);
        }

        public async Task Decrypt(string password)
        {
            if (AccessKey is not null)
                AccessKey = await decryptAsync(AccessKey, password);
        }

        public override string ToString()
        {
            return YamlConverter.Serialize(this);
        }
    }
}
