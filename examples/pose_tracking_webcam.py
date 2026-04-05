#!/usr/bin/env python3
"""
Windows real-time multi-person pose tracking demo
==================================================
Model  : YOLOv8n-pose (ONNX) — single-pass person detection + 17-keypoint pose
Tracker: IoU-based (no extra deps) — stable track_id across frames
Display: OpenCV window — skeleton overlay + bounding-box + ID + FPS

Quick start (run once to download model, then every time after):
    python examples/pose_tracking_webcam.py --download
    python examples/pose_tracking_webcam.py
"""

from __future__ import annotations

import argparse
import time
import urllib.request
from pathlib import Path

import cv2
import numpy as np

try:
    import onnxruntime as ort
except ImportError:
    raise SystemExit(
        "onnxruntime not found.\n"
        "Install CPU version : pip install onnxruntime\n"
        "Install GPU version : pip install onnxruntime-gpu"
    )

# ---------------------------------------------------------------------------
# Constants
# ---------------------------------------------------------------------------

# Official Ultralytics release – CPU-friendly, ~12 MB
MODEL_URL = (
    "https://github.com/ultralytics/assets/releases/download/v8.2.0/yolov8n-pose.onnx"
)
MODEL_DEFAULT = Path(__file__).resolve().parent.parent / "models" / "yolov8n-pose.onnx"

# COCO-17 skeleton connections (0-indexed)
SKELETON: list[tuple[int, int]] = [
    (0, 1), (0, 2), (1, 3), (2, 4),           # head
    (5, 6), (5, 7), (7, 9), (6, 8), (8, 10),  # arms
    (5, 11), (6, 12), (11, 12),                # torso
    (11, 13), (13, 15), (12, 14), (14, 16),    # legs
]

# Distinct BGR colours for up to 8 simultaneous track IDs (cycles if more)
_PALETTE: list[tuple[int, int, int]] = [
    (255, 80, 80),
    (80, 255, 80),
    (80, 80, 255),
    (255, 255, 80),
    (80, 255, 255),
    (255, 80, 255),
    (128, 255, 80),
    (80, 128, 255),
]


def _color(track_id: int) -> tuple[int, int, int]:
    return _PALETTE[track_id % len(_PALETTE)]


# ---------------------------------------------------------------------------
# IoU-based multi-object tracker
# ---------------------------------------------------------------------------


class IoUTracker:
    """
    Minimal IoU-based tracker — no external dependency.

    Each detection is matched to the nearest existing track by bounding-box
    IoU.  Unmatched detections get a new ID; tracks that go unmatched for
    *max_lost* consecutive frames are deleted.
    """

    def __init__(self, iou_thr: float = 0.30, max_lost: int = 30) -> None:
        self.iou_thr = iou_thr
        self.max_lost = max_lost
        self._next_id: int = 1
        self._tracks: dict[int, dict] = {}  # id -> {"box": ndarray, "lost": int}

    @staticmethod
    def _iou(a: np.ndarray, b: np.ndarray) -> float:
        ax1, ay1, ax2, ay2 = a
        bx1, by1, bx2, by2 = b
        ix1 = max(ax1, bx1)
        iy1 = max(ay1, by1)
        ix2 = min(ax2, bx2)
        iy2 = min(ay2, by2)
        inter = max(0.0, ix2 - ix1) * max(0.0, iy2 - iy1)
        area_a = max(0.0, ax2 - ax1) * max(0.0, ay2 - ay1)
        area_b = max(0.0, bx2 - bx1) * max(0.0, by2 - by1)
        union = area_a + area_b - inter
        return inter / union if union > 0 else 0.0

    def update(self, detections: list[np.ndarray]) -> list[int]:
        """
        Match *detections* (each an [x1,y1,x2,y2] array) to existing tracks.

        Returns a list of integer track IDs, one per detection.
        """
        unmatched_tracks: set[int] = set(self._tracks.keys())
        assigned: list[int] = [-1] * len(detections)

        if detections and self._tracks:
            track_ids = list(self._tracks.keys())
            track_boxes = [self._tracks[tid]["box"] for tid in track_ids]

            for di, det in enumerate(detections):
                best_iou, best_ti = 0.0, -1
                for ti, tbox in enumerate(track_boxes):
                    iou = self._iou(det, tbox)
                    if iou > best_iou:
                        best_iou, best_ti = iou, ti

                if best_iou >= self.iou_thr:
                    tid = track_ids[best_ti]
                    assigned[di] = tid
                    self._tracks[tid]["box"] = det
                    self._tracks[tid]["lost"] = 0
                    unmatched_tracks.discard(tid)

        # Spawn new tracks for unmatched detections
        for di, det in enumerate(detections):
            if assigned[di] == -1:
                tid = self._next_id
                self._next_id += 1
                self._tracks[tid] = {"box": det, "lost": 0}
                assigned[di] = tid

        # Age out missing tracks
        for tid in list(unmatched_tracks):
            self._tracks[tid]["lost"] += 1
            if self._tracks[tid]["lost"] > self.max_lost:
                del self._tracks[tid]

        return assigned


