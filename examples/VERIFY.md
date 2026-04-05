# Pose Tracking Demo — Verification Guide

Windows 实时多人骨架关键点 + ID 跟踪示例的快速验证步骤。

---

## 1  前置条件

| 需要 | 最低版本 |
|------|---------|
| Python | 3.9 |
| pip | 23 |
| 摄像头 | 任意 USB 或内置摄像头，能在 Windows 相机 App 里预览即可 |

---

## 2  安装依赖

```powershell
# 在项目根目录下执行（建议先创建虚拟环境）
python -m venv .venv
.venv\Scripts\activate

pip install -r examples/requirements_pose.txt
```

> **GPU 加速（可选）**：如果你有 NVIDIA 显卡并已安装 CUDA 11.8+，把
> `requirements_pose.txt` 里的 `onnxruntime` 换成 `onnxruntime-gpu`，
> 再重新 `pip install`。启动时控制台会打印
> `ONNXRuntime provider : CUDAExecutionProvider`。

---

## 3  获取模型文件

### 方式 A — 自动下载（推荐）

```powershell
python examples/pose_tracking_webcam.py --download
```

脚本会把 `yolov8n-pose.onnx`（约 12 MB）自动下载到 `models/` 目录。

### 方式 B — 手动下载

1. 从 Ultralytics 官方 Release 下载：  
   <https://github.com/ultralytics/assets/releases/download/v8.2.0/yolov8n-pose.onnx>
2. 把文件放到 `models/yolov8n-pose.onnx`（相对于项目根目录）。

---

## 4  运行命令

```powershell
# 使用默认参数（摄像头索引 0，置信度 0.30）
python examples/pose_tracking_webcam.py

# 指定摄像头索引（如果有多个摄像头）
python examples/pose_tracking_webcam.py --device 1

# 自定义置信度阈值（数值越低检测越灵敏，但误检可能增多）
python examples/pose_tracking_webcam.py --conf 0.25

# 一次性下载模型并运行
python examples/pose_tracking_webcam.py --download

# 所有参数说明
python examples/pose_tracking_webcam.py --help
```

---

## 5  预期行为验证清单

运行后窗口标题为 **"Pose Tracking – press Q to quit"**，逐项确认：

- [ ] **摄像头画面正常显示**  
  窗口能看到摄像头实时画面，无黑屏、无花屏。

- [ ] **骨架覆盖（Skeleton overlay）**  
  画面中的人体上方有彩色关键点（圆点）和连线（肢体骨架），
  覆盖头部、肩膀、手臂、髋部、腿部各关节。

- [ ] **FPS 显示**  
  窗口左上角显示绿色 `FPS: xx.x`。  
  CPU 上典型值 **15–30 FPS**；GPU 上可达 **60+ FPS**。

- [ ] **多人检测**  
  画面中有两人或以上时，每人有独立颜色的骨架和边界框。

- [ ] **track_id 稳定性**  
  每个人边界框上方显示 `ID:N`（N 为整数）。  
  正常行走时 ID 应保持不变；短暂遮挡（约 1 秒内）恢复后 ID 应复原。

- [ ] **退出正常**  
  按 **Q** 或 **Esc** 后窗口关闭，Python 进程正常退出，无异常报错。

---

## 6  常见问题排查

### 摄像头打不开（RuntimeError: Cannot open camera index 0）

```
RuntimeError: Cannot open camera index 0.
  • Try a different --device index (0, 1, 2 …)
```

**排查步骤：**

1. 打开 Windows **相机** App，确认摄像头能正常预览。
2. 检查 Windows 设置 → 隐私和安全性 → 摄像头 → 允许应用访问摄像头 → 开启。
3. 依次尝试 `--device 1`、`--device 2`。
4. 如果使用虚拟摄像头（OBS、ManyCam 等），去掉 `CAP_DSHOW` 标志：  
   临时办法是修改脚本第 `cap = cv2.VideoCapture(device, cv2.CAP_DSHOW)` 一行，
   改为 `cap = cv2.VideoCapture(device)`。

---

### 模型文件缺失（FileNotFoundError）

```
FileNotFoundError: Model not found: models/yolov8n-pose.onnx
  Run with --download to auto-download …
```

**解决：** 添加 `--download` 参数或手动把模型放到 `models/` 目录（见第 3 节）。

---

### onnxruntime 未安装（ModuleNotFoundError / SystemExit）

```
SystemExit: onnxruntime not found.
Install CPU version : pip install onnxruntime
```

**解决：** 按提示执行 `pip install onnxruntime`（或 `onnxruntime-gpu`）。  
确保你的 `pip` 对应的是当前激活的虚拟环境。

---

### CPU vs GPU 选择（ONNXRuntime provider）

启动时控制台会打印使用的 Provider：

| 打印内容 | 含义 |
|---------|------|
| `ONNXRuntime provider : CPUExecutionProvider` | 在 CPU 上推理（正常） |
| `ONNXRuntime provider : CUDAExecutionProvider` | 在 NVIDIA GPU 上推理（更快） |

若想强制 CPU（调试用）：安装 `onnxruntime`（非 gpu 版），脚本会自动回退到 CPU。  
若想使用 GPU：安装 `onnxruntime-gpu`，并确认 CUDA 驱动版本匹配。

---

### FPS 很低（< 5）

- 默认 `--input-size 640`，可改为 `--input-size 320` 以换取更高帧率（精度略降）。
- 关闭其他占用 GPU/CPU 的程序。
- 考虑安装 `onnxruntime-gpu`（见上）。

---

### track_id 频繁跳变

- 增大 `IoUTracker` 的 `max_lost`（默认 30 帧）可延长轨迹保持时间。
- 降低置信度阈值 `--conf 0.20` 可减少短暂漏检导致的 ID 重新分配。
- 快速大幅移动或严重遮挡时 IoU 匹配可能失效；如需更强的跟踪，可引入 DeepSORT / ByteTrack。

---

## 7  文件结构参考

```
Cs_Vision/
├── examples/
│   ├── pose_tracking_webcam.py   ← 主入口脚本
│   ├── requirements_pose.txt     ← Python 依赖
│   └── VERIFY.md                 ← 本文档
└── models/
    └── yolov8n-pose.onnx         ← 运行前需放置（或 --download 自动获取）
```
