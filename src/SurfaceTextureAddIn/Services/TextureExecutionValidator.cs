using System;
using System.Collections.Generic;
using SurfaceTextureAddIn.Models;

namespace SurfaceTextureAddIn.Services;

internal sealed class TextureExecutionValidator
{
    public IReadOnlyList<string> Validate(TextureExecutionContext context, IReadOnlyList<FaceSample> samples, IReadOnlyList<PlacementFrame> placements)
    {
        var warnings = new List<string>();

        if (context.Seed is null)
        {
            warnings.Add("Seed analysis did not produce a valid local base frame.");
            return warnings;
        }

        if (samples.Count >= context.Parameters.MaxInstances)
        {
            warnings.Add("Sampling hit the MaxInstances limit. Increase the limit or spacing to cover more area.");
        }

        if (placements.Count == 0)
        {
            warnings.Add("All candidate placements were filtered out. Try increasing spacing or disabling curvature filtering.");
        }

        if (context.Seed.Thickness <= 1e-6)
        {
            warnings.Add("The seed body thickness is extremely small and may cause boolean failures.");
        }

        if (context.Parameters.HeightOrDepth <= 1e-6)
        {
            warnings.Add("The requested boss/cut depth is extremely small and may not produce a stable result.");
        }

        if (context.Parameters.SpacingU < context.Seed.Width * 0.25 || context.Parameters.SpacingV < context.Seed.Height * 0.25)
        {
            warnings.Add("Spacing is significantly smaller than the seed footprint and may cause overlapping booleans.");
        }

        return warnings;
    }
}