# ---------------------------------------------------------------------------
# Model helpers
# ---------------------------------------------------------------------------


def download_model(dest: Path) -> None:
    dest.parent.mkdir(parents=True, exist_ok=True)
    print(f"Downloading YOLOv8n-pose ONNX model → {dest}")
    print(f"  Source: {MODEL_URL}")
    urllib.request.urlretrieve(MODEL_URL, dest)
    print("Download complete.")


def load_session(model_path: Path) -> ort.InferenceSession:
    # Try GPU first, fall back to CPU automatically
    providers = ["CUDAExecutionProvider", "CPUExecutionProvider"]
    sess = ort.InferenceSession(str(model_path), providers=providers)
    active = sess.get_providers()[0]
    print(f"ONNXRuntime provider : {active}")
    return sess


# ---------------------------------------------------------------------------
# Pre- / post-processing (YOLOv8-pose letterbox pipeline)
# ---------------------------------------------------------------------------


def letterbox(
    frame: np.ndarray, target_size: int
) -> tuple[np.ndarray, float, int, int]:
    """
    Resize *frame* to (*target_size* × *target_size*) with aspect-ratio-
    preserving letterbox padding (value 114).

    Returns: (padded_rgb_float32_chw, scale, pad_x, pad_y)
    """
    h, w = frame.shape[:2]
    scale = min(target_size / w, target_size / h)
    nw, nh = int(w * scale), int(h * scale)
    pad_x = (target_size - nw) // 2
    pad_y = (target_size - nh) // 2

    resized = cv2.resize(frame, (nw, nh), interpolation=cv2.INTER_LINEAR)
    canvas = np.full((target_size, target_size, 3), 114, dtype=np.uint8)
    canvas[pad_y : pad_y + nh, pad_x : pad_x + nw] = resized

    blob = canvas[:, :, ::-1].astype(np.float32) / 255.0  # BGR→RGB, normalise
    blob = blob.transpose(2, 0, 1)[np.newaxis]             # HWC→NCHW
    return blob, scale, pad_x, pad_y


