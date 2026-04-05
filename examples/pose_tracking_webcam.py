"""
Real-time multi-person pose estimation + tracking demo (Windows webcam)

Requirements:
    pip install opencv-python onnxruntime numpy

Model:
    Download a YOLOv8-pose ONNX export, e.g.:
        yolov8n-pose.onnx  (lightweight, good for CPU)
    Place it in the  models/  directory at the repository root,
    or pass the exact path via  --model.

Usage:
    python examples/pose_tracking_webcam.py --model models/yolov8n-pose.onnx
    python examples/pose_tracking_webcam.py --model models/yolov8n-pose.onnx --device 1

Keyboard shortcuts (while the window is open):
    q  – quit
"""

import argparse
import os
import time
from collections import OrderedDict
from typing import Dict, List, Tuple

import cv2
import numpy as np
import onnxruntime as ort

# ---------------------------------------------------------------------------
# COCO-17 skeleton definition (pairs of keypoint indices)
# ---------------------------------------------------------------------------
SKELETON = [
    (0, 1), (0, 2),           # nose → eyes
    (1, 3), (2, 4),           # eyes → ears
    (5, 6),                   # shoulders
    (5, 7), (7, 9),           # left  arm
    (6, 8), (8, 10),          # right arm
    (5, 11), (6, 12),         # torso
    (11, 12),                 # hips
    (11, 13), (13, 15),       # left  leg
    (12, 14), (14, 16),       # right leg
]

PALETTE = [
    (255, 128,   0), (255, 153,  51), (255, 178, 102), (230, 230,   0),
    (255, 153, 255), (153, 204, 255), (255, 102, 255), (255,  51, 255),
    (102, 178, 255), ( 51, 153, 255), (255, 153, 153), (255, 102, 102),
    (255,  51,  51), (153, 255, 153), (102, 255, 102), ( 51, 255,  51),
    (0,   255,   0),
]


# ---------------------------------------------------------------------------
# Preprocessing helpers
# ---------------------------------------------------------------------------

def letterbox(image: np.ndarray, new_shape: Tuple[int, int] = (640, 640),
              color: Tuple[int, int, int] = (114, 114, 114)):
    """Resize + pad image to new_shape while preserving aspect ratio."""
    h, w = image.shape[:2]
    nh, nw = new_shape
    scale = min(nw / w, nh / h)
    nw_unpad, nh_unpad = int(round(w * scale)), int(round(h * scale))
    dw, dh = (nw - nw_unpad) / 2, (nh - nh_unpad) / 2

    resized = cv2.resize(image, (nw_unpad, nh_unpad), interpolation=cv2.INTER_LINEAR)
    top, bottom = int(round(dh - 0.1)), int(round(dh + 0.1))
    left, right  = int(round(dw - 0.1)), int(round(dw + 0.1))
    padded = cv2.copyMakeBorder(resized, top, bottom, left, right,
                                cv2.BORDER_CONSTANT, value=color)
    return padded, scale, (dw, dh)


def preprocess(frame: np.ndarray, input_size: int = 640):
    img, scale, (dw, dh) = letterbox(frame, (input_size, input_size))
    img = cv2.cvtColor(img, cv2.COLOR_BGR2RGB)
    img = img.astype(np.float32) / 255.0
    img = np.transpose(img, (2, 0, 1))[np.newaxis]  # NCHW
    return img, scale, dw, dh


# ---------------------------------------------------------------------------
# Post-processing
# ---------------------------------------------------------------------------

def xywh2xyxy(boxes: np.ndarray) -> np.ndarray:
    out = boxes.copy()
    out[..., 0] = boxes[..., 0] - boxes[..., 2] / 2  # x1
    out[..., 1] = boxes[..., 1] - boxes[..., 3] / 2  # y1
    out[..., 2] = boxes[..., 0] + boxes[..., 2] / 2  # x2
    out[..., 3] = boxes[..., 1] + boxes[..., 3] / 2  # y2
    return out


def nms(boxes: np.ndarray, scores: np.ndarray, iou_thresh: float = 0.45) -> List[int]:
    x1, y1, x2, y2 = boxes[:, 0], boxes[:, 1], boxes[:, 2], boxes[:, 3]
    areas = (x2 - x1 + 1) * (y2 - y1 + 1)
    order = scores.argsort()[::-1]
    keep = []
    while order.size > 0:
        i = order[0]
        keep.append(int(i))
        xx1 = np.maximum(x1[i], x1[order[1:]])
        yy1 = np.maximum(y1[i], y1[order[1:]])
        xx2 = np.minimum(x2[i], x2[order[1:]])
        yy2 = np.minimum(y2[i], y2[order[1:]])
        inter = np.maximum(0, xx2 - xx1 + 1) * np.maximum(0, yy2 - yy1 + 1)
        iou = inter / (areas[i] + areas[order[1:]] - inter)
        order = order[1:][iou < iou_thresh]
    return keep


