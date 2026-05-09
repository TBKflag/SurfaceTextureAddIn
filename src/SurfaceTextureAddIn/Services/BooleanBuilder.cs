using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using SolidWorks.Interop.sldworks;
using SurfaceTextureAddIn.Core;
using SurfaceTextureAddIn.Geometry;
using SurfaceTextureAddIn.Models;

namespace SurfaceTextureAddIn.Services;

internal sealed class BooleanBuilder
{
    private static readonly string LogFilePath = Path.Combine(Path.GetTempPath(), "SurfaceTextureAddIn_BooleanBuilder.log");

    public int Apply(SldWorks swApp, TextureExecutionContext context, IReadOnlyList<PlacementFrame> placements, Action<string>? log)
    {
        File.WriteAllText(LogFilePath, "");
        Log("=== Boolean Operation Debug Info ===");
        Log($"Target body type: {context.TargetBody?.GetType()}");
        Log($"Seed body type: {context.Seed?.Body?.GetType()}");
        Log($"Target body name: {((context.TargetBody as IBody2)?.Name ?? "null")}");
        Log($"Seed body name: {((context.Seed?.Body as IBody2)?.Name ?? "null")}");
        Log($"Seed BaseOrigin: ({context.Seed?.BaseOrigin.X}, {context.Seed?.BaseOrigin.Y}, {context.Seed?.BaseOrigin.Z})");
        Log($"Seed BaseNormal: ({context.Seed?.BaseNormal.X}, {context.Seed?.BaseNormal.Y}, {context.Seed?.BaseNormal.Z})");
        Log($"Placement count: {placements.Count}");
        Log($"Operation mode: {context.Mode}");
        Log($"HeightOrDepth: {context.Parameters.HeightOrDepth}");
        
        if (context.Seed?.Body is IBody2 initialSeedBody)
        {
            Log($"Original seed body bounds before any operations:");
            Log(GetBodyInfo(initialSeedBody));
        }
        
        Log("==================================");

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

        log?.Invoke("=== Boolean Operation Debug Info ===");
        log?.Invoke($"Target body type: {targetBody.GetType()}");
        log?.Invoke($"Seed body type: {seedBody.GetType()}");
        log?.Invoke($"Target body name: {targetBody.Name}");
        log?.Invoke($"Seed body name: {seedBody.Name}");
        log?.Invoke($"Seed BaseOrigin: ({context.Seed.BaseOrigin.X}, {context.Seed.BaseOrigin.Y}, {context.Seed.BaseOrigin.Z})");
        log?.Invoke($"Seed BaseNormal: ({context.Seed.BaseNormal.X}, {context.Seed.BaseNormal.Y}, {context.Seed.BaseNormal.Z})");
        log?.Invoke($"Placement count: {placements.Count}");
        log?.Invoke($"Operation mode: {context.Mode}");
        log?.Invoke($"HeightOrDepth: {context.Parameters.HeightOrDepth}");
        log?.Invoke("==================================");

        var successCount = 0;
        var currentBody = targetBody.Copy2(false) as IBody2;
        if (currentBody is null)
        {
            throw new InvalidOperationException("Failed to copy target body.");
        }

        Log($"Initial currentBody type: {currentBody.GetType()}");
        log?.Invoke($"Initial currentBody type: {currentBody.GetType()}");

        foreach (var placement in placements)
        {
            try
            {
                var toolBody = seedBody.Copy2(false) as IBody2;
                if (toolBody is null)
                {
                    Log($"Instance {placement.Index}: Failed to copy seed body.");
                    log?.Invoke($"Boolean operation failed at instance {placement.Index}: Failed to copy seed body.");
                    continue;
                }

                Log($"Instance {placement.Index}: toolBody copied");
                Log($"Instance {placement.Index}: Original seed body info:\n{GetBodyInfo(seedBody)}");
                log?.Invoke($"Instance {placement.Index}: toolBody copied");

                Log($"Instance {placement.Index}: applying transform...");
                MoveBody(toolBody, mathUtility, placement, context.Parameters.HeightOrDepth, context.Mode, context.Seed, placement.Index);
                Log($"Instance {placement.Index}: transform applied");
                Log($"Instance {placement.Index}: Transformed body info:\n{GetBodyInfo(toolBody)}");
                log?.Invoke($"Instance {placement.Index}: transform applied");

                if (context.Mode == TextureOperationMode.CopySingle)
                {
                    Log($"Instance {placement.Index}: CopySingle mode - inserting body directly");
                    log?.Invoke($"Instance {placement.Index}: CopySingle mode - inserting body directly");
                    if (TryInsertBodyDirectly(swApp, toolBody, log))
                    {
                        successCount++;
                        Log($"Instance {placement.Index}: Success count now {successCount}");
                        log?.Invoke($"Instance {placement.Index}: Success count now {successCount}");
                        
                        var bodyInfo = GetBodyInfo(toolBody);
                        System.Windows.Forms.MessageBox.Show(
                            $"Seed Body Info:\n{bodyInfo}",
                            "Copy Single Result",
                            System.Windows.Forms.MessageBoxButtons.OK,
                            System.Windows.Forms.MessageBoxIcon.Information);
                    }
                    else
                    {
                        Log($"Instance {placement.Index}: Failed to insert body directly");
                        log?.Invoke($"Instance {placement.Index}: Failed to insert body directly");
                    }
                    Marshal.ReleaseComObject(toolBody);
                    continue;
                }

                var operation = context.Mode == TextureOperationMode.Boss
                    ? SwApiConstants.BodyOperationAdd
                    : SwApiConstants.BodyOperationCut;

                int errorCode = 0;
                Log($"Instance {placement.Index}: Calling Operations2 with operation={operation}...");
                var result = currentBody.Operations2(operation, toolBody, out errorCode);

                Log($"Instance {placement.Index}: Operations2 returned errorCode={errorCode}, result={(result != null ? "not null" : "null")}");
                log?.Invoke($"Instance {placement.Index}: Operations2 returned errorCode={errorCode}, result={(result != null ? "not null" : "null")}");

                if (errorCode == SwApiConstants.Ok && result is object[] resultBodies && resultBodies.Length > 0)
                {
                    Log($"Instance {placement.Index}: Boolean succeeded, updating currentBody");
                    log?.Invoke($"Instance {placement.Index}: Boolean succeeded, updating currentBody");
                    Marshal.ReleaseComObject(currentBody);
                    currentBody = resultBodies[0] as IBody2;
                    if (currentBody is null)
                    {
                        Log($"Instance {placement.Index}: Result is not IBody2, skipping.");
                        log?.Invoke($"Instance {placement.Index}: Result is not IBody2, skipping.");
                        continue;
                    }
                    context.TargetBody = currentBody;
                    successCount++;
                    Log($"Instance {placement.Index}: Success count now {successCount}");
                    log?.Invoke($"Instance {placement.Index}: Success count now {successCount}");
                }
                else
                {
                    Log($"Boolean operation failed at instance {placement.Index}. Error code: {errorCode}");
                    log?.Invoke($"Boolean operation failed at instance {placement.Index}. Error code: {errorCode}");
                }

                Marshal.ReleaseComObject(toolBody);
            }
            catch (Exception ex)
            {
                Log($"Boolean operation exception at instance {placement.Index}: {ex.Message}");
                log?.Invoke($"Boolean operation exception at instance {placement.Index}: {ex.Message}");
            }
        }

        Log($"Final successCount: {successCount}/{placements.Count}");
        log?.Invoke($"Final successCount: {successCount}/{placements.Count}");
        context.TargetBody = currentBody;
        TryCommitResult(swApp, context, log);
        Log($"Log file saved to: {LogFilePath}");
        log?.Invoke($"Log file saved to: {LogFilePath}");
        return successCount;
    }

