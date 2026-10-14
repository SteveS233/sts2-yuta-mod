#!/usr/bin/env python3
"""扫描卡牌源码，列出指定卡池里每张牌的类型与稀有度。

用于挑选符合「商店卡位需求」的占位卡：
MerchantInventory 固定要 2 攻击 + 2 技能 + 1 能力，且都会过滤掉 Basic 稀有度。

用法:
  python scan_card_pool.py <卡池源码路径> <卡牌源码目录>
"""
import os
import re
import sys
from collections import defaultdict

CTOR_RE = re.compile(
    r"base\(\s*\d+\s*,\s*CardType\.(\w+)\s*,\s*CardRarity\.(\w+)\s*,\s*TargetType\.(\w+)")
CARD_RE = re.compile(r"ModelDb\.Card<(\w+)>\(\)")


def main(pool_path, cards_dir):
    pool_src = open(pool_path, encoding="utf-8").read()
    names = CARD_RE.findall(pool_src)
    print(f"# 卡池 {os.path.basename(pool_path)} 共 {len(names)} 张\n")

    table = defaultdict(list)
    for name in names:
        path = os.path.join(cards_dir, name + ".cs")
        if not os.path.exists(path):
            table[("?", "文件缺失")].append(name)
            continue
        m = CTOR_RE.search(open(path, encoding="utf-8").read())
        if not m:
            table[("?", "无法解析")].append(name)
            continue
        ctype, rarity, _target = m.groups()
        table[(ctype, rarity)].append(name)

    for (ctype, rarity) in sorted(table):
        items = sorted(table[(ctype, rarity)])
        mark = "" if rarity == "Basic" else "   <-- 可进商店"
        print(f"== {ctype} / {rarity} ({len(items)}){mark}")
        print("   " + ", ".join(items))
        print()


if __name__ == "__main__":
    if len(sys.argv) != 3:
        raise SystemExit(__doc__)
    main(sys.argv[1], sys.argv[2])
