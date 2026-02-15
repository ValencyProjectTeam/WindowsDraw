# WindowImagePlayer API 文档

**GIF 演示（请等待加载）**：

![1234566](https://github.com/user-attachments/assets/5dde0031-80de-49d4-a1e9-812d30fb3c93)

`WindowImagePlayerHost` 是一个基于 WinForms 的组件，通过在桌面上动态控制大量原生窗口的位置和尺寸来复现图像序列。它支持增量渲染、性能优化配置以及自动化运行模式。

该工具主要用于创作视觉效果（如：Bad Apple 窗口版）或音乐卡点，利用多个 Windows 窗口渲染指定文件夹内的所有图像（支持黑白及灰度判定）。

---

## 1. 解决方案项目说明

本仓库包含两个主要项目，以便于开发者集成和测试：

| 项目名称 | 类型 | 说明 |
| :--- | :--- | :--- |
| **`WindowsDraw.API`** | 类库 (Library) | **核心项目**。包含播放器核心逻辑和 `WindowImagePlayerHost` 类。集成开发时请引用此项目。 |
| **`WindowsDraw.Test`** | 启动项 (App) | **演示项目**。展示了如何调用 API 进行播放，可作为参考 Demo 直接运行。 |

> **调用建议**：在你的生产项目中，请添加对 `WindowsDraw.API` 项目的引用，并确保引入 `using WindowImagePlayer;` 命名空间。

---

## 2. 快速入门

在你的项目（建议为 Windows 窗体应用或控制台应用）中调用：

```csharp
using WindowImagePlayer; // 引用 API 项目命名空间

static void Main()
{
    Application.EnableVisualStyles();
    Application.SetCompatibleTextRenderingDefault(false);

    // 实例化 API 项目中的宿主类
    var player = new WindowImagePlayerHost(@"C:\PathToImages")
    {
        AutoStart = true,               // 启动后立即播放
        AutoCloseWhenFinished = true,   // 播放完自动退出
        StepSize = 30,                  // 采样步长
        FrameInterval = 50,             // 50ms 一帧
        WindowColor = Color.White       // 窗口颜色
    };

    Application.Run(player);
}
```

---

## 3. API 详细规格

### 构造函数：`WindowImagePlayerHost(string targetFolderPath)`
*   **参数**: `targetFolderPath` (string) - 包含图像文件（.jpg, .png, .bmp）的文件夹路径，这个文件夹内应该包含多张黑白图片以实现连续动画。

### 公开配置属性

| 属性名 | 类型 | 默认值 | 说明 |
| :--- | :--- | :--- | :--- |
| **`AutoStart`** | `bool` | `false` | 为 `true` 时，不显示控制面板，运行即播放（静默模式）。 |
| **`AutoCloseWhenFinished`** | `bool` | `true` | 为 `true` 时，播放完最后一帧会自动结束进程。 |
| **`StepSize`** | `int` | `25` | 采样步长（像素）。值越小画面越细但窗口越多，建议 20-50。 |
| **`BrightnessThreshold`** | `float` | `0.4f` | 亮度阈值（0.0-1.0）。低于此值的颜色将被绘制为窗口。 |
| **`FrameInterval`** | `int` | `200` | 帧率控制（毫秒）。 |
| **`ResetRatio`** | `double` | `1.9` | 窗口池重刷比例。用于处理画面剧烈闪烁时的性能平衡。 |
| **`WindowColor`** | `Color` | `White` | 生成的像素窗口背景色。 |
| **`WindowTitle`** | `string` | `" "` | 像素窗口的标题栏文本。 |

---

## 4. 运行模式说明

1.  **控制台交互模式 (`AutoStart = false`)**:
    *   启动后显示一个 300x150 的控制窗口，点击“开始播放”按钮后触发渲染。
    *   适合调试参数或手动触发效果。

2.  **静默执行模式 (`AutoStart = true`)**:
    *   控制窗体完全透明且不显示在任务栏，用户直接看到桌面窗口跳动效果。
    *   适合配合脚本或自动化演示。

---

## 5. 重要注意事项

*   **性能消耗**: `StepSize` 设置得过小（如 < 15）会导致系统瞬间产生数千个窗口，可能造成桌面窗口管理器 (DWM) 卡顿。
*   **文件依赖**: 图像文件必须按顺序命名（如 `001.jpg`, `002.jpg`），API 内部会按名称进行升序排列。
*   **进程残留警告**: **如果使用静默模式 (`AutoStart = true`)，请务必将 `AutoCloseWhenFinished` 设为 `true`**。否则在播放结束后，由于主窗口隐藏，用户很难手动关闭进程，可能导致内存残留。
*   **资源清理**: 类内部已封装 `OnFormClosing` 逻辑，关闭主程序时会强制销毁所有创建的子窗口。
