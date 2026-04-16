"""
Real-time human body keypoint detection and skeleton tracking.

Uses MediaPipe Pose for 33-keypoint estimation and a greedy centroid-based
tracker to maintain stable person IDs across frames.

Limitation: MediaPipe Pose detects ONE person at a time.  For genuine
multi-person support, replace the detector section with a model that returns
multiple poses (e.g., MMPose, AlphaPose, YOLO-Pose) — the PoseTracker class
works with any list of NormalizedLandmarkList objects.

Usage:
    python pose_tracker.py [--camera 0] [--width 640] [--height 480]
                           [--model-complexity 1] [--min-detection 0.5]
                           [--min-tracking 0.5] [--max-distance 0.25]
                           [--max-lost 15]
"""

import argparse
import time
from collections import deque

import cv2
import mediapipe as mp
import numpy as np

# ---------------------------------------------------------------------------
# MediaPipe setup
# ---------------------------------------------------------------------------
mp_pose = mp.solutions.pose
mp_drawing = mp.solutions.drawing_utils
mp_drawing_styles = mp.solutions.drawing_styles

# Colours used to render each tracked person (cycled by ID)
_PALETTE = [
    (0, 255, 0),       # green
    (255, 128, 0),     # orange
    (0, 128, 255),     # sky-blue
    (255, 0, 128),     # pink
    (128, 0, 255),     # purple
    (0, 255, 200),     # cyan-green
    (200, 255, 0),     # lime
    (255, 0, 0),       # blue (BGR)
]


# ---------------------------------------------------------------------------
# Tracker
# ---------------------------------------------------------------------------
class PoseTracker:
    """Greedy nearest-neighbour tracker for pose detections.

    Each detection is matched to the closest existing track (by pose centroid
    in normalised [0, 1] coordinates).  If the distance exceeds *max_distance*
    a new track is created.  Tracks that are not matched for *max_frames_lost*
    consecutive frames are deleted.

    Args:
        max_distance  (float): Maximum normalised centroid distance for a
                               match.  Default 0.25 (≈ 25 % of frame width).
        max_frames_lost (int): Frames a track may be unmatched before removal.
    """

    def __init__(self, max_distance: float = 0.25, max_frames_lost: int = 15):
        self.max_distance = max_distance
        self.max_frames_lost = max_frames_lost
        self._tracks: dict = {}   # track_id -> {'centroid': np.ndarray, 'frames_lost': int}
        self._next_id: int = 0

    # ------------------------------------------------------------------
    def _centroid(self, landmarks) -> np.ndarray | None:
        """Mean (x, y) of all visible landmarks in normalised coordinates."""
        xs, ys = [], []
        for lm in landmarks.landmark:
            if lm.visibility > 0.5:
                xs.append(lm.x)
                ys.append(lm.y)
        if not xs:
            return None
        return np.array([float(np.mean(xs)), float(np.mean(ys))])

    # ------------------------------------------------------------------
    def update(self, detections: list) -> list:
        """Match *detections* to existing tracks and return labelled results.

        Args:
            detections: List of ``mediapipe.framework.formats.landmark_pb2
                        .NormalizedLandmarkList`` objects (one per person).

        Returns:
            List of ``(track_id: int, landmarks)`` tuples for every active
            detection in this frame.
        """
        det_centroids = [self._centroid(d) for d in detections]

        track_ids = list(self._tracks.keys())
        track_centroids = [self._tracks[t]["centroid"] for t in track_ids]

        n_det = len(detections)
        n_trk = len(track_ids)

        # ----- greedy nearest-neighbour matching -------------------------
        assigned_det: dict[int, int] = {}   # det_idx -> track_id
        assigned_trk: set[int] = set()

        if n_det > 0 and n_trk > 0:
            # Build distance matrix (det × track)
            dist = np.full((n_det, n_trk), np.inf)
            for i, dc in enumerate(det_centroids):
                for j, tc in enumerate(track_centroids):
                    if dc is not None and tc is not None:
                        dist[i, j] = float(np.linalg.norm(dc - tc))

            # Sort all (i, j) pairs by distance and greedily assign
            for flat_idx in np.argsort(dist.ravel()):
                i, j = divmod(int(flat_idx), n_trk)
                if i in assigned_det or j in assigned_trk:
                    continue
                if dist[i, j] > self.max_distance:
                    break
                assigned_det[i] = track_ids[j]
                assigned_trk.add(j)

        # ----- update existing tracks / create new ones ------------------
        results: list = []
        for i, (det, centroid) in enumerate(zip(detections, det_centroids)):
            if i in assigned_det:
                tid = assigned_det[i]
                self._tracks[tid]["centroid"] = centroid
                self._tracks[tid]["frames_lost"] = 0
            else:
                tid = self._next_id
                self._next_id += 1
                self._tracks[tid] = {"centroid": centroid, "frames_lost": 0}
            results.append((tid, det))

        # ----- age unmatched tracks and prune expired ones ---------------
        for j, tid in enumerate(track_ids):
            if j not in assigned_trk:
                self._tracks[tid]["frames_lost"] += 1
                if self._tracks[tid]["frames_lost"] > self.max_frames_lost:
                    del self._tracks[tid]

        return results

    @property
    def num_active(self) -> int:
        """Number of currently active tracks."""
        return len(self._tracks)


