﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using XOutput.Core.DependencyInjection;
using NLog;
using XOutput.Core.External;
using XOutput.Server.Websocket;
using XOutput.Server.Rest;

namespace XOutput.Server.Http
{
    public class HttpServer
    {
        private static ILogger logger = LogManager.GetCurrentClassLogger();

        private readonly CommandRunner commandRunner;
        private readonly RestService restService;
        private readonly WebSocketService webSocketService;

        private bool running;
        private CancellationTokenSource cancellationTokenSource;
        private HttpListener listener;

        [ResolverMethod]
        public HttpServer(CommandRunner commandRunner, RestService restService, WebSocketService webSocketService)
        {
            this.commandRunner = commandRunner;
            this.restService = restService;
            this.webSocketService = webSocketService;
        }

        public void Start(string uri)
        {
            if (running)
            {
                return;
            }
            cancellationTokenSource = new CancellationTokenSource();
            listener = new HttpListener();
            listener.Prefixes.Add(uri);
            try
            {
                listener.Start();
            } catch(HttpListenerException ex)
            {
                logger.Warn(ex);
                var domainUser = WindowsIdentity.GetCurrent().Name;
                commandRunner.RunCmd($"netsh http add urlacl url={uri} user={domainUser}");
                listener = new HttpListener();
                listener.Prefixes.Add(uri);
                listener.Start();
            }
            running = true;
            Task.Run(() => AcceptClientsAsync(listener));
        }

        public void AddPersmissions(string uri)
        {
            var domainUser = WindowsIdentity.GetCurrent().Name;
            commandRunner.RunCmd($"netsh http add urlacl url={uri} user={domainUser}");
        }

        public void Stop()
        {
            if (listener != null && running)
            {
                try
                {
                    cancellationTokenSource.Cancel();
                    running = false;
                    listener.Stop();
                    listener = null;
                }
                catch
                {
                    logger.Error("Failed to stop http server");
                }
            }
        }

        private async Task AcceptClientsAsync(HttpListener server)
        {
            while (running)
            {
                try
                {
                    var httpContext = await server.GetContextAsync();
                    if (!webSocketService.Handle(httpContext, cancellationTokenSource.Token) && !restService.Handle(httpContext))
                    {
                        httpContext.Response.StatusCode = 404;
                        httpContext.Response.Close();
                    }
                } 
                catch (Exception ex)
                {
                    logger.Error(ex, "Failed to handle connection");
                }
            }
        }
    }
}