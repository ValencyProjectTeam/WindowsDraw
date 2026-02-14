# WindowImagePlayer API 文档

`WindowImagePlayerHost` 是一个基于 WinForms 的组件，通过在桌面上动态控制大量原生窗口的位置和尺寸来复现图像序列。它支持增量渲染、性能优化配置以及自动化运行模式。

这个工具可以用于创作视觉效果或者音乐卡点，使用数个Windows窗口渲染指定文件夹内所有黑白图片。

## 1. 快速入门

在 `Program.cs` 中直接初始化并运行：

```csharp
static void Main()
{
    Application.EnableVisualStyles();
    Application.SetCompatibleTextRenderingDefault(false);

    var player = new WindowImagePlayerHost(@"C:\PathToImages")
    {
        AutoStart = true,               // 启动后立即播放
        AutoCloseWhenFinished = true,   // 播放完自动退出
        StepSize = 30,                  // 采样步长
        FrameInterval = 50              // 50ms 一帧
    };

    Application.Run(player);
}
```

## 2. 构造函数

### `WindowImagePlayerHost(string targetFolderPath)`
初始化播放器宿主。
*   **参数**: `targetFolderPath` (string) - 包含图像文件（.jpg, .png, .bmp）的文件夹路径。文件将按文件名排序播放。

---

## 3. 公开配置属性

| 属性名 | 类型 | 默认值 | 说明 |
| :--- | :--- | :--- | :--- |
| `AutoStart` | `bool` | `false` | 为 `true` 时，启动程序不显示控制面板，直接开始播放。 |
| `AutoCloseWhenFinished` | `bool` | `true` | 为 `true` 时，播放完最后一帧会自动关闭宿主程序。 |
| `StepSize` | `int` | `25` | 采样步长（单位：像素）。值越小画面越精细，但生成的窗口数量越多。建议范围 20-50。 |
| `BrightnessThreshold` | `float` | `0.4f` | 黑色判定阈值（0.0-1.0）。亮度低于此值的区域会生成窗口。 |
| `FrameInterval` | `int` | `200` | 帧间隔（单位：毫秒）。控制播放速度。 |
| `ResetRatio` | `double` | `1.9` | 重刷阈值。新一帧窗口数超过当前池 `N` 倍时，会销毁所有窗口重建以提高性能。 |
| `WindowColor` | `Color` | `White` | 像素窗口的背景颜色。 |
| `WindowTitle` | `string` | `" "` | 像素窗口的标题栏文字。 |
| `CurrentIndex` | `int` | `0` | (只读) 当前正在播放的图像索引。 |

---

## 4. 公开方法

### `StartPlayback()`
手动开始播放。如果 `AutoStart` 为 `false`，可调用此方法启动逻辑。

### `StopPlayback()`
停止播放并销毁当前桌面上所有的像素窗口。

---

## 5. 运行模式说明

1.  **控制台模式 (`AutoStart = false`)**:
    *   程序启动后会显示一个中心窗口，点击“开始播放”按钮后触发。
    *   适合手动调试或选择播放时机。

2.  **静默模式 (`AutoStart = true`)**:
    *   宿主窗口将完全透明且不显示在任务栏。
    *   程序启动后直接根据图片序列在屏幕上绘制窗口。
    *   配合 `AutoCloseWhenFinished = true` 可以实现完全自动化的视觉效果演示。

---

## 6. 注意事项

*   **性能消耗**: `StepSize` 设置得过小（如 < 15）会导致系统瞬间产生数千个窗口，可能造成系统卡顿或图形驱动响应缓慢。
*   **资源清理**: 类内部已实现 `OnFormClosing` 逻辑，关闭宿主窗口时会自动释放并销毁所有池中的子窗口。
*   **排序依赖**: 图像文件必须具有可排序的文件名（如 `frame_001.jpg`, `frame_002.jpg`）。
*   **如果使用静默模式，请务必提供关闭方法（比如应用程序为“控制台应用程序”而不是“Windows应用程序”）或者指定 `AutoCloseWhenFinished` 为 `True`否则将会死循环！**