﻿using MessageSilo.BlazorApp.Components.DeadLetterCorrector;
using MessageSilo.BlazorApp.Services;
using MessageSilo.BlazorApp.ViewModels;
using MessageSilo.Features.DeadLetterCorrector;
using MessageSilo.Shared.Models;
using MessageSilo.Shared.Platforms;
using Microsoft.AspNetCore.Components;
using System.Collections.Generic;

namespace MessageSilo.BlazorApp.Pages
{
    public partial class DeadLetterCorrectorPage
    {
        [Inject]
        public IMessageSiloAPIService MessageSiloAPI { get; set; }

        [Parameter]
        public string Id { get; set; }

        public ConnectionSettingsDTO ConnectionSettings { get; set; }

        public List<IPlatform> Platforms { get; set; } = new List<IPlatform>() { new AzurePlatform(), new AWSPlatform() };

        public List<CorrectedMessageViewModel> CorrectedMessages { get; set; }

        public DateTimeOffset From { get; set; } = DateTimeOffset.Now.AddHours(-1);

        public DateTimeOffset To { get; set; } = DateTimeOffset.Now;

        protected override async Task OnInitializedAsync()
        {
            var id = Guid.Parse(Id);

            if (id == Guid.Empty)
                ConnectionSettings = new ConnectionSettingsDTO();
            else
            {
                ConnectionSettings = await MessageSiloAPI.GetDeadLetterCorrector(id);
                await FilterMessages();
            }
        }

        public async Task FilterMessages()
        {
            CorrectedMessages = (await MessageSiloAPI.GetCorrectedMessages(Guid.Parse(Id), From, To)).Select(p => new CorrectedMessageViewModel()
            {
                CorrectedMessage = p
            }).ToList();
        }

        public async Task Save()
        {
            await MessageSiloAPI.UpsertDeadLetterCorrector(ConnectionSettings);
        }
    }
}
