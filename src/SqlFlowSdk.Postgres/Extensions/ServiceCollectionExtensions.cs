// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SqlServerFlowSdk.Core;
using SqlServerFlowSdk.Workers;

namespace SqlServerFlowSdk.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSqlServerFlowWorker(this IServiceCollection services, string queueName, Action<SqlServerFlowWorkerBuilder> configure)
    {
        var registryDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(SqlServerFlowRegistry));

        SqlServerFlowRegistry registry;

        if (registryDescriptor == null)
        {
            registry = new SqlServerFlowRegistry();
            services.AddSingleton(registry);
            services.AddTransient<IJobPublisher, SqlServerFlowJobPublisher>();
        }
        else
        {
            registry = (SqlServerFlowRegistry)registryDescriptor.ImplementationInstance!;
        }

        var workerConfig = new WorkerConfiguration { QueueName = queueName };

        registry.WorkerConfigs.Add(workerConfig);

        var builder = new SqlServerFlowWorkerBuilder(services, registry, workerConfig);

        configure(builder);

        // One to One Pattern. One Worker per Queue. This simplifies the design and
        // avoids complexities of multiple workers consuming from the same queue.
        services.AddSingleton<IHostedService>(sp =>
        {
            return new GenericSqlServerFlowWorker(
                client: sp.GetRequiredService<ISqlFlow>(),
                provider: sp,
                registry: sp.GetRequiredService<SqlServerFlowRegistry>(),
                logger: sp.GetRequiredService<ILogger<GenericSqlServerFlowWorker>>(),
                queueName: queueName
            );
        });

        return services;
    }

    public static IServiceCollection AddSqlServerFlowSdk(this IServiceCollection services, string connectionString)
    {
        // Register the SqlServerFlow Client as a Singleton since it manages its own
        // connection pooling (via ADO.NET) and is thread-safe.
        services.AddSingleton<ISqlFlow>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<SqlServerFlow>>();

            return new SqlServerFlow(logger, connectionString);
        });

        // Register Publish Abstraction
        services.AddTransient<IEventPublisher, SqlServerFlowEventPublisher>();

        return services;
    }
}