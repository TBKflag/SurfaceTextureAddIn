using System;
using System.Collections.Generic;
using System.Linq;
using SurfaceTextureAddIn.Models;
using SurfaceTextureAddIn.Services;
using SurfaceTextureAddIn.UI;

namespace SurfaceTextureAddIn.Commands;

internal sealed class TextureCommandController
{
    private readonly dynamic swApp;
    private readonly SelectionService selectionService = new();
    private readonly TextureSeedAnalyzer seedAnalyzer = new();
    private readonly FaceSampler faceSampler = new();
    private readonly TexturePlacementEngine placementEngine = new();
    private readonly BooleanBuilder booleanBuilder = new();
    private readonly TextureExecutionValidator executionValidator = new();
    private readonly TexturePropertyManagerPage propertyManagerPage;

    public TextureCommandController(dynamic swApp)
    {
        this.swApp = swApp ?? throw new ArgumentNullException(nameof(swApp));
        propertyManagerPage = new TexturePropertyManagerPage();
    }

    public void Run(TextureOperationMode mode)
    {
        var pageResult = propertyManagerPage.Show(swApp, mode);
        if (pageResult is null)
        {
            return;
        }

        try
        {
            var context = selectionService.BuildContext(
                swApp,
                pageResult.Parameters,
                mode,
                pageResult.SelectedTargetFace,
                pageResult.SelectedSeedBody);

            context.Seed = seedAnalyzer.Analyze(pageResult.SelectedSeedBody ?? GetSelectedSeedBody(context.ActiveDocument));

            var samples = faceSampler.SampleFace(context.TargetFace!, context.Parameters);
            var placements = placementEngine.BuildPlacements(samples, context.Seed, context.Parameters);
            var warnings = executionValidator.Validate(context, samples, placements);
            if (placements.Count == 0)
            {
                ShowMessage(string.Join(Environment.NewLine, warnings.DefaultIfEmpty("No valid placements were generated on the selected face.")), isWarning: true);
                return;
            }

            var logs = new List<string>();
            Action<string> appendLog = logs.Add;
            var successCount = booleanBuilder.Apply(swApp, context, placements, appendLog);
            var summary = $"Generated {successCount} texture instances out of {placements.Count} candidate placements.";
            if (warnings.Count > 0)
            {
                summary += $"{Environment.NewLine}{Environment.NewLine}Warnings:{Environment.NewLine}- {string.Join(Environment.NewLine + "- ", warnings)}";
            }

            if (logs.Count > 0)
            {
                summary += $"{Environment.NewLine}{Environment.NewLine}Issues:{Environment.NewLine}- {string.Join(Environment.NewLine + "- ", logs.Take(12))}";
            }

            ShowMessage(summary, isWarning: logs.Count > 0);
        }
        catch (Exception ex)
        {
            ShowMessage(ex.Message, isWarning: true);
        }
    }

    private object GetSelectedSeedBody(object activeDocument)
    {
        dynamic selectionManager = ((dynamic)activeDocument).SelectionManager;
        var count = selectionManager.GetSelectedObjectCount2(-1);
        for (var index = 1; index <= count; index++)
        {
            var candidate = selectionManager.GetSelectedObject6(index, -1);
            try
            {
                ((dynamic)candidate).GetFaces();
                return candidate;
            }
            catch
            {
                // Keep searching.
            }
        }

        throw new InvalidOperationException("Unable to locate the selected texture body.");
    }

    private void ShowMessage(string message, bool isWarning)
    {
        try
        {
            swApp.SendMsgToUser2(message, isWarning ? 1 : 2, 0);
        }
        catch
        {
            System.Windows.Forms.MessageBox.Show(message, "Surface Texture Add-In");
        }
    }
}
