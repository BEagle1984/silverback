using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Common.Domain;
using Microsoft.AspNetCore.Diagnostics;
using Newtonsoft.Json;

namespace Common.Api
{
    public class JsonExceptionsMiddleware
    {
        public async Task InvokeAsync(HttpContext context)
        {
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            var ex = context.Features.Get<IExceptionHandlerFeature>()?.Error;
            if (ex == null)
                return;

            context.Response.StatusCode = GetStatusCode(ex);

            var error = new
            {
                message = ex.Message
            };

            context.Response.ContentType = "application/json";

            using (var writer = new StreamWriter(context.Response.Body))
            {
                new JsonSerializer().Serialize(writer, error);
                await writer.FlushAsync().ConfigureAwait(false);
            }
        }

        private int GetStatusCode(Exception ex)
        {
            switch (ex)
            {
                case DomainValidationException dex:
                    return (int) HttpStatusCode.BadRequest;
                default:
                    return (int) HttpStatusCode.InternalServerError;
            }
        }
    }
}
