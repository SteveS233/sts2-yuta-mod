#!/usr/bin/env python3
"""从游戏 pck 复制资源文件到 mod 资源目录（用于复用官方素材占位）。

自动剥离资源自身的 uid 声明，避免与原版资源 UID 冲突。

用法:
  python copy_assets.py <输出目录> <源路径1> <目标路径1> [<源路径2> <目标路径2> ...]

例:
  python copy_assets.py assets \
    images/atlases/power_atlas.sprites/energy_next_turn_power.tres \
    images/atlases/power_atlas.sprites/rika_power.tres
"""
import importlib.util
import io
import contextlib
import os
import re
import sys

# 游戏 pck 路径：优先读环境变量 STS2_GAME_PCK（build.ps1 会按 -GameDir 设置），
# 没有时回退到默认安装位置。
GAME_PCK = os.environ.get(
    "STS2_GAME_PCK",
    r"D:\SteamLibrary\steamapps\common\Slay the Spire 2\SlayTheSpire2.pck")
_UID_RE = re.compile(r'uid="uid://[^"]*"\s*')


def main(out_dir, pairs):
    spec = importlib.util.spec_from_file_location(
        "pck", os.path.join(os.path.dirname(os.path.abspath(__file__)), "pck.py"))
    pck = importlib.util.module_from_spec(spec)
    with contextlib.redirect_stderr(io.StringIO()):
        spec.loader.exec_module(pck)
    f, base, entries = pck.read_index(GAME_PCK)
    index = {p: (o, s) for p, o, s in entries}

    for src, dst in pairs:
        if src not in index:
            raise SystemExit(f"pck 中不存在: {src}")
        o, s = index[src]
        f.seek(base + o)
        data = f.read(s)
        text = data.decode("utf-8")
        text = _UID_RE.sub("", text, count=1)
        target = os.path.join(out_dir, dst.replace("/", os.sep))
        os.makedirs(os.path.dirname(target), exist_ok=True)
        with open(target, "w", encoding="utf-8", newline="\n") as fh:
            fh.write(text)
        print(f"  {len(text):>7}B  {src}\n        -> {dst}")
    f.close()


if __name__ == "__main__":
    if len(sys.argv) < 4 or (len(sys.argv) - 2) % 2 != 0:
        raise SystemExit(__doc__)
    args = sys.argv[2:]
    main(sys.argv[1], list(zip(args[0::2], args[1::2])))
