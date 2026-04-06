#!/usr/bin/env python3
"""
Real-time multi-person pose tracking via webcam (Windows).

Uses the YOLOv8n-pose ONNX model with ONNXRuntime for inference, and a greedy
center-distance matching algorithm to maintain stable track IDs across frames.

Quick start:
    pip install -r requirements.txt
    # export / download model first (see docs/pose_tracking_webcam.md)
    python pose_tracking_webcam.py --model models/yolov8n-pose.onnx

Press Q to quit.
"""

import argparse
import os
import sys
import time
from collections import deque

import cv2
import numpy as np

try:
    import onnxruntime as ort
except ImportError:
    print("ERROR: onnxruntime is not installed.")
    print("       Run:  pip install onnxruntime")
    sys.exit(1)

# ---------------------------------------------------------------------------
# COCO-17 keypoint definitions
# ---------------------------------------------------------------------------
KEYPOINT_NAMES = [
    "nose",           # 0
    "left_eye",       # 1
    "right_eye",      # 2
    "left_ear",       # 3
    "right_ear",      # 4
    "left_shoulder",  # 5
    "right_shoulder", # 6
    "left_elbow",     # 7
    "right_elbow",    # 8
    "left_wrist",     # 9
    "right_wrist",    # 10
    "left_hip",       # 11
    "right_hip",      # 12
    "left_knee",      # 13
    "right_knee",     # 14
    "left_ankle",     # 15
    "right_ankle",    # 16
]

SKELETON = [
    (0, 1), (0, 2),              # nose → eyes
    (1, 3), (2, 4),              # eyes → ears
    (5, 6),                      # shoulders
    (5, 7), (7, 9),              # left arm
    (6, 8), (8, 10),             # right arm
    (5, 11), (6, 12),            # torso sides
    (11, 12),                    # hips
    (11, 13), (13, 15),          # left leg
    (12, 14), (14, 16),          # right leg
]

# 17 distinct BGR colours, cycling by track_id
_PALETTE = [
    (255, 56,  56),  (255, 157, 151), (255, 112, 31),  (255, 178, 29),
    (207, 210,  49), (72,  249, 10),  (146, 204, 23),  (61,  219, 134),
    (26,  147, 52),  (0,   212, 187), (44,  153, 168), (0,   194, 255),
    (52,  69,  147), (100, 115, 255), (0,   24,  236), (132, 56,  255),
    (82,  0,   133),
]

# YOLOv8n-pose expects a square input of this size (pixels).
INPUT_SIZE = 640

# Minimum elapsed time (seconds) used to guard against division-by-zero
# when computing instantaneous FPS (e.g. first frame or very fast hardware).
_MIN_FRAME_TIME = 1e-9


# ---------------------------------------------------------------------------
# Pre/post-processing helpers
# ---------------------------------------------------------------------------

def letterbox(frame: np.ndarray, size: int = INPUT_SIZE):
    """
    Resize frame with letterboxing to (size x size), keeping aspect ratio.

    Returns:
        canvas   : (size, size, 3) uint8
        scale    : float  — common scale factor applied to both axes
        pad_left : int    — horizontal padding in pixels
        pad_top  : int    — vertical padding in pixels
    """
    h, w = frame.shape[:2]
    scale = min(size / h, size / w)
    new_h, new_w = int(round(h * scale)), int(round(w * scale))
    resized = cv2.resize(frame, (new_w, new_h), interpolation=cv2.INTER_LINEAR)
    canvas = np.full((size, size, 3), 114, dtype=np.uint8)
    pad_top = (size - new_h) // 2
    pad_left = (size - new_w) // 2
    canvas[pad_top:pad_top + new_h, pad_left:pad_left + new_w] = resized
    return canvas, scale, pad_left, pad_top


def preprocess(frame: np.ndarray):
    """Convert BGR frame to model input tensor and return letterbox metadata."""
    canvas, scale, pad_left, pad_top = letterbox(frame)
    inp = canvas.astype(np.float32) / 255.0
    inp = inp.transpose(2, 0, 1)[np.newaxis]  # HWC → 1CHW
    return inp, scale, pad_left, pad_top


def _xywh_to_xyxy(boxes: np.ndarray) -> np.ndarray:
    """Convert (cx, cy, w, h) → (x1, y1, x2, y2)."""
    out = np.empty_like(boxes)
    out[:, 0] = boxes[:, 0] - boxes[:, 2] / 2
    out[:, 1] = boxes[:, 1] - boxes[:, 3] / 2
    out[:, 2] = boxes[:, 0] + boxes[:, 2] / 2
    out[:, 3] = boxes[:, 1] + boxes[:, 3] / 2
    return out


