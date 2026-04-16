# 实时人体关键点 / 骨架跟踪示例

Real-time human body keypoint detection and skeleton tracking — Windows camera demo.

---

## 目录 / Contents

- [功能简介](#功能简介)
- [效果预览](#效果预览)
- [环境要求](#环境要求)
- [快速开始](#快速开始)
- [命令行参数](#命令行参数)
- [技术方案](#技术方案)
- [跟踪逻辑说明](#跟踪逻辑说明)
- [已知限制](#已知限制)
- [常见问题](#常见问题)

---

## 功能简介

| 特性 | 说明 |
|------|------|
| 关键点检测 | MediaPipe Pose — 33 个身体关键点（COCO-extended 格式） |
| 骨架渲染 | 关节圆点 + 骨骼连线叠加到摄像头画面 |
| FPS 显示 | 30 帧滚动平均值，实时叠加在左上角 |
| 跨帧 ID 稳定 | 贪心质心距离匹配 — 短暂遮挡/出画后恢复时保持同一 ID |
| 多人扩展 | 跟踪器与检测器解耦，可替换检测器以支持多人（见 [已知限制](#已知限制)） |

---

## 效果预览

```
┌──────────────────────────────────────┐
│ FPS: 28.3         Tracks: 1          │
│                                      │
│         ●─────●                      │
│        /       \     (骨架渲染示意)   │
│       ●         ●                    │
│      / \       / \                   │
│     ●   ●     ●   ●                  │
│                                      │
│           ID:0                       │
└──────────────────────────────────────┘
```

---

## 环境要求

| 项目 | 版本要求 |
|------|---------|
| 操作系统 | Windows 10 / 11 (x64)；亦可在 Linux/macOS 运行 |
| Python | 3.9 — 3.11（推荐 3.10） |
| 摄像头 | 任意 USB / 内置摄像头（DirectShow 可识别） |

> **注意** — Python 3.12 暂不完整支持 `mediapipe`（截至 2024 年末），建议使用 3.9–3.11。

---

## 快速开始

### 1. 安装 Python

从 [python.org](https://www.python.org/downloads/) 下载 Python 3.10，安装时勾选
**"Add Python to PATH"**。

### 2. 创建虚拟环境（推荐）

```powershell
cd Cs_Vision\pose_tracking

python -m venv .venv
.venv\Scripts\activate
```

### 3. 安装依赖

```powershell
pip install -r requirements.txt
```

> 首次安装约需下载 300 – 500 MB（MediaPipe 包含预训练模型，**无需单独下载模型文件**）。

### 4. 运行

```powershell
python pose_tracker.py
```

窗口标题为 **"Pose Tracker"**，按 **Q** 或 **ESC** 退出。

---

## 命令行参数

```
python pose_tracker.py [选项]

  --camera INT          摄像头索引，默认 0（第一个摄像头）
  --width  INT          采集宽度（像素），默认 640
  --height INT          采集高度（像素），默认 480
  --model-complexity INT  MediaPipe 模型复杂度 0/1/2，默认 1
                           0 = 最快，精度略低
                           1 = 平衡（推荐）
                           2 = 最精确，速度最慢
  --min-detection FLOAT 最低检测置信度，默认 0.5
  --min-tracking  FLOAT 最低跟踪置信度，默认 0.5
  --max-distance  FLOAT 跟踪器最大匹配距离（归一化坐标），默认 0.25
  --max-lost      INT   轨迹允许丢失帧数，默认 15
```

示例 — 使用第二个摄像头，高精度模型：

```powershell
python pose_tracker.py --camera 1 --model-complexity 2
```

---

## 技术方案

```
摄像头帧 (OpenCV VideoCapture)
        │
        ▼
  BGR→RGB 转换
        │
        ▼
  MediaPipe Pose  ──→  33 个 NormalizedLandmark (x, y, z, visibility)
        │
        ▼
  PoseTracker.update()
  ┌─────────────────────────────────────┐
  │  计算本帧各检测的质心                │
  │  构建距离矩阵 (检测 × 轨迹)         │
  │  贪心最近邻匹配（距离阈值过滤）      │
  │  匹配成功 → 更新轨迹质心 + 清零丢帧  │
  │  未匹配检测 → 创建新轨迹             │
  │  未匹配轨迹 → 丢帧计数+1，超限则删除│
  └─────────────────────────────────────┘
        │
        ▼
  draw_pose() / draw_track_id()
  在 BGR 帧上叠加骨骼 + ID 标签
        │
        ▼
  cv2.imshow()  +  FPS 显示
```

### 关键点连接（MediaPipe Pose，共 32 条骨骼连线）

MediaPipe Pose 遵循
[BlazePose GHUM 拓扑](https://google.github.io/mediapipe/solutions/pose.html)，
覆盖躯干、双臂、双腿、面部轮廓共 33 个关键点。

---

## 跟踪逻辑说明

`PoseTracker` 实现了一个**贪心质心距离匹配器**，算法步骤如下：

1. **质心计算** — 对每个检测的所有可见关键点（`visibility > 0.5`）求均值坐标，
   作为该人的代表位置（归一化到 [0, 1]）。

2. **距离矩阵** — 计算当前帧所有检测质心与历史轨迹质心之间的欧氏距离，
   形成 (检测数 × 轨迹数) 矩阵。

3. **贪心匹配** — 按距离从小到大排序所有 (检测, 轨迹) 对，依次分配，
   跳过已匹配的检测/轨迹，同时过滤掉距离超过 `max_distance` 的配对。

4. **轨迹管理**
   - 匹配成功：更新轨迹质心，丢帧计数归零。
   - 新检测未匹配：创建新轨迹，分配自增 ID。
   - 旧轨迹未匹配：丢帧计数 +1；超过 `max_frames_lost` 后删除。

> 该算法与匈牙利算法在单人场景下等价；多人场景下为近似最优解，
> 适合实时应用。如需精确多人多帧最优匹配，可将贪心部分替换为
> `scipy.optimize.linear_sum_assignment`。

---

## 已知限制

| 限制 | 说明 |
|------|------|
| **单人检测** | MediaPipe `Pose` 解决方案每帧只返回一个人的关键点。在多人场景中，模型会选取画面中最突出的一人进行估计，其余人被忽略。 |
| **扩展为多人** | 将检测部分替换为支持多人的模型（如 MMPose RTMPose、YOLOv8-Pose、AlphaPose），`PoseTracker.update()` 可直接接收多个 `NormalizedLandmarkList` 对象，无需修改跟踪逻辑。 |
| **CPU 性能** | 在无 GPU 的 CPU 上，模型复杂度 1 通常可达 15–30 FPS（取决于 CPU 型号）；若需更高帧率，使用 `--model-complexity 0`。 |
| **3D 坐标** | MediaPipe 提供相对深度（`z` 轴），但未校准为真实世界单位，目前仅使用 x/y 渲染。 |

---

## 常见问题

### Q: 运行报错 `Cannot open camera index 0`

**A:** 尝试以下步骤：
1. 确认摄像头已插入并在设备管理器中可见。
2. 尝试 `--camera 1` 或 `--camera 2`。
3. 检查其他应用（如 Teams、Zoom）是否占用摄像头。

---

### Q: `pip install mediapipe` 失败，提示 `No matching distribution`

**A:** MediaPipe 要求 Python ≤ 3.11（官方 wheel）。使用 `python --version` 确认版本，
如果是 3.12+，请安装 Python 3.10 或 3.11。

---

### Q: 画面卡顿，FPS 很低

**A:**
- 使用 `--model-complexity 0` 切换到轻量模型。
- 降低分辨率：`--width 320 --height 240`。
- 关闭后台高负载进程。

---

### Q: 骨架关键点抖动明显

**A:** MediaPipe Pose 默认开启 `smooth_landmarks=True`（卡尔曼滤波平滑），
已尽量减少抖动。若仍不满意，可适当提高 `--min-tracking` 至 0.7–0.8，
或在代码中增加时间窗口滤波。

---

### Q: 想在虚拟机 / WSL 里运行

**A:** 摄像头在 WSL2 下默认不可用（USB-IP 方案较复杂）。建议直接在 Windows
的 CMD / PowerShell / Windows Terminal 中运行本脚本。

---

### Q: 如何扩展为多人跟踪？

**A:** 将 `pose_tracker.py` 中的检测部分替换为多人姿态估计模型：

```python
# 示例：把 MediaPipe 单人检测替换为任意多人检测器
detections = my_multiperson_detector(frame)  # 返回 NormalizedLandmarkList 列表
tracked = tracker.update(detections)
```

`PoseTracker` 的接口已设计为接收任意长度的检测列表，无需其他改动。
