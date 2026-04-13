/*
 * FILE: Program.cs
 * PROJECT: AssetManagement.Client
 * PURPOSE: Entry point for the Blazor WebAssembly application. Bootstraps the WASM
 *          runtime in the browser, registers the root component (App.razor) and the
 *          HeadOutlet for dynamic <head> content, and configures dependency injection.
 *          Registers ServerApiClient (HTTP client pointed at the server base address)
 *          and ClientApplicationState (scoped singleton) into the DI container so they
 *          can be injected into any page or component via @inject.
 */

using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using AssetManagement.Client;
using AssetManagement.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// HttpClient base address points to the Server project
builder.Services.AddScoped(sp =>
    new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddScoped<ServerApiClient>();
builder.Services.AddScoped<ClientApplicationState>();

await builder.Build().RunAsync();
