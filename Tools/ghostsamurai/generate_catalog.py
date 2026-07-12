#!/usr/bin/env python3
"""Generate a local-preview-only GhostSamurai clip catalog."""

from __future__ import annotations

import argparse
from collections import defaultdict
from dataclasses import dataclass
from datetime import datetime, timezone
import json
from pathlib import Path
import re
from typing import Any, Iterable


ASSET_ROOT = Path("Assets/GhostSamurai_Animset")
DEFAULT_OUTPUT = Path("Docs/GhostSamurai_Clip_Catalog.md")
DEFAULT_MANIFEST = Path("Tools/ghostsamurai/clip_mappings.json")
GENERATED_TIME_PATTERN = re.compile(r"^- 生成时间：`.*`$", re.MULTILINE)

SECTION_ORDER = [
    "katana/APose/Attack",
    "katana/APose/Defense",
    "katana/APose/Deflect",
    "katana/APose/Dodge",
    "katana/APose/Movement",
    "katana/APose/Hit",
    "katana/APose/Die",
    "katana/APose/Execution",
    "katana/APose/Crouch",
    "katana/Common",
    "katana/Common/CommonCrouch",
    "katana/Common/Unarm&Equip",
    "Bow/Attack",
    "Bow/Common",
    "Bow/Common/CommonCrouch",
    "Bow/Crouch",
    "Bow/Dodge",
    "Bow/Hit",
    "Bow/Die",
    "Bow/Movement",
]

DISPLAY_NAMES = {
    "katana/APose/Attack": "APose / Attack",
    "katana/APose/Defense": "APose / Defense",
    "katana/APose/Deflect": "APose / Deflect",
    "katana/APose/Dodge": "APose / Dodge",
    "katana/APose/Movement": "APose / Movement",
    "katana/APose/Hit": "APose / Hit",
    "katana/APose/Die": "APose / Die",
    "katana/APose/Execution": "APose / Execution",
    "katana/APose/Crouch": "APose / Crouch",
    "katana/Common": "Common / Base",
    "katana/Common/CommonCrouch": "Common / CommonCrouch",
    "katana/Common/Unarm&Equip": "Common / Unarm&Equip",
    "Bow/Attack": "Bow / Attack",
    "Bow/Common": "Bow / Common",
    "Bow/Common/CommonCrouch": "Bow / CommonCrouch",
    "Bow/Crouch": "Bow / Crouch",
    "Bow/Dodge": "Bow / Dodge",
    "Bow/Hit": "Bow / Hit",
    "Bow/Die": "Bow / Die",
    "Bow/Movement": "Bow / Movement",
}

@dataclass(frozen=True)
class Row:
    label: str
    total: int
    root: int
    inplace: int
    other: int


@dataclass(frozen=True)
class MappingEntry:
    action: str
    category: str
    candidates: tuple[str, ...]
    goal: str


@dataclass(frozen=True)
class ExecutionResearchEntry:
    action: str
    role: str
    match_label: str
    stem_regex: str
    lead_candidates: tuple[str, ...]
    preferred_usage: str
    goal: str


DISPLAY_NAMES["katana/APose"] = "APose / Base"


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--asset-root", type=Path, default=ASSET_ROOT)
    parser.add_argument("--output", type=Path, default=DEFAULT_OUTPUT)
    parser.add_argument("--manifest", type=Path, default=DEFAULT_MANIFEST)
    parser.add_argument(
        "--check",
        action="store_true",
        help="Verify that the current catalog markdown matches the manifest and scanned GhostSamurai package.",
    )
    return parser.parse_args()


def load_manifest(path: Path) -> dict[str, Any]:
    return json.loads(path.read_text(encoding="utf-8"))


def collect_files(asset_root: Path) -> list[Path]:
    return sorted(path for path in asset_root.rglob("*") if path.is_file() and path.suffix.lower() == ".fbx")


def resolve_variant(path: Path) -> str:
    lower_name = path.stem.lower()
    if "inplace" in lower_name:
        return "inplace"
    if "root" in lower_name:
        return "root"
    return "other"


def resolve_variant_from_name(name: str) -> str:
    lower_name = name.lower()
    if "inplace" in lower_name:
        return "Inplace"
    if "root" in lower_name:
        return "Root"
    return "Other"


