# Cs_Vision

A C# WinForms computer-vision toolkit with add-on Python examples for rapid prototyping.

---

## Python Example: Real-time Pose Tracking (Windows webcam)

> **Entry point:** `examples/pose_tracking_webcam.py`

This standalone script runs **multi-person skeleton detection + persistent track IDs** live
from a webcam on Windows, using [ONNXRuntime](https://onnxruntime.ai/) (CPU or GPU) and a
YOLOv8-pose ONNX model.

### Prerequisites

| Requirement | Version tested |
|---|---|
| Python | ≥ 3.9 |
| opencv-python | ≥ 4.8 |
| onnxruntime | ≥ 1.17 (CPU) |
| numpy | ≥ 1.24 |

Install dependencies:

```bash
pip install opencv-python onnxruntime numpy
```

> **GPU acceleration** (optional): replace `onnxruntime` with `onnxruntime-gpu`
> and ensure your CUDA / DirectML stack is set up.

### Download the model

The example expects a **YOLOv8-pose ONNX** file.  The lightweight `yolov8n-pose` variant
works well on CPU:

```bash
pip install ultralytics
python -c "from ultralytics import YOLO; YOLO('yolov8n-pose.pt').export(format='onnx')"
```

Place the exported file in the `models/` directory at the repository root:

```
Cs_Vision/
├── examples/
│   └── pose_tracking_webcam.py   ← entry point
├── models/
│   └── yolov8n-pose.onnx         ← model goes here
└── Cs_Vision.sln
```

### Run

```bash
# Default: webcam 0, model at models/yolov8n-pose.onnx
python examples/pose_tracking_webcam.py

# Specify a different camera index or model path
python examples/pose_tracking_webcam.py --device 1 --model models/yolov8n-pose.onnx

# All options
python examples/pose_tracking_webcam.py --help
```

| Flag | Default | Description |
|---|---|---|
| `--model` | `models/yolov8n-pose.onnx` | Path to the ONNX model file |
| `--device` | `0` | Webcam device index |
| `--input-size` | `640` | Model input resolution (square) |
| `--conf` | `0.35` | Confidence threshold |
| `--iou` | `0.45` | NMS IoU threshold |

Press **`q`** in the window to quit.

### What you should see

- Live webcam feed with **coloured skeleton keypoints + limb lines** overlaid on each person.
- Each person has an **`ID N`** label that stays stable as they move or temporarily leave the frame.
- **FPS counter** in the top-left corner.

### Verification checklist

- [ ] Window opens without errors.
- [ ] Skeleton keypoints and limb lines are drawn on detected people.
- [ ] `ID N` labels persist across frames (stable tracking).
- [ ] FPS counter updates continuously.
- [ ] Pressing **`q`** closes the window cleanly.

### Troubleshooting

| Symptom | Fix |
|---|---|
| `FileNotFoundError: Model not found` | Download the ONNX model and place it at the path shown in the error. |
| Black screen / camera won't open | Try `--device 1` or `--device 2`; check that no other app is using the camera. |
| Very low FPS | Lower `--input-size` (e.g. `320`) or reduce the preview window size. |
| `onnxruntime` not found | Run `pip install onnxruntime`. |
