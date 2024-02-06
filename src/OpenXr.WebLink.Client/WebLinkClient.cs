﻿using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using OpenXr.WebLink.Entities;
using System.Text.Json;

namespace OpenXr.WebLink.Client
{
    public class WebLinkClient
    {
        private HubConnection? _connection;
        private string _endpoint;
        private bool _isStarted;

        public WebLinkClient(string endpoint) 
        {
            _endpoint = endpoint;   
        }

        public async Task DisconnectAsync()
        {
            _isStarted = false;

            if (_connection != null)
            {
                await _connection.DisposeAsync();
                _connection = null;
            }

        }

        public async Task ConnectAsync(string accessToken)
        {
            await DisconnectAsync();

            _isStarted = true;

            _connection = new HubConnectionBuilder()
                .WithUrl($"{_endpoint}/hub/openxr", options =>
                {
                    options.AccessTokenProvider = () => Task.FromResult((string?)accessToken);
                })
                .AddJsonProtocol(options =>
                {
                    options.PayloadSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                    options.PayloadSerializerOptions.PropertyNameCaseInsensitive = true;
                    options.PayloadSerializerOptions.IncludeFields = true;
                })
            .Build();

            HubConnection x;

            _connection.Closed += async (error) =>
            {
                if (!_isStarted)
                    return;
                await Task.Delay(new Random().Next(0, 5) * 1000);
                await _connection.StartAsync();
            };

            await _connection.StartAsync();

            _isStarted = true;
        }

        protected async Task<bool> EnsureConnectedAsync()
        {
            if (_connection == null)
                return false;

            if (_connection.State == HubConnectionState.Disconnected)
                await _connection.StartAsync();

            return true;
        }

        public async Task StartSessionAsync()
        {
            if (!await EnsureConnectedAsync())
                return;
            await _connection!.InvokeAsync("StartSession");

        }

        public async Task StopSessionAsync()
        {
            if (!await EnsureConnectedAsync())
                return;
            await _connection!.InvokeAsync("StopSession");
        }

        public async Task<IList<XrAnchorDetails>?> GetAnchorsAsync(XrAnchorFilter filter)
        {
            if (!await EnsureConnectedAsync())
                return null;
            return await _connection!.InvokeAsync<IList<XrAnchorDetails>>("GetAnchors", filter);
        }

    }
}
