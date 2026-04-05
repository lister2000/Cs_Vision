#!/usr/bin/env python3
"""
Real-time multi-person pose detection and tracking on Windows webcam.

Supported ONNX model backends
-------------------------------
* **YOLOv8 / YOLOv11-Pose**  (recommended – ONNX export is one command)
  Input  : [1, 3, H, W]  float32  values 0-1
  Output : [1, 56, anchors]  (cx, cy, w, h, conf, 17×(x, y, kp_score))

* **MoveNet MultiPose Lightning**  (single-stage, up to 6 persons)
  Input  : [1, 256, 256, 3]  float32  values 0-255
  Output : [1, 6, 56]  (ymin, xmin, ymax, xmax, score, 17×(y, x, kp_score))

The model type is auto-detected from the filename; override with --model-type.

Usage examples
--------------
  python pose_tracking_webcam.py --model models/yolov8n-pose.onnx
  python pose_tracking_webcam.py --model models/movenet_multipose.onnx --camera 0
  python pose_tracking_webcam.py --model models/yolov8n-pose.onnx \\
      --conf 0.4 --kp-conf 0.35 --max-age 45 --gpu

Press  q / Esc  to quit,  b  to toggle bounding-box overlay.
"""

import argparse
import sys
import time
from pathlib import Path
from typing import List, Tuple

import numpy as np
import cv2

# ---------------------------------------------------------------------------
# COCO 17-keypoint skeleton definition
# ---------------------------------------------------------------------------
NUM_KP: int = 17

# (start_index, end_index) pairs for skeleton lines
SKELETON: List[Tuple[int, int]] = [
    (0, 1), (0, 2), (1, 3), (2, 4),          # head / face
    (5, 6),                                    # shoulders
    (5, 7), (7, 9),                            # left arm
    (6, 8), (8, 10),                           # right arm
    (5, 11), (6, 12),                          # torso sides
    (11, 12),                                  # hips
    (11, 13), (13, 15),                        # left leg
    (12, 14), (14, 16),                        # right leg
]

# 12 visually distinct BGR colours for track IDs
_TRACK_PALETTE: List[Tuple[int, int, int]] = [
    (80,  80, 255), (80, 255,  80), (255,  80,  80),
    (80, 255, 255), (255, 255,  80), (255,  80, 255),
    (80, 160, 255), (160, 255,  80), (255, 160,  80),
    (160,  80, 255), (80, 255, 160), (255,  80, 160),
]


def _track_color(track_id: int) -> Tuple[int, int, int]:
    return _TRACK_PALETTE[(track_id - 1) % len(_TRACK_PALETTE)]


# ---------------------------------------------------------------------------
# IoU helper
# ---------------------------------------------------------------------------
def _iou(b1: np.ndarray, b2: np.ndarray) -> float:
    """2-D IoU for axis-aligned boxes given as [x1, y1, x2, y2]."""
    ix1 = max(b1[0], b2[0])
    iy1 = max(b1[1], b2[1])
    ix2 = min(b1[2], b2[2])
    iy2 = min(b1[3], b2[3])
    inter = max(0.0, ix2 - ix1) * max(0.0, iy2 - iy1)
    if inter == 0.0:
        return 0.0
    a1 = max(1e-9, (b1[2] - b1[0]) * (b1[3] - b1[1]))
    a2 = max(1e-9, (b2[2] - b2[0]) * (b2[3] - b2[1]))
    return float(inter / (a1 + a2 - inter))


# ---------------------------------------------------------------------------
# Greedy IoU multi-object tracker
# ---------------------------------------------------------------------------
class _Track:
    __slots__ = ("id", "bbox", "kps", "age", "hits")

    def __init__(self, tid: int, bbox: np.ndarray, kps: np.ndarray) -> None:
        self.id: int = tid
        self.bbox: np.ndarray = bbox   # [x1, y1, x2, y2] pixel coords
        self.kps: np.ndarray = kps     # [NUM_KP, 3]  (x, y, score)
        self.age: int = 0              # frames since last successful match
        self.hits: int = 1             # total matched frames