def resolve_section(path: Path) -> str | None:
    parts = path.parts
    if "Animation" not in parts:
        return None

    idx = parts.index("Animation")
    rel_parts = list(parts[idx + 1 : -1])
    if not rel_parts:
        return None

    if rel_parts[0] == "katana":
        if len(rel_parts) >= 3 and rel_parts[1] == "APose":
            return "/".join(rel_parts[:3])
        if len(rel_parts) >= 3 and rel_parts[1] == "Common":
            if rel_parts[2] in {"Inplace", "Root"}:
                return "katana/Common"
            return "/".join(rel_parts[:3])
        return "/".join(rel_parts[: min(3, len(rel_parts))])

    if rel_parts[0] == "Bow":
        if len(rel_parts) >= 3 and rel_parts[1] == "Common" and rel_parts[2] == "CommonCrouch":
            return "Bow/Common/CommonCrouch"
        if len(rel_parts) >= 2:
            return "/".join(rel_parts[:2])
        return rel_parts[0]

    return "/".join(rel_parts[: min(3, len(rel_parts))])


def build_rows(files: Iterable[Path]) -> list[Row]:
    counts: dict[str, dict[str, int]] = defaultdict(lambda: {"root": 0, "inplace": 0, "other": 0})

    for path in files:
        section = resolve_section(path)
        if section is None:
            continue
        counts[section][resolve_variant(path)] += 1

    rows: list[Row] = []
    for section in SECTION_ORDER:
        variants = counts.get(section)
        if not variants:
            continue

        total = variants["root"] + variants["inplace"] + variants["other"]
        rows.append(
            Row(
                label=DISPLAY_NAMES.get(section, section),
                total=total,
                root=variants["root"],
                inplace=variants["inplace"],
                other=variants["other"],
            )
        )

    return rows


def branch_counts(files: Iterable[Path]) -> tuple[int, int, int]:
    katana = 0
    bow = 0
    other = 0
    for path in files:
        parts = path.parts
        if "Animation" not in parts:
            other += 1
            continue

        idx = parts.index("Animation")
        branch = parts[idx + 1] if idx + 1 < len(parts) else ""
        if branch == "katana":
            katana += 1
        elif branch == "Bow":
            bow += 1
        else:
            other += 1
    return katana, bow, other


def variant_counts(files: Iterable[Path]) -> tuple[int, int, int]:
    root = 0
    inplace = 0
    other = 0
    for path in files:
        variant = resolve_variant(path)
        if variant == "root":
            root += 1
        elif variant == "inplace":
            inplace += 1
        else:
            other += 1
    return root, inplace, other


def format_try_first_section(try_first_groups: list[dict[str, Any]]) -> list[str]:
    lines = [
        "## 3. 最值得先试接的 Clip",
        "",
        "这些条目不是“全部都接”，而是本地研究预览最值得优先验证的一线候选。",
        "",
        "| 分类 | 先试 Clip |",
        "|---|---|",
    ]

    for group in try_first_groups:
        clips = "<br>".join(f"`{clip}`" for clip in group["clips"])
        lines.append(f"| `{group['category']}` | {clips} |")

    return lines


def build_stem_index(files: Iterable[Path]) -> dict[str, Path]:
    index: dict[str, Path] = {}
    for path in files:
        index[path.stem] = path
    return index


def format_source_label(path: Path | None) -> str:
    if path is None:
        return "n/a"

    section = resolve_section(path)
    if section is None:
        return "n/a"

    return DISPLAY_NAMES.get(section, section.replace("/", " / "))


def load_mapping_sections(manifest: dict[str, Any]) -> tuple[tuple[str, tuple[MappingEntry, ...]], ...]:
    sections: list[tuple[str, tuple[MappingEntry, ...]]] = []
    for section in manifest["mapping_sections"]:
        entries = tuple(
            MappingEntry(
                action=entry["action"],
                category=entry["category"],
                candidates=tuple(entry["candidates"]),
                goal=entry["goal"],
            )
            for entry in section["entries"]
        )
        sections.append((section["title"], entries))

    return tuple(sections)


def load_execution_research_entries(manifest: dict[str, Any]) -> tuple[ExecutionResearchEntry, ...]:
    return tuple(
        ExecutionResearchEntry(
            action=entry["action"],
            role=entry["role"],
            match_label=entry["match_label"],
            stem_regex=entry["stem_regex"],
            lead_candidates=tuple(entry["lead_candidates"]),
            preferred_usage=entry["preferred_usage"],
            goal=entry["goal"],
        )
        for entry in manifest.get("execution_research", [])
    )


