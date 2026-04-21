using System;
using SolidWorks.Interop.sldworks;
using SurfaceTextureAddIn.Core;
using SurfaceTextureAddIn.Models;

namespace SurfaceTextureAddIn.Services;

internal sealed class SelectionService
{
    public TextureExecutionContext BuildContext(
        SldWorks swApp,
        TextureParameters parameters,
        TextureOperationMode mode,
        object? selectedTargetFace = null,
        object? selectedSeedBody = null)
    {
        // Use SldWorks (early-bound interop), not dynamic — IDispatch on ActiveDoc often throws TYPE_E_ELEMENTNOTFOUND (0x8002802B).
        ModelDoc2? activeDoc = swApp.IActiveDoc2 as ModelDoc2;
        if (activeDoc is null)
        {
            throw new InvalidOperationException("No active SolidWorks document is open.");
        }

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

        object? targetBody = null;
        try
        {
            if (selectedTargetFace is Face2 face2)
            {
                targetBody = face2.GetBody();
            }
            else
            {
                targetBody = ((dynamic)selectedTargetFace!).GetBody();
            }
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

    private static object? TryGetSelectedObject(ModelDoc2 activeDoc, int selectionType)
    {
        var selectionManager = activeDoc.SelectionManager as SelectionMgr;
        if (selectionManager is null)
        {
            return null;
        }

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