class PoseTracker:
    """
    Greedy highest-IoU multi-person pose tracker.

    Each frame the tracker:
    1. Builds a pairwise IoU matrix between existing tracks and new detections.
    2. Greedily assigns pairs in descending IoU order (above *iou_thresh*).
    3. Creates new tracks for unmatched detections.
    4. Increments ``age`` for unmatched tracks; removes those older than
       *max_age* frames.
    """

    def __init__(self, max_age: int = 30, iou_thresh: float = 0.25) -> None:
        self.max_age = max_age
        self.iou_thresh = iou_thresh
        self._tracks: List[_Track] = []
        self._next_id: int = 1

    def update(
        self,
        bboxes: np.ndarray,    # (N, 4)       [x1, y1, x2, y2]
        kps_list: np.ndarray,  # (N, NUM_KP, 3)
    ) -> List[Tuple[int, np.ndarray, np.ndarray]]:
        """
        Associate detections to tracks and return active results.

        Returns
        -------
        list of (track_id, bbox[4], kps[NUM_KP, 3]) for every detection
        that was matched to an existing track or is a new track this frame.
        """
        N = int(len(bboxes))
        matched_t: set = set()
        matched_d: set = set()

        if self._tracks and N > 0:
            T = len(self._tracks)
            iou_mat = np.zeros((T, N), dtype=np.float32)
            for ti, tr in enumerate(self._tracks):
                for di in range(N):
                    iou_mat[ti, di] = _iou(tr.bbox, bboxes[di])

            # Sort all (track, detection) pairs by descending IoU
            flat_order = np.argsort(-iou_mat, axis=None)
            rows, cols = np.unravel_index(flat_order, iou_mat.shape)
            for ti, di in zip(rows.tolist(), cols.tolist()):
                if iou_mat[ti, di] < self.iou_thresh:
                    break
                if ti in matched_t or di in matched_d:
                    continue
                self._tracks[ti].bbox = bboxes[di].copy()
                self._tracks[ti].kps = kps_list[di].copy()
                self._tracks[ti].age = 0
                self._tracks[ti].hits += 1
                matched_t.add(ti)
                matched_d.add(di)

        # Age unmatched tracks
        for ti, tr in enumerate(self._tracks):
            if ti not in matched_t:
                tr.age += 1

        # Spawn new tracks for unmatched detections
        for di in range(N):
            if di not in matched_d:
                self._tracks.append(
                    _Track(self._next_id, bboxes[di].copy(), kps_list[di].copy())
                )
                self._next_id += 1

        # Prune stale tracks
        self._tracks = [t for t in self._tracks if t.age <= self.max_age]

        # Return only tracks active this frame (age == 0)
        return [
            (t.id, t.bbox.copy(), t.kps.copy())
            for t in self._tracks
            if t.age == 0
        ]


# ---------------------------------------------------------------------------
# Model backend: MoveNet MultiPose Lightning
# ---------------------------------------------------------------------------
class _MoveNetMultiPose:
    """
    MoveNet MultiPose Lightning ONNX.

    Input  shape : [1, H, W, 3]  float32  values in [0, 255]
                   (H = W = 256 for the Lightning variant)
    Output shape : [1, 6, 56]   float32
                   Per person: [ymin, xmin, ymax, xmax, score,
                                kp0_y, kp0_x, kp0_s, …×17]
                   Coordinates are normalised to [0, 1].
    """

    def __init__(self, model_path: str, providers: List[str]) -> None:
        import onnxruntime as ort

        self._sess = ort.InferenceSession(model_path, providers=providers)
        inp = self._sess.get_inputs()[0]
        self._iname: str = inp.name
        shape = inp.shape
        # Derive input spatial size from the model graph (default 256)
        self._size: int = int(shape[1]) if isinstance(shape[1], int) and shape[1] > 0 else 256

    def infer(
        self,
        frame_bgr: np.ndarray,
        conf_thresh: float,
        kp_thresh: float,
    ) -> Tuple[np.ndarray, np.ndarray]:
        h, w = frame_bgr.shape[:2]
        s = self._size
        rgb = cv2.cvtColor(frame_bgr, cv2.COLOR_BGR2RGB)
        resized = cv2.resize(rgb, (s, s)).astype(np.float32)[np.newaxis]  # [1,s,s,3]

        out = self._sess.run(None, {self._iname: resized})[0][0]  # [6, 56]

        bboxes, kps_all = [], []
        for p in out:
            if float(p[4]) < conf_thresh:
                continue
            ymin, xmin, ymax, xmax = float(p[0]), float(p[1]), float(p[2]), float(p[3])
            bbox = np.array([xmin * w, ymin * h, xmax * w, ymax * h], dtype=np.float32)
            # keypoints: [kp_y, kp_x, kp_score] × 17  (normalised)
            kp_raw = p[5:].reshape(NUM_KP, 3)
            kps = np.stack(
                [kp_raw[:, 1] * w, kp_raw[:, 0] * h, kp_raw[:, 2]], axis=1
            ).astype(np.float32)
            bboxes.append(bbox)
            kps_all.append(kps)

        if not bboxes:
            return np.zeros((0, 4), np.float32), np.zeros((0, NUM_KP, 3), np.float32)
        return np.array(bboxes), np.array(kps_all)


