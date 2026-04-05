# Human Pose Tracking Task — PR Report

## Summary

The human pose tracking task (Windows real-time webcam, ONNXRuntime, multi-person + stable track IDs) was implemented and submitted as a pull request to this repository.

## Pull Request

| Field | Details |
|---|---|
| **PR #** | [#4](https://github.com/lister2000/Cs_Vision/pull/4) |
| **Title** | Add Windows real-time multi-person pose tracking example via ONNXRuntime |
| **URL** | https://github.com/lister2000/Cs_Vision/pull/4 |
| **Status** | Open (draft) |
| **Created** | 2026-04-05 |
| **Branch** | `copilot/add-windows-multi-pose-detection-again` → `main` |

## What's Included

- **`examples/pose_tracking_webcam.py`** — entry point for real-time webcam inference
  - Supports YOLOv8/v11-Pose and MoveNet MultiPose ONNX backends (auto-detected from filename)
  - Greedy IoU-based `PoseTracker` for stable multi-person track IDs across frames
  - COCO-17 skeleton overlay, per-track colour-coded ID, live FPS display
  - Full CLI: `--model`, `--camera`, `--conf`, `--kp-conf`, `--max-age`, `--iou-thresh`, `--gpu`, `--no-bbox`
- **`docs/pose_tracking_webcam.md`** — Windows setup guide (deps, model export/download, parameters, troubleshooting)
- **`models/README.md`** — model download instructions
- **`.gitignore`** — `*.onnx`, venv, and Python cache exclusions

## Quick Start (from the PR)

```bat
cd examples
pip install onnxruntime opencv-python numpy
python -c "from ultralytics import YOLO; YOLO('yolov8n-pose.pt').export(format='onnx', opset=12)"
python pose_tracking_webcam.py --model models/yolov8n-pose.onnx --camera 0
```

Press **Q** to quit. Pass `--gpu` or `--providers CUDAExecutionProvider CPUExecutionProvider` for GPU acceleration.

## Related PRs

| PR | Title | Status |
|---|---|---|
| [#2](https://github.com/lister2000/Cs_Vision/pull/2) | feat: real-time human body keypoint/skeleton tracking example (Windows, MediaPipe) | Open (draft) |
| [#3](https://github.com/lister2000/Cs_Vision/pull/3) | Add Windows real-time multi-person pose tracking example via ONNXRuntime + YOLOv8n-pose | Open (draft) |
| [**#4**](https://github.com/lister2000/Cs_Vision/pull/4) | **Add Windows real-time multi-person pose tracking example via ONNXRuntime** ← *this task* | **Open (draft)** |
