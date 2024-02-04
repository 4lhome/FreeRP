using FreeRP.Net.Client.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeRP.Net.Client;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Add common services required by the FreeRP for Blazor library
    /// </summary>
    /// <param name="services">Service collection</param>
    public static IServiceCollection AddFreeRP(this IServiceCollection services)
    {
        services.AddScoped<Translation.I18nService>();
        services.AddScoped<Services.ConnectService>();
        services.AddScoped<Services.PdfService>();
        services.AddScoped<Services.DatabaseService>();
        services.AddScoped<Services.AdminService>();
        services.AddScoped<Services.UserService>();
        services.AddFluentUIComponents();
        services.AddScoped<Dialog.IBusyDialogService, Dialog.BusyDialogService>();

        return services;
    }
}
