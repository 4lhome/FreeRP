using FreeRP.Net.Server.GrpcServices;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeRP.Net.Server.Middleware
{
    public class Auth
    {
        private readonly RequestDelegate _next;

        public Auth(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext httpContext, Data.AuthService authService)
        {
            if (httpContext.User.Identity != null && httpContext.User.Identity.IsAuthenticated)
            {
                authService.SetUser(httpContext.User);
            }
            await _next(httpContext);
        }
    }
}
