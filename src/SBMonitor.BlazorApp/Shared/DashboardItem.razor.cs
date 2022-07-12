﻿using Blazorise;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using SBMonitor.BlazorApp.Pages;
using SBMonitor.Core.Enums;
using SBMonitor.Core.Models;

namespace SBMonitor.BlazorApp.Shared
{
    public partial class DashboardItem
    {
        [Parameter]
        public ConnectionProps Connection { get; set; }

        [Parameter]
        public HttpClient ApiClient { get; set; }

        [Parameter]
        public ConnectionSettingsModal ConnectionSettingsModal { get; set; }

        [Parameter]
        public Func<Guid, Task> DeleteItemAction { get; set; }

        private string MessageDetails { get; set; }

        private HubConnection HubConnection;

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();

            HubConnection = new HubConnectionBuilder()
            .WithUrl($"{ApiClient.BaseAddress}MessageMonitor")
            .AddJsonProtocol()
            .Build();

            HubConnection.On<string>("ReceiveMessage", (message) =>
            {
                MessageDetails = message;
                InvokeAsync(StateHasChanged);
            });

            await HubConnection.StartAsync();

            await HubConnection.SendAsync("JoinGroup", Connection.Id);
        }

        public async Task Delete()
        {
            await DeleteItemAction(Connection.Id);
        }
    }
}
