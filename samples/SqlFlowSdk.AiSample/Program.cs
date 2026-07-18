// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using SqlServerFlowSdk;
using SqlServerFlowSdk.AiSample;
using SqlServerFlowSdk.AiSample.Docker;
using SqlServerFlowSdk.AiSample.Models;
using SqlServerFlowSdk.AiSample.Services;
using SqlServerFlowSdk.Core;
using SqlServerFlowSdk.Extensions;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Start Docker Containers for dependencies
await DockerContainers.StartAllContainersAsync();

string connectionString = DockerContainers.SqlServerContainer.GetConnectionString();

// Add Logging
builder.Services.AddLogging(loggingBuilder => loggingBuilder.AddConsole());

builder.Services.AddSingleton<ILlmService, LlmService>();
builder.Services.AddSingleton<IGitHubService, GitHubService>();
builder.Services.AddSingleton<ILocalNotificationService, LocalNotificationService>();

// Register the SqlServerFlow SDK
builder.Services.AddSqlServerFlowSdk(connectionString);

// Configure Workers and Jobs. In this example, we have a queue for AI agents that process tasks related to bug fixing. The
// worker is configured to handle one task at a time and poll for new tasks every second. The job "solve-bug" is defined
// with a maximum of 3 attempts for each task.
builder.Services.AddSqlServerFlowWorker("ai-agent-queue", worker =>
{
    worker
        .SetConcurrency(1)
        .SetPollInterval(1);

    worker.AddJob<AutonomousAgentJob, AgentTask, AgentResult>("solve-bug", options =>
    {
        options.WithMaxAttempts(3);
    });
});

var app = builder.Build();

// A Webhook triggers the Agent, such as a new JIRA ticket or GitHub issue
app.MapPost("/agent/start", async (ISqlFlow client, [FromBody] AgentTask task, CancellationToken ct) =>
{
    var result = await client.SpawnAsync(new SpawnOptions
    {
        Queue = "ai-agent-queue"
    }, "solve-bug", task, ct);

    return Results.Ok(new { RunId = result.RunId, Status = $"Agent dispatched to fix Isse #{task.IssueId}" });
});

// A Lead-Developer clicks on "Approve" or "Reject", with Feeedback
app.MapPost("/agent/review/{issueId}/{correlationId}", async (
    IEventPublisher publisher,
    string issueId,
    string correlationId,
    [FromBody] HumanApproval approval,
    CancellationToken ct) =>
{
    // Wake up the agent, that is working on the ticket
    await publisher.EmitEventAsync(queue: "ai-agent-queue", eventName: $"agent-approval:{issueId}:{correlationId}", payload: approval, ct);

    string message = approval.Approved
        ? $"Fix for {correlationId} approved. Agent is now completing its work."
        : $"Fix for {correlationId} rejected. Agent tries again with feedback: '{approval.Reason}'";

    return Results.Ok(new { Message = message });
});

app.Run();