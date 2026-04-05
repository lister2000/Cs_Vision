# Windows 实时多人姿态检测与跟踪

> **示例入口：** `examples/pose_tracking_webcam.py`  
> **依赖：** Python ≥ 3.8、OpenCV、ONNXRuntime、NumPy  
> **平台：** Windows 10/11（亦可在 Linux/macOS 运行，摄像头后端自动适配）

---

## 1. Windows 依赖安装

```powershell
# 1. 安装 Python（推荐 3.10/3.11，从 python.org 下载）

# 2. 安装运行时依赖
pip install opencv-python numpy onnxruntime

# 若有 NVIDIA GPU（CUDA 11.x / 12.x），可使用 GPU 版加速：
pip install onnxruntime-gpu
```

> **注意：** 使用 `onnxruntime-gpu` 时运行时须加 `--gpu` 参数（见第 4 节）。

---

## 2. 模型下载与放置

### 方案 A（推荐）— YOLOv8n-Pose ONNX

YOLOv8n-Pose 是单阶段多人姿态模型，支持最多 100 人，ONNX 导出简单快捷。

```powershell
pip install ultralytics

# 导出 ONNX（首次运行会自动下载 PT 权重，约 6 MB）
python -c "from ultralytics import YOLO; YOLO('yolov8n-pose.pt').export(format='onnx')"

# 将导出的文件移入 models\
move yolov8n-pose.onnx models\
```

生成文件：`models\yolov8n-pose.onnx`（约 7 MB）。

---

### 方案 B — MoveNet MultiPose Lightning ONNX

MoveNet MultiPose 单次推理最多检测 6 人，模型体积更小（~3 MB）。

> **前提：** 需要安装 `tensorflow`（或 `tflite-runtime`）及 `tf2onnx`。

```powershell
pip install tensorflow tf2onnx

# 1. 下载 TFLite 模型（通过 TF Hub 或 tf.saved_model）
python - << 'EOF'
import tensorflow as tf
import tensorflow_hub as hub

# 加载 SavedModel 格式
model = hub.load("https://tfhub.dev/google/movenet/multipose/lightning/1")
tf.saved_model.save(model, "movenet_multipose_saved")
EOF

# 2. 转换为 ONNX
python -m tf2onnx.convert \
    --saved-model movenet_multipose_saved \
    --output models/movenet_multipose.onnx \
    --opset 13

# （可选）验证
python -c "
import onnxruntime as ort
sess = ort.InferenceSession('models/movenet_multipose.onnx')
print('Input :', sess.get_inputs()[0].shape)
print('Output:', sess.get_outputs()[0].shape)
"
```

生成文件：`models\movenet_multipose.onnx`（约 3 MB）。

---

### 目录结构

运行前，`models\` 目录应包含至少一个模型文件：

```
Cs_Vision/
├── examples/
│   └── pose_tracking_webcam.py
├── models/
│   ├── README.md
│   ├── yolov8n-pose.onnx          ← 方案 A
│   └── movenet_multipose.onnx     ← 方案 B
└── docs/
    └── pose_tracking_webcam.md    ← 本文档
```

---

## 3. 运行命令

### 基础启动（YOLOv8n-Pose，默认摄像头）

```powershell
python examples\pose_tracking_webcam.py --model models\yolov8n-pose.onnx
```

### 使用 MoveNet MultiPose

```powershell
python examples\pose_tracking_webcam.py --model models\movenet_multipose.onnx
```

### 调整摄像头与分辨率

```powershell
python examples\pose_tracking_webcam.py \
    --model models\yolov8n-pose.onnx \
    --camera 1 \
    --width 1920 --height 1080
```

### 调整检测阈值

```powershell
python examples\pose_tracking_webcam.py \
    --model models\yolov8n-pose.onnx \
    --conf 0.45 \
    --kp-conf 0.35
```

### GPU 加速（需 onnxruntime-gpu + CUDA）

```powershell
python examples\pose_tracking_webcam.py \
    --model models\yolov8n-pose.onnx \
    --gpu
```

---

## 4. 参数说明

| 参数 | 默认值 | 说明 |
|------|--------|------|
| `--model` | *(必填)* | ONNX 模型文件路径 |
| `--model-type` | `auto` | 模型类型：`auto` / `yolo` / `movenet`（auto 从文件名推断） |
| `--camera` | `0` | 摄像头设备索引（0 = 默认摄像头） |
| `--width` | `1280` | 采集宽度（像素） |
| `--height` | `720` | 采集高度（像素） |
| `--conf` | `0.35` | 人体检测置信度阈值 |
| `--kp-conf` | `0.30` | 关键点绘制置信度阈值 |
| `--max-age` | `30` | 轨迹超时帧数（超过后删除） |
| `--iou-thresh` | `0.25` | 跟踪 IoU 匹配阈值 |
| `--no-bbox` | *(flag)* | 隐藏检测框，只显示骨架 |
| `--gpu` | *(flag)* | 启用 CUDA 推理（需 onnxruntime-gpu） |

---

## 5. 键盘快捷键

| 按键 | 功能 |
|------|------|
| `q` 或 `Esc` | 退出 |
| `b` | 切换检测框显示 |

---

## 6. 跟踪算法说明

示例实现了一个基于 **贪心 IoU 匹配** 的轻量多目标关联器（`PoseTracker`）：

1. 每帧计算现有轨迹与新检测框的成对 IoU 矩阵。  
2. 按 IoU 降序贪心分配（超过 `--iou-thresh` 阈值才匹配）。  
3. 未匹配的检测框生成新轨迹（分配递增 `track_id`）。  
4. 未匹配的轨迹 `age` 计数增加；超过 `--max-age` 帧后删除。

> 短时遮挡/交叉场景下，合理增大 `--iou-thresh`（如 0.35）或减小
> `--max-age`（如 15）可优化 ID 稳定性与内存占用的平衡。

---

## 7. 故障排查

### 找不到模型文件

```
ERROR: Model file not found: models\yolov8n-pose.onnx
```

确认 `models\` 目录中已有对应 `.onnx` 文件，并检查路径与当前工作目录。

### 无法打开摄像头

```
ERROR: Cannot open camera (index=0).
```

- 确认摄像头已连接并未被其他程序占用。  
- 尝试 `--camera 1` 或 `--camera 2` 切换设备索引。  
- 在设备管理器中确认摄像头驱动正常。

### onnxruntime 未安装

```
ERROR: onnxruntime is not installed.
```

```powershell
pip install onnxruntime        # CPU
pip install onnxruntime-gpu    # GPU（需 CUDA）
```

### 推理速度慢

- 使用 `--gpu` + `onnxruntime-gpu` 开启 GPU 加速。  
- 降低分辨率：`--width 640 --height 480`。  
- 换用更小的模型（YOLOv8n-pose 已是轻量级，可尝试更小的自定义导出）。
