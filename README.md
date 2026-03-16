# WindowsDraw 项目文档

**GIF 演示（请等待加载）**：

![1234566](https://github.com/user-attachments/assets/5dde0031-80de-49d4-a1e9-812d30fb3c93)

`WindowImagePlayerHost` 是一个基于 WinForms 的组件，通过在桌面上动态控制大量原生窗口的位置和尺寸来复现图像序列。它支持增量渲染、性能优化配置以及自动化运行模式。

该工具主要用于创作视觉效果（如：Bad Apple 窗口版）或音乐卡点，利用多个 Windows 窗口渲染指定文件夹内的所有图像（支持黑白及灰度判定）。
---

## 1. 解决方案项目说明

本解决方案包含三个主要项目，涵盖了核心逻辑、演示示例以及底层实现：

| 项目名称 | 语言 | 类型 | 说明 |
| :--- | :--- | :--- | :--- |
| **`WindowsDraw.API`** | C# | 类库 (.NET 4.7.2) | **核心组件**。包含 `WindowImagePlayerHost` 类，负责图像采样、窗口池管理及增量渲染逻辑。 |
| **`WindowsDraw.Test`** | C# | 桌面应用 | **集成演示**。内置了 Bad Apple 视频帧资源（ZIP），启动时自动解压并调用 API 进行播放。 |
| **`WindowsDraw.WinAPI`**| C++ | 控制台应用 | **高性能实现**。基于 Win32 API 和 C++20 编写的底层版本，适合追求极致响应速度的场景。 |

---

## 2. 快速入门 (C#)

在你的项目中引用 `WindowsDraw.API.dll`，并确保命名空间正确：

```csharp
using System.Drawing;
using System.Windows.Forms;
using WindowImagePlayer; // 核心命名空间

static void Main()
{
    Application.EnableVisualStyles();
    
    // 初始化宿主，传入图片序列文件夹路径
    var player = new WindowImagePlayerHost(@"C:\Images\Frames")
    {
        StepSize = 30,              // 采样步长（像素），控制窗口密度
        FrameInterval = 40,         // 帧间隔（ms），40ms 约等于 25fps
        BrightnessThreshold = 0.4f, // 亮度阈值，低于此值将生成窗口
        WindowColor = Color.White,  // 窗口填充颜色
        WindowTitle = "Bad Apple",  // 窗口标题栏文本
        AutoStart = false,          // 是否显示控制台界面
        AutoCloseWhenFinished = true
    };

    Application.Run(player);
}
```

---

## 3. 核心 API 规格 (`WindowImagePlayerHost`)

### 属性 (Properties)

| 属性名 | 类型 | 默认值 | 说明 |
| :--- | :--- | :--- | :--- |
| **`StepSize`** | `int` | `25` | 采样像素步长。值越小图像越精细，但生成的窗口数量呈指数级增加。 |
| **`BrightnessThreshold`** | `float` | `0.4f` | 判定阈值（0.0~1.0）。低于此亮度的区域被识别为“实体”。 |
| **`ResetRatio`** | `double` | `1.9` | 窗口池重刷比例。当新一帧所需窗口超过当前池 `1.9` 倍时强制清空池。 |
| **`FrameInterval`** | `int` | `200` | 播放速率（毫秒/帧）。 |
| **`AutoStart`** | `bool` | `false` | 为 `true` 时进入“静默模式”直接播放，不显示控制窗体。 |
| **`WindowColor`** | `Color` | `White` | 子窗口的背景颜色。 |

### 事件 (Events)

*   **`FrameRendering`**: 在每一帧开始计算渲染逻辑前触发。
*   **`FrameRendered`**: 在当前帧的所有窗口位置/数量更新完成后触发。

---

## 4. 特色功能说明

1.  **窗口池增量更新 (Incremental Rendering)**:
    API 不会每一帧都销毁重建窗口，而是通过 `ApplyIncrementalRender` 逻辑，优先复用已有的窗口对象并更新其 `Bounds` 属性，极大降低了系统句柄分配开销。

2.  **内置资源自解压 (WindowsDraw.Test)**:
    测试项目演示了如何从资源文件 (`Resource1`) 中提取 `badapple_output_frames.zip` 到临时目录并自动加载，实现了“开箱即用”的动画演示。

3.  **高性能采样**:
    计算逻辑在 `CalculateFrameRectList` 中通过 `Task.Run` 异步执行，并在低分辨率缩略图上进行区域合并算法（`FindLargestRect`），有效减少了碎小窗口的数量。

---

## 5. 开发建议与注意事项

*   **性能警告**: 如果 `StepSize` 设置低于 `15`，在复杂画面下可能会产生超过 2000 个窗口，这会导致系统 DWM (桌面窗口管理器) 负载过高，产生明显的画面撕裂或卡顿。
*   **DPI 感知**: `WindowsDraw.Test` 已内置 `app.manifest` 开启 DPI 感知，确保在高分屏下窗口位置不会偏移。
*   **图像预处理**: 为了获得最佳效果，建议输入的图片序列为高对比度的黑白图像。
*   **环境要求**: 
    *   .NET Framework 4.7.2+
    *   C++ 实现需 Visual Studio 2022 (v143 工具链) 并支持 C++20 标准。

---
**License**: MIT License | Copyright (c) 2026 ValencyProject Team - Higashitani Yume