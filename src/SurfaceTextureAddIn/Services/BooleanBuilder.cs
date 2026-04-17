using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using SurfaceTextureAddIn.Core;
using SurfaceTextureAddIn.Models;

namespace SurfaceTextureAddIn.Services;

internal sealed class BooleanBuilder
{
    public int Apply(dynamic swApp, TextureExecutionContext context, IReadOnlyList<PlacementFrame> placements, Action<string>? log)
    {
        if (context.TargetBody is null || context.Seed?.Body is null)
        {
            throw new InvalidOperationException("Texture execution context is missing a target body or seed body.");
        }

        var successCount = 0;
        dynamic currentBody = ((dynamic)context.TargetBody).Copy();

        foreach (var placement in placements)
        {
            try
            {
                dynamic toolBody = ((dynamic)context.Seed.Body).Copy();
                MoveBody(toolBody, placement, context.Parameters.HeightOrDepth, context.Mode);

                var operation = context.Mode == TextureOperationMode.Boss
                    ? SwApiConstants.BodyOperationAdd
                    : SwApiConstants.BodyOperationCut;

                int errorCode = 0;
                var result = currentBody.Operations2(operation, toolBody, out errorCode);

                if (errorCode == SwApiConstants.Ok && result is object[] resultBodies && resultBodies.Length > 0)
                {
                    ReleaseCom(currentBody);
                    currentBody = resultBodies[0];
                    context.TargetBody = currentBody;
                    successCount++;
                }
                else
                {
                    log?.Invoke($"Boolean operation failed at instance {placement.Index}. Error code: {errorCode}");
                }

                ReleaseCom(toolBody);
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

    private static void MoveBody(dynamic toolBody, PlacementFrame placement, double depth, TextureOperationMode mode)
    {
        var sign = mode == TextureOperationMode.Boss ? 1.0 : -1.0;
        var offset = placement.ZAxis * (depth * sign);
        var transform = new double[16];
        transform[0] = placement.XAxis.X;
        transform[1] = placement.XAxis.Y;
        transform[2] = placement.XAxis.Z;
        transform[3] = 0;
        transform[4] = placement.YAxis.X;
        transform[5] = placement.YAxis.Y;
        transform[6] = placement.YAxis.Z;
        transform[7] = 0;
        transform[8] = placement.ZAxis.X;
        transform[9] = placement.ZAxis.Y;
        transform[10] = placement.ZAxis.Z;
        transform[11] = 0;
        transform[12] = placement.Sample.Position.X + offset.X;
        transform[13] = placement.Sample.Position.Y + offset.Y;
        transform[14] = placement.Sample.Position.Z + offset.Z;
        transform[15] = 1;

        toolBody.ApplyTransform(transform);
    }

    private static void TryCommitResult(dynamic swApp, TextureExecutionContext context, Action<string>? log)
    {
        try
        {
            dynamic activeDoc = swApp.ActiveDoc;
            activeDoc.ClearSelection2(true);
            if (TryCreateFeatureFromBody(activeDoc, context.TargetBody, log))
            {
                return;
            }

            activeDoc.InsertImportedBody2(context.TargetBody, false);
        }
        catch (Exception ex)
        {
            log?.Invoke($"Failed to insert result body back into document: {ex.Message}");
        }
    }

    private static bool TryCreateFeatureFromBody(dynamic activeDoc, dynamic resultBody, Action<string>? log)
    {
        try
        {
            dynamic partDoc = activeDoc;
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

    private static void ReleaseCom(object? instance)
    {
        if (instance is not null && Marshal.IsComObject(instance))
        {
            Marshal.ReleaseComObject(instance);
        }
    }
}