    private static void Log(string message)
    {
        try
        {
            File.AppendAllText(LogFilePath, $"{DateTime.Now:HH:mm:ss.fff} | {message}{System.Environment.NewLine}");
        }
        catch
        {
        }
    }

    private static string GetBodyInfo(IBody2 body)
    {
        try
        {
            var bounds = body.GetBodyBox() as double[];
            if (bounds != null && bounds.Length >= 6)
            {
                double minX = bounds[0], minY = bounds[1], minZ = bounds[2];
                double maxX = bounds[3], maxY = bounds[4], maxZ = bounds[5];
                
                double centerX = (minX + maxX) / 2;
                double centerY = (minY + maxY) / 2;
                double centerZ = (minZ + maxZ) / 2;
                
                double width = maxX - minX;
                double height = maxY - minY;
                double depth = maxZ - minZ;
                
                return $"Center: ({centerX:F6}, {centerY:F6}, {centerZ:F6}) m\nDimensions: {width:F6} x {height:F6} x {depth:F6} m\nDimensions: {width*1000:F3} x {height*1000:F3} x {depth*1000:F3} mm";
            }
            return "Failed to get body bounds";
        }
        catch (Exception ex)
        {
            return $"Error getting body info: {ex.Message}";
        }
    }

    private static void MoveBody(IBody2 toolBody, IMathUtility mathUtility, PlacementFrame placement, double depth, TextureOperationMode mode, TextureSeedDefinition seed, int instanceIndex)
    {
        // CopySingle 模式不需要深度偏移，只需要移动到面的中心位置
        bool applyDepthOffset = mode != TextureOperationMode.CopySingle;
        var sign = mode == TextureOperationMode.Boss ? 1.0 : -1.0;

        Log($"Instance {instanceIndex} transform:");
        Log($"  Seed BaseOrigin: ({seed.BaseOrigin.X}, {seed.BaseOrigin.Y}, {seed.BaseOrigin.Z})");
        Log($"  Seed BaseNormal: ({seed.BaseNormal.X}, {seed.BaseNormal.Y}, {seed.BaseNormal.Z})");
        Log($"  Sample.Position: ({placement.Sample.Position.X}, {placement.Sample.Position.Y}, {placement.Sample.Position.Z})");
        Log($"  Sample.Normal: ({placement.Sample.Normal.X}, {placement.Sample.Normal.Y}, {placement.Sample.Normal.Z})");
        Log($"  HeightOrDepth: {depth}, Mode: {mode}, ApplyDepthOffset: {applyDepthOffset}");

        // 关键修复：CopySingle 模式添加一个小的偏移确保可见
        Vector3D offset = new Vector3D();
        if (applyDepthOffset)
        {
            // 偏移应该沿着 Sample.Normal 的法线方向（外侧）偏移
            // 注意：UI 输入的 HeightOrDepth 单位是毫米，需要转换为米
            double depthM = depth / 1000.0;
            offset = placement.Sample.Normal * (-depthM * sign);
            Log($"  Offset (-Sample.Normal * depth * sign, in meters): ({offset.X}, {offset.Y}, {offset.Z})");
        }
        else
        {
            // CopySingle 模式：添加一个小的偏移(0.5mm = 0.0005m)，确保能看到 seed body
            // SolidWorks API 使用米作为单位，所以 0.5mm = 0.0005m
            // 注意：偏移方向与法向量相反，将 body 推到面的正面/外侧
            offset = placement.Sample.Normal * (-0.0005);
            Log($"  Offset (-Sample.Normal * 0.5mm for visibility): ({offset.X}, {offset.Y}, {offset.Z})");
        }

        // 第一步：把 seed body 从 BaseOrigin 移动到原点
        // SolidWorks 矩阵格式（16个元素）：
        // | 0  1  2  13 |
        // | 3  4  5  14 |
        // | 6  7  8  15 |
        // | 9 10 11  12 |
        // 0-8: 旋转, 9-11: 平移(x,y,z), 12: 缩放, 13-15: 未使用
        var translateToOriginData = new double[16];
        translateToOriginData[0] = 1;
        translateToOriginData[1] = 0;
        translateToOriginData[2] = 0;
        translateToOriginData[3] = 0;
        translateToOriginData[4] = 1;
        translateToOriginData[5] = 0;
        translateToOriginData[6] = 0;
        translateToOriginData[7] = 0;
        translateToOriginData[8] = 1;
        translateToOriginData[9] = -seed.BaseOrigin.X;
        translateToOriginData[10] = -seed.BaseOrigin.Y;
        translateToOriginData[11] = -seed.BaseOrigin.Z;
        translateToOriginData[12] = 1;
        translateToOriginData[13] = 0;
        translateToOriginData[14] = 0;
        translateToOriginData[15] = 0;
        var translateToOrigin = mathUtility.CreateTransform(translateToOriginData) as MathTransform;
        if (translateToOrigin is null)
        {
            throw new InvalidOperationException("Failed to create translate to origin transform.");
        }
        toolBody.ApplyTransform(translateToOrigin);
        Log($"Instance {instanceIndex}: Step 1 - Translated to origin");
        Log($"Instance {instanceIndex}: After Step 1 - {GetBodyInfo(toolBody)}");

        // 第二步：应用旋转变换，将 seed body 的朝向从 Seed.BaseNormal 旋转到 Sample.Normal
        var rotationMatrix = BuildRotationMatrix(seed.BaseNormal, placement.Sample.Normal, mathUtility);
        if (rotationMatrix != null)
        {
            toolBody.ApplyTransform(rotationMatrix);
            Log($"Instance {instanceIndex}: Step 2 - Rotated from Seed.BaseNormal to Sample.Normal");
            Log($"Instance {instanceIndex}: After Step 2 - {GetBodyInfo(toolBody)}");
        }
        else
        {
            Log($"Instance {instanceIndex}: Step 2 - Skipped rotation (vectors parallel or same)");
            Log($"Instance {instanceIndex}: After Step 2 (no rotation) - {GetBodyInfo(toolBody)}");
        }

        // 第三步：计算最终位置（平移到采样点位置）
        double finalX = placement.Sample.Position.X + offset.X;
        double finalY = placement.Sample.Position.Y + offset.Y;
        double finalZ = placement.Sample.Position.Z + offset.Z;

        Log($"  Final Position: ({finalX}, {finalY}, {finalZ})");

        // 第四步：应用平移变换
        // SolidWorks 矩阵格式（16个元素）：
        // | 0  1  2  13 |
        // | 3  4  5  14 |
        // | 6  7  8  15 |
        // | 9 10 11  12 |
        // 0-8: 旋转, 9-11: 平移(x,y,z), 12: 缩放, 13-15: 未使用
        var translateToPositionData = new double[16];
        translateToPositionData[0] = 1;
        translateToPositionData[1] = 0;
        translateToPositionData[2] = 0;
        translateToPositionData[3] = 0;
        translateToPositionData[4] = 1;
        translateToPositionData[5] = 0;
        translateToPositionData[6] = 0;
        translateToPositionData[7] = 0;
        translateToPositionData[8] = 1;
        translateToPositionData[9] = finalX;
        translateToPositionData[10] = finalY;
        translateToPositionData[11] = finalZ;
        translateToPositionData[12] = 1;
        translateToPositionData[13] = 0;
        translateToPositionData[14] = 0;
        translateToPositionData[15] = 0;
        var translateToPosition = mathUtility.CreateTransform(translateToPositionData) as MathTransform;
        if (translateToPosition is null)
        {
            throw new InvalidOperationException("Failed to create translate to position transform.");
        }
        toolBody.ApplyTransform(translateToPosition);
        Log($"Instance {instanceIndex}: Step 3 - Translated to final position");
        Log($"Instance {instanceIndex}: After Step 3 - {GetBodyInfo(toolBody)}");
    }