def postprocess(output: np.ndarray, scale: float, dw: float, dh: float,
                orig_w: int, orig_h: int,
                conf_thresh: float = 0.35, iou_thresh: float = 0.45):
    """
    YOLOv8-pose raw output shape: (1, 56, N) where N = number of anchor predictions.
    Each column: [cx, cy, cw, ch, conf, kp0_x, kp0_y, kp0_v, ..., kp16_x, kp16_y, kp16_v]
    """
    preds = output[0]           # (56, N)
    preds = preds.T             # (N, 56)

    boxes_xywh = preds[:, :4]
    scores = preds[:, 4]
    keypoints = preds[:, 5:]    # (N, 51)  → 17 × (x, y, visibility)

    mask = scores >= conf_thresh
    if mask.sum() == 0:
        return [], []

    boxes_xywh = boxes_xywh[mask]
    scores = scores[mask]
    keypoints = keypoints[mask]

    boxes_xyxy = xywh2xyxy(boxes_xywh)

    # Undo letterbox padding and scale
    boxes_xyxy[:, [0, 2]] = (boxes_xyxy[:, [0, 2]] - dw) / scale
    boxes_xyxy[:, [1, 3]] = (boxes_xyxy[:, [1, 3]] - dh) / scale
    boxes_xyxy = np.clip(boxes_xyxy,
                         [0, 0, 0, 0],
                         [orig_w, orig_h, orig_w, orig_h])

    keypoints_xy = keypoints.reshape(-1, 17, 3)
    keypoints_xy[:, :, 0] = (keypoints_xy[:, :, 0] - dw) / scale
    keypoints_xy[:, :, 1] = (keypoints_xy[:, :, 1] - dh) / scale

    keep = nms(boxes_xyxy, scores, iou_thresh)
    return boxes_xyxy[keep], keypoints_xy[keep]


# ---------------------------------------------------------------------------
# Minimal IoU-based tracker
# ---------------------------------------------------------------------------

class SimpleTracker:
    """Assigns stable integer track_ids to detections across frames using IoU."""

    def __init__(self, max_age: int = 30, min_iou: float = 0.3):
        self.max_age = max_age
        self.min_iou = min_iou
        self._next_id = 1
        # track_id → {box, age}
        self._tracks: Dict[int, dict] = OrderedDict()

    @staticmethod
    def _iou(a: np.ndarray, b: np.ndarray) -> float:
        ax1, ay1, ax2, ay2 = a
        bx1, by1, bx2, by2 = b
        ix1, iy1 = max(ax1, bx1), max(ay1, by1)
        ix2, iy2 = min(ax2, bx2), min(ay2, by2)
        inter = max(0, ix2 - ix1) * max(0, iy2 - iy1)
        if inter == 0:
            return 0.0
        area_a = (ax2 - ax1) * (ay2 - ay1)
        area_b = (bx2 - bx1) * (by2 - by1)
        return inter / (area_a + area_b - inter)

    def update(self, boxes: np.ndarray) -> List[int]:
        """
        Match new detections to existing tracks.
        Returns list of track_ids (same order as input boxes).
        """
        if len(boxes) == 0:
            # Age out all tracks
            dead = [tid for tid, t in self._tracks.items()
                    if t['age'] >= self.max_age]
            for tid in dead:
                del self._tracks[tid]
            for t in self._tracks.values():
                t['age'] += 1
            return []

        # Build IoU matrix: tracks × detections
        track_ids = list(self._tracks.keys())
        assigned_tracks = {}    # det_idx → track_id
        used_tracks = set()

        if track_ids:
            iou_matrix = np.zeros((len(track_ids), len(boxes)))
            for ti, tid in enumerate(track_ids):
                for di, box in enumerate(boxes):
                    iou_matrix[ti, di] = self._iou(self._tracks[tid]['box'], box)

            # Greedy matching by highest IoU
            while True:
                idx = np.unravel_index(np.argmax(iou_matrix), iou_matrix.shape)
                ti, di = idx
                if iou_matrix[ti, di] < self.min_iou:
                    break
                tid = track_ids[ti]
                if di not in assigned_tracks and tid not in used_tracks:
                    assigned_tracks[di] = tid
                    used_tracks.add(tid)
                iou_matrix[ti, :] = -1
                iou_matrix[:, di] = -1

        # Age out unmatched tracks
        matched_track_ids = set(assigned_tracks.values())
        dead = []
        for tid, t in self._tracks.items():
            if tid not in matched_track_ids:
                t['age'] += 1
                if t['age'] >= self.max_age:
                    dead.append(tid)
        for tid in dead:
            del self._tracks[tid]

        # Create new tracks for unmatched detections; update matched ones
        result_ids = []
        for di, box in enumerate(boxes):
            if di in assigned_tracks:
                tid = assigned_tracks[di]
                self._tracks[tid]['box'] = box
                self._tracks[tid]['age'] = 0
                result_ids.append(tid)
            else:
                new_id = self._next_id
                self._next_id += 1
                self._tracks[new_id] = {'box': box, 'age': 0}
                result_ids.append(new_id)

        return result_ids


