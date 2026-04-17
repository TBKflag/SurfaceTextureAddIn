using System;
using System.Collections.Generic;
using SurfaceTextureAddIn.Geometry;
using SurfaceTextureAddIn.Models;

namespace SurfaceTextureAddIn.Services;

internal sealed class TexturePlacementEngine
{
    public IReadOnlyList<PlacementFrame> BuildPlacements(
        IReadOnlyList<FaceSample> samples,
        TextureSeedDefinition seed,
        TextureParameters parameters)
    {
        var placements = new List<PlacementFrame>(samples.Count);
        Vector3D? lastNormal = null;
        var minimumDistance = Math.Max(Math.Min(seed.Width, seed.Height) * 0.5, 1e-5);

        for (var index = 0; index < samples.Count; index++)
        {
            var sample = samples[index];
            if (parameters.SkipHighCurvature && lastNormal.HasValue)
            {
                var delta = Math.Acos(Clamp(lastNormal.Value.Dot(sample.Normal), -1.0, 1.0)) * 180.0 / Math.PI;
                if (delta > parameters.MaxNormalDeltaDegrees)
                {
                    continue;
                }
            }

            var xAxis = sample.TangentU.RotateAround(sample.Normal, parameters.RotationRadians).Normalize();
            var yAxis = sample.Normal.Cross(xAxis).Normalize();
            var placement = new PlacementFrame
            {
                Index = placements.Count,
                Sample = sample,
                XAxis = xAxis,
                YAxis = yAxis,
                ZAxis = sample.Normal,
                Collides = HasCollision(placements, sample.Position, minimumDistance),
            };

            if (placement.Collides)
            {
                continue;
            }

            placements.Add(placement);
            lastNormal = sample.Normal;
        }

        return placements;
    }

    private static bool HasCollision(IEnumerable<PlacementFrame> placements, Vector3D candidate, double minimumDistance)
    {
        foreach (var existing in placements)
        {
            var distance = (existing.Sample.Position - candidate).Length;
            if (distance < minimumDistance)
            {
                return true;
            }
        }

        return false;
    }

    private static double Clamp(double value, double minimum, double maximum)
    {
        if (value < minimum)
        {
            return minimum;
        }

        if (value > maximum)
        {
            return maximum;
        }

        return value;
    }
}