    private static MathTransform? BuildRotationMatrix(Vector3D from, Vector3D to, IMathUtility mathUtility)
    {
        var cross = from.Cross(to);
        var crossLength = cross.Length;

        if (crossLength < 1e-10)
        {
            if (from.Dot(to) < 0)
            {
                var perpendicular = Math.Abs(from.X) < 0.9 ? new Vector3D(1, 0, 0) : new Vector3D(0, 1, 0);
                var axis = from.Cross(perpendicular).Normalize();
                var angleFor180 = Math.PI;

                return CreateRotationMatrix(axis, angleFor180, mathUtility);
            }
            return null;
        }

        var rotationAxis = cross.Normalize();
        var dot = from.Dot(to);
        var rotationAngle = Math.Acos(Math.Max(-1, Math.Min(1, dot)));

        return CreateRotationMatrix(rotationAxis, rotationAngle, mathUtility);
    }

    private static MathTransform? CreateRotationMatrix(Vector3D axis, double angle, IMathUtility mathUtility)
    {
        // 使用 SolidWorks 自带的 CreateTransformRotateAxis 方法，避免手动构造矩阵出错
        var axisVec = mathUtility.CreateVector(new double[] { axis.X, axis.Y, axis.Z }) as MathVector;
        var origin = mathUtility.CreatePoint(new double[] { 0, 0, 0 }) as MathPoint;
        return mathUtility.CreateTransformRotateAxis(origin, axisVec, angle) as MathTransform;
    }