# ---------------------------------------------------------------------------
# Drawing helpers
# ---------------------------------------------------------------------------

def draw_pose(frame: np.ndarray, box: np.ndarray, kps: np.ndarray,
              track_id: int, vis_thresh: float = 0.5):
    x1, y1, x2, y2 = box.astype(int)
    color = PALETTE[track_id % len(PALETTE)]

    # Bounding box + ID label
    cv2.rectangle(frame, (x1, y1), (x2, y2), color, 2)
    label = f"ID {track_id}"
    lw, lh = cv2.getTextSize(label, cv2.FONT_HERSHEY_SIMPLEX, 0.55, 2)[0]
    cv2.rectangle(frame, (x1, y1 - lh - 6), (x1 + lw + 4, y1), color, -1)
    cv2.putText(frame, label, (x1 + 2, y1 - 4),
                cv2.FONT_HERSHEY_SIMPLEX, 0.55, (255, 255, 255), 2)

    # Skeleton
    for a, b in SKELETON:
        if kps[a, 2] >= vis_thresh and kps[b, 2] >= vis_thresh:
            pa = (int(kps[a, 0]), int(kps[a, 1]))
            pb = (int(kps[b, 0]), int(kps[b, 1]))
            cv2.line(frame, pa, pb, color, 2, cv2.LINE_AA)

    # Keypoints
    for i, (kx, ky, kv) in enumerate(kps):
        if kv >= vis_thresh:
            cv2.circle(frame, (int(kx), int(ky)), 4, PALETTE[i % len(PALETTE)], -1)


# ---------------------------------------------------------------------------
# Main
# ---------------------------------------------------------------------------

def main():
    parser = argparse.ArgumentParser(
        description="Real-time multi-person pose tracking (ONNXRuntime, YOLOv8-pose)"
    )
    parser.add_argument("--model",  default="models/yolov8n-pose.onnx",
                        help="Path to YOLOv8-pose ONNX model file")
    parser.add_argument("--device", type=int, default=0,
                        help="Webcam device index (0, 1, 2 …)")
    parser.add_argument("--input-size", type=int, default=640,
                        help="Model input resolution (square). Default: 640")
    parser.add_argument("--conf", type=float, default=0.35,
                        help="Confidence threshold. Default: 0.35")
    parser.add_argument("--iou",  type=float, default=0.45,
                        help="NMS IoU threshold. Default: 0.45")
    args = parser.parse_args()

    # Load ONNX model
    if not os.path.isfile(args.model):
        raise FileNotFoundError(
            f"\n[ERROR] Model not found: {args.model}\n"
            "Download a YOLOv8-pose ONNX export and place it at that path.\n"
            "Example (using ultralytics):\n"
            "    pip install ultralytics\n"
            "    python -c \"from ultralytics import YOLO; "
            "YOLO('yolov8n-pose.pt').export(format='onnx')\"\n"
            "Then move yolov8n-pose.onnx → models/yolov8n-pose.onnx"
        )

    providers = ["CPUExecutionProvider"]
    session = ort.InferenceSession(args.model, providers=providers)
    input_name = session.get_inputs()[0].name
    print(f"[INFO] Loaded model: {args.model}")
    print(f"[INFO] Providers: {session.get_providers()}")

    cap = cv2.VideoCapture(args.device)
    if not cap.isOpened():
        raise RuntimeError(
            f"[ERROR] Cannot open webcam device {args.device}. "
            "Try --device 1 or --device 2."
        )

    tracker = SimpleTracker()
    fps_buf: List[float] = []

    print("[INFO] Press 'q' in the window to quit.")
    while True:
        t0 = time.perf_counter()
        ret, frame = cap.read()
        if not ret:
            print("[WARN] Failed to read frame — retrying …")
            continue

        orig_h, orig_w = frame.shape[:2]

        # Inference
        blob, scale, dw, dh = preprocess(frame, args.input_size)
        outputs = session.run(None, {input_name: blob})

        boxes, kps_all = postprocess(
            outputs[0], scale, dw, dh, orig_w, orig_h,
            conf_thresh=args.conf, iou_thresh=args.iou
        )

        # Tracking
        if len(boxes) > 0:
            track_ids = tracker.update(boxes)
            for box, kps, tid in zip(boxes, kps_all, track_ids):
                draw_pose(frame, box, kps, tid)
        else:
            tracker.update(np.empty((0, 4)))

        # FPS overlay
        elapsed = time.perf_counter() - t0
        fps_buf.append(1.0 / max(elapsed, 1e-3))
        if len(fps_buf) > 30:
            fps_buf.pop(0)
        fps = sum(fps_buf) / len(fps_buf)
        cv2.putText(frame, f"FPS: {fps:.1f}", (10, 30),
                    cv2.FONT_HERSHEY_SIMPLEX, 1.0, (0, 255, 0), 2)

        cv2.imshow("Pose Tracking  |  press q to quit", frame)
        if cv2.waitKey(1) & 0xFF == ord('q'):
            break

    cap.release()
    cv2.destroyAllWindows()


if __name__ == "__main__":
    main()
