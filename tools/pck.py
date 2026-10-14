#!/usr/bin/env python3
"""Godot 4 .pck 读取器（pack format v2）。

用途：从《杀戮尖塔 2》本体或已装 mod 的 .pck 中列出/提取资源，
      以获取权威的本地化表格式与 mod 资源布局。

用法:
  python pck.py list <pck路径> [正则]
  python pck.py extract <pck路径> <内部路径> <输出文件>
"""
import io
import re
import struct
import sys
from pathlib import Path

MAGIC = 0x43504447  # "GDPC"


def read_index(pck_path):
    """返回 (file_base, [(path, offset, size)])。"""
    f = open(pck_path, "rb")
    header = f.read(4)
    if len(header) < 4:
        raise SystemExit("文件太小，不是有效的 pck")
    (magic,) = struct.unpack("<I", header)
    if magic != MAGIC:
        raise SystemExit(f"magic 不匹配: 0x{magic:08X}，期望 GDPC")

    (pack_format,) = struct.unpack("<I", f.read(4))
    ver_major, ver_minor, ver_patch = struct.unpack("<III", f.read(12))
    (pack_flags,) = struct.unpack("<I", f.read(4))
    (file_base,) = struct.unpack("<Q", f.read(8))

    # pack_format >= 3 把目录索引移到了文件末尾，头部此处是 u64 目录偏移。
    dir_offset = None
    if pack_format >= 3:
        (dir_offset,) = struct.unpack("<Q", f.read(8))
    f.read(16 * 4)  # reserved
    (file_count,) = struct.unpack("<I", f.read(4))

    if dir_offset:
        f.seek(dir_offset)
        (file_count,) = struct.unpack("<I", f.read(4))

    entries = []
    for _ in range(file_count):
        (path_len,) = struct.unpack("<I", f.read(4))
        raw = f.read(path_len)
        pad = (4 - (path_len % 4)) % 4
        if pad:
            f.read(pad)
        offset, size = struct.unpack("<QQ", f.read(16))
        f.read(16)  # md5
        if pack_format >= 2:
            f.read(4)  # per-file flags
        # path_len 按 4 字节对齐存储，路径尾部可能带 NUL 填充。
        entries.append((raw.decode("utf-8", "replace").rstrip("\x00"), offset, size))

    return f, file_base, entries


def cmd_list(pck_path, pattern=None):
    f, _base, entries = read_index(pck_path)
    rx = re.compile(pattern) if pattern else None
    shown = 0
    for path, offset, size in entries:
        if rx and not rx.search(path):
            continue
        print(f"{size:>12}  {path}")
        shown += 1
    print(f"# 匹配 {shown} / 总计 {len(entries)}", file=sys.stderr)
    f.close()


def cmd_extract(pck_path, inner, out_path):
    f, base, entries = read_index(pck_path)
    target = inner.lstrip("/")
    for path, offset, size in entries:
        if path == target:
            f.seek(base + offset)
            data = f.read(size)
            Path(out_path).parent.mkdir(parents=True, exist_ok=True)
            Path(out_path).write_bytes(data)
            print(f"已提取 {size} 字节 -> {out_path}", file=sys.stderr)
            f.close()
            return
    f.close()
    raise SystemExit(f"未找到内部路径: {inner}")


if __name__ == "__main__":
    if len(sys.argv) < 3:
        raise SystemExit(__doc__)
    cmd = sys.argv[1]
    if cmd == "list":
        cmd_list(sys.argv[2], sys.argv[3] if len(sys.argv) > 3 else None)
    elif cmd == "extract":
        cmd_extract(sys.argv[2], sys.argv[3], sys.argv[4])
    else:
        raise SystemExit(__doc__)