def postprocess(
    raw: np.ndarray,
    scale: float,
    pad_x: int,
    pad_y: int,
    frame_shape: tuple[int, int],
    conf_thr: float = 0.30,
    nms_iou: float = 0.45,
) -> tuple[list[np.ndarray], list[np.ndarray]]:
    """
    Parse YOLOv8-pose raw output (1 × 56 × 8400) into pixel-space boxes and
    keypoints.

    Returns:
        boxes    – list of [x1, y1, x2, y2] float arrays (frame coordinates)
        kps_list – list of (17 × 3) float arrays [x, y, conf] (frame coords)
    """
    pred = raw[0].T  # (8400, 56)

    cx, cy, bw, bh = pred[:, 0], pred[:, 1], pred[:, 2], pred[:, 3]
    obj_conf = pred[:, 4]

    keep = obj_conf > conf_thr
    if not keep.any():
        return [], []
    pred = pred[keep]
    cx, cy, bw, bh = pred[:, 0], pred[:, 1], pred[:, 2], pred[:, 3]
    obj_conf = pred[:, 4]

    # Invert letterbox transform → original frame coordinates
    x1 = (cx - bw / 2 - pad_x) / scale
    y1 = (cy - bh / 2 - pad_y) / scale
    x2 = (cx + bw / 2 - pad_x) / scale
    y2 = (cy + bh / 2 - pad_y) / scale

    fh, fw = frame_shape[:2]
    x1 = np.clip(x1, 0, fw)
    y1 = np.clip(y1, 0, fh)
    x2 = np.clip(x2, 0, fw)
    y2 = np.clip(y2, 0, fh)

    boxes_arr = np.stack([x1, y1, x2, y2], axis=1)

    # NMS
    indices = cv2.dnn.NMSBoxes(
        boxes_arr.tolist(), obj_conf.tolist(), conf_thr, nms_iou
    )
    if indices is None or len(indices) == 0:
        return [], []
    indices = np.array(indices).flatten()

    out_boxes: list[np.ndarray] = []
    out_kps: list[np.ndarray] = []
    for i in indices:
        out_boxes.append(boxes_arr[i])

        kps_raw = pred[i, 5:].reshape(17, 3)   # x, y, visibility
        kps_xy = (kps_raw[:, :2] - [pad_x, pad_y]) / scale
        kps_v = kps_raw[:, 2:3]
        out_kps.append(np.concatenate([kps_xy, kps_v], axis=1))  # (17,3)

    return out_boxes, out_kps


# ---------------------------------------------------------------------------
# Drawing helpers
# ---------------------------------------------------------------------------


def draw_skeleton(
    frame: np.ndarray,
    kps: np.ndarray,
    color: tuple[int, int, int],
    kp_conf_thr: float = 0.40,
) -> None:
    """Draw skeleton connections and keypoint dots on *frame* in-place."""
    for ka, kb in SKELETON:
        if kps[ka, 2] > kp_conf_thr and kps[kb, 2] > kp_conf_thr:
            pt_a = (int(kps[ka, 0]), int(kps[ka, 1]))
            pt_b = (int(kps[kb, 0]), int(kps[kb, 1]))
            cv2.line(frame, pt_a, pt_b, color, 2, cv2.LINE_AA)
    for kp in kps:
        if kp[2] > kp_conf_thr:
            cv2.circle(frame, (int(kp[0]), int(kp[1])), 4, color, -1, cv2.LINE_AA)


# ---------------------------------------------------------------------------
# Main loop
# ---------------------------------------------------------------------------


