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

### 如何获取本示例代码（PR 未合并时）

> PR #3 当前处于 **Open（草稿）** 状态，尚未合并到 `main`。  
> 请按以下任一方式在本地获取代码：

#### 方式 1 — 检出 PR 分支（推荐）

```bat
# 先克隆仓库（如果还没克隆）
git clone https://github.com/lister2000/Cs_Vision.git
cd Cs_Vision

# 拉取并检出 PR #3 对应的分支
git fetch origin copilot/add-windows-pose-tracking-example
git checkout copilot/add-windows-pose-tracking-example
```

检出后你会在 `examples/` 目录中看到 `pose_tracking_webcam.py` 和 `requirements.txt`。

#### 方式 2 — 直接下载 ZIP

1. 打开 PR #3 页面：<https://github.com/lister2000/Cs_Vision/pull/3>
2. 点击 **"copilot/add-windows-pose-tracking-example"** 分支链接，进入分支页面。
3. 点击绿色 **Code** 按钮 → **Download ZIP**，解压即可。

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

### ✅ 验收清单（逐条打勾）

运行后对照以下清单，**全部满足即视为验收通过**：

- [ ] 程序启动后控制台打印 `[INFO] Loading model: ...`，无报错退出。
- [ ] 弹出摄像头预览窗口，窗口标题为 `Pose Tracking  [Q = quit]`。
- [ ] 画面左上角持续显示 **`FPS: xx.x`**（数字持续刷新，不冻结）。
- [ ] 画面左上角持续显示 **`Persons: N`**（N 随画面中人数变化）。
- [ ] 画面中有人时，**每个人身上有彩色骨架连线**（16 条肢体线段）。
- [ ] 画面中有人时，**关键点（圆点）**叠加在关节位置（鼻子、肩、肘、腕、髋、膝、踝等）。
- [ ] 每个人的边框左上角有 **`ID:N`** 标签（不同人标签数字不同）。
- [ ] 多人场景下，**不同人的骨架颜色不同**（每个 ID 对应唯一颜色）。
- [ ] 人在画面中移动时，**track_id 保持不变**（ID 数字不随位置变化而跳变）。
- [ ] 人短暂离开画面（约 1 秒 / 30 帧以内）再回来，**ID 通常可以恢复**（贪心匹配允许偶尔跳变）。
- [ ] 按 **Q** 键可以正常退出，窗口关闭，控制台打印 `[INFO] Done.`。

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

### 常见问题 / 故障排查

**Q: 找不到摄像头 / `Cannot open camera index 0`**  
A: 检查摄像头是否已连接，尝试 `--camera 1` 或更大索引；确认没有其他程序（如微信视频、Zoom）占用摄像头。

**Q: 推理很慢 / FPS 很低（<5）**  
A: CPU 模式下 yolov8n-pose 约 5–15 FPS（视硬件）。可降低采集分辨率（`--width 640 --height 480`），或使用 GPU（`onnxruntime-gpu` + `--providers CUDAExecutionProvider CPUExecutionProvider`）。

**Q: 模型文件找不到 / `ERROR: Model file not found`**  
A: 程序启动时会打印详细下载指引，按提示操作。常见原因：
- 模型放错目录——需放在 `examples/models/yolov8n-pose.onnx`（相对于 `examples/` 目录）。
- 文件名拼写不一致——确认文件名为 `yolov8n-pose.onnx`（区分大小写）。

