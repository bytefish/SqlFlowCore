// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DotNet.Testcontainers;
using Testcontainers.MsSql;

namespace SqlServerFlowSdk.Tests.Docker
{
    public class DockerContainers
    {
        public static MsSqlContainer SqlServerContainer = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
            .WithName("sqlserver")
            .WithPassword("P@ssw0rd123!")
            .WithBindMount(Path.Combine(AppContext.BaseDirectory, "Resources\\sql\\ssf.sql"), "/var/opt/mssql/scripts/ssf.sql")
            .WithPortBinding(1433, 1433)
            .WithLogger(ConsoleLogger.Instance)
            .Build();

        public static async Task StartAllContainersAsync()
        {
            await SqlServerContainer.StartAsync();

            string scriptContent = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "Resources\\sql\\ssf.sql"));

            await SqlServerContainer.ExecScriptAsync(scriptContent);
        }

        public static async Task StopAllContainersAsync()
        {
            await SqlServerContainer.StopAsync();
        }
    }
}