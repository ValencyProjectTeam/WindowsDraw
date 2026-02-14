using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using WindowImagePlayer;
using WindowsDraw.Test;

static class Program
{

	static DirectoryInfo _tempDir => Directory.CreateDirectory(Path.Combine(Path.GetTempPath(),"BadApple"));

	[STAThread]
	static void Main()
	{
		//解压图片文件到目录
		Unzip();
		Application.EnableVisualStyles();
		Application.SetCompatibleTextRenderingDefault(false);

		// --- 直接在这里调用 API ---

		string myImages = _tempDir.FullName; // 你的图片目录

		var player = new WindowImagePlayerHost(myImages)
		{
			StepSize = 30,               // 增大步长，性能更好
			FrameInterval = 200,          // 帧率更快
			BrightnessThreshold = 0.4f,  // 判定阈值更灵敏
			WindowColor = Color.Black,   // 改成黑色窗口
			WindowTitle = "Apple",        // 每个窗口的标题
			AutoCloseWhenFinished = true,   // 播放结束后自动关闭窗口
			AutoStart = true
		};

		Application.Run(player);
	}

	static void Unzip()
	{
		if (_tempDir.Exists)
		{
			Directory.Delete(_tempDir.FullName,true);
		}
		File.WriteAllBytes(Path.Combine(_tempDir.FullName,"content.zip"),Resource1.badapple_output_frames);
		System.IO.Compression.ZipFile.ExtractToDirectory(Path.Combine(_tempDir.FullName,"content.zip"), _tempDir.FullName);
	}
}