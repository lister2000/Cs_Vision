# Windows 实时多人姿态跟踪示例 / Windows Real-time Multi-Person Pose Tracking

[English](#english) | [中文](#中文)

---

## 中文

### 功能说明

`pose_tracking_webcam.py` 是一个基于 **ONNXRuntime** 推理的实时多人骨架检测与跟踪示例，运行于 Windows 摄像头输入（OpenCV）。

主要特性：
- **单模型多人**：使用 YOLOv8n-pose ONNX，一次推理同时输出所有人的边框与 17 个 COCO 关键点。
- **骨架可视化**：绘制关键点（圆点）与骨架连线，不同人用不同颜色区分。
- **多人 ID 稳定跟踪**：跨帧使用 **贪心最近距离匹配**（bbox 中心欧氏距离 + 距离阈值），失配时新建轨迹，超时自动删除。
- **实时 FPS 显示**：屏幕左上角显示当前帧率与在场人数。
- **友好错误提示**：运行时检测模型文件是否存在，不存在则给出完整的下载/导出指引。

---

### 环境要求

| 软件 | 版本要求 |
|------|---------|
| Python | 3.9 以上 |
| Windows | 10 / 11 (x64) |
| 摄像头 | USB 摄像头或内置摄像头（OpenCV 可识别） |

> **可选 GPU 加速**：如需使用 NVIDIA GPU，将 `onnxruntime` 替换为 `onnxruntime-gpu`，并在运行时加 `--providers CUDAExecutionProvider CPUExecutionProvider`。

---

### 安装依赖

```bat
cd examples
pip install -r requirements.txt
```

若需 GPU 推理：

```bat
pip install onnxruntime-gpu opencv-python numpy
```

---

### 下载并放置模型

#### 方式 A — 从 Ultralytics 导出（推荐）

```bat
pip install ultralytics
python -c "from ultralytics import YOLO; YOLO('yolov8n-pose.pt').export(format='onnx', opset=12)"
```

命令执行后会生成 `yolov8n-pose.onnx`，将其放到：

```
examples/
└── models/
    └── yolov8n-pose.onnx   ← 放在这里
```

#### 方式 B — 直接下载预导出 ONNX

在 GitHub / Hugging Face 搜索 `yolov8n-pose onnx` 下载，同样放到 `examples/models/yolov8n-pose.onnx`。

---

### 运行

在 `examples/` 目录下执行：

```bat
python pose_tracking_webcam.py
```

或指定参数：

```bat
python pose_tracking_webcam.py ^
    --model   models/yolov8n-pose.onnx ^
    --camera  0 ^
    --conf    0.35 ^
    --width   1280 ^
    --height  720
```

按 **Q** 退出。

---

### 参数说明

| 参数 | 默认值 | 说明 |
|------|--------|------|
| `--model` | `models/yolov8n-pose.onnx` | ONNX 模型文件路径 |
| `--camera` | `0` | 摄像头设备索引（0 = 第一个摄像头） |
| `--conf` | `0.30` | 检测置信度阈值（0–1） |
| `--iou` | `0.45` | NMS IoU 阈值（0–1） |
| `--dist-thresh` | `100.0` | 跨帧跟踪的最大中心距离（像素），超出则视为新目标 |
| `--max-age` | `30` | 轨迹最多可缺席帧数，超出则删除该轨迹 |
| `--kp-thresh` | `0.30` | 绘制关键点的最低可见度置信度 |
| `--width` | `0` | 摄像头采集宽度（0 = 摄像头默认） |
| `--height` | `0` | 摄像头采集高度（0 = 摄像头默认） |
| `--providers` | `CPUExecutionProvider` | ONNXRuntime 执行提供器，GPU 推理时设为 `CUDAExecutionProvider CPUExecutionProvider` |

---

### 目录结构

```
examples/
├── pose_tracking_webcam.py   # 主程序
├── requirements.txt          # Python 依赖
└── models/
    └── yolov8n-pose.onnx     # 需自行下载/导出，不随仓库分发
```

---

### 常见问题

**Q: 找不到摄像头 / `Cannot open camera index 0`**  
A: 检查摄像头是否已连接，尝试 `--camera 1` 或更大索引；确认没有其他程序占用。

**Q: 推理很慢 / FPS 很低**  
A: CPU 模式下 yolov8n-pose 约 5–15 FPS（视硬件）。可降低采集分辨率（`--width 640 --height 480`），或使用 GPU（`onnxruntime-gpu` + `--providers CUDAExecutionProvider CPUExecutionProvider`）。

**Q: 模型文件找不到**  
A: 程序启动时会打印详细下载指引，请按提示操作。

---

## English

### Overview

`pose_tracking_webcam.py` is a real-time **multi-person skeleton detection and tracking** demo that runs on Windows using a webcam.

Key features:
- **Single-model multi-person**: YOLOv8n-pose ONNX — one inference call produces bounding boxes and 17 COCO keypoints for all visible people simultaneously.
- **Skeleton visualisation**: keypoint dots + limb connections drawn per person in a unique colour.
- **Stable multi-person ID tracking**: greedy nearest-neighbour matching on bbox centre (Euclidean distance + threshold); new tracks spawned for unmatched detections; stale tracks pruned after `--max-age` frames.
- **Live FPS overlay**: current frame rate and active person count shown in the top-left corner.
- **Friendly error messages**: checks model file existence on startup and prints full download/export instructions if missing.

---

### Requirements

| Software | Version |
|----------|---------|
| Python | 3.9+ |
| Windows | 10 / 11 (x64) |
| Webcam | USB or built-in camera recognised by OpenCV |

> **Optional GPU acceleration**: replace `onnxruntime` with `onnxruntime-gpu` and pass `--providers CUDAExecutionProvider CPUExecutionProvider` at runtime.

---

### Install dependencies

```bat
cd examples
pip install -r requirements.txt
```

For GPU inference:

```bat
pip install onnxruntime-gpu opencv-python numpy
```

---

### Download and place the model

#### Option A — export from Ultralytics (recommended)

```bat
pip install ultralytics
python -c "from ultralytics import YOLO; YOLO('yolov8n-pose.pt').export(format='onnx', opset=12)"
```

Move the generated `yolov8n-pose.onnx` to:

```
examples/
└── models/
    └── yolov8n-pose.onnx   ← place here
```

#### Option B — download a pre-exported ONNX

Search for `yolov8n-pose onnx` on GitHub / Hugging Face, download, and place it at `examples/models/yolov8n-pose.onnx`.

---

### Run

From inside the `examples/` directory:

```bat
python pose_tracking_webcam.py
```

With custom parameters:

```bat
python pose_tracking_webcam.py ^
    --model   models/yolov8n-pose.onnx ^
    --camera  0 ^
    --conf    0.35 ^
    --width   1280 ^
    --height  720
```

Press **Q** to quit.

---

### Command-line reference

| Argument | Default | Description |
|----------|---------|-------------|
| `--model` | `models/yolov8n-pose.onnx` | Path to the YOLOv8n-pose ONNX model |
| `--camera` | `0` | Camera device index (0 = first webcam) |
| `--conf` | `0.30` | Detection confidence threshold (0–1) |
| `--iou` | `0.45` | NMS IoU threshold (0–1) |
| `--dist-thresh` | `100.0` | Max centre-to-centre pixel distance for cross-frame matching |
| `--max-age` | `30` | Max consecutive missing frames before a track is deleted |
| `--kp-thresh` | `0.30` | Minimum keypoint visibility to draw a joint |
| `--width` | `0` | Camera capture width in pixels (0 = camera default) |
| `--height` | `0` | Camera capture height in pixels (0 = camera default) |
| `--providers` | `CPUExecutionProvider` | ONNXRuntime providers; use `CUDAExecutionProvider CPUExecutionProvider` for GPU |

---

### Directory layout

```
examples/
├── pose_tracking_webcam.py   # main script
├── requirements.txt          # Python dependencies
└── models/
    └── yolov8n-pose.onnx     # download/export yourself — not shipped with the repo
```

---

### Troubleshooting

**Q: `Cannot open camera index 0`**  
A: Confirm the webcam is plugged in and not used by another app. Try `--camera 1`.

**Q: Low FPS on CPU**  
A: On CPU, yolov8n-pose runs ~5–15 FPS depending on hardware. Lower the capture resolution (`--width 640 --height 480`) or use an NVIDIA GPU (`onnxruntime-gpu` + `--providers CUDAExecutionProvider CPUExecutionProvider`).

**Q: Model file not found**  
A: The script prints complete download/export instructions on startup. Follow them to obtain `yolov8n-pose.onnx`.