**Q: `Failed to load ONNX model` / ONNXRuntime 报错**  
A: 默认使用 `CPUExecutionProvider`（纯 CPU，无需 CUDA）。如果你安装了 `onnxruntime-gpu` 但没有 NVIDIA GPU，则加 `--providers CPUExecutionProvider` 强制使用 CPU。  
如需 GPU：确认 `onnxruntime-gpu` 版本与 CUDA 版本匹配，参见 [ONNXRuntime 发行说明](https://onnxruntime.ai/docs/execution-providers/CUDA-ExecutionProvider.html)。

**Q: 画面正常但没有骨架显示（画面无叠加）**  
A: 可能置信度阈值过高，没有检测到人。尝试 `--conf 0.20` 降低阈值，或确保画面中有清晰可见的人体。

**Q: track_id 频繁跳变**  
A: 当多人交叉或快速移动时，贪心最近距离匹配可能会跳变。可以：
- 调大 `--dist-thresh`（例如 `150`）以允许更大的位移被匹配。
- 调大 `--max-age`（例如 `60`）以保留轨迹更长时间。

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

### How to get the code (PR not yet merged)

> PR #3 is currently **Open (draft)** and has not been merged into `main`.  
> Use either method below to run it locally:

#### Method 1 — Check out the PR branch (recommended)

```bat
REM Clone the repo first (if you haven't already)
git clone https://github.com/lister2000/Cs_Vision.git
cd Cs_Vision

REM Fetch and check out the PR branch
git fetch origin copilot/add-windows-pose-tracking-example
git checkout copilot/add-windows-pose-tracking-example
```

After checking out you will find `pose_tracking_webcam.py` and `requirements.txt` inside `examples/`.

#### Method 2 — Download ZIP

1. Open the PR page: <https://github.com/lister2000/Cs_Vision/pull/3>
2. Click the **"copilot/add-windows-pose-tracking-example"** branch link to go to the branch page.
3. Click the green **Code** button → **Download ZIP**, then extract.

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

### ✅ Verification checklist

Run the script and tick off each item — **all items must pass** for acceptance:

- [ ] Console prints `[INFO] Loading model: ...` on startup with no error exit.
- [ ] A webcam preview window opens, titled `Pose Tracking  [Q = quit]`.
- [ ] Top-left of window shows **`FPS: xx.x`** that continuously refreshes (not frozen).
- [ ] Top-left of window shows **`Persons: N`** that changes with the number of people on screen.
- [ ] When a person is visible, **coloured skeleton lines** (16 limb segments) are drawn over their body.
- [ ] **Keypoint dots** appear at joint positions (nose, shoulders, elbows, wrists, hips, knees, ankles).
- [ ] Each person's bounding box has an **`ID:N`** label in the top-left corner (different people have different numbers).
- [ ] In multi-person scenes, **each person has a distinct skeleton colour** (unique colour per ID).
- [ ] As a person moves around the frame, their **track_id stays the same** (number does not jump).
- [ ] After briefly leaving the frame (within ~1 second / 30 frames), the **same ID is typically restored** (occasional ID changes are normal for greedy matching).
- [ ] Pressing **Q** exits cleanly: window closes and console prints `[INFO] Done.`

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
A: Confirm the webcam is plugged in and not used by another app (e.g., Zoom, Teams). Try `--camera 1` or `--camera 2`.

**Q: Low FPS on CPU**  
A: On CPU, yolov8n-pose runs ~5–15 FPS depending on hardware. Lower the capture resolution (`--width 640 --height 480`) or use an NVIDIA GPU (`onnxruntime-gpu` + `--providers CUDAExecutionProvider CPUExecutionProvider`).

**Q: `ERROR: Model file not found`**  
A: The script prints complete download/export instructions on startup. Common causes:
- Wrong directory — model must be at `examples/models/yolov8n-pose.onnx` (relative to `examples/`).
- Filename mismatch — ensure the filename is exactly `yolov8n-pose.onnx` (case-sensitive).

**Q: `Failed to load ONNX model` / ONNXRuntime error**  
A: By default `CPUExecutionProvider` is used (no CUDA required). If you installed `onnxruntime-gpu` without a compatible NVIDIA GPU, force CPU mode: `--providers CPUExecutionProvider`.  
For GPU: ensure `onnxruntime-gpu` version matches your CUDA version — see the [ONNXRuntime CUDA release notes](https://onnxruntime.ai/docs/execution-providers/CUDA-ExecutionProvider.html).

**Q: Video is live but no skeleton overlay**  
A: The confidence threshold may be too high. Try `--conf 0.20` and ensure there is a clearly visible person in frame.

**Q: track_id keeps changing (ID instability)**  
A: Frequent ID changes occur when people cross or move quickly. Mitigations:
- Increase `--dist-thresh` (e.g., `150`) to allow larger displacements to be matched.
- Increase `--max-age` (e.g., `60`) to keep tracks alive longer.
