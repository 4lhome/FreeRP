using InvoiceReader;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.FluentUI.AspNetCore.Components;
using FreeRP.Net.Client;
using FreeRP.Net.Client.Translation;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddFreeRP();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var i18n = scope.ServiceProvider.GetRequiredService<I18nService>();
    string code = "de";
    await i18n.LoadTextAsync(code);
}

await app.RunAsync();