def _nms(boxes: np.ndarray, scores: np.ndarray, iou_thresh: float) -> list:
    """Vectorised greedy NMS."""
    x1, y1, x2, y2 = boxes[:, 0], boxes[:, 1], boxes[:, 2], boxes[:, 3]
    areas = (x2 - x1).clip(0) * (y2 - y1).clip(0)
    order = scores.argsort()[::-1]
    keep = []
    while order.size:
        i = order[0]
        keep.append(int(i))
        if order.size == 1:
            break
        rest = order[1:]
        xx1 = np.maximum(x1[i], x1[rest])
        yy1 = np.maximum(y1[i], y1[rest])
        xx2 = np.minimum(x2[i], x2[rest])
        yy2 = np.minimum(y2[i], y2[rest])
        inter = np.maximum(0.0, xx2 - xx1) * np.maximum(0.0, yy2 - yy1)
        iou = inter / (areas[i] + areas[rest] - inter + 1e-7)
        order = rest[iou <= iou_thresh]
    return keep


def postprocess(
    raw: np.ndarray,
    scale: float,
    pad_left: int,
    pad_top: int,
    conf_thresh: float,
    iou_thresh: float,
    orig_w: int,
    orig_h: int,
) -> list:
    """
    Parse YOLOv8-pose ONNX output of shape (1, 56, 8400).

    Layout per anchor (after transpose to (8400, 56)):
      0:4   cx, cy, w, h   — pixel coords in 640×640 input space
      4     confidence
      5:56  17 × (kp_x, kp_y, kp_vis)   — pixel coords in 640×640 input space

    Returns list of dicts: {bbox, keypoints, score, center}
    """
    pred = raw[0].T          # (8400, 56)
    conf = pred[:, 4]
    mask = conf >= conf_thresh
    pred = pred[mask]
    if pred.shape[0] == 0:
        return []

    boxes_xywh = pred[:, :4]
    scores = pred[:, 4]
    kps = pred[:, 5:].reshape(-1, 17, 3)   # (N, 17, 3): x, y, vis

    boxes_xyxy = _xywh_to_xyxy(boxes_xywh)
    keep = _nms(boxes_xyxy, scores, iou_thresh)

    results = []
    for i in keep:
        # Undo letterbox: remove padding, undo scale
        x1 = (boxes_xyxy[i, 0] - pad_left) / scale
        y1 = (boxes_xyxy[i, 1] - pad_top)  / scale
        x2 = (boxes_xyxy[i, 2] - pad_left) / scale
        y2 = (boxes_xyxy[i, 3] - pad_top)  / scale
        x1 = float(max(0.0, x1))
        y1 = float(max(0.0, y1))
        x2 = float(min(orig_w, x2))
        y2 = float(min(orig_h, y2))

        kp = kps[i].copy()
        kp[:, 0] = np.clip((kp[:, 0] - pad_left) / scale, 0, orig_w)
        kp[:, 1] = np.clip((kp[:, 1] - pad_top)  / scale, 0, orig_h)

        cx = (x1 + x2) / 2.0
        cy = (y1 + y2) / 2.0
        results.append({
            "bbox":      (x1, y1, x2, y2),
            "keypoints": kp,              # (17, 3): x, y, visibility_conf
            "score":     float(scores[i]),
            "center":    (cx, cy),
        })
    return results


# ---------------------------------------------------------------------------
# Tracking
# ---------------------------------------------------------------------------

class Track:
    """One tracked person with a stable ID."""

    _counter = 0

    def __init__(self, det: dict):
        Track._counter += 1
        self.track_id = Track._counter
        self._from_det(det)
        self.age = 1
        self.frames_since_seen = 0

    def update(self, det: dict):
        self._from_det(det)
        self.age += 1
        self.frames_since_seen = 0

    def _from_det(self, det: dict):
        self.center    = det["center"]
        self.keypoints = det["keypoints"]
        self.bbox      = det["bbox"]
        self.score     = det["score"]


def _match_greedy(
    tracks: list,
    detections: list,
    dist_thresh: float,
) -> tuple:
    """
    Greedy nearest-neighbour matching of detections to existing tracks
    using Euclidean distance between bbox centres.

    Returns:
        matches          : list of (track_idx, det_idx) pairs
        unmatched_dets   : list of detection indices with no track assigned
        unmatched_tracks : list of track indices with no detection assigned
    """
    if not tracks or not detections:
        return [], list(range(len(detections))), list(range(len(tracks)))

    tc = np.array([t.center for t in tracks])          # (T, 2)
    dc = np.array([d["center"] for d in detections])   # (D, 2)

    # Pairwise Euclidean distance
    diff  = tc[:, np.newaxis, :] - dc[np.newaxis, :, :]   # (T, D, 2)
    dists = np.linalg.norm(diff, axis=2)                   # (T, D)

    matched_t, matched_d = set(), set()
    matches = []

    # Iterate over all (track, det) pairs sorted by ascending distance
    valid = np.argwhere(dists < dist_thresh)
    if valid.size:
        order = dists[valid[:, 0], valid[:, 1]].argsort()
        for idx in order:
            ti, di = valid[idx]
            if ti not in matched_t and di not in matched_d:
                matches.append((int(ti), int(di)))
                matched_t.add(int(ti))
                matched_d.add(int(di))

    unmatched_dets   = [i for i in range(len(detections)) if i not in matched_d]
    unmatched_tracks = [i for i in range(len(tracks))     if i not in matched_t]
    return matches, unmatched_dets, unmatched_tracks


