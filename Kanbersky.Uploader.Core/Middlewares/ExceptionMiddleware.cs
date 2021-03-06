﻿using Kanbersky.Uploader.Core.Extensions;
using Kanbersky.Uploader.Core.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kanbersky.Uploader.Core.Middlewares
{
    public class ExceptionMiddleware
    {
        #region fields

        private readonly RequestDelegate _next;
        private static readonly ILogger Logger = Log.ForContext<ExceptionMiddleware>();

        #endregion

        #region ctor

        public ExceptionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        #endregion

        #region methods

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await InternalServerError(context, ex, ex.Message);
            }
        }

        #endregion

        #region private methods

        private static async Task InternalServerError(HttpContext context, Exception exception, string data, string contentType = "text/plain")
        {
            await Task.Run(() =>
            {
                var request = context.Request;
                var encodedPathAndQuery = request.GetEncodedPathAndQuery();

                var logModel = new LoggingModel(request.Host.Host, request.Protocol, request.Method, request.Path, encodedPathAndQuery, StatusCodes.Status500InternalServerError)
                {
                    RequestHeaders = request.Headers.ToDictionary(x => x.Key, x => (object)x.Value.ToString()),
                    RequestBody = string.Empty,
                    Exception = exception,
                    Data = data
                };

                Logger.HandleLogging(logModel).Error(LogTemplates.Error);
            });

            context.Response.Clear();
            context.Response.ContentType = contentType;
            context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsync("Sunucuda beklenmeyen bir hata oluştu.", Encoding.UTF8);
        }

        #endregion
    }
}