# ---------------------------------------------------------------------------
# Drawing helpers
# ---------------------------------------------------------------------------
_KEYPOINT_RADIUS = 5
_BONE_THICKNESS = 2
_KEYPOINT_THICKNESS = -1  # filled


def draw_pose(frame: np.ndarray, landmarks, color: tuple) -> None:
    """Overlay skeleton connections and joint circles on *frame* (in-place).

    Args:
        frame:     BGR image (modified in-place).
        landmarks: ``NormalizedLandmarkList`` from MediaPipe.
        color:     BGR colour tuple for bones and joints.
    """
    h, w = frame.shape[:2]

    # Draw bones
    for start_idx, end_idx in mp_pose.POSE_CONNECTIONS:
        s = landmarks.landmark[start_idx]
        e = landmarks.landmark[end_idx]
        if s.visibility > 0.5 and e.visibility > 0.5:
            pt1 = (int(s.x * w), int(s.y * h))
            pt2 = (int(e.x * w), int(e.y * h))
            cv2.line(frame, pt1, pt2, color, _BONE_THICKNESS)

    # Draw joint circles (yellow core so keypoints are distinguishable)
    for lm in landmarks.landmark:
        if lm.visibility > 0.5:
            cx, cy = int(lm.x * w), int(lm.y * h)
            cv2.circle(frame, (cx, cy), _KEYPOINT_RADIUS, (0, 220, 220), _KEYPOINT_THICKNESS)
            cv2.circle(frame, (cx, cy), _KEYPOINT_RADIUS, color, 1)


def draw_track_id(frame: np.ndarray, landmarks, track_id: int, color: tuple) -> None:
    """Draw the track ID near the nose landmark (landmark 0)."""
    h, w = frame.shape[:2]
    nose = landmarks.landmark[0]
    if nose.visibility > 0.5:
        nx, ny = int(nose.x * w), int(nose.y * h)
        label = f"ID:{track_id}"
        cv2.putText(frame, label, (nx - 20, ny - 18),
                    cv2.FONT_HERSHEY_SIMPLEX, 0.65, (0, 0, 0), 3)
        cv2.putText(frame, label, (nx - 20, ny - 18),
                    cv2.FONT_HERSHEY_SIMPLEX, 0.65, color, 2)


# ---------------------------------------------------------------------------
# FPS counter
# ---------------------------------------------------------------------------
class FPSCounter:
    """Rolling-average FPS counter."""

    def __init__(self, window: int = 30):
        self._times: deque = deque(maxlen=window)
        self._last = time.perf_counter()

    def tick(self) -> float:
        now = time.perf_counter()
        self._times.append(now - self._last)
        self._last = now
        if not self._times:
            return 0.0
        return len(self._times) / sum(self._times)


