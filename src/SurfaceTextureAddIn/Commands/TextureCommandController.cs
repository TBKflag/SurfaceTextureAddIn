using System;
using System.Collections.Generic;
using System.Linq;
using SysEnv = System.Environment;
using SolidWorks.Interop.sldworks;
using SurfaceTextureAddIn.Models;
using SurfaceTextureAddIn.Services;
using SurfaceTextureAddIn.UI;
using SurfaceTextureAddIn.Geometry;

namespace SurfaceTextureAddIn.Commands;

internal sealed class TextureCommandController
{
    private readonly SldWorks swApp;
    private readonly SelectionService selectionService = new();
    private readonly TextureSeedAnalyzer seedAnalyzer = new();
    private readonly FaceSampler faceSampler = new();
    private readonly TexturePlacementEngine placementEngine = new();
    private readonly BooleanBuilder booleanBuilder = new();
    private readonly TextureExecutionValidator executionValidator = new();
    private readonly TexturePropertyManagerPage propertyManagerPage;

    public TextureCommandController(SldWorks swApp)
    {
        this.swApp = swApp ?? throw new ArgumentNullException(nameof(swApp));
        propertyManagerPage = new TexturePropertyManagerPage();
    }

    public void Run(TextureOperationMode mode)
    {
        TextureParameters? initialParams = null;
        var baseMode = GetBaseMode(mode);

        if (IsEditMode(mode))
        {
            initialParams = ExtractParametersFromSelectedFeature();
        }

        var pageResult = propertyManagerPage.Show(swApp, mode, initialParams);
        if (pageResult is null)
        {
            return;
        }

        try
        {
            var context = selectionService.BuildContext(
                swApp,
                pageResult.Parameters,
                baseMode,
                pageResult.SelectedTargetFace,
                pageResult.SelectedSeedBody);

            context.Seed = seedAnalyzer.Analyze(pageResult.SelectedSeedBody ?? GetSelectedSeedBody(context.ActiveDocument));

            IReadOnlyList<PlacementFrame> placements;

            if (baseMode == TextureOperationMode.CopySingle)
            {
                placements = GenerateSinglePlacement(context);
            }
            else
            {
                var samples = faceSampler.SampleFace(context.TargetFace!, context.Parameters);
                placements = placementEngine.BuildPlacements(samples, context.Seed, context.Parameters);
            }

            var warnings = executionValidator.Validate(context, Array.Empty<FaceSample>(), placements);
            if (placements.Count == 0)
            {
                ShowMessage(string.Join(global::System.Environment.NewLine, warnings.DefaultIfEmpty("No valid placements were generated on the selected face.")), isWarning: true);
                return;
            }

            if (IsEditMode(mode))
            {
                RemoveExistingTextures();
            }

            var logs = new List<string>();
            Action<string> appendLog = logs.Add;
            var successCount = booleanBuilder.Apply(swApp, context, placements, appendLog);
            var summary = baseMode == TextureOperationMode.CopySingle 
                ? (IsEditMode(mode) ? "Single texture instance updated successfully." : "Single texture instance created successfully.")
                : (IsEditMode(mode) ? $"Updated {successCount} texture instances out of {placements.Count} candidate placements." : $"Generated {successCount} texture instances out of {placements.Count} candidate placements.");
            if (warnings.Count > 0)
            {
                summary += $"{SysEnv.NewLine}{SysEnv.NewLine}Warnings:{SysEnv.NewLine}- {string.Join(SysEnv.NewLine + "- ", warnings)}";
            }

            if (logs.Count > 0)
            {
                summary += $"{SysEnv.NewLine}{SysEnv.NewLine}Issues:{SysEnv.NewLine}- {string.Join(SysEnv.NewLine + "- ", logs.Take(12))}";
            }

            ShowMessage(summary, isWarning: logs.Count > 0);
        }
        catch (Exception ex)
        {
            ShowMessage(ex.Message, isWarning: true);
        }
    }

    private static TextureOperationMode GetBaseMode(TextureOperationMode mode)
    {
        return mode switch
        {
            TextureOperationMode.EditBoss => TextureOperationMode.Boss,
            TextureOperationMode.EditCut => TextureOperationMode.Cut,
            TextureOperationMode.EditCopySingle => TextureOperationMode.CopySingle,
            _ => mode
        };
    }

    private static bool IsEditMode(TextureOperationMode mode)
    {
        return mode is TextureOperationMode.EditBoss or TextureOperationMode.EditCut or TextureOperationMode.EditCopySingle;
    }