    private static void TryCommitResult(SldWorks swApp, TextureExecutionContext context, Action<string>? log)
    {
        Log("TryCommitResult: Starting...");
        log?.Invoke("TryCommitResult: Starting...");
        try
        {
            ModelDoc2? activeDoc = swApp.IActiveDoc2 as ModelDoc2;
            if (activeDoc is null)
            {
                Log("TryCommitResult: No active doc");
                log?.Invoke("TryCommitResult: No active doc");
                return;
            }

            activeDoc.ClearSelection2(true);
            if (context.TargetBody is IBody2 resultBody)
            {
                Log($"TryCommitResult: resultBody type = {resultBody.GetType()}");
                log?.Invoke($"TryCommitResult: resultBody type = {resultBody.GetType()}");
                var partDoc = activeDoc as IPartDoc;
                if (partDoc is not null)
                {
                    Log("TryCommitResult: Trying CreateFeatureFromBody3...");
                    log?.Invoke("TryCommitResult: Trying CreateFeatureFromBody3...");
                    if (TryCreateFeatureFromBody(partDoc, resultBody, context.Parameters, log))
                    {
                        Log("TryCommitResult: CreateFeatureFromBody3 succeeded");
                        log?.Invoke("TryCommitResult: CreateFeatureFromBody3 succeeded");
                        return;
                    }

                    Log("TryCommitResult: CreateFeatureFromBody3 failed, trying InsertImportedBody2...");
                    log?.Invoke("TryCommitResult: CreateFeatureFromBody3 failed, trying InsertImportedBody2...");
                    dynamic docExt = activeDoc.Extension;
                    bool insertSuccess = docExt.InsertImportedBody2(resultBody, false);
                    Log($"TryCommitResult: InsertImportedBody2 called, success={insertSuccess}");
                    log?.Invoke($"TryCommitResult: InsertImportedBody2 called, success={insertSuccess}");
                    
                    if (insertSuccess && context.Parameters != null)
                    {
                        dynamic lastFeature = activeDoc.FirstFeature();
                        while (lastFeature != null)
                        {
                            dynamic nextFeature = lastFeature.GetNextFeature();
                            if (nextFeature == null)
                            {
                                break;
                            }
                            lastFeature = nextFeature;
                        }
                        SaveParametersToFeature(lastFeature, context.Parameters);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log($"TryCommitResult exception: {ex.Message}");
            log?.Invoke($"TryCommitResult exception: {ex.Message}");
        }
    }

    private static bool TryCreateFeatureFromBody(IPartDoc partDoc, IBody2 resultBody, Action<string>? log)
    {
        return TryCreateFeatureFromBody(partDoc, resultBody, null, log);
    }

    private static bool TryCreateFeatureFromBody(IPartDoc partDoc, IBody2 resultBody, TextureParameters? parameters, Action<string>? log)
    {
        try
        {
            var feature = partDoc.CreateFeatureFromBody3(resultBody, false, 0);
            if (feature is not null)
            {
                Log("CreateFeatureFromBody3: feature created successfully");
                log?.Invoke("CreateFeatureFromBody3: feature created successfully");
                
                if (parameters != null)
                {
                    SaveParametersToFeature(feature, parameters);
                }
                
                return true;
            }
            Log("CreateFeatureFromBody3: feature is null");
            log?.Invoke("CreateFeatureFromBody3: feature is null");
        }
        catch (Exception ex)
        {
            Log($"CreateFeatureFromBody3 failed: {ex.Message}");
            log?.Invoke($"CreateFeatureFromBody3 failed: {ex.Message}");
        }

        return false;
    }

    private static void SaveParametersToFeature(dynamic feature, TextureParameters parameters)
    {
        try
        {
            dynamic customProps = feature.CustomProperties;
            if (customProps != null)
            {
                customProps.Add("TxSpacingU", (double)parameters.SpacingU);
                customProps.Add("TxSpacingV", (double)parameters.SpacingV);
                customProps.Add("TxHeightOrDepth", (double)parameters.HeightOrDepth);
                customProps.Add("TxMargin", (double)parameters.Margin);
                customProps.Add("TxRotation", (double)parameters.RotationDegrees);
                customProps.Add("TxMaxInstances", (double)parameters.MaxInstances);
                customProps.Add("TxMaxNormalDelta", (double)parameters.MaxNormalDeltaDegrees);
                customProps.Add("TxSkipHighCurvature", parameters.SkipHighCurvature ? "True" : "False");
            }
        }
        catch
        {
            // Ignore errors when saving parameters
        }
    }

    private static bool TryInsertBodyDirectly(SldWorks swApp, IBody2 toolBody, Action<string>? log)
    {
        try
        {
            var activeDoc = swApp.IActiveDoc2 as ModelDoc2;
            if (activeDoc is null)
            {
                Log("TryInsertBodyDirectly: No active document");
                log?.Invoke("TryInsertBodyDirectly: No active document");
                return false;
            }

            activeDoc.ClearSelection2(true);
            
            // 使用 InsertImportedBody2 而不是 CreateFeatureFromBody3
            // CreateFeatureFromBody3 会将body合并到现有实体中
            // InsertImportedBody2 会创建独立的实体
            // InsertImportedBody2 属于 IModelDocExtension 接口，需要通过动态调用
            Log("TryInsertBodyDirectly: Using InsertImportedBody2 to create independent body...");
            log?.Invoke("TryInsertBodyDirectly: Using InsertImportedBody2 to create independent body...");
            
            dynamic docExt = activeDoc.Extension;
            try
            {
                bool success = docExt.InsertImportedBody2(toolBody, false);
                if (success)
                {
                    Log("TryInsertBodyDirectly: InsertImportedBody2 succeeded");
                    log?.Invoke("TryInsertBodyDirectly: InsertImportedBody2 succeeded");
                    return true;
                }
                else
                {
                    Log("TryInsertBodyDirectly: InsertImportedBody2 returned false");
                    log?.Invoke("TryInsertBodyDirectly: InsertImportedBody2 returned false");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Log($"TryInsertBodyDirectly - InsertImportedBody2 failed: {ex.Message}");
                log?.Invoke($"TryInsertBodyDirectly - InsertImportedBody2 failed: {ex.Message}");
                
                // 备用方案：使用 CreateFeatureFromBody3 创建特征
                Log("TryInsertBodyDirectly: Falling back to CreateFeatureFromBody3...");
                log?.Invoke("TryInsertBodyDirectly: Falling back to CreateFeatureFromBody3...");
                var partDoc = activeDoc as IPartDoc;
                if (partDoc != null)
                {
                    var feature = partDoc.CreateFeatureFromBody3(toolBody, false, 0);
                    if (feature != null)
                    {
                        Log("TryInsertBodyDirectly: CreateFeatureFromBody3 succeeded");
                        log?.Invoke("TryInsertBodyDirectly: CreateFeatureFromBody3 succeeded");
                        return true;
                    }
                }
                return false;
            }
        }
        catch (Exception ex)
        {
            Log($"TryInsertBodyDirectly exception: {ex.Message}");
            log?.Invoke($"TryInsertBodyDirectly exception: {ex.Message}");
            return false;
        }
    }
}