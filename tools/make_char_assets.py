#!/usr/bin/env python3
"""为 mod 角色生成「资源直通」文件：复制官方角色的资源到新角色的路径上。

背景:
  CharacterModel 的资源路径全部由 Id.Entry 拼成（如 res://scenes/creature_visuals/<id>.tscn），
  其中大部分属性不可覆写，而 PreloadManager 会遍历每个角色的全部资源路径做预加载 —— 缺一个都可能启动失败。
  官方资源在 pck 里是「.import（内含 [remap] 指向 .godot/imported/*.ctex）+ .ctex」的结构，
  因此可以纯文件操作复制 + 重写映射，无需 Godot 编辑器。

必须处理的两个坑:
  1. 复制出来的资源带着原版的 uid，会与原版资源 UID 冲突，可能连带破坏原版角色 —— 一律剥离。
  2. 贴图的 .import 可能引用多个变体（path / path.bptc / path.s3tc ...），需逐个复制并重写。

用法:
  python make_char_assets.py <源角色id> <目标角色id> <输出目录>
  例: python make_char_assets.py ironclad yiggu E:\\sljtmod\\乙骨\\assets
"""
import importlib.util
import io
import contextlib
import os
import re
import sys

GAME_PCK = r"D:\SteamLibrary\steamapps\common\Slay the Spire 2\SlayTheSpire2.pck"

# 场景与材质：源路径 -> 目标路径（以角色 id 为模板）
SCENE_TARGETS = [
    "scenes/creature_visuals/{id}.tscn",
    "scenes/combat/energy_counters/{id}_energy_counter.tscn",
    "scenes/merchant/characters/{id}_merchant.tscn",
    "scenes/rest_site/characters/{id}_rest_site.tscn",
    "scenes/screens/char_select/char_select_bg_{id}.tscn",
    "scenes/vfx/card_trail_{id}.tscn",
    "materials/transitions/{id}_transition_mat.tres",
]

# 贴图：源 png 路径 -> 目标 png 路径（模板相同，只是 id 不同）
TEXTURE_TARGETS = [
    ("images/ui/top_panel/character_icon_{id}.png",) * 2,
    ("images/ui/top_panel/character_icon_{id}_outline.png",) * 2,
    # 以下 4 个仅多人模式使用（举手/石头/布/剪刀手势）
    ("images/ui/hands/multiplayer_hand_{id}_point.png",) * 2,
    ("images/ui/hands/multiplayer_hand_{id}_rock.png",) * 2,
    ("images/ui/hands/multiplayer_hand_{id}_paper.png",) * 2,
    ("images/ui/hands/multiplayer_hand_{id}_scissors.png",) * 2,
]

_UID_RE = re.compile(r'uid="uid://[^"]*"\s*')


def load_pck_module():
    spec = importlib.util.spec_from_file_location(
        "pck", os.path.join(os.path.dirname(os.path.abspath(__file__)), "pck.py"))
    mod = importlib.util.module_from_spec(spec)
    with contextlib.redirect_stderr(io.StringIO()):
        spec.loader.exec_module(mod)
    return mod


def strip_uid(text):
    """去掉资源自身的 uid 声明，避免与原版资源 UID 冲突。"""
    return _UID_RE.sub("", text, count=1)


def main(src_id, dst_id, out_dir):
    pck = load_pck_module()
    f, base, entries = pck.read_index(GAME_PCK)
    index = {p: (o, s) for p, o, s in entries}

    def read(path):
        if path not in index:
            raise SystemExit(f"pck 中不存在: {path}")
        o, s = index[path]
        f.seek(base + o)
        return f.read(s)

    def write(rel, data):
        dst = os.path.join(out_dir, rel.replace("/", os.sep))
        os.makedirs(os.path.dirname(dst), exist_ok=True)
        with open(dst, "wb") as fh:
            fh.write(data)
        print(f"  {len(data):>9}  {rel}")

    made = 0

    # --- 场景 / 材质 ---
    print("场景与材质:")
    for tpl in SCENE_TARGETS:
        src = tpl.format(id=src_id)
        dst = tpl.format(id=dst_id)
        text = read(src).decode("utf-8")
        write(dst, strip_uid(text).encode("utf-8"))
        made += 1

    # --- 贴图：复制 .import 及其引用的所有 .ctex ---
    print("贴图:")
    for src_tpl, dst_tpl in TEXTURE_TARGETS:
        src_png = src_tpl.format(id=src_id)
        dst_png = dst_tpl.format(id=dst_id)
        imp = read(src_png + ".import").decode("utf-8")

        # 逐个变体复制 .ctex，并把路径改写为不会与原版冲突的新路径
        def repl(m):
            attr, ctex_src = m.group(1), m.group(2)
            stamp = ctex_src.rsplit("/", 1)[-1].replace(".ctex", "")
            ctex_dst = f"res://.godot/imported/{dst_id}_{stamp}.ctex"
            write(ctex_dst.replace("res://", ""), read(ctex_src.replace("res://", "")))
            return f'{attr}="{ctex_dst}"'

        imp = re.sub(r'(path[\w.]*)="res://([^"]+\.ctex)"',
                     lambda m: repl(m), imp)
        write(dst_png + ".import", strip_uid(imp).encode("utf-8"))
        made += 1

    f.close()
    print(f"\n共生成 {made} 个资源文件 -> {out_dir}")


if __name__ == "__main__":
    if len(sys.argv) != 4:
        raise SystemExit(__doc__)
    main(sys.argv[1], sys.argv[2], sys.argv[3])
