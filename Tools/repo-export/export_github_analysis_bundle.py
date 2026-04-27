#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import subprocess
import sys
import zipfile
from collections import Counter
from dataclasses import dataclass
from datetime import datetime, timezone
from pathlib import PurePosixPath, Path


ROOT = Path(__file__).resolve().parents[2]

ROOT_INCLUDE_FILES = {
    ".gitattributes",
    ".gitignore",
    "AGENT.md",
    "TY_NEW.slnx",
}

TOP_LEVEL_INCLUDE_DIRS = {
    ".vscode",
    "Docs",
    "Packages",
    "ProjectSettings",
    "Tools",
}

FIRST_PARTY_ASSET_PREFIX = "Assets/_Game/"

TEXT_CODE_EXTENSIONS = {
    ".asmdef",
    ".asmref",
    ".cginc",
    ".compute",
    ".cs",
    ".css",
    ".hlsl",
    ".html",
    ".inputactions",
    ".json",
    ".md",
    ".py",
    ".rst",
    ".shader",
    ".sh",
    ".sql",
    ".toml",
    ".txt",
    ".uxml",
    ".uss",
    ".xml",
    ".yaml",
    ".yml",
}

UNITY_YAML_EXTENSIONS = {
    ".anim",
    ".asset",
    ".controller",
    ".mat",
    ".overridecontroller",
    ".playable",
    ".prefab",
    ".unity",
}

ALWAYS_EXCLUDED_PREFIXES = (
    ".git/",
    "Build/",
    "Builds/",
    "Library/",
    "Logs/",
    "MemoryCaptures/",
    "Obj/",
    "Temp/",
    "TestResults/",
    "UserSettings/",
    "cod_omnimovement_unity_starter_/",
    "coverage/",
    "obj/",
)

THIRD_PARTY_ASSET_PREFIXES = (
    "Assets/DoubleL/",
    "Assets/Free medieval weapons/",
    "Assets/ithappy/",
    "Assets/JC_LP_MedievalCharacters_LITE/",
    "Assets/Kevin Iglesias/",
    "Assets/MYFG-Weapon Pack Lite/",
    "Assets/Polytope Studio/",
)


@dataclass(frozen=True)
class Decision:
    include: bool
    reason: str


def run_git(*args: str, binary: bool = False) -> bytes | str:
    completed = subprocess.run(
        ["git", *args],
        cwd=ROOT,
        check=True,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
    )
    if binary:
        return completed.stdout
    return completed.stdout.decode("utf-8")


def list_tracked_files(revision: str) -> list[str]:
    output = run_git("ls-tree", "-r", "--name-only", revision)
    return [line for line in output.splitlines() if line]


def normalize_suffix(path: str) -> str:
    return PurePosixPath(path).suffix.lower()


def is_under(path: str, prefix: str) -> bool:
    return path == prefix or path.startswith(prefix.rstrip("/") + "/")


def decide_inclusion(path: str, include_unity_yaml: bool) -> Decision:
    if path.endswith("/"):
        return Decision(False, "directory")

    if path.endswith(".meta"):
        return Decision(False, "unity-meta")

    if path.startswith(ALWAYS_EXCLUDED_PREFIXES):
        return Decision(False, "generated-or-local")

    if path.startswith(THIRD_PARTY_ASSET_PREFIXES):
        return Decision(False, "third-party-asset-source")

    suffix = normalize_suffix(path)

    if path in ROOT_INCLUDE_FILES:
        return Decision(True, "root-config")

    for top_level_dir in TOP_LEVEL_INCLUDE_DIRS:
        if is_under(path, top_level_dir):
            if top_level_dir == "ProjectSettings":
                return Decision(True, "project-config")

            return Decision(True, "text-config-docs")

    if path.startswith(FIRST_PARTY_ASSET_PREFIX):
        if suffix in TEXT_CODE_EXTENSIONS:
            return Decision(True, "first-party-code")

        if include_unity_yaml and suffix in UNITY_YAML_EXTENSIONS:
            return Decision(True, "first-party-unity-yaml")

        return Decision(False, "first-party-asset")

    return Decision(False, "outside-analysis-scope")