# ---------------------------------------------------------------------------
# Model backend: YOLOv8 / YOLOv11-Pose
# ---------------------------------------------------------------------------
class _YOLOPose:
    """
    YOLOv8 / YOLOv11-Pose ONNX backend.

    Input  shape : [1, 3, H, W]  float32  values in [0, 1]  (default H=W=640)
    Output shape : [1, 56, anchors]
                   Transposed to [anchors, 56]:
                     [:, 0:4]  cx, cy, w, h  (model-input pixel space)
                     [:, 4]    confidence
                     [:, 5:]   17 × (x, y, score)  (model-input pixel space)
    """

    def __init__(self, model_path: str, providers: List[str]) -> None:
        import onnxruntime as ort

        self._sess = ort.InferenceSession(model_path, providers=providers)
        inp = self._sess.get_inputs()[0]
        self._iname: str = inp.name
        shape = inp.shape
        self._size: int = int(shape[2]) if isinstance(shape[2], int) and shape[2] > 0 else 640

    def infer(
        self,
        frame_bgr: np.ndarray,
        conf_thresh: float,
        kp_thresh: float,
    ) -> Tuple[np.ndarray, np.ndarray]:
        h, w = frame_bgr.shape[:2]
        s = self._size
        rgb = cv2.cvtColor(frame_bgr, cv2.COLOR_BGR2RGB)
        blob = (
            cv2.resize(rgb, (s, s))
            .astype(np.float32)
            .transpose(2, 0, 1)[np.newaxis] / 255.0
        )  # [1, 3, s, s]

        raw = self._sess.run(None, {self._iname: blob})[0][0].T  # [anchors, 56]

        # Filter by object confidence
        mask = raw[:, 4] > conf_thresh
        raw = raw[mask]
        if len(raw) == 0:
            return np.zeros((0, 4), np.float32), np.zeros((0, NUM_KP, 3), np.float32)

        # Convert cx,cy,w,h → x1,y1,x2,y2 in original image space
        scale_x, scale_y = w / s, h / s
        cx, cy, bw, bh = raw[:, 0], raw[:, 1], raw[:, 2], raw[:, 3]
        x1 = (cx - bw / 2) * scale_x
        y1 = (cy - bh / 2) * scale_y
        x2 = (cx + bw / 2) * scale_x
        y2 = (cy + bh / 2) * scale_y

        # NMS
        boxes_xywh = np.stack([x1, y1, x2 - x1, y2 - y1], axis=1).tolist()
        scores = raw[:, 4].tolist()
        indices = cv2.dnn.NMSBoxes(
            boxes_xywh, scores,
            score_threshold=float(conf_thresh),
            nms_threshold=0.45,
        )
        if len(indices) == 0:
            return np.zeros((0, 4), np.float32), np.zeros((0, NUM_KP, 3), np.float32)

        indices = np.asarray(indices).flatten()
        bboxes, kps_all = [], []
        for i in indices:
            bbox = np.array([x1[i], y1[i], x2[i], y2[i]], dtype=np.float32)
            kp_raw = raw[i, 5:].reshape(NUM_KP, 3)
            kps = np.stack(
                [kp_raw[:, 0] * scale_x, kp_raw[:, 1] * scale_y, kp_raw[:, 2]], axis=1
            ).astype(np.float32)
            bboxes.append(bbox)
            kps_all.append(kps)

        return np.array(bboxes), np.array(kps_all)


# ---------------------------------------------------------------------------
# Model factory
# ---------------------------------------------------------------------------
def _load_model(model_path: str, providers: List[str], model_type: str):
    """Load the appropriate ONNX backend; auto-detect from filename."""
    name = Path(model_path).name.lower()
    if model_type == "auto":
        model_type = "movenet" if "movenet" in name else "yolo"

    if model_type == "movenet":
        print(f"[INFO] Backend: MoveNet MultiPose  |  {model_path}")
        return _MoveNetMultiPose(model_path, providers)
    else:
        print(f"[INFO] Backend: YOLOv8/v11-Pose    |  {model_path}")
        return _YOLOPose(model_path, providers)


