#!/usr/bin/env python3
"""把目录打包成 Godot 4 pack_format v3 的 .pck。

格式依据 Godot 4.5 的 PackedSourcePCK::try_open_pack 实现：
  header: magic(u32) version(u32) ver_major/minor/patch(3xu32) flags(u32)
          file_base(u64) dir_offset(u64) reserved(16xu32)
  V3 的目录索引位于 dir_offset，不在头部；
  目录项: path_len(u32，含 4 字节对齐填充) path(pad 个 NUL) offset(u64) size(u64) md5(16) flags(u32)
  文件数据的 offset 相对于 file_base。

用法:
  python pack_pck.py <源目录> <输出.pck>
源目录下所有文件按相对路径写入 pck。
"""
import hashlib
import struct
import sys
from pathlib import Path

MAGIC = b"GDPC"
PACK_FORMAT = 3
VER = (4, 5, 1)
PACK_REL_FILEBASE = 0x2
HEADER_SIZE = 104          # magic..reserved
PKC_PADDING = 16


def align(n, a=PKC_PADDING):
    return (n + a - 1) // a * a


def pack(src_dir, out_path):
    src = Path(src_dir)
    files = sorted(p for p in src.rglob("*") if p.is_file())
    if not files:
        raise SystemExit(f"源目录没有文件: {src}")

    file_base = align(HEADER_SIZE)

    # 1) 数据区，逐文件 16 字节对齐
    blob = bytearray()
    entries = []
    for p in files:
        inner = p.relative_to(src).as_posix()
        data = p.read_bytes()
        ofs = len(blob)                      # 相对 file_base
        blob += data
        entries.append((inner, ofs, len(data), hashlib.md5(data).digest()))
        pad = align(len(blob)) - len(blob)
        blob += b"\x00" * pad

    dir_offset = file_base + len(blob)
    dir_offset = align(dir_offset)
    data_pad = dir_offset - (file_base + len(blob))

    # 2) 目录区
    directory = bytearray()
    directory += struct.pack("<I", len(entries))
    for inner, ofs, size, md5 in entries:
        raw = inner.encode("utf-8")
        pad = (4 - (len(raw) % 4)) % 4
        directory += struct.pack("<I", len(raw) + pad)   # 注意: 长度包含填充
        directory += raw + b"\x00" * pad
        directory += struct.pack("<QQ", ofs, size)
        directory += md5
        directory += struct.pack("<I", 0)               # flags

    # 3) 头部
    header = bytearray()
    header += MAGIC
    header += struct.pack("<I", PACK_FORMAT)
    header += struct.pack("<III", *VER)
    header += struct.pack("<I", PACK_REL_FILEBASE)
    header += struct.pack("<Q", file_base)
    header += struct.pack("<Q", dir_offset)
    header += b"\x00" * (16 * 4)                        # reserved
    assert len(header) == HEADER_SIZE, len(header)

    out = Path(out_path)
    out.parent.mkdir(parents=True, exist_ok=True)
    with open(out, "wb") as f:
        f.write(header)
        f.write(b"\x00" * (file_base - HEADER_SIZE))
        f.write(blob)
        f.write(b"\x00" * data_pad)
        f.write(directory)

    total = out.stat().st_size
    print(f"已生成 {out} 文件数={len(entries)} file_base={file_base} "
          f"dir_offset={dir_offset} 大小={total}")
    for inner, ofs, size, _ in entries:
        print(f"   {size:>8}  {inner}")


if __name__ == "__main__":
    if len(sys.argv) != 3:
        raise SystemExit(__doc__)
    pack(sys.argv[1], sys.argv[2])
