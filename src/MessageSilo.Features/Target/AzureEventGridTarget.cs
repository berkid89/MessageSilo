﻿using MessageSilo.Domain.Entities;
using MessageSilo.Shared.Models;
using RestSharp;

namespace MessageSilo.Features.Target
{
    public class AzureEventGridTarget : ITarget
    {
        private readonly string endpoint;

        private readonly string accessKey;

        private IRestClient client = new RestClient();

        public AzureEventGridTarget(string endpoint, string accessKey)
        {
            this.endpoint = endpoint;
            this.accessKey = accessKey;
        }

        public async Task Send(Message message)
        {
            var request = new RestRequest(endpoint, Method.Post);
            request.AddHeader("aeg-sas-key", accessKey);
            request.AddBody(message.Body, contentType: ContentType.Json);

            var response = await client.ExecutePostAsync(request);

            if (!response.IsSuccessful)
                throw response.ErrorException;
        }
    }
}
