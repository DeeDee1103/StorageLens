import sys
from pathlib import Path

import cv2


VIDEOS_DIR = Path(r"a:\source\_VSCode\StorageLens\videos")
DEFAULT_VIDEO_PATH = VIDEOS_DIR / "19700121_0747_69be0d06c8b48191bc4e2e827f822d9f.mp4"
EXTRACTED_FRAMES_DIR = VIDEOS_DIR / "extracted-frames"


def resolve_video_path() -> Path:
    if len(sys.argv) > 1:
        return Path(sys.argv[1]).resolve()

    return DEFAULT_VIDEO_PATH


def main() -> None:
    video_path = resolve_video_path()
    output_dir = EXTRACTED_FRAMES_DIR / video_path.stem
    summary_path = EXTRACTED_FRAMES_DIR / f"{video_path.stem}-summary.txt"

    output_dir.mkdir(parents=True, exist_ok=True)

    capture = cv2.VideoCapture(str(video_path))
    if not capture.isOpened():
        raise SystemExit(f"Unable to open video: {video_path}")

    fps = capture.get(cv2.CAP_PROP_FPS) or 0.0
    frame_count = int(capture.get(cv2.CAP_PROP_FRAME_COUNT) or 0)
    interval_seconds = 1.0
    interval_frames = max(int(round(fps * interval_seconds)), 1) if fps > 0 else 30

    saved = 0
    index = 0

    while True:
        ok, frame = capture.read()
        if not ok:
            break

        if index == 0 or index % interval_frames == 0:
            timestamp_seconds = index / fps if fps > 0 else saved * interval_seconds
            filename = f"frame_{saved:03d}_{timestamp_seconds:06.2f}s.png"
            cv2.imwrite(str(output_dir / filename), frame)
            saved += 1

        index += 1

    capture.release()

    summary_path.write_text(
        "\n".join(
            [
                f"video={video_path}",
                f"output_dir={output_dir}",
                f"fps={fps}",
                f"frame_count={frame_count}",
                f"interval_frames={interval_frames}",
                f"saved={saved}",
            ]
        )
        + "\n",
        encoding="utf-8",
    )


if __name__ == "__main__":
    main()