# ---------------------------------------------------------------------------
# Drawing
# ---------------------------------------------------------------------------

def _color(track_id: int):
    return _PALETTE[track_id % len(_PALETTE)]


def draw_pose(frame: np.ndarray, tracks: list, kp_thresh: float = 0.3):
    """Render keypoints, skeleton lines, bounding box, and track ID."""
    active = [t for t in tracks if t.frames_since_seen == 0]
    for track in active:
        col = _color(track.track_id)
        kp  = track.keypoints   # (17, 3)

        # Skeleton
        for a, b in SKELETON:
            if kp[a, 2] >= kp_thresh and kp[b, 2] >= kp_thresh:
                cv2.line(frame,
                         (int(kp[a, 0]), int(kp[a, 1])),
                         (int(kp[b, 0]), int(kp[b, 1])),
                         col, 2, cv2.LINE_AA)

        # Keypoints
        for j in range(17):
            if kp[j, 2] >= kp_thresh:
                cv2.circle(frame, (int(kp[j, 0]), int(kp[j, 1])),
                           4, col, -1, cv2.LINE_AA)

        # Bounding box + label
        x1, y1, x2, y2 = (int(v) for v in track.bbox)
        cv2.rectangle(frame, (x1, y1), (x2, y2), col, 1, cv2.LINE_AA)
        label = f"ID:{track.track_id}"
        (tw, th), bl = cv2.getTextSize(label, cv2.FONT_HERSHEY_SIMPLEX, 0.5, 1)
        cv2.rectangle(frame, (x1, y1 - th - bl - 4), (x1 + tw + 4, y1), col, -1)
        cv2.putText(frame, label, (x1 + 2, y1 - bl - 2),
                    cv2.FONT_HERSHEY_SIMPLEX, 0.5, (255, 255, 255), 1, cv2.LINE_AA)


# ---------------------------------------------------------------------------
# Main
# ---------------------------------------------------------------------------

def parse_args():
    parser = argparse.ArgumentParser(
        description="Real-time multi-person pose tracking — YOLOv8n-pose ONNX + ONNXRuntime",
        formatter_class=argparse.ArgumentDefaultsHelpFormatter,
    )
    parser.add_argument(
        "--model", default="models/yolov8n-pose.onnx",
        help="Path to the YOLOv8n-pose ONNX model file.",
    )
    parser.add_argument(
        "--camera", type=int, default=0,
        help="OpenCV camera device index (0 = first webcam).",
    )
    parser.add_argument(
        "--conf", type=float, default=0.30,
        help="Detection confidence threshold (0–1).",
    )
    parser.add_argument(
        "--iou", type=float, default=0.45,
        help="NMS IoU threshold (0–1).",
    )
    parser.add_argument(
        "--dist-thresh", type=float, default=100.0,
        help="Max centre-to-centre distance (pixels) to match a detection to an existing track.",
    )
    parser.add_argument(
        "--max-age", type=int, default=30,
        help="Maximum number of consecutive frames a track may be missing before it is deleted.",
    )
    parser.add_argument(
        "--kp-thresh", type=float, default=0.30,
        help="Minimum keypoint visibility confidence required for drawing.",
    )
    parser.add_argument(
        "--width", type=int, default=0,
        help="Camera capture width in pixels (0 = use camera default).",
    )
    parser.add_argument(
        "--height", type=int, default=0,
        help="Camera capture height in pixels (0 = use camera default).",
    )
    parser.add_argument(
        "--providers", nargs="+",
        default=["CPUExecutionProvider"],
        help=(
            "ONNXRuntime execution providers in priority order.  "
            "Use CUDAExecutionProvider CPUExecutionProvider for GPU acceleration."
        ),
    )
    return parser.parse_args()


