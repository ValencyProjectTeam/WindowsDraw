/**
 MIT License
 Copyright (c) 2026 ValencyProject Team - Higashitani Yume

 你可以自由地使用、复制、修改、合并、发布、分发、再授权和/或销售本软件的副本，但是你必须
 */

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowImagePlayer
{
	/// <summary>
	/// 窗口图像播放器宿主。
	/// 该类本身是一个控制窗体，运行后会根据图像内容在桌面上动态创建、移动和缩放大量子窗体。
	/// </summary>
	public class WindowImagePlayerHost : Form
	{
		// ================= 公开配置属性 =================

		/// <summary>
		/// 是否直接开始播放。
		/// 如果为 true，则不显示控制台界面（启动即播放）；如果为 false，则显示带有“开始”按钮的界面。
		/// </summary>
		public bool AutoStart { get; set; } = false;

		/// <summary>
		/// 播放完成后是否自动关闭程序。
		/// </summary>
		public bool AutoCloseWhenFinished { get; set; } = true;

		/// <summary>
		/// 采样步长（像素）。值越小，图像越精细，但生成的窗口数量越多。建议值：20-50。
		/// </summary>
		public int StepSize { get; set; } = 25;

		/// <summary>
		/// 黑色判定阈值（0.0 到 1.0）。亮度低于此值的像素区域将被识别为“实体”并生成窗口。
		/// </summary>
		public float BrightnessThreshold { get; set; } = 0.4f;

		/// <summary>
		/// 帧间隔（毫秒）。控制播放速度（如 40ms 对应 25fps）。
		/// </summary>
		public int FrameInterval
		{
			get => _playTimer.Interval;
			set => _playTimer.Interval = value;
		}

		/// <summary>
		/// 触发全量重刷的比例阈值。
		/// 如果下一帧需要的窗口数超过当前窗口池数量的 N 倍，则清空池重新创建。
		/// 默认 1.9，这有助于处理转场剧烈的画面。
		/// </summary>
		public double ResetRatio { get; set; } = 1.9;

		/// <summary>
		/// 生成的“像素窗口”的背景颜色。
		/// </summary>
		public Color WindowColor { get; set; } = Color.White;

		/// <summary>
		/// 生成的“像素窗口”的标题文字。
		/// </summary>
		public string WindowTitle { get; set; } = " ";

		/// <summary>
		/// 播放列表中的当前图像索引。
		/// </summary>
		public int CurrentIndex => _currentIndex;

		// ================= 私有变量 =================

		private List<Form> _windowPool = new List<Form>();
		private string[] _imageFiles;
		private int _currentIndex = 0;
		private bool _isProcessing = false;
		private Timer _playTimer;
		private string _targetFolderPath;
		private Button _btnStart;

		/// <summary>
		/// 初始化播放器宿主窗口。
		/// </summary>
		/// <param name="targetFolderPath">包含图像序列（jpg/png/bmp）的文件夹路径</param>
		public WindowImagePlayerHost(string targetFolderPath)
		{
			_targetFolderPath = targetFolderPath;

			this.Icon = SystemIcons.Information;

			// 初始化宿主 UI
			this.Text = "窗口图像播放器 - 控制台";
			this.Width = 500;
			this.Height = 250;
			this.FormBorderStyle = FormBorderStyle.FixedSingle;
			this.StartPosition = FormStartPosition.CenterScreen;

			// 初始化播放计时器
			_playTimer = new Timer { Interval = 200 };
			_playTimer.Tick += OnTimerTick;

			// 创建开始按钮（控制台模式可见）
			_btnStart = new Button
			{
				Text = "开始播放",
				Dock = DockStyle.Fill,
				BackColor = Color.LightBlue,
				Font = new Font("微软雅黑", 12, FontStyle.Bold)
			};
			_btnStart.Click += (s, e) => StartPlayback();
			this.Controls.Add(_btnStart);

			LoadImages();
		}

		/// <summary>
		/// 在窗体加载时判断是否需要直接开始
		/// </summary>
		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);

			if (AutoStart)
			{
				// 隐藏控制台界面并直接开始
				this.Opacity = 0;
				this.ShowInTaskbar = false;
				StartPlayback();
			}
		}

		/// <summary>
		/// 加载目标文件夹下的所有图片文件。
		/// </summary>
		private void LoadImages()
		{
			if (Directory.Exists(_targetFolderPath))
			{
				string[] extensions = { "*.jpg", "*.jpeg", "*.png", "*.bmp" };
				_imageFiles = extensions.SelectMany(ext => Directory.GetFiles(_targetFolderPath, ext))
										.OrderBy(f => f).ToArray();
			}

			if (_imageFiles == null || _imageFiles.Length == 0)
			{
				MessageBox.Show("文件夹内未找到有效的图片文件！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		/// <summary>
		/// 开始播放图像序列。
		/// </summary>
		public void StartPlayback()
		{
			if (_imageFiles == null || _imageFiles.Length == 0) return;
			_currentIndex = 0;
			_playTimer.Start();
		}

		/// <summary>
		/// 停止播放并清理所有窗口。
		/// </summary>
		public void StopPlayback()
		{
			_playTimer.Stop();
			ClearAllPoolWindows();
		}

		private async void OnTimerTick(object sender, EventArgs e)
		{
			if (_isProcessing || _imageFiles == null || _imageFiles.Length == 0) return;
			_isProcessing = true;

			// --- 播放完成逻辑 ---
			if (_currentIndex >= _imageFiles.Length)
			{
				StopPlayback();
				_isProcessing = false;

				if (AutoCloseWhenFinished)
				{
					this.Close(); // 自动关闭宿主，触发资源清理
				}
				return;
			}

			string currentFile = _imageFiles[_currentIndex];

			// 1. 在后台线程计算当前帧需要显示的矩形集合
			List<Rectangle> targetRects = await Task.Run(() => CalculateFrameRectList(currentFile));

			// 2. 将计算结果应用到窗口池（增量更新或重刷）
			ApplyIncrementalRender(targetRects);

			_currentIndex++;
			_isProcessing = false;
		}

		private void ApplyIncrementalRender(List<Rectangle> targetRects)
		{
			int targetCount = targetRects.Count;
			int currentPoolCount = _windowPool.Count;

			if (currentPoolCount == 0 || targetCount > currentPoolCount * ResetRatio)
			{
				ClearAllPoolWindows();
				foreach (var rect in targetRects)
				{
					Form f = CreateStandardForm(rect);
					_windowPool.Add(f);
					f.Show();
				}
			}
			else
			{
				if (currentPoolCount > targetCount)
				{
					for (int i = currentPoolCount - 1; i >= targetCount; i--)
					{
						_windowPool[i].Close();
						_windowPool[i].Dispose();
						_windowPool.RemoveAt(i);
					}
				}
				else if (currentPoolCount < targetCount)
				{
					for (int i = currentPoolCount; i < targetCount; i++)
					{
						Form f = CreateStandardForm(Rectangle.Empty);
						_windowPool.Add(f);
						f.Show();
					}
				}

				for (int i = 0; i < _windowPool.Count; i++)
				{
					if (_windowPool[i].Bounds != targetRects[i])
					{
						_windowPool[i].Bounds = targetRects[i];
					}
				}
			}
		}

		private List<Rectangle> CalculateFrameRectList(string filePath)
		{
			List<Rectangle> rects = new List<Rectangle>();
			try
			{
				using (Bitmap original = new Bitmap(filePath))
				{
					int screenW = Screen.PrimaryScreen.Bounds.Width;
					int screenH = Screen.PrimaryScreen.Bounds.Height;
					int cols = Math.Max(1, screenW / StepSize);
					int rows = Math.Max(1, screenH / StepSize);

					using (Bitmap lowRes = new Bitmap(cols, rows))
					{
						using (Graphics g = Graphics.FromImage(lowRes))
						{
							g.InterpolationMode = InterpolationMode.Low;
							g.DrawImage(original, 0, 0, cols, rows);
						}

						bool[,] visited = new bool[cols, rows];
						float cellWidth = (float)screenW / cols;
						float cellHeight = (float)screenH / rows;

						for (int y = 0; y < rows; y++)
						{
							for (int x = 0; x < cols; x++)
							{
								if (!visited[x, y] && lowRes.GetPixel(x, y).GetBrightness() < BrightnessThreshold)
								{
									Rectangle logicRect = FindLargestRect(lowRes, visited, x, y);
									Rectangle screenRect = new Rectangle(
										(int)(logicRect.X * cellWidth),
										(int)(logicRect.Y * cellHeight),
										(int)(logicRect.Width * cellWidth),
										(int)(logicRect.Height * cellHeight)
									);
									if (screenRect.Width >= 10 && screenRect.Height >= 10)
										rects.Add(screenRect);
								}
							}
						}
					}
				}
			}
			catch { }
			return rects.OrderBy(r => r.Y).ThenBy(r => r.X).ToList();
		}

		private Form CreateStandardForm(Rectangle rect)
		{
			return new Form
			{
				FormBorderStyle = FormBorderStyle.Sizable,
				BackColor = this.WindowColor,
				Text = this.WindowTitle,
				StartPosition = FormStartPosition.Manual,
				Bounds = rect,
				ShowInTaskbar = false,
				TopMost = true
			};
		}

		private void ClearAllPoolWindows()
		{
			foreach (var f in _windowPool)
			{
				if (!f.IsDisposed)
				{
					f.Close();
					f.Dispose();
				}
			}
			_windowPool.Clear();
		}

		private Rectangle FindLargestRect(Bitmap bmp, bool[,] visited, int startX, int startY)
		{
			int maxWidth = 0;
			for (int x = startX; x < bmp.Width; x++)
			{
				if (!visited[x, startY] && bmp.GetPixel(x, startY).GetBrightness() < BrightnessThreshold)
					maxWidth++;
				else break;
			}

			int maxHeight = 0;
			for (int y = startY; y < bmp.Height; y++)
			{
				bool rowOk = true;
				for (int x = startX; x < startX + maxWidth; x++)
				{
					if (visited[x, y] || bmp.GetPixel(x, y).GetBrightness() >= BrightnessThreshold)
					{
						rowOk = false; break;
					}
				}
				if (rowOk) maxHeight++;
				else break;
			}

			for (int i = startX; i < startX + maxWidth; i++)
				for (int j = startY; j < startY + maxHeight; j++)
					visited[i, j] = true;

			return new Rectangle(startX, startY, maxWidth, maxHeight);
		}

		protected override void OnFormClosing(FormClosingEventArgs e)
		{
			_playTimer?.Stop();
			ClearAllPoolWindows();
			base.OnFormClosing(e);
		}
	}
}