# ---------------------------------------------------------------------------
# Argument parsing
# ---------------------------------------------------------------------------
def parse_args() -> argparse.Namespace:
    p = argparse.ArgumentParser(description="Real-time human pose tracking")
    p.add_argument("--camera", type=int, default=0,
                   help="Camera index (default: 0)")
    p.add_argument("--width", type=int, default=640,
                   help="Capture width in pixels (default: 640)")
    p.add_argument("--height", type=int, default=480,
                   help="Capture height in pixels (default: 480)")
    p.add_argument("--model-complexity", type=int, default=1, choices=[0, 1, 2],
                   help="MediaPipe model complexity 0/1/2 (default: 1)")
    p.add_argument("--min-detection", type=float, default=0.5,
                   help="Minimum detection confidence (default: 0.5)")
    p.add_argument("--min-tracking", type=float, default=0.5,
                   help="Minimum tracking confidence (default: 0.5)")
    p.add_argument("--max-distance", type=float, default=0.25,
                   help="Tracker max centroid distance in normalised coords "
                        "(default: 0.25)")
    p.add_argument("--max-lost", type=int, default=15,
                   help="Tracker max frames before track is removed (default: 15)")
    return p.parse_args()


# ---------------------------------------------------------------------------
# Main loop
# ---------------------------------------------------------------------------
def main() -> None:
    args = parse_args()

    cap = cv2.VideoCapture(args.camera)
    if not cap.isOpened():
        raise RuntimeError(
            f"Cannot open camera index {args.camera}. "
            "Try a different --camera value or check that the device is connected."
        )
    cap.set(cv2.CAP_PROP_FRAME_WIDTH, args.width)
    cap.set(cv2.CAP_PROP_FRAME_HEIGHT, args.height)

    tracker = PoseTracker(
        max_distance=args.max_distance,
        max_frames_lost=args.max_lost,
    )
    id_colors: dict[int, tuple] = {}
    fps_counter = FPSCounter(window=30)

    print("Pose Tracker started.  Press 'q' or ESC to quit.")

    with mp_pose.Pose(
        model_complexity=args.model_complexity,
        smooth_landmarks=True,
        enable_segmentation=False,
        min_detection_confidence=args.min_detection,
        min_tracking_confidence=args.min_tracking,
    ) as pose:

        while True:
            ret, frame = cap.read()
            if not ret:
                print("Warning: failed to grab frame — retrying …")
                time.sleep(0.05)
                continue

            fps = fps_counter.tick()

            # ---- pose estimation ----------------------------------------
            # MediaPipe expects RGB; mark as non-writeable for performance
            rgb = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
            rgb.flags.writeable = False
            results = pose.process(rgb)
            rgb.flags.writeable = True

            # ---- collect detections (single person for MediaPipe Pose) ---
            detections = []
            if results.pose_landmarks:
                detections.append(results.pose_landmarks)

            # ---- update tracker -----------------------------------------
            tracked = tracker.update(detections)

            # ---- render skeleton + IDs ----------------------------------
            for track_id, landmarks in tracked:
                if track_id not in id_colors:
                    id_colors[track_id] = _PALETTE[track_id % len(_PALETTE)]
                color = id_colors[track_id]
                draw_pose(frame, landmarks, color)
                draw_track_id(frame, landmarks, track_id, color)

            # ---- HUD (FPS + active tracks) ------------------------------
            cv2.putText(frame, f"FPS: {fps:.1f}", (10, 32),
                        cv2.FONT_HERSHEY_SIMPLEX, 1.0, (0, 0, 0), 4)
            cv2.putText(frame, f"FPS: {fps:.1f}", (10, 32),
                        cv2.FONT_HERSHEY_SIMPLEX, 1.0, (0, 230, 230), 2)
            cv2.putText(frame, f"Tracks: {tracker.num_active}", (10, 66),
                        cv2.FONT_HERSHEY_SIMPLEX, 0.75, (0, 0, 0), 3)
            cv2.putText(frame, f"Tracks: {tracker.num_active}", (10, 66),
                        cv2.FONT_HERSHEY_SIMPLEX, 0.75, (0, 230, 230), 2)

            cv2.imshow("Pose Tracker  (press Q / ESC to quit)", frame)

            key = cv2.waitKey(1) & 0xFF
            if key in (ord("q"), ord("Q"), 27):  # q, Q, or ESC
                break

    cap.release()
    cv2.destroyAllWindows()
    print("Pose Tracker stopped.")


if __name__ == "__main__":
    main()
