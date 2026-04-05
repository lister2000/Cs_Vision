# Cs_Vision

Windows 计算机视觉工具集 — 基于 C# (.NET 6) + OpenCvSharp4 + DlibDotNet。

---

## 项目结构

```
Cs_Vision/
├── Cs_Vision/              # C# Windows Forms 主程序（对象检测、颜色追踪等）
│   ├── Form1.cs            # 主窗口（图像显示、区域选择、目标跟踪）
│   ├── Form2.cs            # 参数调节面板（HSV 阈值等）
│   ├── Form3.cs            # 数据监视窗口
│   ├── CsTools.cs          # 图像处理工具（SURF、直方图匹配、HOG 等）
│   ├── CsPram.cs           # 全局参数单例
│   ├── CsDatas.cs          # 数据结构（坐标、距离、亮度等特征）
│   ├── ColorDetector.cs    # 颜色检测
│   └── PolyFeature.cs      # 多边形特征提取
│
├── pose_tracking/          # ✨ 实时人体关键点 / 骨架跟踪示例（Python）
│   ├── pose_tracker.py     # 主脚本：MediaPipe Pose + 贪心质心跟踪器
│   ├── requirements.txt    # Python 依赖
│   └── README.md           # 安装说明、使用文档、常见问题（中文）
│
└── Cs_Vision.sln           # Visual Studio 解决方案
```

---

## 快速入口

### 实时人体骨架跟踪（推荐新用户从这里开始）

→ **[pose_tracking/README.md](pose_tracking/README.md)**

功能亮点：
- 🎥 实时摄像头输入
- 🦴 33 个身体关键点 + 骨骼连线渲染
- 🏷️ 稳定的跨帧人物 ID（贪心质心匹配跟踪器）
- 📊 实时 FPS 显示
- 🪟 Windows 一键运行（仅需 `pip install -r requirements.txt`）

```powershell
cd pose_tracking
pip install -r requirements.txt
python pose_tracker.py
```

---

### C# 主程序（对象/颜色跟踪）

**环境要求：**
- Visual Studio 2022（含 .NET 6 工作负载）
- Windows 10 / 11 x64

**构建与运行：**
```powershell
# 打开解决方案
start Cs_Vision.sln
# 在 Visual Studio 中按 F5 构建并运行
```

**NuGet 依赖**（自动恢复）：
- `OpenCvSharp4.Windows` 4.6.0
- `OpenCvSharp4.Extensions` 4.6.0
- `DlibDotNet` 19.21.0

---

## 技术栈

| 模块 | 语言/框架 | 主要依赖 |
|------|-----------|---------|
| C# 主程序 | C# / .NET 6 / WinForms | OpenCvSharp4, DlibDotNet |
| 人体姿态跟踪 | Python 3.9–3.11 | MediaPipe, OpenCV-Python, NumPy |

---

## License

本项目代码遵循仓库默认许可证。MediaPipe 遵循 [Apache 2.0](https://github.com/google-ai-edge/mediapipe/blob/master/LICENSE)。