# ---------------------------------------------------------------------------
# Drawing helpers
# ---------------------------------------------------------------------------
def _draw_results(
    frame: np.ndarray,
    results: List[Tuple[int, np.ndarray, np.ndarray]],
    kp_thresh: float,
    show_bbox: bool,
) -> None:
    """Overlay skeletons, keypoints and track-ID labels onto *frame* in place."""
    for track_id, bbox, kps in results:
        color = _track_color(track_id)

        if show_bbox:
            x1, y1, x2, y2 = bbox.astype(int)
            cv2.rectangle(frame, (x1, y1), (x2, y2), color, 2, cv2.LINE_AA)

        # Track-ID label with filled background
        lx, ly = int(bbox[0]), int(bbox[1])
        label = f"#{track_id}"
        (tw, th), baseline = cv2.getTextSize(label, cv2.FONT_HERSHEY_SIMPLEX, 0.65, 2)
        cv2.rectangle(
            frame, (lx, ly - th - baseline - 6), (lx + tw + 4, ly), color, -1
        )
        cv2.putText(
            frame, label, (lx + 2, ly - baseline - 2),
            cv2.FONT_HERSHEY_SIMPLEX, 0.65, (255, 255, 255), 2, cv2.LINE_AA,
        )

        # Skeleton lines
        for a, b in SKELETON:
            if kps[a, 2] > kp_thresh and kps[b, 2] > kp_thresh:
                pa = (int(kps[a, 0]), int(kps[a, 1]))
                pb = (int(kps[b, 0]), int(kps[b, 1]))
                cv2.line(frame, pa, pb, color, 2, cv2.LINE_AA)

        # Keypoint circles
        for ki in range(NUM_KP):
            if kps[ki, 2] > kp_thresh:
                cx_kp, cy_kp = int(kps[ki, 0]), int(kps[ki, 1])
                cv2.circle(frame, (cx_kp, cy_kp), 4, color, -1, cv2.LINE_AA)
                cv2.circle(frame, (cx_kp, cy_kp), 4, (255, 255, 255), 1, cv2.LINE_AA)


def _draw_fps(frame: np.ndarray, fps: float) -> None:
    label = f"FPS: {fps:.1f}"
    cv2.putText(
        frame, label, (10, 32),
        cv2.FONT_HERSHEY_SIMPLEX, 0.9, (0, 0, 0), 3, cv2.LINE_AA,
    )
    cv2.putText(
        frame, label, (10, 32),
        cv2.FONT_HERSHEY_SIMPLEX, 0.9, (0, 255, 0), 2, cv2.LINE_AA,
    )


# ---------------------------------------------------------------------------
# CLI
# ---------------------------------------------------------------------------
def _parse_args() -> argparse.Namespace:
    p = argparse.ArgumentParser(
        description="Real-time multi-person pose tracking – ONNXRuntime",
        formatter_class=argparse.ArgumentDefaultsHelpFormatter,
    )
    p.add_argument(
        "--model", required=True,
        help="Path to the ONNX model file (YOLOv8-Pose or MoveNet MultiPose)",
    )
    p.add_argument(
        "--model-type", choices=["auto", "yolo", "movenet"], default="auto",
        help="Model type; 'auto' detects from filename",
    )
    p.add_argument(
        "--camera", type=int, default=0,
        help="Camera device index (0 = default webcam)",
    )
    p.add_argument("--width",  type=int, default=1280, help="Capture width in pixels")
    p.add_argument("--height", type=int, default=720,  help="Capture height in pixels")
    p.add_argument(
        "--conf", type=float, default=0.35,
        help="Minimum person detection confidence threshold",
    )
    p.add_argument(
        "--kp-conf", type=float, default=0.30,
        help="Minimum keypoint confidence threshold (for drawing)",
    )
    p.add_argument(
        "--max-age", type=int, default=30,
        help="Frames a track survives without a matched detection",
    )
    p.add_argument(
        "--iou-thresh", type=float, default=0.25,
        help="IoU threshold for tracker association",
    )
    p.add_argument(
        "--no-bbox", action="store_true",
        help="Hide bounding-box rectangles",
    )
    p.add_argument(
        "--gpu", action="store_true",
        help="Use CUDAExecutionProvider (requires onnxruntime-gpu)",
    )
    return p.parse_args()