def run(
    device: int,
    model_path: Path,
    conf_thr: float,
    input_size: int,
    auto_download: bool,
) -> None:
    # ── model ───────────────────────────────────────────────────────────────
    if not model_path.exists():
        if auto_download:
            download_model(model_path)
        else:
            raise FileNotFoundError(
                f"Model not found: {model_path}\n"
                "  Run with --download to auto-download, or copy the file manually.\n"
                f"  Manual URL: {MODEL_URL}"
            )

    sess = load_session(model_path)
    inp_name = sess.get_inputs()[0].name

    # ── camera ──────────────────────────────────────────────────────────────
    # cv2.CAP_DSHOW avoids the ~1-second MSMF initialisation delay on Windows
    cap = cv2.VideoCapture(device, cv2.CAP_DSHOW)
    if not cap.isOpened():
        raise RuntimeError(
            f"Cannot open camera index {device}.\n"
            "  • Try a different --device index (0, 1, 2 …)\n"
            "  • Check Windows Device Manager / Camera privacy settings\n"
            "  • For virtual cameras, omit cv2.CAP_DSHOW"
        )

    tracker = IoUTracker()
    fps: float = 0.0
    fps_alpha: float = 0.05   # EMA smoothing factor
    t_prev: float = time.perf_counter()

    print("Camera opened.  Press  Q  or  Esc  to quit.")

    while True:
        ret, frame = cap.read()
        if not ret:
            print("Warning: failed to read frame – retrying …")
            time.sleep(0.01)
            continue

        # ── inference ───────────────────────────────────────────────────────
        blob, scale, pad_x, pad_y = letterbox(frame, input_size)
        raw_output = sess.run(None, {inp_name: blob})[0]
        boxes, kps_list = postprocess(
            raw_output, scale, pad_x, pad_y, frame.shape, conf_thr
        )

        # ── tracking ────────────────────────────────────────────────────────
        track_ids = tracker.update(boxes)

        # ── visualisation ───────────────────────────────────────────────────
        for box, kps, tid in zip(boxes, kps_list, track_ids):
            color = _color(tid)
            x1, y1, x2, y2 = box.astype(int)
            cv2.rectangle(frame, (x1, y1), (x2, y2), color, 2, cv2.LINE_AA)
            cv2.putText(
                frame,
                f"ID:{tid}",
                (x1, max(y1 - 6, 12)),
                cv2.FONT_HERSHEY_SIMPLEX,
                0.6,
                color,
                2,
                cv2.LINE_AA,
            )
            draw_skeleton(frame, kps, color)

        # ── FPS overlay ─────────────────────────────────────────────────────
        t_now = time.perf_counter()
        inst_fps = 1.0 / max(t_now - t_prev, 1e-9)
        fps = fps_alpha * inst_fps + (1.0 - fps_alpha) * fps if fps > 0.0 else inst_fps
        t_prev = t_now
        cv2.putText(
            frame,
            f"FPS: {fps:.1f}",
            (10, 30),
            cv2.FONT_HERSHEY_SIMPLEX,
            0.9,
            (0, 255, 0),
            2,
            cv2.LINE_AA,
        )

        cv2.imshow("Pose Tracking  –  press Q to quit", frame)
        key = cv2.waitKey(1) & 0xFF
        if key in (ord("q"), ord("Q"), 27):  # Q or Esc
            break

    cap.release()
    cv2.destroyAllWindows()


# ---------------------------------------------------------------------------
# CLI entry-point
# ---------------------------------------------------------------------------


def main() -> None:
    parser = argparse.ArgumentParser(
        description=(
            "Windows real-time multi-person pose tracking\n"
            "  Model : YOLOv8n-pose ONNX (single-pass detection + pose)\n"
            "  Deps  : opencv-python  onnxruntime  numpy"
        ),
        formatter_class=argparse.RawDescriptionHelpFormatter,
    )
    parser.add_argument(
        "--device",
        type=int,
        default=0,
        metavar="INDEX",
        help="Camera device index (default: 0)",
    )
    parser.add_argument(
        "--model",
        type=Path,
        default=MODEL_DEFAULT,
        metavar="PATH",
        help=f"Path to YOLOv8n-pose ONNX model (default: {MODEL_DEFAULT})",
    )
    parser.add_argument(
        "--conf",
        type=float,
        default=0.30,
        metavar="[0-1]",
        help="Detection confidence threshold (default: 0.30)",
    )
    parser.add_argument(
        "--input-size",
        type=int,
        default=640,
        metavar="PIXELS",
        help="Square input size fed to the model (default: 640)",
    )
    parser.add_argument(
        "--download",
        action="store_true",
        help="Auto-download the model if it is not already present",
    )

    args = parser.parse_args()
    run(args.device, args.model, args.conf, args.input_size, args.download)


if __name__ == "__main__":
    main()
