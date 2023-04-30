﻿using Amazon.Util;
using RestSharp.Authenticators;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InfluxDB.Client.Api.Domain;

namespace MessageSilo.SiloCTL.Options
{
    public abstract class AuthorizedOptions : Options
    {
        protected readonly AuthAPIService authApi;

        protected readonly MessageSiloAPIService api;

        public AuthorizedOptions() : base()
        {
            authApi = new AuthAPIService(config);

            if (!authApi.IsValidtoken())
            {
                Process.Start(new ProcessStartInfo(authApi.GetAuthUrl()) { UseShellExecute = true });

                var code = authApi.HandleAuthResponse();

                config.Token = authApi.GetToken(code);

                config.Save();
            }

            var restOptions = new RestClientOptions(this.config.ApiUrl)
            {
                RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true,
                Authenticator = new JwtAuthenticator(this.config.Token)
            };

            api = new MessageSiloAPIService(new RestClient(restOptions));
        }
    }
}
