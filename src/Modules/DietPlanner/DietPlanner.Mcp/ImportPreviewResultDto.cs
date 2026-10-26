namespace DietPlanner.Mcp;

using DietPlanner.Application.Commands.PreviewImport;

/// <summary>
/// An import preview plus, when it can execute, the token that lets <c>execute_plan_import</c> run exactly this plan.
/// </summary>
/// <param name="Preview">What executing the plan would do.</param>
/// <param name="PreviewToken">Token binding the caller, options and resolution; <see langword="null"/> when the plan cannot execute.</param>
/// <param name="ExpiresAt">When the token stops being accepted.</param>
public sealed record ImportPreviewResultDto(ImportPreviewDto Preview, string? PreviewToken, DateTimeOffset? ExpiresAt);