def main():
    args = parse_args()

    # ------------------------------------------------------------------
    # Model file existence check with friendly error message
    # ------------------------------------------------------------------
    model_path = os.path.abspath(args.model)
    if not os.path.isfile(model_path):
        print("=" * 60)
        print("ERROR: Model file not found.")
        print(f"       Expected path: {model_path}")
        print()
        print("To obtain the model, run ONE of the following:")
        print()
        print("  Option A — export from Ultralytics (recommended):")
        print("    pip install ultralytics")
        print(
            "    python -c \""
            "from ultralytics import YOLO; "
            "YOLO('yolov8n-pose.pt').export(format='onnx', opset=12)"
            "\""
        )
        print(f"    Then move yolov8n-pose.onnx to: {model_path}")
        print()
        print("  Option B — download a pre-exported ONNX from Hugging Face / GitHub")
        print("    (search 'yolov8n-pose onnx' and place it at the path above)")
        print("=" * 60)
        sys.exit(1)

    # ------------------------------------------------------------------
    # Load ONNX model
    # ------------------------------------------------------------------
    print(f"[INFO] Loading model: {model_path}")
    print(f"[INFO] Providers    : {args.providers}")
    try:
        sess = ort.InferenceSession(model_path, providers=args.providers)
    except Exception as exc:
        print(f"ERROR: Failed to load ONNX model — {exc}")
        sys.exit(1)

    inp_meta  = sess.get_inputs()[0]
    inp_name  = inp_meta.name
    out_shape = sess.get_outputs()[0].shape
    print(f"[INFO] Input  : '{inp_name}' {inp_meta.shape}")
    print(f"[INFO] Output : '{sess.get_outputs()[0].name}' {out_shape}")

    # Sanity-check expected output shape (1, 56, 8400)
    if len(out_shape) == 3 and out_shape[1] != 56:
        print(
            f"WARNING: Unexpected output shape {out_shape}. "
            "This script is designed for YOLOv8n-pose (17 keypoints = 56 output channels). "
            "Results may be incorrect."
        )

    # ------------------------------------------------------------------
    # Open camera
    # ------------------------------------------------------------------
    print(f"[INFO] Opening camera index {args.camera} ...")
    cap = cv2.VideoCapture(args.camera)
    if not cap.isOpened():
        print(f"ERROR: Cannot open camera index {args.camera}.")
        print("       Check that the webcam is connected and not in use by another application.")
        sys.exit(1)

    if args.width > 0:
        cap.set(cv2.CAP_PROP_FRAME_WIDTH, args.width)
    if args.height > 0:
        cap.set(cv2.CAP_PROP_FRAME_HEIGHT, args.height)

    actual_w = int(cap.get(cv2.CAP_PROP_FRAME_WIDTH))
    actual_h = int(cap.get(cv2.CAP_PROP_FRAME_HEIGHT))
    print(f"[INFO] Camera resolution: {actual_w}×{actual_h}")
    print("[INFO] Press  Q  to quit.")

    # ------------------------------------------------------------------
    # Main loop
    # ------------------------------------------------------------------
    tracks: list = []
    fps_buf = deque(maxlen=30)
    t_prev  = time.perf_counter()

    while True:
        ret, frame = cap.read()
        if not ret:
            print("WARNING: Failed to grab frame — retrying...")
            time.sleep(0.01)
            continue

        orig_h, orig_w = frame.shape[:2]

        # --- Inference ------------------------------------------------
        inp, scale, pad_left, pad_top = preprocess(frame)
        raw_outputs = sess.run(None, {inp_name: inp})
        detections = postprocess(
            raw_outputs[0],
            scale, pad_left, pad_top,
            args.conf, args.iou,
            orig_w, orig_h,
        )

        # --- Track update --------------------------------------------
        matches, new_dets, lost = _match_greedy(tracks, detections, args.dist_thresh)

        for ti, di in matches:
            tracks[ti].update(detections[di])

        for ti in lost:
            tracks[ti].frames_since_seen += 1

        # Prune stale tracks
        tracks = [t for t in tracks if t.frames_since_seen <= args.max_age]

        # Spawn new tracks for unmatched detections
        for di in new_dets:
            tracks.append(Track(detections[di]))

        # --- Draw -----------------------------------------------------
        draw_pose(frame, tracks, args.kp_thresh)

        # FPS overlay
        t_now = time.perf_counter()
        fps_buf.append(1.0 / max(t_now - t_prev, _MIN_FRAME_TIME))
        t_prev = t_now
        fps = sum(fps_buf) / len(fps_buf)

        n_active = sum(1 for t in tracks if t.frames_since_seen == 0)
        cv2.putText(frame, f"FPS: {fps:.1f}",
                    (10, 30), cv2.FONT_HERSHEY_SIMPLEX, 1.0, (0, 255, 0), 2, cv2.LINE_AA)
        cv2.putText(frame, f"Persons: {n_active}",
                    (10, 65), cv2.FONT_HERSHEY_SIMPLEX, 0.8, (0, 255, 255), 2, cv2.LINE_AA)

        cv2.imshow("Pose Tracking  [Q = quit]", frame)
        if cv2.waitKey(1) & 0xFF == ord("q"):
            break

    cap.release()
    cv2.destroyAllWindows()
    print("[INFO] Done.")


if __name__ == "__main__":
    main()
