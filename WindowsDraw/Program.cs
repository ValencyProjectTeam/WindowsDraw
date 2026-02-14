using System;
using System.Drawing;
using System.Windows.Forms;
using WindowImagePlayer;

static class Program
{
	[STAThread]
	static void Main()
	{
		Application.EnableVisualStyles();
		Application.SetCompatibleTextRenderingDefault(false);

		// --- 直接在这里调用 API ---

		string myImages = @"C:\Users\shimikoi\AppData\Local\Temp\Badapple"; // 你的图片目录

		var player = new WindowImagePlayerHost(myImages)
		{
			StepSize = 25,               // 增大步长，性能更好
			FrameInterval = 200,          // 帧率更快
			BrightnessThreshold = 0.4f,  // 判定阈值更灵敏
			WindowColor = Color.Black,   // 改成黑色窗口
			WindowTitle = "Apple"        // 每个窗口的标题
		};

		Application.Run(player);
	}
}