# ---------------------------------------------------------------------------
# Entry point
# ---------------------------------------------------------------------------
def main() -> None:
    args = _parse_args()

    # ---- dependency check ----
    try:
        import onnxruntime  # noqa: F401
    except ImportError:
        print(
            "ERROR: onnxruntime is not installed.\n"
            "  CPU:  pip install onnxruntime\n"
            "  GPU:  pip install onnxruntime-gpu"
        )
        sys.exit(1)

    # ---- model file check ----
    model_path = Path(args.model)
    if not model_path.is_file():
        print(
            f"ERROR: Model file not found: {model_path.resolve()}\n"
            "\nHow to obtain a model:\n"
            "  Option A – YOLOv8n-Pose (recommended, ~7 MB):\n"
            "    pip install ultralytics\n"
            "    python -c \"from ultralytics import YOLO; "
            "YOLO('yolov8n-pose.pt').export(format='onnx')\"\n"
            "    Copy yolov8n-pose.onnx → models/\n\n"
            "  Option B – MoveNet MultiPose Lightning (~3 MB):\n"
            "    See docs/pose_tracking_webcam.md for conversion steps.\n\n"
            "  Then run:\n"
            "    python examples/pose_tracking_webcam.py "
            "--model models/yolov8n-pose.onnx"
        )
        sys.exit(1)

    # ---- ONNXRuntime providers ----
    providers: List[str] = (
        ["CUDAExecutionProvider", "CPUExecutionProvider"]
        if args.gpu
        else ["CPUExecutionProvider"]
    )

    # ---- load model ----
    try:
        model = _load_model(str(model_path), providers, args.model_type)
    except Exception as exc:
        print(f"ERROR: Failed to load model '{model_path}': {exc}")
        sys.exit(1)

    # ---- open webcam ----
    # CAP_DSHOW is the preferred Windows backend for lower latency
    cap = cv2.VideoCapture(args.camera, cv2.CAP_DSHOW)
    if not cap.isOpened():
        cap = cv2.VideoCapture(args.camera)   # fallback without backend hint
    if not cap.isOpened():
        print(
            f"ERROR: Cannot open camera (index={args.camera}).\n"
            "  Make sure a webcam is connected.\n"
            "  Try --camera 1 or --camera 2 for additional cameras."
        )
        sys.exit(1)

    cap.set(cv2.CAP_PROP_FRAME_WIDTH,  args.width)
    cap.set(cv2.CAP_PROP_FRAME_HEIGHT, args.height)
    cap.set(cv2.CAP_PROP_BUFFERSIZE, 1)

    tracker = PoseTracker(max_age=args.max_age, iou_thresh=args.iou_thresh)

    print(
        "[INFO] Press  q / Esc  to quit |  b  to toggle bounding boxes\n"
        f"       Camera: {args.camera}  |  "
        f"Resolution: {args.width}×{args.height}  |  "
        f"Model: {model_path.name}"
    )

    show_bbox: bool = not args.no_bbox
    fps: float = 0.0
    t_start: float = time.perf_counter()
    frame_count: int = 0

    while True:
        ret, frame = cap.read()
        if not ret:
            print("WARNING: Failed to read frame from camera – exiting.")
            break

        # ---- inference ----
        bboxes, kps_list = model.infer(frame, args.conf, args.kp_conf)

        # ---- tracking ----
        if len(bboxes) > 0:
            results = tracker.update(bboxes, kps_list)
        else:
            results = tracker.update(
                np.zeros((0, 4), np.float32),
                np.zeros((0, NUM_KP, 3), np.float32),
            )

        # ---- visualise ----
        _draw_results(frame, results, kp_thresh=args.kp_conf, show_bbox=show_bbox)
        _draw_fps(frame, fps)

        # ---- display ----
        cv2.imshow("Pose Tracking  (q / Esc = quit)", frame)
        key = cv2.waitKey(1) & 0xFF
        if key in (ord("q"), 27):
            break
        if key == ord("b"):
            show_bbox = not show_bbox

        # ---- rolling FPS average (every 10 frames) ----
        frame_count += 1
        if frame_count >= 10:
            t_now = time.perf_counter()
            fps = frame_count / (t_now - t_start)
            t_start = t_now
            frame_count = 0

    cap.release()
    cv2.destroyAllWindows()


if __name__ == "__main__":
    main()
