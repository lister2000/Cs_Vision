# Cs_Vision

Computer-vision experiments and examples.

---

## PR \#3 — Windows Real-time Multi-Person Pose Tracking (ONNXRuntime + YOLOv8n-pose)

### Status

| Field | Value |
|-------|-------|
| PR | [#3 — Add Windows real-time multi-person pose tracking example via ONNXRuntime + YOLOv8n-pose](https://github.com/lister2000/Cs_Vision/pull/3) |
| State | **Open** (draft) |
| Branch | `copilot/add-windows-multi-pose-detection` |

### Check out the branch locally

```bat
git fetch origin copilot/add-windows-multi-pose-detection
git checkout copilot/add-windows-multi-pose-detection
```

Or, to try it without switching away from your current branch:

```bat
git fetch origin copilot/add-windows-multi-pose-detection:pose-tracking
git checkout pose-tracking
```

---

## Quick Validation (Windows)

### 1 — Entrypoint

```
examples/pose_tracking_webcam.py
```

### 2 — Install dependencies

```bat
cd examples
pip install -r requirements.txt
```

`requirements.txt` pins:

```
onnxruntime>=1.16.0
opencv-python>=4.8.0
numpy>=1.24.0
```

> **Optional GPU acceleration**: replace `onnxruntime` with `onnxruntime-gpu`:
> ```bat
> pip install onnxruntime-gpu opencv-python numpy
> ```

### 3 — Obtain the model

The ONNX model is **not** bundled with the repository (`examples/models/` is git-ignored).
Choose one of the two methods below.

#### Method A — Export with Ultralytics (recommended)

```bat
pip install ultralytics
python -c "from ultralytics import YOLO; YOLO('yolov8n-pose.pt').export(format='onnx', opset=12)"
```

Running this command downloads `yolov8n-pose.pt` (~6 MB) automatically and writes
`yolov8n-pose.onnx` (~13 MB) to the current directory.

Then move it to the expected location:

```bat
mkdir examples\models
move yolov8n-pose.onnx examples\models\yolov8n-pose.onnx
```

Expected directory layout after placement:

```
examples/
├── pose_tracking_webcam.py
├── requirements.txt
└── models/
    └── yolov8n-pose.onnx   ← place here
```

#### Method B — Direct download (pre-exported ONNX)

Download `yolov8n-pose.onnx` from:

- **Ultralytics PT weights** (then export once): <https://github.com/ultralytics/assets/releases/download/v8.2.0/yolov8n-pose.pt>
- **Community ONNX on Hugging Face**: search `yolov8n-pose onnx` at <https://huggingface.co/models> and save as `examples/models/yolov8n-pose.onnx`.

### 4 — Run

From inside the `examples/` directory:

```bat
python pose_tracking_webcam.py
```

With explicit parameters:

```bat
python pose_tracking_webcam.py ^
    --model   models/yolov8n-pose.onnx ^
    --camera  0 ^
    --conf    0.35 ^
    --width   1280 ^
    --height  720
```

For GPU inference (NVIDIA):

```bat
python pose_tracking_webcam.py ^
    --model     models/yolov8n-pose.onnx ^
    --camera    0 ^
    --providers CUDAExecutionProvider CPUExecutionProvider
```

Press **Q** to quit.

### 5 — Acceptance checklist

After the window opens, verify all five:

- [ ] Webcam feed is visible and updates continuously.
- [ ] Skeleton keypoints (dots) and limb connections (lines) are drawn over each detected person.
- [ ] Each person has a labelled **`ID:N`** box; the ID remains stable through short occlusions or movement.
- [ ] **FPS** counter in the top-left corner is updating.
- [ ] **Persons** count in the top-left corner reflects the number of people visible.

### 6 — Key CLI options

| Option | Default | Description |
|--------|---------|-------------|
| `--model` | `models/yolov8n-pose.onnx` | Path to the YOLOv8n-pose ONNX file |
| `--camera` | `0` | Camera device index (try `1`, `2` … if `0` fails) |
| `--conf` | `0.30` | Detection confidence threshold |
| `--iou` | `0.45` | NMS IoU threshold |
| `--dist-thresh` | `100.0` | Max pixel distance for cross-frame ID matching |
| `--max-age` | `30` | Frames a track may be missing before deletion |
| `--kp-thresh` | `0.30` | Minimum keypoint visibility to draw a joint |
| `--width` / `--height` | `0` | Camera capture resolution (`0` = camera default) |
| `--providers` | `CPUExecutionProvider` | ONNXRuntime providers |

### 7 — Troubleshooting

| Symptom | Fix |
|---------|-----|
| `Cannot open camera index 0` | Unplug/replug webcam; try `--camera 1`; close Teams/Zoom/OBS |
| `Model file not found` | The script prints the exact expected path — place the ONNX there |
| Low FPS on CPU | Lower resolution (`--width 640 --height 480`); use `onnxruntime-gpu` |
| Window opens but is black | Wrong camera index; try `--camera 1` or `--camera 2` |

---

Full bilingual documentation: [`docs/pose_tracking_webcam.md`](docs/pose_tracking_webcam.md)
