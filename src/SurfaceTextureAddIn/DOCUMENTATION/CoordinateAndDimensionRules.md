# 坐标转换与尺寸转换规则

## 1. 矩阵格式规则

### 1.1 SolidWorks 变换矩阵格式
SolidWorks 的 `IMathUtility.CreateTransform` 使用特定的 16 元素数组格式：

```
| 0  1  2  13 |  ← X轴旋转分量
| 3  4  5  14 |  ← Y轴旋转分量  
| 6  7  8  15 |  ← Z轴旋转分量
| 9 10 11  12 |  ← 索引9-11:平移(x,y,z), 索引12:缩放因子
```

**规则**:
- 索引 0-8：3x3 旋转矩阵
- 索引 9-11：平移向量 (tx, ty, tz)
- 索引 12：缩放因子（默认1）
- 索引 13-15：未使用（设为0）

**错误示例**:
```csharp
// 错误：平移分量放错位置
var matrix = new double[16] {
    1, 0, 0, tx,   // 错误！tx放在了索引3
    0, 1, 0, ty,
    0, 0, 1, tz,
    0, 0, 0, 1
};
```

**正确示例**:
```csharp
// 正确：平移分量在索引9-11
var matrix = new double[16];
matrix[0] = 1; matrix[1] = 0; matrix[2] = 0; matrix[3] = 0;
matrix[4] = 1; matrix[5] = 0; matrix[6] = 0; matrix[7] = 0;
matrix[8] = 1; matrix[9] = tx; matrix[10] = ty; matrix[11] = tz;
matrix[12] = 1; matrix[13] = 0; matrix[14] = 0; matrix[15] = 0;
```

---

## 2. 单位转换规则

### 2.1 基本单位约定
| 来源 | 单位 | 说明 |
|------|------|------|
| SolidWorks API | 米 (m) | 所有坐标、尺寸参数 |
| UI 用户输入 | 毫米 (mm) | 间距、高度、深度等 |
| UV 参数空间 | 无量纲 | 范围通常为 [0,1] |

**规则**: 从 UI 获取的值必须除以 1000 转换为米后再传递给 SolidWorks API

**错误示例**:
```csharp
// 错误：直接使用毫米值
double depth = parameters.HeightOrDepth;  // 用户输入1mm
offset = normal * depth;  // 实际偏移1米！
```

**正确示例**:
```csharp
// 正确：转换为米
double depthM = parameters.HeightOrDepth / 1000.0;  // 1mm → 0.001m
offset = normal * depthM;  // 正确偏移0.001米
```

---

## 3. UV 参数空间规则

### 3.1 UV 坐标与物理尺寸的区别
- **UV 参数空间**：参数化坐标，范围通常 [0,1]，用于定位曲面上的点
- **物理尺寸**：实际几何尺寸（毫米/米），用于测量距离

**规则**: 物理间距不能直接作为 UV 步长使用，必须进行转换

**错误示例**:
```csharp
// 错误：直接使用物理间距作为UV步长
for (var u = minU; u <= maxU; u += parameters.SpacingU) { ... }
// SpacingU=1mm → u步长=1.0，UV范围[0,1]只采样1个点！
```

**正确示例**:
```csharp
// 正确：计算UV步长
var uLength = (corner1.Position - corner0.Position).Length;  // 物理长度（米）
var uStep = parameters.SpacingU / 1000.0 / uLength * (maxU - minU);
for (var u = minU; u <= maxU; u += uStep) { ... }
```

---

## 4. 法线方向规则

### 4.1 法线方向的意义
- **外法线**：指向实体外部
- **内法线**：指向实体内部

SolidWorks 面的法线方向取决于面的创建方式和拓扑结构，不能假设一定指向外部。

**规则**: 偏移方向需要考虑法线的实际方向

**错误示例**:
```csharp
// 错误：假设法线指向外部
offset = normal * depth;  // 如果法线指向内部，会偏移到错误方向
```

**正确示例**:
```csharp
// 正确：明确偏移方向
// Boss模式：向法线反方向偏移（推到面外侧）
// Cut模式：向法线正方向偏移（推入面内侧）
var sign = mode == TextureOperationMode.Boss ? 1.0 : -1.0;
offset = normal * (-depth * sign);
```

---

## 5. 代码审查检查清单

在提交代码前，检查以下项目：

- [ ] 所有从 UI 获取的尺寸值是否已转换为米
- [ ] 变换矩阵的平移分量是否在正确位置（索引9-11）
- [ ] UV 循环的步长是否经过正确转换
- [ ] 法线方向是否正确处理
- [ ] 是否有足够的日志记录关键转换步骤
- [ ] 是否处理了除零异常（如计算 UV 步长时分母为零）

---

## 6. 调试技巧

### 6.1 日志记录
在关键转换点添加日志：
```csharp
Log($"Input depth (mm): {depth}");
Log($"Converted depth (m): {depthM}");
Log($"UV step: {uStep}, Physical step (mm): {parameters.SpacingU}");
Log($"Normal direction: ({normal.X}, {normal.Y}, {normal.Z})");
```

### 6.2 验证方法
1. 使用已知尺寸的简单模型测试
2. 检查变换后的 body 边界是否符合预期
3. 确认最终位置与目标位置的偏差

---

## 7. 常见错误对照表

| 现象 | 可能原因 | 解决方案 |
|------|----------|----------|
| Body 移动到很远的位置 | 单位未转换，毫米当作米使用 | 将值除以1000 |
| Body 消失或尺寸异常 | 矩阵格式错误 | 检查索引位置 |
| 只生成一个纹理 | UV步长过大 | 转换物理间距到UV步长 |
| 纹理出现在错误的面（正面/反面） | 法线方向处理错误 | 检查偏移方向 |
| 布尔运算失败 | Body 位置不正确或尺寸过小 | 检查所有转换步骤 |