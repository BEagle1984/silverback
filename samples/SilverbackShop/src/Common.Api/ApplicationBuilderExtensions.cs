using Microsoft.AspNetCore.Builder;

namespace Common.Api
{
    public static class ApplicationBuilderExtensions
    {
        /// <summary>
        /// Configures the <see cref="JsonExceptionsMiddleware"/> to return all unhandled exceptions as JSON.
        /// </summary>
        /// <param name="app">The application.</param>
        /// <returns></returns>
        public static IApplicationBuilder ReturnExceptionsAsJson(this IApplicationBuilder app)
        {
            app.UseExceptionHandler(options => options.Run(new JsonExceptionsMiddleware().InvokeAsync));

            return app;
        }
    }
}