using System;
using System.Collections.Generic;
using SurfaceTextureAddIn.Geometry;
using SurfaceTextureAddIn.Models;

namespace SurfaceTextureAddIn.Services;

internal sealed class FaceSampler
{
    public IReadOnlyList<FaceSample> SampleFace(object targetFace, TextureParameters parameters)
    {
        dynamic face = targetFace;
        var uv = (double[]?)face.GetUVBounds() ?? Array.Empty<double>();
        if (uv.Length < 4)
        {
            throw new InvalidOperationException("Failed to read target face UV bounds.");
        }

        var minU = uv[0] + parameters.Margin;
        var maxU = uv[1] - parameters.Margin;
        var minV = uv[2] + parameters.Margin;
        var maxV = uv[3] - parameters.Margin;
        var samples = new List<FaceSample>();

        for (var u = minU; u <= maxU; u += Math.Max(parameters.SpacingU, 1e-5))
        {
            for (var v = minV; v <= maxV; v += Math.Max(parameters.SpacingV, 1e-5))
            {
                if (samples.Count >= parameters.MaxInstances)
                {
                    return samples;
                }

                var sample = TryEvaluate(face, u, v);
                if (!sample.IsValid)
                {
                    continue;
                }

                samples.Add(sample);
            }
        }

        return samples;
    }

    private static FaceSample TryEvaluate(dynamic face, double u, double v)
    {
        try
        {
            dynamic surface = face.GetSurface();
            var evaluation = (double[]?)surface.Evaluate(u, v, 1, 1) ?? Array.Empty<double>();
            if (evaluation.Length < 9)
            {
                return new FaceSample();
            }

            var point = new Vector3D(evaluation[0], evaluation[1], evaluation[2]);
            var tangentU = new Vector3D(evaluation[3], evaluation[4], evaluation[5]).Normalize();
            var tangentV = new Vector3D(evaluation[6], evaluation[7], evaluation[8]).Normalize();
            var normal = tangentU.Cross(tangentV).Normalize();

            if (normal.Length <= 1e-9)
            {
                return new FaceSample();
            }

            return new FaceSample
            {
                U = u,
                V = v,
                Position = point,
                TangentU = tangentU,
                TangentV = tangentV,
                Normal = normal,
                IsValid = true,
            };
        }
        catch
        {
            return new FaceSample();
        }
    }
}
