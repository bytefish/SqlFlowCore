using Microsoft.Extensions.DependencyInjection;
using SqlServerFlowSdk.Configuration;
using SqlServerFlowSdk.Core;
using SqlServerFlowSdk.Workers;

namespace SqlServerFlowSdk.Extensions;

public class SqlServerFlowWorkerBuilder
{
    private readonly IServiceCollection _services;
    private readonly SqlServerFlowRegistry _registry;
    private readonly WorkerConfiguration _workerConfig;

    public SqlServerFlowWorkerBuilder(IServiceCollection services, SqlServerFlowRegistry registry, WorkerConfiguration workerConfig)
    {
        _services = services;
        _registry = registry;
        _workerConfig = workerConfig;

        if (!_registry.JobRegistrationsByQueue.ContainsKey(_workerConfig.QueueName))
        {
            _registry.JobRegistrationsByQueue[_workerConfig.QueueName] = new();
        }
    }

    public SqlServerFlowWorkerBuilder SetConcurrency(int concurrency) { _workerConfig.Concurrency = concurrency; return this; }

    public SqlServerFlowWorkerBuilder SetPollInterval(double pollIntervalInSeconds) { _workerConfig.PollIntervalInSeconds = pollIntervalInSeconds; return this; }

    public SqlServerFlowWorkerBuilder SetClaimTimeout(int seconds) { _workerConfig.ClaimTimeoutInSeconds = seconds; return this; }

    public SqlServerFlowWorkerBuilder SetBatchSize(int batchSize) { _workerConfig.BatchSize = batchSize; return this; }

    public SqlServerFlowWorkerBuilder SetFatalOnLeaseTimeout(bool fatal) { _workerConfig.FatalOnLeaseTimeout = fatal; return this; }

    public SqlServerFlowWorkerBuilder SetOnError(Action<Exception> handler) { _workerConfig.OnError = handler; return this; }

    public SqlServerFlowWorkerBuilder AddJob<TJob, TRequest, TResult>(string jobName, Action<JobOptionsBuilder>? configure = null)
        where TJob : class, IJob<TRequest, TResult>
    {
        var options = new JobOptions(jobName);
        configure?.Invoke(new JobOptionsBuilder(options));

        _services.AddTransient<TJob>();

        if (_registry.Routes.ContainsKey(options.Name))
        {
            throw new InvalidOperationException($"Job name '{options.Name}' has already been used.");
        }

        _registry.Routes[options.Name] = (typeof(TJob), _workerConfig.QueueName);

        _registry.JobRegistrationsByQueue[_workerConfig.QueueName].Add((client, provider) =>
        {
            client.UseJob<TJob, TRequest, TResult>(provider, options.Name, options.MaxAttempts);
            return Task.CompletedTask;
        });

        return this;
    }
}