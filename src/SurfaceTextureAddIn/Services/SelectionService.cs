using System;
using SurfaceTextureAddIn.Core;
using SurfaceTextureAddIn.Models;

namespace SurfaceTextureAddIn.Services;

internal sealed class SelectionService
{
    public TextureExecutionContext BuildContext(
        dynamic swApp,
        TextureParameters parameters,
        TextureOperationMode mode,
        object? selectedTargetFace = null,
        object? selectedSeedBody = null)
    {
        object? activeDocObject = swApp?.ActiveDoc;
        if (activeDocObject is null)
        {
            throw new InvalidOperationException("No active SolidWorks document is open.");
        }
        dynamic activeDoc = activeDocObject;

        selectedTargetFace ??= TryGetSelectedObject(activeDoc, SwApiConstants.SelectionTypeFaces);
        selectedSeedBody ??= TryGetSelectedObject(activeDoc, SwApiConstants.SelectionTypeBodies);

        if (selectedTargetFace is null)
        {
            throw new InvalidOperationException("Please pre-select one target face.");
        }

        if (selectedSeedBody is null)
        {
            throw new InvalidOperationException("Please pre-select one texture seed body.");
        }

        dynamic targetFace = selectedTargetFace;
        object? targetBody = null;

        try
        {
            targetBody = targetFace.GetBody();
        }
        catch
        {
            targetBody = null;
        }

        return new TextureExecutionContext
        {
            ActiveDocument = activeDoc,
            TargetFace = selectedTargetFace,
            TargetBody = targetBody,
            SelectedSeedBody = selectedSeedBody,
            Parameters = parameters,
            Mode = mode,
        };
    }

    private static object? TryGetSelectedObject(dynamic activeDoc, int selectionType)
    {
        object? selectionManagerObject = activeDoc?.SelectionManager;
        if (selectionManagerObject is null)
        {
            return null;
        }
        dynamic selectionManager = selectionManagerObject;

        int count;
        try
        {
            count = selectionManager.GetSelectedObjectCount2(-1);
        }
        catch
        {
            return null;
        }

        for (var index = 1; index <= count; index++)
        {
            int type;
            try
            {
                type = selectionManager.GetSelectedObjectType3(index, -1);
            }
            catch
            {
                continue;
            }

            if (type != selectionType)
            {
                continue;
            }

            try
            {
                return selectionManager.GetSelectedObject6(index, -1);
            }
            catch
            {
                return null;
            }
        }

        return null;
    }
}
