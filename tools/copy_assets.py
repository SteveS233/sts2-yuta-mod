#!/usr/bin/env python3
"""从游戏 pck 复制资源文件到 mod 资源目录（用于复用官方素材占位）。

支持两类源文件：
  · 文本资源（.tres / .tscn 等）：原样复制，剥离资源自身的 uid 声明，避免与原版资源 UID 冲突；
  · 贴图（.png）：pck 里只有「.png.import（内含 [remap] 指向 .godot/imported/*.ctex）+ .ctex」，
    因此复制 .import 并把其中每个 .ctex 变体复制到新路径、改写引用，同样剥离 uid。
    做法与 make_char_assets.py 一致。

用法:
  python copy_assets.py <输出目录> <源路径1> <目标路径1> [<源路径2> <目标路径2> ...]

例（能力图标：游戏按 res://images/powers/<能力id小写>.png 兜底查找，见 AtlasResourceLoader）:
  python copy_assets.py assets \
    images/powers/thorns_power.png \
    images/powers/autonomous_guard_power.png
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
_CTEX_RE = re.compile(r'(path[\w.]*)="res://([^"]+\.ctex)"')


def main(out_dir, pairs):
    spec = importlib.util.spec_from_file_location(
        "pck", os.path.join(os.path.dirname(os.path.abspath(__file__)), "pck.py"))
    pck = importlib.util.module_from_spec(spec)
    with contextlib.redirect_stderr(io.StringIO()):
        spec.loader.exec_module(pck)
    f, base, entries = pck.read_index(GAME_PCK)
    index = {p: (o, s) for p, o, s in entries}

    def read(path):
        if path not in index:
            raise SystemExit(f"pck 中不存在: {path}")
        o, s = index[path]
        f.seek(base + o)
        return f.read(s)

    def write(rel, data):
        target = os.path.join(out_dir, rel.replace("/", os.sep))
        os.makedirs(os.path.dirname(target), exist_ok=True)
        with open(target, "wb") as fh:
            fh.write(data)

    for src, dst in pairs:
        if src.endswith(".png"):
            # 贴图：复制 .import，并把它引用的每个 .ctex 变体复制到不会与原版冲突的新路径
            stem = os.path.splitext(os.path.basename(dst))[0]

            def repl(m):
                attr, ctex_src = m.group(1), m.group(2)
                stamp = ctex_src.rsplit("/", 1)[-1].replace(".ctex", "")
                ctex_dst = f".godot/imported/{stem}_{stamp}.ctex"
                write(ctex_dst, read(ctex_src))
                return f'{attr}="res://{ctex_dst}"'

            imp = _CTEX_RE.sub(repl, read(src + ".import").decode("utf-8"))
            imp = _UID_RE.sub("", imp, count=1)
            write(dst + ".import", imp.encode("utf-8"))
            print(f"  [贴图]  {src}\n        -> {dst}")
        else:
            text = _UID_RE.sub("", read(src).decode("utf-8"), count=1)
            write(dst, text.encode("utf-8"))
            print(f"  {len(text):>7}B  {src}\n        -> {dst}")
    f.close()


if __name__ == "__main__":
    if len(sys.argv) < 4 or (len(sys.argv) - 2) % 2 != 0:
        raise SystemExit(__doc__)
    args = sys.argv[2:]
    main(sys.argv[1], list(zip(args[0::2], args[1::2])))