    private TextureParameters? ExtractParametersFromSelectedFeature()
    {
        try
        {
            dynamic activeDoc = swApp.ActiveDoc;
            dynamic selectionManager = activeDoc.SelectionManager;
            int count = selectionManager.GetSelectedObjectCount2(-1);

            for (int i = 1; i <= count; i++)
            {
                try
                {
                    dynamic feature = selectionManager.GetSelectedObject6(i, -1);
                    string featureName = feature.Name;

                    if (featureName.Contains("Texture") || featureName.Contains("Boss") || featureName.Contains("Cut"))
                    {
                        dynamic customProps = feature.CustomProperties;
                        if (customProps != null)
                        {
                            return new TextureParameters
                            {
                                SpacingU = GetCustomPropValue(customProps, "TxSpacingU", 1.0),
                                SpacingV = GetCustomPropValue(customProps, "TxSpacingV", 1.0),
                                HeightOrDepth = GetCustomPropValue(customProps, "TxHeightOrDepth", 1.0),
                                Margin = GetCustomPropValue(customProps, "TxMargin", 0.0),
                                RotationDegrees = GetCustomPropValue(customProps, "TxRotation", 0.0),
                                MaxInstances = (int)GetCustomPropValue(customProps, "TxMaxInstances", 500.0),
                                SkipHighCurvature = GetCustomPropBool(customProps, "TxSkipHighCurvature", true),
                                MaxNormalDeltaDegrees = GetCustomPropValue(customProps, "TxMaxNormalDelta", 25.0)
                            };
                        }
                    }
                }
                catch
                {
                    continue;
                }
            }
        }
        catch
        {
            // Ignore errors
        }

        return null;
    }

    private static double GetCustomPropValue(dynamic customProps, string name, double defaultValue)
    {
        try
        {
            string val = customProps.Item(name);
            if (double.TryParse(val, out double result))
            {
                return result;
            }
        }
        catch
        {
            // Ignore errors
        }
        return defaultValue;
    }

    private static bool GetCustomPropBool(dynamic customProps, string name, bool defaultValue)
    {
        try
        {
            string val = customProps.Item(name);
            if (string.Equals(val, "True", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            if (string.Equals(val, "False", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }
        catch
        {
            // Ignore errors
        }
        return defaultValue;
    }

    private void RemoveExistingTextures()
    {
        try
        {
            dynamic activeDoc = swApp.ActiveDoc;
            dynamic featureManager = activeDoc.FeatureManager;
            dynamic features = activeDoc.FeatureManager.GetFeatures(true);

            var texturesToDelete = new List<dynamic>();
            foreach (dynamic feature in features)
            {
                try
                {
                    string name = feature.Name;
                    if (name.Contains("Texture") || name.Contains("Boss") || name.Contains("Cut"))
                    {
                        texturesToDelete.Add(feature);
                    }
                }
                catch
                {
                    continue;
                }
            }

            foreach (var feature in texturesToDelete)
            {
                try
                {
                    featureManager.DeleteFeature(feature);
                }
                catch
                {
                    continue;
                }
            }
        }
        catch
        {
            // Ignore errors
        }
    }

    private IReadOnlyList<PlacementFrame> GenerateSinglePlacement(TextureExecutionContext context)
    {
        if (context.TargetFace is null || context.Seed is null)
        {
            return Array.Empty<PlacementFrame>();
        }

        dynamic targetFace = context.TargetFace;
        
        // 获取面的 UV 边界
        var uv = (double[]?)targetFace.GetUVBounds() ?? Array.Empty<double>();
        if (uv.Length < 4)
        {
            return Array.Empty<PlacementFrame>();
        }

        // 计算中心点的 UV 值
        var centerU = (uv[0] + uv[1]) / 2;
        var centerV = (uv[2] + uv[3]) / 2;

        // 使用 Evaluate 获取中心点坐标和法向量
        dynamic surface = targetFace.GetSurface();
        var evaluation = (double[]?)surface.Evaluate(centerU, centerV, 1, 1) ?? Array.Empty<double>();
        if (evaluation.Length < 9)
        {
            return Array.Empty<PlacementFrame>();
        }

        var sample = new FaceSample
        {
            Position = new Vector3D(evaluation[0], evaluation[1], evaluation[2]),
            TangentU = new Vector3D(evaluation[3], evaluation[4], evaluation[5]).Normalize(),
            TangentV = new Vector3D(evaluation[6], evaluation[7], evaluation[8]).Normalize(),
            IsValid = true
        };
        // 计算法向量
        sample.Normal = sample.TangentU.Cross(sample.TangentV).Normalize();

        var placement = new PlacementFrame
        {
            Sample = sample,
            XAxis = sample.TangentU,
            YAxis = sample.TangentV,
            ZAxis = sample.Normal,
            Index = 0
        };

        return new[] { placement };
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
