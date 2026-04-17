using System;
using SurfaceTextureAddIn.Geometry;
using SurfaceTextureAddIn.Models;

namespace SurfaceTextureAddIn.Services;

internal sealed class TextureSeedAnalyzer
{
    public TextureSeedDefinition Analyze(object seedBody)
    {
        dynamic body = seedBody;
        object[] faces = body.GetFaces() as object[] ?? Array.Empty<object>();
        if (faces.Length == 0)
        {
            throw new InvalidOperationException("The texture seed body does not contain faces.");
        }

        object? bestFace = null;
        double bestArea = double.MaxValue;
        Vector3D bestNormal = new(0, 0, 1);
        Vector3D bestOrigin = new(0, 0, 0);

        foreach (var faceObject in faces)
        {
            dynamic face = faceObject;
            double area;
            try
            {
                area = face.GetArea();
            }
            catch
            {
                continue;
            }

            if (area >= bestArea)
            {
                continue;
            }

            var sample = TryGetMidPointFrame(faceObject);
            if (!sample.IsValid)
            {
                continue;
            }

            bestArea = area;
            bestFace = faceObject;
            bestNormal = sample.Normal;
            bestOrigin = sample.Position;
        }

        if (bestFace is null)
        {
            throw new InvalidOperationException("Unable to infer a base face from the texture seed body.");
        }

        var box = (double[]?)body.GetBodyBox() ?? Array.Empty<double>();
        var width = box.Length >= 6 ? Math.Abs(box[3] - box[0]) : 0.005;
        var height = box.Length >= 6 ? Math.Abs(box[4] - box[1]) : 0.005;
        var thickness = box.Length >= 6 ? Math.Abs(box[5] - box[2]) : 0.001;

        var tangent = BuildStableTangent(bestNormal);
        var bitangent = bestNormal.Cross(tangent).Normalize();

        return new TextureSeedDefinition
        {
            Body = seedBody,
            BaseFace = bestFace,
            BaseOrigin = bestOrigin,
            BaseNormal = bestNormal.Normalize(),
            BaseTangent = tangent,
            BaseBitangent = bitangent,
            Width = width,
            Height = height,
            Thickness = thickness,
        };
    }

    private static FaceSample TryGetMidPointFrame(object faceObject)
    {
        dynamic face = faceObject;
        try
        {
            var uv = (double[]?)face.GetUVBounds() ?? Array.Empty<double>();
            if (uv.Length < 4)
            {
                return new FaceSample();
            }

            var midU = (uv[0] + uv[1]) * 0.5;
            var midV = (uv[2] + uv[3]) * 0.5;
            dynamic surface = face.GetSurface();
            var evaluation = (double[]?)surface.Evaluate(midU, midV, 1, 1) ?? Array.Empty<double>();
            if (evaluation.Length < 6)
            {
                return new FaceSample();
            }

            var position = new Vector3D(evaluation[0], evaluation[1], evaluation[2]);
            var tangentU = new Vector3D(evaluation[3], evaluation[4], evaluation[5]).Normalize();
            Vector3D tangentV;

            if (evaluation.Length >= 9)
            {
                tangentV = new Vector3D(evaluation[6], evaluation[7], evaluation[8]).Normalize();
            }
            else
            {
                tangentV = BuildStableTangent(tangentU);
            }

            var normal = tangentU.Cross(tangentV).Normalize();

            return new FaceSample
            {
                U = midU,
                V = midV,
                Position = position,
                TangentU = tangentU,
                TangentV = tangentV,
                Normal = normal,
                IsValid = normal.Length > 0,
            };
        }
        catch
        {
            return new FaceSample();
        }
    }

    private static Vector3D BuildStableTangent(Vector3D normal)
    {
        var reference = Math.Abs(normal.Z) < 0.95 ? new Vector3D(0, 0, 1) : new Vector3D(1, 0, 0);
        return reference.Cross(normal).Normalize();
    }
}