def format_mapping_section(files: list[Path], mapping_sections: tuple[tuple[str, tuple[MappingEntry, ...]], ...]) -> list[str]:
    stem_index = build_stem_index(files)
    lines = [
        "## 4. 动作映射覆盖",
        "",
        "下表把当前 local-preview 设计里真正要用的 GhostSamurai clip 收成同源证据，避免“清单、设计、生成链”继续漂移。",
        "",
    ]

    for title, entries in mapping_sections:
        lines.extend(
            [
                f"### {title}",
                "",
                "| 动作 | 分类 | 来源 | 首选 clip | 变体 | 备选 clip | 状态 | 目标 |",
                "|---|---|---|---|---|---|---|---|",
            ]
        )

        for entry in entries:
            resolved_paths = [stem_index.get(candidate) for candidate in entry.candidates]
            primary_index = next((i for i, path in enumerate(resolved_paths) if path is not None), None)
            primary_stem = entry.candidates[0] if primary_index is None else entry.candidates[primary_index]
            primary_path = None if primary_index is None else resolved_paths[primary_index]
            backup_names = [candidate for i, candidate in enumerate(entry.candidates) if i != primary_index]
            backup_cell = "<br>".join(f"`{name}`" for name in backup_names) if backup_names else "-"
            status = "ready" if primary_path is not None else "missing"

            lines.append(
                "| "
                f"`{entry.action}` | "
                f"`{entry.category}` | "
                f"{format_source_label(primary_path)} | "
                f"`{primary_stem}` | "
                f"`{resolve_variant_from_name(primary_stem)}` | "
                f"{backup_cell} | "
                f"`{status}` | "
                f"{entry.goal} |"
            )

        lines.append("")

    return lines


def collect_execution_family(files: Iterable[Path], stem_regex: str) -> tuple[list[Path], dict[str, int]]:
    pattern = re.compile(stem_regex)
    matched_paths: list[Path] = []
    counts = {"root": 0, "inplace": 0, "other": 0}

    for path in files:
        if resolve_section(path) != "katana/APose/Execution" or pattern.match(path.stem) is None:
            continue

        matched_paths.append(path)
        counts[resolve_variant(path)] += 1

    return matched_paths, counts


def format_execution_research_section(
    files: list[Path],
    execution_research_entries: tuple[ExecutionResearchEntry, ...],
) -> list[str]:
    if not execution_research_entries:
        return []

    lines = [
        "## 5. 处决研究分层",
        "",
        "把 `Execution / Executed / Ambush / Ambushed` 拆成攻击方与受体两侧，后续若真要做终结或背刺，本节就是挑 clip 的第一落点。",
        "",
        "| 研究块 | 角色面 | 命名族 | 总数 | Root | Inplace | Other | 先试 clip | 优先用法 | 研究目标 |",
        "|---|---|---|---:|---:|---:|---:|---|---|---|",
    ]

    execution_section_paths = [
        path
        for path in files
        if resolve_section(path) == "katana/APose/Execution"
    ]
    matched_stems: set[str] = set()

    for entry in execution_research_entries:
        matched_paths, counts = collect_execution_family(files, entry.stem_regex)
        matched_stems.update(path.stem for path in matched_paths)
        lead_candidates = "<br>".join(f"`{clip}`" for clip in entry.lead_candidates)

        lines.append(
            "| "
            f"`{entry.action}` | "
            f"{entry.role} | "
            f"`{entry.match_label}` | "
            f"{len(matched_paths)} | "
            f"{counts['root']} | "
            f"{counts['inplace']} | "
            f"{counts['other']} | "
            f"{lead_candidates} | "
            f"{entry.preferred_usage} | "
            f"{entry.goal} |"
        )

    misc_stems = sorted({path.stem for path in execution_section_paths if path.stem not in matched_stems})

    lines.extend(
        [
            "",
            "- `Other` 主要是同套 `Sample` 研究片段；它们先保留给双人配对观察，不作为首选接线候选。",
        ]
    )

    if misc_stems:
        misc_preview = "、".join(f"`{stem}`" for stem in misc_stems[:3])
        lines.append(
            f"- `Execution` 目录里还有 {len(misc_stems)} 个未纳入上述四组的辅助 clip（当前是 {misc_preview}），先不放进研究锚点。"
        )

    lines.append("")
    return lines


