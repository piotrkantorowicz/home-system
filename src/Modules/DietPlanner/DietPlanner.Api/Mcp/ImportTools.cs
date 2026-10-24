namespace DietPlanner.Api.Mcp;

using System.ComponentModel;
using DietPlanner.Application.Commands.ExecutePreviewedImport;
using DietPlanner.Application.Commands.PreviewImport;
using DietPlanner.Application.Commands.ValidateImport;
using Microsoft.AspNetCore.Authorization;
using ModelContextProtocol;
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

    [McpServerTool(Name = "execute_plan_import", ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Imports a previewed plan in one all-or-nothing transaction: new products and recipes plus every planned meal (never marked eaten). Pass the exact plan and options you previewed, the previewToken, and a new operationKey. If anything changed since the preview, nothing is written and you must preview again. If the response is lost, call again with the same operationKey and plan to get the original result instead of importing twice.")]
    [Authorize(Policy = DietPlannerMcp.WriteScope)]
    [Authorize(Policy = DietPlannerMcp.ImportScope)]
    public static async Task<ExecutedImportDto> ExecutePlanImport(
        RequestContext<CallToolRequestParams> context,
        ICommandDispatcher commands,
        ImportPreviewTokens tokens,
        [Description("Exactly the plan that was previewed.")] ImportDto plan,
        [Description("previewToken from preview_plan_import.")] string previewToken,
        [Description("Idempotency key for this import, 8-128 characters (a UUID works). Reuse it only to retry this same import.")] string operationKey,
        [Description("The same appendToOccupiedSlots value used for the preview.")] bool appendToOccupiedSlots = false,
        CancellationToken ct = default)
    {
        var subject = McpCaller.AuthSubject(context);
        var personId = McpCaller.PersonId(context);

        // An unusable token still lets a retry replay its earlier result; it only blocks a first run.
        var (fingerprint, tokenError) = tokens.Read(previewToken, subject, personId, appendToOccupiedSlots);
        try
        {
            return await McpErrors.AsToolErrorsAsync(() => commands.SendAsync<ExecutePreviewedImportCommand, ExecutedImportDto>(
                new ExecutePreviewedImportCommand(plan, subject, personId, appendToOccupiedSlots, fingerprint, operationKey), ct));
        }
        catch (McpException) when (tokenError is not null)
        {
            throw new McpException(tokenError);
        }
    }
}
