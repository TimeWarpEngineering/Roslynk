#!/usr/bin/env -S dotnet --
#region Purpose
// Dev CLI entry point - discovers and runs Nuru endpoints
#endregion
#region Design
// Thin Nuru wrapper. Shared endpoints (clean, self-install) come from TimeWarp.Nuru.DevCli
// and need the services registered below. check-version and release are excluded in
// Directory.Build.targets because they assume the TimeWarp source/ layout. Local endpoints:
// build and test, both against Source/Morris.Roslynk.slnx.
//
// Usage:
//   As runfile:  dotnet run --file tools/dev-cli/dev.cs -- <command>
//   As AOT:      ./bin/dev <command>   (bootstrap: dotnet run --file tools/dev-cli/dev.cs -- self-install)
#endregion

using TimeWarp.Nuru;

NuruApp app = NuruApp.CreateBuilder()
  .WithName("dev")
  .WithDescription("Development CLI for Roslynk")
  .ConfigureServices(services =>
  {
    services.AddSingleton<IRepoCleanService, RepoCleanService>();
    services.AddSingleton<NuGetVersionService>();
    services.AddSingleton<IRepoConfigService, RepoConfigService>();
    services.AddSingleton<IPackableProjectService, PackableProjectService>();
  })
  .DiscoverEndpoints()
  .Build();

return await app.RunAsync(args);