def resolve_generated_timestamp(existing_markdown: str | None) -> str:
    if existing_markdown:
        match = GENERATED_TIME_PATTERN.search(existing_markdown)
        if match is not None:
            line = match.group(0)
            return line.removeprefix("- 生成时间：`").removesuffix("`")

    return datetime.now(timezone.utc).strftime("%Y-%m-%d %H:%M UTC")


def normalize_for_check(markdown: str) -> str:
    normalized = GENERATED_TIME_PATTERN.sub("- 生成时间：`<normalized>`", markdown)
    return normalized.rstrip() + "\n"


def build_markdown(
    asset_root: Path,
    files: list[Path],
    manifest: dict[str, Any],
    generated_timestamp: str | None = None,
) -> str:
    timestamp = generated_timestamp or datetime.now(timezone.utc).strftime("%Y-%m-%d %H:%M UTC")
    if not files:
        return "\n".join(
            [
                "# GhostSamurai 动画清单",
                "",
                "> local research preview only。`Assets/GhostSamurai_Animset/` 缺失时，本清单只保留占位说明，不作为发布基线。",
                "",
                f"- 生成时间：`{timestamp}`",
                f"- 扫描目录：`{asset_root.as_posix()}`",
                "- 结果：未找到任何 FBX。",
                "",
            ]
        )

    katana_count, bow_count, other_branch_count = branch_counts(files)
    root_count, inplace_count, other_variant_count = variant_counts(files)
    rows = build_rows(files)
    mapping_sections = load_mapping_sections(manifest)
    execution_research_entries = load_execution_research_entries(manifest)

    lines = [
        "# GhostSamurai 动画清单",
        "",
        "> local research preview only。`Assets/GhostSamurai_Animset/` 只用于本机研究预览，不能作为公开仓库发布基线。",
        "",
        "## 1. 总览",
        "",
        f"- 生成时间：`{timestamp}`",
        f"- 扫描目录：`{asset_root.as_posix()}`",
        f"- FBX 总数：`{len(files)}`",
        f"- `katana`：`{katana_count}`",
        f"- `Bow`：`{bow_count}`",
        f"- 其它分支 / 模型 / 场景：`{other_branch_count}`",
        f"- `Root`：`{root_count}`",
        f"- `Inplace`：`{inplace_count}`",
        f"- `Other / Pose / Sample / Unmarked`：`{other_variant_count}`",
        "",
        "## 2. 分类统计",
        "",
        "| 分组 | 总数 | Root | Inplace | Other |",
        "|---|---:|---:|---:|---:|",
    ]

    for row in rows:
        lines.append(f"| {row.label} | {row.total} | {row.root} | {row.inplace} | {row.other} |")

    lines.extend(
        [
            "",
            *format_try_first_section(manifest["try_first"]),
            "",
            *format_mapping_section(files, mapping_sections),
            *format_execution_research_section(files, execution_research_entries),
            "## 6. 备注",
            "",
            "- `Other` 主要是 pose、sample 或未显式带 `Root` / `Inplace` 后缀的 FBX。",
            "- 若后续新增研究映射，先更新 `Docs/GhostSamurai_Action_Integration_Plan.md` 的设计表，再决定是否把对应 clip 接进 local preview 生成链。",
            "",
        ]
    )
    return "\n".join(lines)


def main() -> int:
    args = parse_args()
    manifest = load_manifest(args.manifest)
    files = collect_files(args.asset_root)
    existing_markdown = args.output.read_text(encoding="utf-8") if args.output.exists() else None

    if args.check:
        markdown = build_markdown(
            args.asset_root,
            files,
            manifest,
            generated_timestamp=resolve_generated_timestamp(existing_markdown),
        )

        if normalize_for_check(markdown) != normalize_for_check(existing_markdown or ""):
            print(f"GhostSamurai catalog is out of date: {args.output}")
            return 1

        print(f"GhostSamurai catalog is up to date: {args.output}")
        return 0

    markdown = build_markdown(args.asset_root, files, manifest)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(markdown, encoding="utf-8")
    print(f"Wrote {args.output}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
