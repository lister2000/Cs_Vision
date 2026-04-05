# models/

Place your ONNX model files in this directory before running the example.

## Recommended models

### Option A – YOLOv8n-Pose (easiest, ~7 MB, recommended)

```bash
pip install ultralytics
python -c "from ultralytics import YOLO; YOLO('yolov8n-pose.pt').export(format='onnx')"
# move the generated file here
move yolov8n-pose.onnx models\
```

### Option B – MoveNet MultiPose Lightning (~3 MB)

See [docs/pose_tracking_webcam.md](../docs/pose_tracking_webcam.md) for
step-by-step TFLite → ONNX conversion instructions.

## Directory layout after setup

```
models/
  yolov8n-pose.onnx         ← YOLOv8n-Pose (Option A)
  movenet_multipose.onnx    ← MoveNet MultiPose (Option B)
```

> **Note:** `.onnx` files are excluded from version control via `.gitignore`.
