using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using SolidWorks.Interop.sldworks;
using SurfaceTextureAddIn.Core;
using SurfaceTextureAddIn.Models;

namespace SurfaceTextureAddIn.Services;

internal sealed class BooleanBuilder
{
    public int Apply(SldWorks swApp, TextureExecutionContext context, IReadOnlyList<PlacementFrame> placements, Action<string>? log)
    {
        if (context.TargetBody is null || context.Seed?.Body is null)
        {
            throw new InvalidOperationException("Texture execution context is missing a target body or seed body.");
        }

        var mathUtility = swApp.GetMathUtility() as IMathUtility;
        if (mathUtility is null)
        {
            throw new InvalidOperationException("Unable to get IMathUtility from SolidWorks application.");
        }

        var targetBody = context.TargetBody as IBody2;
        if (targetBody is null)
        {
            throw new InvalidOperationException("Target body is not a valid IBody2.");
        }

        var seedBody = context.Seed.Body as IBody2;
        if (seedBody is null)
        {
            throw new InvalidOperationException("Seed body is not a valid IBody2.");
        }

        log?.Invoke($"Target body type: {targetBody.GetType()}");
        log?.Invoke($"Seed body type: {seedBody.GetType()}");

        var successCount = 0;
        var currentBody = targetBody.Copy2(false) as IBody2;
        if (currentBody is null)
        {
            throw new InvalidOperationException("Failed to copy target body.");
        }

        foreach (var placement in placements)
        {
            try
            {
                var toolBody = seedBody.Copy2(false) as IBody2;
                if (toolBody is null)
                {
                    log?.Invoke($"Boolean operation failed at instance {placement.Index}: Failed to copy seed body.");
                    continue;
                }

                MoveBody(toolBody, mathUtility, placement, context.Parameters.HeightOrDepth, context.Mode);

                var operation = context.Mode == TextureOperationMode.Boss
                    ? SwApiConstants.BodyOperationAdd
                    : SwApiConstants.BodyOperationCut;

                int errorCode = 0;
                var result = currentBody.Operations2(operation, toolBody, out errorCode);

                if (errorCode == SwApiConstants.Ok && result is object[] resultBodies && resultBodies.Length > 0)
                {
                    Marshal.ReleaseComObject(currentBody);
                    currentBody = resultBodies[0] as IBody2;
                    if (currentBody is null)
                    {
                        log?.Invoke($"Boolean operation failed at instance {placement.Index}: Result is not IBody2.");
                        continue;
                    }
                    context.TargetBody = currentBody;
                    successCount++;
                }
                else
                {
                    log?.Invoke($"Boolean operation failed at instance {placement.Index}. Error code: {errorCode}");
                }

                Marshal.ReleaseComObject(toolBody);
            }
            catch (Exception ex)
            {
                log?.Invoke($"Boolean operation exception at instance {placement.Index}: {ex.Message}");
            }
        }

        context.TargetBody = currentBody;
        TryCommitResult(swApp, context, log);
        return successCount;
    }

    private static void MoveBody(IBody2 toolBody, IMathUtility mathUtility, PlacementFrame placement, double depth, TextureOperationMode mode)
    {
        var sign = mode == TextureOperationMode.Boss ? 1.0 : -1.0;
        var offset = placement.ZAxis * (depth * sign);

        var transformData = new double[16];
        transformData[0] = placement.XAxis.X;
        transformData[1] = placement.XAxis.Y;
        transformData[2] = placement.XAxis.Z;
        transformData[3] = 0;
        transformData[4] = placement.YAxis.X;
        transformData[5] = placement.YAxis.Y;
        transformData[6] = placement.YAxis.Z;
        transformData[7] = 0;
        transformData[8] = placement.ZAxis.X;
        transformData[9] = placement.ZAxis.Y;
        transformData[10] = placement.ZAxis.Z;
        transformData[11] = 0;
        transformData[12] = placement.Sample.Position.X + offset.X;
        transformData[13] = placement.Sample.Position.Y + offset.Y;
        transformData[14] = placement.Sample.Position.Z + offset.Z;
        transformData[15] = 1;

        var transform = mathUtility.CreateTransform(transformData) as MathTransform;
        if (transform is null)
        {
            throw new InvalidOperationException("Failed to create MathTransform from transformation data.");
        }

        toolBody.ApplyTransform(transform);
    }

    private static void TryCommitResult(SldWorks swApp, TextureExecutionContext context, Action<string>? log)
    {
        try
        {
            ModelDoc2? activeDoc = swApp.IActiveDoc2 as ModelDoc2;
            if (activeDoc is null)
            {
                return;
            }

            activeDoc.ClearSelection2(true);
            if (context.TargetBody is IBody2 resultBody)
            {
                var partDoc = activeDoc as IPartDoc;
                if (partDoc is not null)
                {
                    if (TryCreateFeatureFromBody(partDoc, resultBody, log))
                    {
                        return;
                    }

                    dynamic docExt = activeDoc;
                    docExt.InsertImportedBody2(resultBody, false);
                }
            }
        }
        catch (Exception ex)
        {
            log?.Invoke($"Failed to insert result body back into document: {ex.Message}");
        }
    }

    private static bool TryCreateFeatureFromBody(IPartDoc partDoc, IBody2 resultBody, Action<string>? log)
    {
        try
        {
            var feature = partDoc.CreateFeatureFromBody3(resultBody, false, 0);
            if (feature is not null)
            {
                return true;
            }
        }
        catch (Exception ex)
        {
            log?.Invoke($"CreateFeatureFromBody3 failed, falling back to imported body: {ex.Message}");
        }

        return false;
    }
}