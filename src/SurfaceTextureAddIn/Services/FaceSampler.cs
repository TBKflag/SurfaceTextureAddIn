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

        var minU = uv[0];
        var maxU = uv[1];
        var minV = uv[2];
        var maxV = uv[3];
        
        // 应用边距
        var marginU = parameters.Margin / 1000.0; // 转换为米
        var marginV = parameters.Margin / 1000.0;
        
        var corner0 = TryEvaluate(face, minU, minV);
        var corner1 = TryEvaluate(face, maxU, minV);
        var corner2 = TryEvaluate(face, minU, maxV);
        
        if (!corner0.IsValid || !corner1.IsValid || !corner2.IsValid)
        {
            return new List<FaceSample>();
        }
        
        // 计算物理尺寸（米）
        var uLength = (corner1.Position - corner0.Position).Length;
        var vLength = (corner2.Position - corner0.Position).Length;
        
        // 计算 UV 步长：根据物理间距计算 UV 参数空间步长
        var uStep = parameters.SpacingU / 1000.0 / uLength * (maxU - minU);
        var vStep = parameters.SpacingV / 1000.0 / vLength * (maxV - minV);
        
        // 确保最小步长
        uStep = Math.Max(uStep, 1e-5);
        vStep = Math.Max(vStep, 1e-5);
        
        var samples = new List<FaceSample>();
        
        // 计算实际采样范围（考虑边距）
        var marginUParam = marginU / uLength * (maxU - minU);
        var marginVParam = marginV / vLength * (maxV - minV);
        var startU = minU + marginUParam;
        var endU = maxU - marginUParam;
        var startV = minV + marginVParam;
        var endV = maxV - marginVParam;

        for (var u = startU; u <= endU; u += uStep)
        {
            for (var v = startV; v <= endV; v += vStep)
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