def build_prompt(revision: str, zip_name: str, manifest_name: str, include_unity_yaml: bool) -> str:
    unity_yaml_note = (
        "本包已额外包含部分 Unity YAML 文本资产（如 prefab/scene/controller/asset）。"
        if include_unity_yaml
        else "本包默认排除了 Unity 内容资产，只保留代码、文档、配置和工具脚本。"
    )

    return f"""请基于我上传的仓库导出包做一次工程分析。

背景：
- 项目：Unity 第三人称动作 RPG
- 导出来源提交：`{revision}`
- 压缩包：`{zip_name}`
- 清单文件：`{manifest_name}`
- 说明：{unity_yaml_note}

请优先回答这些问题：
1. 当前代码结构的主干是什么？模块边界是否清晰？
2. 动作/战斗/状态机系统里，最容易产生技术债或手感问题的点在哪里？
3. 哪些代码是高风险耦合点？
4. 如果继续做“动作更顺、读招更清晰”，最值得优先改的 3 件事是什么？
5. 哪些文件值得我优先继续给你补充上下文？

输出要求：
- 先给高层结构总结
- 再列风险点，按严重度排序
- 最后给一个“下一轮建议清单”
"""


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Export a Git-tracked analysis bundle for GPT-5.4 Pro."
    )
    parser.add_argument(
        "--revision",
        default="HEAD",
        help="Git revision to export. Defaults to HEAD.",
    )
    parser.add_argument(
        "--output-dir",
        default="Exports/github-analysis",
        help="Directory where the bundle and sidecar files will be written.",
    )
    parser.add_argument(
        "--include-unity-yaml",
        action="store_true",
        help="Include first-party Unity YAML text assets such as .unity/.prefab/.controller/.asset.",
    )
    args = parser.parse_args()

    try:
        resolved_revision = run_git("rev-parse", args.revision).strip()
    except subprocess.CalledProcessError as exc:
        sys.stderr.write(exc.stderr.decode("utf-8"))
        return 1

    tracked_files = list_tracked_files(resolved_revision)
    output_dir = (ROOT / args.output_dir).resolve()
    output_dir.mkdir(parents=True, exist_ok=True)

    timestamp = datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%SZ")
    short_revision = resolved_revision[:12]
    bundle_stem = f"ty_new_gpt54pro_bundle_{short_revision}_{timestamp}"
    zip_path = output_dir / f"{bundle_stem}.zip"
    manifest_path = output_dir / f"{bundle_stem}.manifest.json"
    prompt_path = output_dir / f"{bundle_stem}.prompt.md"

    included_files: list[str] = []
    excluded_counter: Counter[str] = Counter()

    with zipfile.ZipFile(zip_path, "w", compression=zipfile.ZIP_DEFLATED) as archive:
        for path in tracked_files:
            decision = decide_inclusion(path, args.include_unity_yaml)
            if not decision.include:
                excluded_counter[decision.reason] += 1
                continue

            blob = run_git("show", f"{resolved_revision}:{path}", binary=True)
            archive.writestr(path, blob)
            included_files.append(path)

        manifest_payload = {
            "generated_at_utc": timestamp,
            "revision": resolved_revision,
            "archive_name": zip_path.name,
            "include_unity_yaml": args.include_unity_yaml,
            "included_file_count": len(included_files),
            "excluded_file_count": sum(excluded_counter.values()),
            "excluded_reasons": dict(sorted(excluded_counter.items())),
            "included_files": included_files,
        }
        archive.writestr(
            "_analysis/MANIFEST.json",
            json.dumps(manifest_payload, ensure_ascii=False, indent=2) + "\n",
        )
        archive.writestr(
            "_analysis/PROMPT_FOR_GPT54PRO.md",
            build_prompt(resolved_revision, zip_path.name, manifest_path.name, args.include_unity_yaml),
        )

    manifest_payload = {
        "generated_at_utc": timestamp,
        "revision": resolved_revision,
        "archive_path": str(zip_path.relative_to(ROOT)),
        "include_unity_yaml": args.include_unity_yaml,
        "included_file_count": len(included_files),
        "excluded_file_count": sum(excluded_counter.values()),
        "excluded_reasons": dict(sorted(excluded_counter.items())),
        "included_files": included_files,
    }
    manifest_path.write_text(
        json.dumps(manifest_payload, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
    )
    prompt_path.write_text(
        build_prompt(resolved_revision, zip_path.name, manifest_path.name, args.include_unity_yaml),
        encoding="utf-8",
    )

    zip_size_mb = zip_path.stat().st_size / (1024 * 1024)
    print(f"revision: {resolved_revision}")
    print(f"bundle: {zip_path.relative_to(ROOT)}")
    print(f"manifest: {manifest_path.relative_to(ROOT)}")
    print(f"prompt: {prompt_path.relative_to(ROOT)}")
    print(f"included_files: {len(included_files)}")
    print(f"excluded_files: {sum(excluded_counter.values())}")
    print(f"zip_size_mb: {zip_size_mb:.2f}")
    print("top_excluded_reasons:")
    for reason, count in excluded_counter.most_common():
        print(f"  - {reason}: {count}")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
