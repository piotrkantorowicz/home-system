namespace DietPlanner.Api.Mcp;

using System.ComponentModel;
using DietPlanner.Application.Commands.PreviewImport;
using DietPlanner.Application.Commands.ValidateImport;
using Microsoft.AspNetCore.Authorization;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Shared.Abstractions.Cqrs;

/// <summary>
/// Monthly plan import: preview first, then execute exactly what was previewed. Imported meals are
/// planned, never marked as eaten.
/// </summary>
[McpServerToolType]
internal sealed class ImportTools
{
    [McpServerTool(Name = "preview_plan_import", ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false)]
    [Description("Checks a meal plan (products, recipes and dated meals, at most 31 days and 250 meals) and describes what importing it would create, reuse and plan, without changing anything. Names resolve against the caller's own catalog, ignoring case. Every problem has a JSON path such as schedule[4].meals[1].recipe. Occupied slots are conflicts unless appendToOccupiedSlots is true. When the plan can be imported, the result carries a previewToken valid for 30 minutes: show the preview to the user and pass the token to execute_plan_import once they agree.")]
    [Authorize(Policy = DietPlannerMcp.ReadScope)]
    public static async Task<ImportPreviewResultDto> PreviewPlanImport(
        RequestContext<CallToolRequestParams> context,
        ICommandDispatcher commands,
        ImportPreviewTokens tokens,
        [Description("The plan in the import file format: products (nutrition per 100 g, omit unknown values), recipes (ingredients by product name, amounts for the whole recipe) and schedule (ISO dates, meal type = one of the caller's meal slots, recipe by name).")] ImportDto plan,
        [Description("Allow meals in slots that already have planned meals.")] bool appendToOccupiedSlots = false,
        CancellationToken ct = default)
    {
        var subject = McpCaller.AuthSubject(context);
        var personId = McpCaller.PersonId(context);

        ImportPreviewDto preview = await commands.SendAsync<PreviewImportCommand, ImportPreviewDto>(
            new PreviewImportCommand(plan, subject, personId, appendToOccupiedSlots), ct);

        if (!preview.CanExecute)
            return new ImportPreviewResultDto(preview, null, null);

        var (token, expiresAt) = tokens.Issue(subject, personId, appendToOccupiedSlots, preview.Fingerprint);
        return new ImportPreviewResultDto(preview, token, expiresAt);
    }
}
