#include <windows.h>
#include <gdiplus.h>
#include <vector>
#include <string>
#include <filesystem>
#include <algorithm>
#include <iostream>

#pragma comment(lib, "gdiplus.lib")

using namespace Gdiplus;
namespace fs = std::filesystem;

// ================= 配置参数 =================
const int STEP_SIZE = 25;
const float BRIGHTNESS_THRESHOLD = 0.4f;
const int FRAME_INTERVAL = 60;
const wchar_t* WINDOW_CLASS_NAME = L"PixelWindow";
const int EXIT_HOTKEY_ID = 1; // 热键ID

// ================= 全局变量 =================
std::vector<HWND> g_windowPool;
std::vector<std::wstring> g_imageFiles;
bool g_keepRunning = true; // 运行控制标志

// 消息处理函数：用于检查是否按下了退出快捷键
void ProcessMessages() {
    MSG msg;
    // 使用 PeekMessage 而不是 GetMessage，因为它不会阻塞播放循环
    while (PeekMessage(&msg, NULL, 0, 0, PM_REMOVE)) {
        if (msg.message == WM_HOTKEY && msg.wParam == EXIT_HOTKEY_ID) {
            std::wcout << L"检测到退出快捷键，正在关闭..." << std::endl;
            g_keepRunning = false;
        }
        TranslateMessage(&msg);
        DispatchMessage(&msg);
    }
    // 额外兜底：检测 Esc 键是否按下
    if (GetAsyncKeyState(VK_ESCAPE) & 0x8000) {
        g_keepRunning = false;
    }
}

void InitGDIPlus(ULONG_PTR& token) {
    GdiplusStartupInput input;
    GdiplusStartup(&token, &input, NULL);
}

void RegisterPixelClass(HINSTANCE hInst) {
    WNDCLASSEXW wc = { sizeof(WNDCLASSEXW) };
    wc.lpfnWndProc = DefWindowProc;
    wc.hInstance = hInst;
    wc.lpszClassName = WINDOW_CLASS_NAME;
    wc.hbrBackground = (HBRUSH)CreateSolidBrush(RGB(255, 255, 255));
    wc.hCursor = LoadCursor(NULL, IDC_ARROW);
    RegisterClassExW(&wc);
}

void LoadImageFiles(const std::wstring& path) {
    if (!fs::exists(path)) return;
    for (const auto& entry : fs::directory_iterator(path)) {
        if (entry.is_regular_file()) {
            auto ext = entry.path().extension().wstring();
            std::transform(ext.begin(), ext.end(), ext.begin(), ::tolower);
            if (ext == L".jpg" || ext == L".png" || ext == L".bmp" || ext == L".jpeg") {
                g_imageFiles.push_back(entry.path().wstring());
            }
        }
    }
    std::sort(g_imageFiles.begin(), g_imageFiles.end());
}

std::vector<RECT> CalculateRects(const std::wstring& file) {
    std::vector<RECT> rects;
    Bitmap bmp(file.c_str());
    if (bmp.GetLastStatus() != Ok) return rects;

    int screenW = GetSystemMetrics(SM_CXSCREEN);
    int screenH = GetSystemMetrics(SM_CYSCREEN);
    int cols = max(1, screenW / STEP_SIZE);
    int rows = max(1, screenH / STEP_SIZE);

    float cellW = (float)screenW / cols;
    float cellH = (float)screenH / rows;

    std::vector<std::vector<bool>> grid(cols, std::vector<bool>(rows, false));
    for (int y = 0; y < rows; ++y) {
        for (int x = 0; x < cols; ++x) {
            Color c;
            int imgX = (int)(x * bmp.GetWidth() / cols);
            int imgY = (int)(y * bmp.GetHeight() / rows);
            bmp.GetPixel(imgX, imgY, &c);
            float brightness = (0.299f * c.GetR() + 0.587f * c.GetG() + 0.114f * c.GetB()) / 255.0f;
            if (brightness < BRIGHTNESS_THRESHOLD) grid[x][y] = true;
        }
    }

    std::vector<std::vector<bool>> visited(cols, std::vector<bool>(rows, false));
    for (int y = 0; y < rows; ++y) {
        for (int x = 0; x < cols; ++x) {
            if (grid[x][y] && !visited[x][y]) {
                int w = 0;
                while (x + w < cols && grid[x + w][y] && !visited[x + w][y]) w++;
                int h = 1;
                while (y + h < rows) {
                    bool rowOk = true;
                    for (int k = 0; k < w; ++k) { if (!grid[x + k][y + h] || visited[x + k][y + h]) { rowOk = false; break; } }
                    if (rowOk) h++; else break;
                }
                for (int i = 0; i < w; ++i) for (int j = 0; j < h; ++j) visited[x + i][y + j] = true;
                RECT r = { (int)(x * cellW), (int)(y * cellH), (int)((x + w) * cellW), (int)((y + h) * cellH) };
                rects.push_back(r);
            }
        }
    }
    return rects;
}

int main() {
    ULONG_PTR gdipToken;
    InitGDIPlus(gdipToken);
    HINSTANCE hInst = GetModuleHandle(NULL);
    RegisterPixelClass(hInst);

    // 1. 注册全局快捷键: Ctrl + Shift + Q
    // MOD_CONTROL (0x0002) | MOD_SHIFT (0x0004)
    if (!RegisterHotKey(NULL, EXIT_HOTKEY_ID, MOD_CONTROL | MOD_SHIFT, 'Q')) {
        std::cout << "无法注册热键 Ctrl+Shift+Q" << std::endl;
    }

    std::wstring folderPath = L"C:\\PublicFiles\\Downloads\\badapple_output_frames";
    LoadImageFiles(folderPath);

    std::wcout << L"正在播放。按下 Ctrl + Shift + Q 或 Esc 键退出程序。" << std::endl;

    for (const auto& file : g_imageFiles) {
        // 每帧开始前处理系统消息
        ProcessMessages();
        if (!g_keepRunning) break;

        std::vector<RECT> targetRects = CalculateRects(file);

        while (g_windowPool.size() < targetRects.size()) {
            HWND hwnd = CreateWindowEx(
                WS_EX_TOPMOST /*| WS_EX_TOOLWINDOW*/ | WS_EX_NOACTIVATE,
                WINDOW_CLASS_NAME, L"BadApple",
                WS_OVERLAPPEDWINDOW | WS_VISIBLE,
                -2000, -2000, 0, 0, NULL, NULL, hInst, NULL
            );
            g_windowPool.push_back(hwnd);
        }

        HDWP hdwp = BeginDeferWindowPos((int)g_windowPool.size());
        for (size_t i = 0; i < g_windowPool.size(); ++i) {
            HWND hwnd = g_windowPool[i];
            if (i < targetRects.size()) {
                RECT r = targetRects[i];
                hdwp = DeferWindowPos(hdwp, hwnd, NULL, r.left, r.top, r.right - r.left, r.bottom - r.top, SWP_NOZORDER | SWP_NOACTIVATE | SWP_SHOWWINDOW);
            }
            else {
                hdwp = DeferWindowPos(hdwp, hwnd, NULL, -2000, -2000, 0, 0, SWP_NOZORDER | SWP_NOACTIVATE);
            }
        }
        EndDeferWindowPos(hdwp);

        Sleep(FRAME_INTERVAL);
    }

    // 清理
    UnregisterHotKey(NULL, EXIT_HOTKEY_ID);
    for (HWND hwnd : g_windowPool) DestroyWindow(hwnd);
    GdiplusShutdown(gdipToken);
    return 0;
}