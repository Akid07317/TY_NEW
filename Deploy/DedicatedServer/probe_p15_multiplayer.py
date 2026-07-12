#!/usr/bin/env python3
"""Run a P1.5/P3/P3.5/P4 NGO/UTP multiplayer probe against TY_NEW server health."""

from __future__ import annotations

import argparse
import math
import socket
import subprocess
import sys
import time
from pathlib import Path


DEFAULT_SERVER_BIN = Path(
    "Builds/DedicatedServer/MacLocal/TYServer.app/Contents/MacOS/TY_NEW"
)
DEFAULT_CLIENT_BIN = Path("Builds/ReleaseCandidate/Mac/TY_NEW.app/Contents/MacOS/TY_NEW")


class ProbeError(RuntimeError):
    pass


def parse_health_line(line: str) -> dict[str, str]:
    values: dict[str, str] = {}
    for token in line.strip().split():
        if "=" not in token:
            continue

        key, value = token.split("=", 1)
        values[key] = value

    return values


def read_health(host: str, port: int, timeout: float) -> tuple[str, dict[str, str]]:
    with socket.create_connection((host, port), timeout=timeout) as sock:
        sock.settimeout(timeout)
        chunks: list[bytes] = []

        while True:
            chunk = sock.recv(4096)
            if not chunk:
                break

            chunks.append(chunk)
            if b"\n" in chunk:
                break

    line = b"".join(chunks).decode("utf-8", errors="replace").strip()
    if not line:
        raise ProbeError("health response was empty")

    return line, parse_health_line(line)


def wait_for_health(
    host: str,
    port: int,
    timeout: float,
    socket_timeout: float,
    label: str,
    predicate,
) -> tuple[str, dict[str, str]]:
    deadline = time.monotonic() + timeout
    last_line = ""
    last_error: Exception | None = None

    while time.monotonic() < deadline:
        try:
            line, values = read_health(host, port, socket_timeout)
            last_line = line
            last_error = None
            if predicate(values):
                return line, values
        except Exception as exc:
            last_error = exc

        time.sleep(0.5)

    details = f"last_health={last_line!r}" if last_line else f"last_error={last_error!r}"
    raise ProbeError(f"timed out waiting for {label}: {details}")


def require_path(path: Path, label: str) -> Path:
    resolved = path.expanduser()
    if not resolved.exists():
        raise ProbeError(f"missing {label}: {resolved}")

    return resolved


def ensure_log_parent(path: Path) -> Path:
    resolved = path.expanduser()
    resolved.parent.mkdir(parents=True, exist_ok=True)
    return resolved


def start_process(name: str, command: list[str], log_path: Path) -> subprocess.Popen:
    log_file = open(log_path, "ab")
    try:
        process = subprocess.Popen(
            command,
            stdout=log_file,
            stderr=subprocess.STDOUT,
            stdin=subprocess.DEVNULL,
        )
    except Exception:
        log_file.close()
        raise

    process._ty_new_log_file = log_file  # type: ignore[attr-defined]
    print(f"started {name}: pid={process.pid} log={log_path}")
    return process


def stop_process(process: subprocess.Popen | None, name: str, timeout: float) -> None:
    if process is None:
        return

    if process.poll() is None:
        process.terminate()
        try:
            process.wait(timeout=timeout)
        except subprocess.TimeoutExpired:
            process.kill()
            process.wait(timeout=timeout)

    log_file = getattr(process, "_ty_new_log_file", None)
    if log_file is not None:
        log_file.close()

    print(f"stopped {name}: returncode={process.returncode}")


def wait_process(process: subprocess.Popen, name: str, timeout: float) -> int:
    try:
        return process.wait(timeout=timeout)
    except subprocess.TimeoutExpired as exc:
        raise ProbeError(f"{name} did not exit within {timeout:.0f}s") from exc


def int_value(values: dict[str, str], key: str) -> int:
    try:
        return int(values.get(key, "0"))
    except ValueError:
        return 0


def bool_value(values: dict[str, str], key: str) -> bool:
    return values.get(key, "").lower() == "true"


def parse_smoke_records(log_path: Path) -> list[dict[str, str]]:
    marker = "[MultiplayerSmoke]"
    records: list[dict[str, str]] = []

    if not log_path.exists():
        return records

    for line in log_path.read_text(encoding="utf-8", errors="replace").splitlines():
        marker_index = line.find(marker)
        if marker_index < 0:
            continue

        payload = line[marker_index + len(marker):].strip()
        values = parse_health_line(payload)
        values["_line"] = line.strip()
        records.append(values)

    return records


def require_smoke_visibility(
    client1_log: Path,
    client2_log: Path,
    require_despawn: bool,
    require_remote_movement: bool,
    require_health_sync: bool,
    require_formal_attack_sync: bool,
    require_formal_hit_sync: bool,
    require_death_sync: bool,
    require_formal_death_sync: bool,
    min_remote_move_distance: float,
    min_remote_health_drop: int,
) -> tuple[
    dict[str, str],
    dict[str, str],
    dict[str, str] | None,
    tuple[float, dict[str, str], dict[str, str]] | None,
    tuple[int, int, int, dict[str, str], dict[str, str]] | None,
    tuple[bool, bool, dict[str, str], dict[str, str]] | None,
    tuple[bool, bool, dict[str, str], dict[str, str]] | None,
    tuple[bool, bool, dict[str, str], dict[str, str]] | None,
    tuple[bool, bool, dict[str, str], dict[str, str]] | None,
]:
    client1_records = parse_smoke_records(client1_log)
    client2_records = parse_smoke_records(client2_log)
    client1_spawn = first_visible_remote(client1_records)
    client2_spawn = first_visible_remote(client2_records)

    if client1_spawn is None:
        raise ProbeError(f"client1 never observed a remote avatar in {client1_log}")

    if client2_spawn is None:
        raise ProbeError(f"client2 never observed a remote avatar in {client2_log}")

    client1_despawn = None
    if require_despawn:
        client1_despawn = first_remote_despawn_after(client1_records, client1_spawn)
        if client1_despawn is None:
            raise ProbeError(f"client1 never observed remote avatar despawn in {client1_log}")

    client2_remote_move = None
    if require_remote_movement:
        client2_remote_move = first_remote_position_change(client2_records, min_remote_move_distance)
        if client2_remote_move is None:
            raise ProbeError(
                f"client2 never observed remote avatar movement >= {min_remote_move_distance:.2f} "
                f"in {client2_log}"
            )

    client2_remote_formal_attack = None
    if require_formal_attack_sync:
        client2_remote_formal_attack = first_avatar_death_transition(
            client2_records,
            "remote",
            "formalAttacks")
        if client2_remote_formal_attack is None:
            raise ProbeError(
                f"client2 never observed remote formal PlayerAttackState transition in {client2_log}"
            )

    client2_target_health = None
    if require_health_sync:
        client2_target_health = first_avatar_health_drop(client2_records, "local", min_remote_health_drop)
        if client2_target_health is None:
            raise ProbeError(
                f"client2 never observed local avatar health drop >= {min_remote_health_drop} "
                f"in {client2_log}"
            )

    client2_target_formal_hit = None
    if require_formal_hit_sync:
        client2_target_formal_hit = first_avatar_death_transition(
            client2_records,
            "local",
            "formalHits")
        if client2_target_formal_hit is None:
            raise ProbeError(
                f"client2 never observed local formal PlayerHitState transition in {client2_log}"
            )

    client2_target_death = None
    if require_death_sync:
        client2_target_death = first_avatar_death_transition(client2_records, "local")
        if client2_target_death is None:
            raise ProbeError(f"client2 never observed local avatar death transition in {client2_log}")

    client2_target_formal_death = None
    if require_formal_death_sync:
        client2_target_formal_death = first_avatar_death_transition(
            client2_records,
            "local",
            "formalDeaths")
        if client2_target_formal_death is None:
            raise ProbeError(
                f"client2 never observed local formal PlayerDeathState transition in {client2_log}"
            )

    return (
        client1_spawn,
        client2_spawn,
        client1_despawn,
        client2_remote_move,
        client2_target_health,
        client2_remote_formal_attack,
        client2_target_formal_hit,
        client2_target_death,
        client2_target_formal_death,
    )


def first_visible_remote(records: list[dict[str, str]]) -> dict[str, str] | None:
    for record in records:
        if (
            int_value(record, "avatarCount") >= 2
            and int_value(record, "owned") >= 1
            and int_value(record, "remote") >= 1
            and ":remote:" in record.get("avatars", "")
        ):
            return record

    return None


def first_visible_network_enemy(records: list[dict[str, str]]) -> dict[str, str] | None:
    for record in records:
        if int_value(record, "enemyCount") >= 1 and ":network:" in record.get("enemies", ""):
            return record

    return None


def first_visible_network_enemy_count(
    records: list[dict[str, str]],
    min_count: int,
) -> dict[str, str] | None:
    for record in records:
        enemy_ids = parse_summary_ids(record.get("enemies", ""), "network")
        if int_value(record, "enemyCount") >= min_count and len(enemy_ids) >= min_count:
            return record

    return None


def parse_summary_ids(summary: str, role: str) -> list[int]:
    ids: list[int] = []

    for item in summary.split("|"):
        parts = item.split(":")
        if len(parts) != 3 or parts[1] != role:
            continue

        try:
            ids.append(int(parts[0]))
        except ValueError:
            continue

    return sorted(set(ids))


def first_remote_despawn_after(
    records: list[dict[str, str]],
    spawn_record: dict[str, str],
) -> dict[str, str] | None:
    found_spawn = False

    for record in records:
        if record is spawn_record:
            found_spawn = True
            continue

        if not found_spawn:
            continue

        if (
            int_value(record, "avatarCount") == 1
            and int_value(record, "owned") == 1
            and int_value(record, "remote") == 0
        ):
            return record

    return None


def first_remote_position_change(
    records: list[dict[str, str]],
    min_distance: float,
) -> tuple[float, dict[str, str], dict[str, str]] | None:
    return first_summary_position_change(records, "avatars", "remote", min_distance)


def first_network_enemy_position_change(
    records: list[dict[str, str]],
    min_distance: float,
) -> tuple[float, dict[str, str], dict[str, str]] | None:
    return first_summary_position_change(records, "enemies", "network", min_distance)


def first_summary_position_change(
    records: list[dict[str, str]],
    summary_key: str,
    role: str,
    min_distance: float,
) -> tuple[float, dict[str, str], dict[str, str]] | None:
    first_record: dict[str, str] | None = None
    first_position: tuple[float, float, float] | None = None

    for record in records:
        current_position = parse_first_avatar_position(record.get(summary_key, ""), role)
        if current_position is None:
            continue

        if first_record is None or first_position is None:
            first_record = record
            first_position = current_position
            continue

        distance = vector_distance(first_position, current_position)
        if distance >= min_distance:
            return distance, first_record, record

    return None


def parse_first_avatar_position(summary: str, role: str) -> tuple[float, float, float] | None:
    role_token = f":{role}:"

    for item in summary.split("|"):
        if role_token not in item:
            continue

        parts = item.split(":")
        if len(parts) != 3:
            continue

        coordinates = parts[2].split(",")
        if len(coordinates) != 3:
            continue

        try:
            return float(coordinates[0]), float(coordinates[1]), float(coordinates[2])
        except ValueError:
            return None

    return None


def first_avatar_health_drop(
    records: list[dict[str, str]],
    role: str,
    min_drop: int,
) -> tuple[int, int, int, dict[str, str], dict[str, str]] | None:
    return first_summary_health_drop(records, "healths", role, min_drop)


def first_summary_health_drop(
    records: list[dict[str, str]],
    summary_key: str,
    role: str,
    min_drop: int,
) -> tuple[int, int, int, dict[str, str], dict[str, str]] | None:
    first_record: dict[str, str] | None = None
    first_health: int | None = None

    for record in records:
        remote_health = parse_first_avatar_health(record.get(summary_key, ""), role)
        if remote_health is None:
            continue

        if first_record is None or first_health is None:
            first_record = record
            first_health = remote_health
            continue

        health_drop = first_health - remote_health
        if health_drop >= min_drop:
            return health_drop, first_health, remote_health, first_record, record

    return None


def parse_first_avatar_health(summary: str, role: str) -> int | None:
    role_token = f":{role}:"

    for item in summary.split("|"):
        if role_token not in item:
            continue

        parts = item.split(":")
        if len(parts) != 3:
            continue

        try:
            return int(parts[2])
        except ValueError:
            return None

    return None


def first_summary_value(
    records: list[dict[str, str]],
    summary_key: str,
    role: str,
    expected_value: str,
) -> dict[str, str] | None:
    for record in records:
        value = parse_first_summary_value(record.get(summary_key, ""), role)
        if value == expected_value:
            return record

    return None


def parse_first_summary_value(summary: str, role: str) -> str | None:
    role_token = f":{role}:"

    for item in summary.split("|"):
        if role_token not in item:
            continue

        parts = item.split(":")
        if len(parts) != 3:
            continue

        return parts[2]

    return None


def first_avatar_death_transition(
    records: list[dict[str, str]],
    role: str,
    summary_key: str = "deaths",
) -> tuple[bool, bool, dict[str, str], dict[str, str]] | None:
    first_record: dict[str, str] | None = None
    first_dead: bool | None = None

    for record in records:
        dead = parse_first_avatar_death(record.get(summary_key, ""), role)
        if dead is None:
            continue

        if first_record is None or first_dead is None:
            first_record = record
            first_dead = dead
            continue

        if not first_dead and dead:
            return first_dead, dead, first_record, record

    return None


def parse_first_avatar_death(summary: str, role: str) -> bool | None:
    role_token = f":{role}:"

    for item in summary.split("|"):
        if role_token not in item:
            continue

        parts = item.split(":")
        if len(parts) != 3:
            continue

        value = parts[2].lower()
        if value in {"true", "dead", "1"}:
            return True

        if value in {"false", "alive", "0"}:
            return False

    return None


def vector_distance(
    first: tuple[float, float, float],
    second: tuple[float, float, float],
) -> float:
    return math.sqrt(
        (second[0] - first[0]) ** 2
        + (second[1] - first[1]) ** 2
        + (second[2] - first[2]) ** 2
    )


def print_health(label: str, line: str) -> None:
    print(f"{label}: {line}")


def wait_for_network_server_health(args: argparse.Namespace) -> tuple[str, dict[str, str]]:
    health_host = args.health_host or args.host
    return wait_for_health(
        health_host,
        args.health_port,
        args.startup_timeout,
        args.socket_timeout,
        "server health ready",
        lambda current: bool_value(current, "networkStarted")
        and bool_value(current, "networkListening")
        and bool_value(current, "networkIsServer"),
    )


def run_health_only_probe(args: argparse.Namespace) -> None:
    line, values = wait_for_network_server_health(args)
    print_health("server-ready", line)
    print(
        "P1.5_HEALTH_OK"
        + f" host={args.host}"
        + f" healthPort={args.health_port}"
        + f" networkPort={values.get('networkPort', '')}"
        + f" connected={values.get('networkConnectedClients', '')}"
        + f" spawned={values.get('networkSpawnedPlayers', '')}"
    )


def tail_log(path: Path, lines: int = 50) -> str:
    if not path.exists():
        return f"{path} does not exist"

    data = path.read_text(encoding="utf-8", errors="replace").splitlines()
    return "\n".join(data[-lines:])


def require_server_network_enemy_navmesh_chase(server_log: Path) -> str:
    if not server_log.exists():
        raise ProbeError(f"server log does not exist: {server_log}")

    lines = server_log.read_text(encoding="utf-8", errors="replace").splitlines()
    fallback_line = next(
        (line.strip() for line in lines if "Failed to create agent because there is no valid NavMesh" in line),
        None,
    )

    if fallback_line is not None:
        raise ProbeError(f"server used fallback enemy chase instead of NavMesh chase: {fallback_line}")

    ready_line = next(
        (
            line.strip()
            for line in lines
            if (
                "Brain smoke enemy attack status" in line
                or "Server tick enemy status" in line
            )
            and "navMeshReady=True" in line
        ),
        None,
    )

    if ready_line is None:
        raise ProbeError(f"server never reported navMeshReady=True in {server_log}")

    return ready_line


def parse_server_tick_attack_events(applied_lines: list[str]) -> list[dict[str, object]]:
    events: list[dict[str, object]] = []

    for line in applied_lines:
        values = parse_health_line(line)
        if (
            "enemyId" not in values
            or "targetOwner" not in values
            or "health" not in values
            or "targetDead" not in values
        ):
            raise ProbeError(f"could not parse Server tick enemy attack applied line: {line}")

        health_values = values["health"].split("->", 1)
        if len(health_values) != 2:
            raise ProbeError(f"could not parse health transition in line: {line}")

        try:
            previous_health = int(health_values[0])
            next_health = int(health_values[1])
        except ValueError as exc:
            raise ProbeError(f"could not parse health values in line: {line}") from exc

        events.append(
            {
                "line": line,
                "enemy_id": values["enemyId"],
                "target_owner": values["targetOwner"],
                "previous_health": previous_health,
                "next_health": next_health,
                "target_dead": values["targetDead"].lower() == "true",
            }
        )

    return events


def require_server_tick_target_switch(
    applied_lines: list[str],
    switch_lines: list[str],
    min_initial_target_attacks: int,
) -> str:
    events = parse_server_tick_attack_events(applied_lines)
    if not events:
        raise ProbeError("server reported no parsed Server tick enemy attack applied lines")

    initial_owner = str(events[0]["target_owner"])
    sticky_attack_count = 0
    initial_death_index: int | None = None
    switched_owner: str | None = None

    for index, event in enumerate(events):
        owner = str(event["target_owner"])
        target_dead = bool(event["target_dead"]) or int(event["next_health"]) <= 0

        if owner == initial_owner and initial_death_index is None:
            sticky_attack_count += 1
            if target_dead:
                initial_death_index = index
            continue

        if initial_death_index is None:
            raise ProbeError(
                "server tick enemy switched targets before the initial target died: "
                f"initialTargetOwner={initial_owner} switchedTargetOwner={owner} line={event['line']}"
            )

        if owner != initial_owner:
            switched_owner = owner
            break

    if sticky_attack_count < min_initial_target_attacks:
        raise ProbeError(
            "server tick enemy did not retain the initial target for enough applied attacks: "
            f"initialTargetOwner={initial_owner} expected>={min_initial_target_attacks} actual={sticky_attack_count}"
        )

    if initial_death_index is None:
        raise ProbeError(
            "server tick enemy never killed the initial target before target switch: "
            f"initialTargetOwner={initial_owner}"
        )

    if switched_owner is None:
        raise ProbeError(
            "server tick enemy never applied damage to another live target after the initial target died: "
            f"initialTargetOwner={initial_owner}"
        )

    switch_line = None
    for line in switch_lines:
        values = parse_health_line(line)
        if (
            values.get("previousTargetOwner") == initial_owner
            and values.get("nextTargetOwner") == switched_owner
            and values.get("previousTargetDead", "").lower() == "true"
        ):
            switch_line = line
            break

    if switch_line is None:
        raise ProbeError(
            "server tick enemy target switch log was missing or did not prove death-gated switching: "
            f"initialTargetOwner={initial_owner} switchedTargetOwner={switched_owner}"
        )

    return (
        f"initialTargetOwner={initial_owner}"
        + f" initialTargetAttackCount={sticky_attack_count}"
        + f" switchedTargetOwner={switched_owner}"
        + " previousTargetDead=true"
    )


def sort_numeric_strings(values: list[str]) -> list[str]:
    return sorted(
        values,
        key=lambda value: (0, int(value)) if value.isdigit() else (1, value),
    )


def require_server_tick_target_distribution(
    applied_lines: list[str],
    min_enemy_count: int,
    min_target_count: int,
) -> str:
    events = parse_server_tick_attack_events(applied_lines)
    if not events:
        raise ProbeError("server reported no parsed Server tick enemy attack applied lines")

    first_target_by_enemy: dict[str, str] = {}
    attack_count_by_enemy: dict[str, int] = {}

    for event in events:
        enemy_id = str(event["enemy_id"])
        target_owner = str(event["target_owner"])
        attack_count_by_enemy[enemy_id] = attack_count_by_enemy.get(enemy_id, 0) + 1
        if enemy_id not in first_target_by_enemy:
            first_target_by_enemy[enemy_id] = target_owner

    if len(first_target_by_enemy) < min_enemy_count:
        raise ProbeError(
            "server tick attacks did not involve enough distinct enemies: "
            f"expected>={min_enemy_count} actual={len(first_target_by_enemy)} "
            + "enemyIds="
            + ",".join(sort_numeric_strings(list(first_target_by_enemy.keys())))
        )

    ordered_enemy_ids = sort_numeric_strings(list(first_target_by_enemy.keys()))
    initial_targets = [first_target_by_enemy[enemy_id] for enemy_id in ordered_enemy_ids]
    unique_targets = set(initial_targets)

    if len(unique_targets) < min_target_count:
        pairs = ",".join(f"{enemy_id}->{first_target_by_enemy[enemy_id]}" for enemy_id in ordered_enemy_ids)
        raise ProbeError(
            "server tick enemy initial target distribution did not cover enough players: "
            f"expectedTargets>={min_target_count} actualTargets={len(unique_targets)} "
            + f"enemyTargets={pairs}"
        )

    pairs = ",".join(f"{enemy_id}->{first_target_by_enemy[enemy_id]}" for enemy_id in ordered_enemy_ids)
    counts = ",".join(f"{enemy_id}:{attack_count_by_enemy[enemy_id]}" for enemy_id in ordered_enemy_ids)
    return (
        f"minEnemyCount={min_enemy_count}"
        + f" uniqueTargetCount={len(unique_targets)}"
        + f" enemyTargets={pairs}"
        + f" enemyAttackCounts={counts}"
    )


def summarize_server_tick_target_retention_failure(
    applied_lines: list[str],
    min_enemy_count: int,
    min_retained_attacks: int,
) -> str:
    events = parse_server_tick_attack_events(applied_lines)
    required_attacks_per_enemy = max(1, min_retained_attacks)
    required_attack_count = max(1, min_enemy_count) * required_attacks_per_enemy
    attack_counts: dict[str, int] = {}
    target_stats: dict[str, dict[str, object]] = {}
    sequence: list[str] = []

    for index, event in enumerate(events, start=1):
        enemy_id = str(event["enemy_id"])
        target_owner = str(event["target_owner"])
        previous_health = int(event["previous_health"])
        next_health = int(event["next_health"])
        target_dead = bool(event["target_dead"]) or next_health <= 0
        applied_damage = max(0, previous_health - next_health)

        attack_counts[enemy_id] = attack_counts.get(enemy_id, 0) + 1
        stats = target_stats.setdefault(
            target_owner,
            {
                "hits": 0,
                "damage": 0,
                "initial_health": previous_health,
                "final_health": next_health,
                "dead": False,
                "killed_by_enemy": "none",
            },
        )
        stats["hits"] = int(stats["hits"]) + 1
        stats["damage"] = int(stats["damage"]) + applied_damage
        stats["final_health"] = next_health
        if target_dead:
            stats["dead"] = True
            stats["killed_by_enemy"] = enemy_id

        sequence.append(
            f"{index}:{enemy_id}>{target_owner}:{previous_health}->{next_health}"
            + (":dead" if target_dead else "")
        )

    ordered_enemy_ids = sort_numeric_strings(list(attack_counts.keys()))
    ordered_target_owners = sort_numeric_strings(list(target_stats.keys()))
    enemy_counts = ",".join(
        f"{enemy_id}:{attack_counts[enemy_id]}" for enemy_id in ordered_enemy_ids
    ) or "none"
    deficits = {
        enemy_id: required_attacks_per_enemy - attack_counts[enemy_id]
        for enemy_id in ordered_enemy_ids
        if attack_counts[enemy_id] < required_attacks_per_enemy
    }
    excesses = {
        enemy_id: attack_counts[enemy_id] - required_attacks_per_enemy
        for enemy_id in ordered_enemy_ids
        if attack_counts[enemy_id] > required_attacks_per_enemy
    }
    deficit_summary = ",".join(
        f"{enemy_id}:{deficits[enemy_id]}" for enemy_id in sort_numeric_strings(list(deficits.keys()))
    ) or "none"
    target_budget_summary = "|".join(
        f"{owner}:hits={target_stats[owner]['hits']}"
        + f"/damage={target_stats[owner]['damage']}"
        + f"/health={target_stats[owner]['initial_health']}->{target_stats[owner]['final_health']}"
        + f"/dead={str(bool(target_stats[owner]['dead'])).lower()}"
        + f"/killedByEnemy={target_stats[owner]['killed_by_enemy']}"
        for owner in ordered_target_owners
    ) or "none"
    all_targets_dead = bool(target_stats) and all(
        bool(stats["dead"]) for stats in target_stats.values()
    )
    missing_enemy_attack_slots = sum(deficits.values())
    excess_enemy_attacks = sum(excesses.values())

    if deficits and all_targets_dead and len(events) >= required_attack_count:
        classification = "health_budget_exhausted_with_uneven_enemy_scheduling"
    elif deficits and any(bool(stats["dead"]) for stats in target_stats.values()):
        classification = "target_health_budget_exhausted_before_fair_retention"
    elif len(events) < required_attack_count:
        classification = "insufficient_attack_volume"
    elif deficits:
        classification = "uneven_enemy_scheduling"
    else:
        classification = "retention_constraint_mismatch"

    return (
        f"classification={classification}"
        + f" observedAttacks={len(events)}"
        + f" requiredAttacks={required_attack_count}"
        + f" observedEnemyCount={len(attack_counts)}"
        + f" requiredEnemyCount={max(1, min_enemy_count)}"
        + f" attacksPerEnemyRequired={required_attacks_per_enemy}"
        + f" enemyAttackCounts={enemy_counts}"
        + f" enemyAttackDeficits={deficit_summary}"
        + f" missingEnemyAttackSlots={missing_enemy_attack_slots}"
        + f" excessEnemyAttacks={excess_enemy_attacks}"
        + f" targetBudgets={target_budget_summary}"
        + f" allTargetsDead={str(all_targets_dead).lower()}"
        + " attackSequence=" + ",".join(sequence)
    )


def _require_server_tick_target_retention(
    applied_lines: list[str],
    min_enemy_count: int,
    min_target_count: int,
    min_retained_attacks: int,
) -> str:
    events = parse_server_tick_attack_events(applied_lines)
    if not events:
        raise ProbeError("server reported no parsed Server tick enemy attack applied lines")

    events_by_enemy: dict[str, list[dict[str, object]]] = {}
    for event in events:
        enemy_id = str(event["enemy_id"])
        events_by_enemy.setdefault(enemy_id, []).append(event)

    if len(events_by_enemy) < min_enemy_count:
        raise ProbeError(
            "server tick target retention did not involve enough distinct enemies: "
            f"expected>={min_enemy_count} actual={len(events_by_enemy)} "
            + "enemyIds="
            + ",".join(sort_numeric_strings(list(events_by_enemy.keys())))
        )

    retained_targets: dict[str, str] = {}
    retained_attack_counts: dict[str, int] = {}
    required_attacks = max(1, min_retained_attacks)
    ordered_enemy_ids = sort_numeric_strings(list(events_by_enemy.keys()))

    for enemy_id in ordered_enemy_ids:
        enemy_events = events_by_enemy[enemy_id]
        if len(enemy_events) < required_attacks:
            raise ProbeError(
                "server tick target retention did not observe enough attacks for enemy: "
                f"enemyId={enemy_id} expected>={required_attacks} actual={len(enemy_events)}"
            )

        retained_window = enemy_events[:required_attacks]
        target_owners = {str(event["target_owner"]) for event in retained_window}
        if len(target_owners) != 1:
            transitions = ",".join(str(event["target_owner"]) for event in retained_window)
            raise ProbeError(
                "server tick target retention changed target inside the retained window: "
                f"enemyId={enemy_id} targets={transitions}"
            )

        early_dead_event = next(
            (
                event
                for event in retained_window[:-1]
                if bool(event["target_dead"]) or int(event["next_health"]) <= 0
            ),
            None,
        )
        if early_dead_event is not None:
            raise ProbeError(
                "server tick target retention killed the target before the retained window completed: "
                f"enemyId={enemy_id} targetOwner={early_dead_event['target_owner']} line={early_dead_event['line']}"
            )

        retained_targets[enemy_id] = str(retained_window[0]["target_owner"])
        retained_attack_counts[enemy_id] = len(retained_window)

    unique_targets = set(retained_targets.values())
    if len(unique_targets) < min_target_count:
        pairs = ",".join(f"{enemy_id}->{retained_targets[enemy_id]}" for enemy_id in ordered_enemy_ids)
        raise ProbeError(
            "server tick target retention did not preserve enough distinct live targets: "
            f"expectedTargets>={min_target_count} actualTargets={len(unique_targets)} "
            + f"enemyTargets={pairs}"
        )

    pairs = ",".join(f"{enemy_id}->{retained_targets[enemy_id]}" for enemy_id in ordered_enemy_ids)
    counts = ",".join(f"{enemy_id}:{retained_attack_counts[enemy_id]}" for enemy_id in ordered_enemy_ids)
    return (
        f"minEnemyCount={min_enemy_count}"
        + f" minRetainedAttacks={required_attacks}"
        + f" uniqueTargetCount={len(unique_targets)}"
        + f" enemyTargets={pairs}"
        + f" retainedAttackCounts={counts}"
    )


def require_server_tick_target_retention(
    applied_lines: list[str],
    min_enemy_count: int,
    min_target_count: int,
    min_retained_attacks: int,
) -> str:
    try:
        return _require_server_tick_target_retention(
            applied_lines,
            min_enemy_count,
            min_target_count,
            min_retained_attacks,
        )
    except ProbeError as exc:
        try:
            diagnostic = summarize_server_tick_target_retention_failure(
                applied_lines,
                min_enemy_count,
                min_retained_attacks,
            )
        except ProbeError:
            raise exc

        raise ProbeError(
            f"{exc}\nP6_NETWORK_ENEMY_TARGET_RETENTION_DIAGNOSTIC {diagnostic}"
        ) from exc


def require_server_network_enemy_server_tick(
    server_log: Path,
    min_attack_count: int = 1,
    require_target_switch: bool = False,
    min_initial_target_attacks: int = 2,
    require_target_distribution: bool = False,
    min_target_distribution_enemy_count: int = 2,
    min_target_distribution_target_count: int = 2,
    require_target_retention: bool = False,
    min_target_retention_attacks: int = 3,
) -> tuple[str, int, str | None, str | None, str | None]:
    if not server_log.exists():
        raise ProbeError(f"server log does not exist: {server_log}")

    lines = server_log.read_text(encoding="utf-8", errors="replace").splitlines()
    status_line = next(
        (
            line.strip()
            for line in lines
            if "Server tick enemy status" in line
            and "navMeshReady=True" in line
            and "serverTick=True" in line
        ),
        None,
    )
    applied_lines = [line.strip() for line in lines if "Server tick enemy attack applied" in line]
    switch_lines = [line.strip() for line in lines if "Server tick enemy target switched" in line]

    if status_line is None:
        raise ProbeError(f"server never reported Server tick enemy status with navMeshReady=True in {server_log}")

    if len(applied_lines) < min_attack_count:
        raise ProbeError(
            "server reported too few Server tick enemy attack applied lines "
            f"in {server_log}: expected>={min_attack_count} actual={len(applied_lines)}"
        )

    target_switch_summary = (
        require_server_tick_target_switch(applied_lines, switch_lines, max(1, min_initial_target_attacks))
        if require_target_switch
        else None
    )
    target_distribution_summary = (
        require_server_tick_target_distribution(
            applied_lines,
            max(1, min_target_distribution_enemy_count),
            max(1, min_target_distribution_target_count))
        if require_target_distribution
        else None
    )
    target_retention_summary = (
        require_server_tick_target_retention(
            applied_lines,
            max(1, min_target_distribution_enemy_count),
            max(1, min_target_distribution_target_count),
            max(1, min_target_retention_attacks))
        if require_target_retention
        else None
    )

    return status_line, len(applied_lines), target_switch_summary, target_distribution_summary, target_retention_summary


def run_probe(args: argparse.Namespace) -> None:
    host = args.host
    health_host = args.health_host or host
    server_bind_address = args.server_bind_address or host
    auto_require_formal_sync = (
        not args.skip_auto_formal_sync_requirements
        and args.network_player_prefab.endswith("PF_NetworkPlayerCombatTest")
    )
    require_formal_attack_sync = args.require_formal_attack_sync or auto_require_formal_sync
    require_formal_hit_sync = args.require_formal_hit_sync or auto_require_formal_sync
    require_formal_death_sync = args.require_formal_death_sync or auto_require_formal_sync
    require_network_enemy_sync = args.require_network_enemy_sync or args.require_network_enemy_attack_sync
    server_log = ensure_log_parent(Path(args.server_log))
    client1_log = ensure_log_parent(Path(args.client1_log))
    client2_log = ensure_log_parent(Path(args.client2_log))
    server_process: subprocess.Popen | None = None
    client_processes: list[tuple[str, subprocess.Popen, Path]] = []

    if (
        not args.skip_client_despawn_check
        and args.client1_quit_after_seconds <= args.client2_quit_after_seconds + args.smoke_report_interval_seconds
    ):
        raise ProbeError(
            "client1 must outlive client2 by more than the smoke report interval "
            "when client despawn checking is enabled"
        )

    if not args.skip_server_start:
        server_bin = require_path(Path(args.server_bin), "server binary")
        server_command = [
            str(server_bin),
            "-batchmode",
            "-nographics",
            "--port",
            str(args.game_port),
            "--bind-address",
            server_bind_address,
            "--network-port",
            str(args.game_port),
            "--network-bind-address",
            server_bind_address,
            "--health-port",
            str(args.health_port),
            "--health-bind-address",
            server_bind_address,
            "--quit-after-seconds",
            str(args.server_quit_after_seconds),
            "-logFile",
            str(server_log),
        ]
        if args.network_player_prefab:
            server_command += ["--network-player-prefab", args.network_player_prefab]

        if args.network_enemy_server_tick_death_delay_seconds is not None:
            server_command += [
                "--network-enemy-server-tick-death-delay-seconds",
                str(max(0, args.network_enemy_server_tick_death_delay_seconds)),
            ]

        if args.network_enemy_server_tick_damage is not None:
            server_command += [
                "--network-enemy-server-tick-damage",
                str(max(1, args.network_enemy_server_tick_damage)),
            ]

        if args.network_enemy_count is not None:
            server_command += ["--network-enemy-count", str(max(0, args.network_enemy_count))]

        if args.require_network_enemy_server_tick:
            server_command += ["--enable-network-enemy-server-tick"]
        elif args.require_network_enemy_attack_sync and args.use_brain_chase_network_enemy_attack_smoke:
            server_command += ["--enable-network-enemy-brain-chase-attack-smoke"]
        elif args.require_network_enemy_attack_sync and args.use_brain_network_enemy_attack_smoke:
            server_command += ["--enable-network-enemy-brain-attack-smoke"]
        elif args.require_network_enemy_attack_sync and args.use_formal_network_enemy_attack_smoke:
            server_command += ["--enable-network-enemy-formal-attack-smoke"]
        elif args.require_network_enemy_attack_sync:
            server_command += ["--enable-network-enemy-attack-smoke"]

        server_process = start_process("server", server_command, server_log)
    else:
        print("server start skipped; probing an already running server")

    try:
        line, values = wait_for_network_server_health(args)
        print_health("server-ready", line)

        client_bin = require_path(Path(args.client_bin), "client binary")
        client_base_command = [
            str(client_bin),
            "-batchmode",
            "-nographics",
            "-multiplayer-client",
            "--server-address",
            host,
            "--network-port",
            str(args.game_port),
            "--multiplayer-smoke-report",
            "--smoke-report-interval-seconds",
            str(args.smoke_report_interval_seconds),
        ]
        if args.network_player_prefab:
            client_base_command += ["--network-player-prefab", args.network_player_prefab]

        client_specs = [
            ("client1", client1_log, args.client1_quit_after_seconds),
            ("client2", client2_log, args.client2_quit_after_seconds),
        ]

        for name, log_path, quit_after_seconds in client_specs:
            command = client_base_command + [
                "--quit-after-seconds",
                str(quit_after_seconds),
                "--smoke-report-label",
                name,
                "-logFile",
                str(log_path),
            ]

            if name == "client1" and not args.skip_remote_movement_check:
                command += [
                    "--multiplayer-smoke-move",
                    "--smoke-move-x",
                    "1",
                    "--smoke-move-y",
                    "0",
                    "--smoke-move-delay-seconds",
                    str(args.smoke_move_delay_seconds),
                    "--smoke-move-duration-seconds",
                    str(args.smoke_move_duration_seconds),
                ]

            if name == "client1" and not args.skip_health_sync_check:
                command += [
                    "--multiplayer-smoke-attack",
                    "--smoke-attack-id",
                    args.smoke_attack_id,
                    "--smoke-attack-damage-amount",
                    str(args.smoke_attack_damage_amount),
                    "--smoke-attack-delay-seconds",
                    str(args.smoke_attack_delay_seconds),
                    "--smoke-attack-count",
                    str(args.smoke_attack_count),
                    "--smoke-attack-interval-seconds",
                    str(args.smoke_attack_interval_seconds),
                ]

            process = start_process(name, command, log_path)
            client_processes.append((name, process, log_path))

        line, values = wait_for_health(
            health_host,
            args.health_port,
            args.connected_timeout,
            args.socket_timeout,
            "two connected NGO clients",
            lambda current: int_value(current, "networkConnectedClients") >= 2
            and int_value(current, "networkSpawnedPlayers") >= 2,
        )
        connected_clients = int_value(values, "networkConnectedClients")
        spawned_players = int_value(values, "networkSpawnedPlayers")
        print_health("clients-connected", line)

        client_by_name = {name: process for name, process, _log_path in client_processes}
        client2_returncode = wait_process(
            client_by_name["client2"],
            "client2",
            args.client2_quit_after_seconds + args.client_exit_grace_seconds,
        )
        if client2_returncode != 0:
            raise ProbeError(f"client2 exited with non-zero code {client2_returncode}")

        if not args.skip_client_despawn_check:
            line, values = wait_for_health(
                health_host,
                args.health_port,
                args.disconnect_timeout,
                args.socket_timeout,
                "one client remaining after client2 quit",
                lambda current: int_value(current, "networkConnectedClients") == 1
                and int_value(current, "networkSpawnedPlayers") == 1,
            )
            print_health("client2-disconnected", line)

        client1_returncode = wait_process(
            client_by_name["client1"],
            "client1",
            args.client1_quit_after_seconds + args.client_exit_grace_seconds,
        )
        if client1_returncode != 0:
            raise ProbeError(f"client1 exited with non-zero code {client1_returncode}")

        line, values = wait_for_health(
            health_host,
            args.health_port,
            args.disconnect_timeout,
            args.socket_timeout,
            "client disconnect cleanup",
            lambda current: int_value(current, "networkConnectedClients") == 0
            and int_value(current, "networkSpawnedPlayers") == 0,
        )
        print_health("clients-disconnected", line)

        (
            client1_spawn,
            client2_spawn,
            client1_despawn,
            client2_remote_move,
            client2_target_health,
            client2_remote_formal_attack,
            client2_target_formal_hit,
            client2_target_death,
            client2_target_formal_death,
        ) = require_smoke_visibility(
            client1_log,
            client2_log,
            not args.skip_client_despawn_check,
            not args.skip_remote_movement_check,
            not args.skip_health_sync_check,
            require_formal_attack_sync,
            require_formal_hit_sync,
            args.require_death_sync,
            require_formal_death_sync,
            args.min_remote_move_distance,
            args.min_remote_health_drop,
        )
        client1_enemy_spawn = None
        client2_enemy_spawn = None
        client1_enemy_count_spawn = None
        client2_enemy_count_spawn = None
        client1_enemy_health = None
        client2_enemy_health = None
        client1_enemy_death = None
        client2_enemy_death = None
        client1_enemy_chase = None
        client2_enemy_chase = None
        client1_formal_enemy_death = None
        client2_formal_enemy_death = None
        client1_formal_enemy_driver = None
        client2_formal_enemy_driver = None
        client1_enemy_attack_health = None
        client2_enemy_attack_health = None
        client1_enemy_attack_role = "local"
        client2_enemy_attack_role = "remote"
        client1_formal_enemy_attack = None
        client2_formal_enemy_attack = None
        client1_records = []
        client2_records = []

        if require_network_enemy_sync or args.min_network_enemy_count > 1:
            client1_records = parse_smoke_records(client1_log)
            client2_records = parse_smoke_records(client2_log)

        if args.min_network_enemy_count > 1:
            client1_enemy_count_spawn = first_visible_network_enemy_count(
                client1_records,
                args.min_network_enemy_count)
            client2_enemy_count_spawn = first_visible_network_enemy_count(
                client2_records,
                args.min_network_enemy_count)

            if client1_enemy_count_spawn is None:
                raise ProbeError(
                    f"client1 never observed >= {args.min_network_enemy_count} network enemies in {client1_log}"
                )

            if client2_enemy_count_spawn is None:
                raise ProbeError(
                    f"client2 never observed >= {args.min_network_enemy_count} network enemies in {client2_log}"
                )

        if require_network_enemy_sync:
            client1_enemy_spawn = first_visible_network_enemy(client1_records)
            client2_enemy_spawn = first_visible_network_enemy(client2_records)

            if client1_enemy_spawn is None:
                raise ProbeError(f"client1 never observed a network enemy in {client1_log}")

            if client2_enemy_spawn is None:
                raise ProbeError(f"client2 never observed a network enemy in {client2_log}")

            if args.require_network_enemy_chase_sync:
                client1_enemy_chase = first_network_enemy_position_change(
                    client1_records,
                    args.min_network_enemy_chase_distance)
                client2_enemy_chase = first_network_enemy_position_change(
                    client2_records,
                    args.min_network_enemy_chase_distance)

                if client1_enemy_chase is None:
                    raise ProbeError(
                        "client1 never observed network enemy movement "
                        f">= {args.min_network_enemy_chase_distance:.2f} in {client1_log}"
                    )

                if client2_enemy_chase is None:
                    raise ProbeError(
                        "client2 never observed network enemy movement "
                        f">= {args.min_network_enemy_chase_distance:.2f} in {client2_log}"
                    )

            client1_enemy_health = first_summary_health_drop(
                client1_records,
                "enemyHealths",
                "network",
                args.min_network_enemy_health_drop)
            client2_enemy_health = first_summary_health_drop(
                client2_records,
                "enemyHealths",
                "network",
                args.min_network_enemy_health_drop)

            if client1_enemy_health is None:
                raise ProbeError(
                    f"client1 never observed network enemy HP drop >= {args.min_network_enemy_health_drop} "
                    f"in {client1_log}"
                )

            if client2_enemy_health is None:
                raise ProbeError(
                    f"client2 never observed network enemy HP drop >= {args.min_network_enemy_health_drop} "
                    f"in {client2_log}"
                )

            client1_enemy_death = first_avatar_death_transition(client1_records, "network", "enemyDeaths")
            client2_enemy_death = first_avatar_death_transition(client2_records, "network", "enemyDeaths")

            if client1_enemy_death is None:
                raise ProbeError(f"client1 never observed network enemy death transition in {client1_log}")

            if client2_enemy_death is None:
                raise ProbeError(f"client2 never observed network enemy death transition in {client2_log}")

            if args.require_formal_network_enemy_sync:
                client1_formal_enemy_death = first_avatar_death_transition(
                    client1_records,
                    "network",
                    "enemyFormalDeaths")
                client2_formal_enemy_death = first_avatar_death_transition(
                    client2_records,
                    "network",
                    "enemyFormalDeaths")

                if client1_formal_enemy_death is None:
                    raise ProbeError(
                        f"client1 never observed formal network enemy death transition in {client1_log}"
                    )

                if client2_formal_enemy_death is None:
                    raise ProbeError(
                        f"client2 never observed formal network enemy death transition in {client2_log}"
                    )

                client1_formal_enemy_driver = first_summary_value(
                    client1_records,
                    "enemyFormalDrivers",
                    "network",
                    "suppressed")
                client2_formal_enemy_driver = first_summary_value(
                    client2_records,
                    "enemyFormalDrivers",
                    "network",
                    "suppressed")

                if client1_formal_enemy_driver is None:
                    raise ProbeError(
                        f"client1 never observed formal network enemy driver suppression in {client1_log}"
                    )

                if client2_formal_enemy_driver is None:
                    raise ProbeError(
                        f"client2 never observed formal network enemy driver suppression in {client2_log}"
                    )

            if args.require_network_enemy_attack_sync:
                client1_enemy_attack_health = first_summary_health_drop(
                    client1_records,
                    "healths",
                    "local",
                    args.min_network_enemy_attack_health_drop)
                client2_enemy_attack_health = first_summary_health_drop(
                    client2_records,
                    "healths",
                    "remote",
                    args.min_network_enemy_attack_health_drop)

                if client1_enemy_attack_health is None or client2_enemy_attack_health is None:
                    alternate_client1_enemy_attack_health = first_summary_health_drop(
                        client1_records,
                        "healths",
                        "remote",
                        args.min_network_enemy_attack_health_drop)
                    alternate_client2_enemy_attack_health = first_summary_health_drop(
                        client2_records,
                        "healths",
                        "local",
                        args.min_network_enemy_attack_health_drop)

                    if (
                        alternate_client1_enemy_attack_health is not None
                        and alternate_client2_enemy_attack_health is not None
                    ):
                        client1_enemy_attack_health = alternate_client1_enemy_attack_health
                        client2_enemy_attack_health = alternate_client2_enemy_attack_health
                        client1_enemy_attack_role = "remote"
                        client2_enemy_attack_role = "local"

                if client1_enemy_attack_health is None:
                    raise ProbeError(
                        "client1 never observed any avatar HP drop from server enemy attack "
                        f">= {args.min_network_enemy_attack_health_drop} in {client1_log}"
                    )

                if client2_enemy_attack_health is None:
                    raise ProbeError(
                        "client2 never observed the matching avatar HP drop from server enemy attack "
                        f">= {args.min_network_enemy_attack_health_drop} in {client2_log}"
                    )

                client1_formal_enemy_attack = first_avatar_death_transition(
                    client1_records,
                    "network",
                    "enemyFormalAttacks")
                client2_formal_enemy_attack = first_avatar_death_transition(
                    client2_records,
                    "network",
                    "enemyFormalAttacks")

                if client1_formal_enemy_attack is None:
                    raise ProbeError(
                        f"client1 never observed formal network enemy attack transition in {client1_log}"
                    )

                if client2_formal_enemy_attack is None:
                    raise ProbeError(
                        f"client2 never observed formal network enemy attack transition in {client2_log}"
                    )

        print("client1-visible: " + client1_spawn["_line"])
        print("client2-visible: " + client2_spawn["_line"])

        if client1_despawn is not None:
            print("client1-despawn: " + client1_despawn["_line"])

        if client2_remote_move is not None:
            distance, first_move_record, later_move_record = client2_remote_move
            print("client2-remote-move-start: " + first_move_record["_line"])
            print("client2-remote-move-later: " + later_move_record["_line"])
            print(f"P1.5_REMOTE_POSITION_SYNC_OK client2ObservedRemoteMoveDistance={distance:.2f}")

        if client2_target_health is not None:
            (
                health_drop,
                first_health,
                later_health,
                first_health_record,
                later_health_record,
            ) = client2_target_health
            print("client2-local-health-start: " + first_health_record["_line"])
            print("client2-local-health-later: " + later_health_record["_line"])
            print(
                "P3_ATTACK_HIT_OK"
                + f" attackId={args.smoke_attack_id}"
                + f" client2ObservedLocalHealthStart={first_health}"
                + f" client2ObservedLocalHealthLater={later_health}"
                + f" client2ObservedLocalHealthDrop={health_drop}"
                + f" clientRequestedDamage={args.smoke_attack_damage_amount}"
                + f" serverAppliedDamage={health_drop}"
            )

        if client2_remote_formal_attack is not None:
            (
                first_formal_attack,
                later_formal_attack,
                first_formal_attack_record,
                later_formal_attack_record,
            ) = client2_remote_formal_attack
            print("client2-remote-formal-attack-start: " + first_formal_attack_record["_line"])
            print("client2-remote-formal-attack-later: " + later_formal_attack_record["_line"])
            print(
                "P5_FORMAL_ATTACK_SYNC_OK"
                + f" attackId={args.smoke_attack_id}"
                + f" client2ObservedRemoteFormalAttackStart={str(first_formal_attack).lower()}"
                + f" client2ObservedRemoteFormalAttackLater={str(later_formal_attack).lower()}"
            )

        if client2_target_formal_hit is not None:
            (
                first_formal_hit,
                later_formal_hit,
                first_formal_hit_record,
                later_formal_hit_record,
            ) = client2_target_formal_hit
            print("client2-local-formal-hit-start: " + first_formal_hit_record["_line"])
            print("client2-local-formal-hit-later: " + later_formal_hit_record["_line"])
            print(
                "P5_FORMAL_HIT_SYNC_OK"
                + f" attackId={args.smoke_attack_id}"
                + f" client2ObservedLocalFormalHitStart={str(first_formal_hit).lower()}"
                + f" client2ObservedLocalFormalHitLater={str(later_formal_hit).lower()}"
            )

        if client2_target_death is not None:
            (
                first_dead,
                later_dead,
                first_death_record,
                later_death_record,
            ) = client2_target_death
            print("client2-local-death-start: " + first_death_record["_line"])
            print("client2-local-death-later: " + later_death_record["_line"])
            print(
                "P4_DEATH_SYNC_OK"
                + f" attackId={args.smoke_attack_id}"
                + f" smokeAttackCount={args.smoke_attack_count}"
                + f" client2ObservedLocalDeathStart={str(first_dead).lower()}"
                + f" client2ObservedLocalDeathLater={str(later_dead).lower()}"
            )

        if client2_target_formal_death is not None:
            (
                first_formal_dead,
                later_formal_dead,
                first_formal_death_record,
                later_formal_death_record,
            ) = client2_target_formal_death
            print("client2-local-formal-death-start: " + first_formal_death_record["_line"])
            print("client2-local-formal-death-later: " + later_formal_death_record["_line"])
            print(
                "P5_FORMAL_DEATH_SYNC_OK"
                + f" attackId={args.smoke_attack_id}"
                + f" smokeAttackCount={args.smoke_attack_count}"
                + f" client2ObservedLocalFormalDeathStart={str(first_formal_dead).lower()}"
                + f" client2ObservedLocalFormalDeathLater={str(later_formal_dead).lower()}"
            )

        if client1_enemy_spawn is not None and client2_enemy_spawn is not None:
            print("client1-network-enemy-visible: " + client1_enemy_spawn["_line"])
            print("client2-network-enemy-visible: " + client2_enemy_spawn["_line"])

        if client1_enemy_count_spawn is not None and client2_enemy_count_spawn is not None:
            client1_enemy_ids = parse_summary_ids(client1_enemy_count_spawn.get("enemies", ""), "network")
            client2_enemy_ids = parse_summary_ids(client2_enemy_count_spawn.get("enemies", ""), "network")
            print("client1-network-enemy-count-visible: " + client1_enemy_count_spawn["_line"])
            print("client2-network-enemy-count-visible: " + client2_enemy_count_spawn["_line"])
            print(
                "P6_NETWORK_ENEMY_COUNT_OK"
                + f" minNetworkEnemyCount={args.min_network_enemy_count}"
                + f" client1ObservedEnemyCount={int_value(client1_enemy_count_spawn, 'enemyCount')}"
                + f" client2ObservedEnemyCount={int_value(client2_enemy_count_spawn, 'enemyCount')}"
                + f" client1EnemyIds={','.join(str(enemy_id) for enemy_id in client1_enemy_ids)}"
                + f" client2EnemyIds={','.join(str(enemy_id) for enemy_id in client2_enemy_ids)}"
            )

        if client1_enemy_chase is not None and client2_enemy_chase is not None:
            client1_enemy_chase_distance, client1_enemy_chase_start, client1_enemy_chase_later = client1_enemy_chase
            client2_enemy_chase_distance, client2_enemy_chase_start, client2_enemy_chase_later = client2_enemy_chase
            print("client1-network-enemy-chase-start: " + client1_enemy_chase_start["_line"])
            print("client1-network-enemy-chase-later: " + client1_enemy_chase_later["_line"])
            print("client2-network-enemy-chase-start: " + client2_enemy_chase_start["_line"])
            print("client2-network-enemy-chase-later: " + client2_enemy_chase_later["_line"])
            print(
                "P6_NETWORK_ENEMY_CHASE_SYNC_OK"
                + f" client1ObservedEnemyMoveDistance={client1_enemy_chase_distance:.2f}"
                + f" client2ObservedEnemyMoveDistance={client2_enemy_chase_distance:.2f}"
            )

        if client1_enemy_health is not None and client2_enemy_health is not None:
            (
                client1_enemy_drop,
                client1_enemy_start_health,
                client1_enemy_later_health,
                client1_enemy_first_record,
                client1_enemy_later_record,
            ) = client1_enemy_health
            (
                client2_enemy_drop,
                client2_enemy_start_health,
                client2_enemy_later_health,
                client2_enemy_first_record,
                client2_enemy_later_record,
            ) = client2_enemy_health
            print("client1-network-enemy-health-start: " + client1_enemy_first_record["_line"])
            print("client1-network-enemy-health-later: " + client1_enemy_later_record["_line"])
            print("client2-network-enemy-health-start: " + client2_enemy_first_record["_line"])
            print("client2-network-enemy-health-later: " + client2_enemy_later_record["_line"])

            if client1_enemy_death is not None and client2_enemy_death is not None:
                (
                    client1_enemy_dead_start,
                    client1_enemy_dead_later,
                    client1_enemy_death_first_record,
                    client1_enemy_death_later_record,
                ) = client1_enemy_death
                (
                    client2_enemy_dead_start,
                    client2_enemy_dead_later,
                    client2_enemy_death_first_record,
                    client2_enemy_death_later_record,
                ) = client2_enemy_death
                print("client1-network-enemy-death-start: " + client1_enemy_death_first_record["_line"])
                print("client1-network-enemy-death-later: " + client1_enemy_death_later_record["_line"])
                print("client2-network-enemy-death-start: " + client2_enemy_death_first_record["_line"])
                print("client2-network-enemy-death-later: " + client2_enemy_death_later_record["_line"])
                print(
                    "P6_NETWORK_ENEMY_SYNC_OK"
                    + f" client1ObservedEnemyHealthStart={client1_enemy_start_health}"
                    + f" client1ObservedEnemyHealthLater={client1_enemy_later_health}"
                    + f" client1ObservedEnemyHealthDrop={client1_enemy_drop}"
                    + f" client1ObservedEnemyDeathStart={str(client1_enemy_dead_start).lower()}"
                    + f" client1ObservedEnemyDeathLater={str(client1_enemy_dead_later).lower()}"
                    + f" client2ObservedEnemyHealthStart={client2_enemy_start_health}"
                    + f" client2ObservedEnemyHealthLater={client2_enemy_later_health}"
                    + f" client2ObservedEnemyHealthDrop={client2_enemy_drop}"
                    + f" client2ObservedEnemyDeathStart={str(client2_enemy_dead_start).lower()}"
                    + f" client2ObservedEnemyDeathLater={str(client2_enemy_dead_later).lower()}"
                )

                if client1_formal_enemy_death is not None and client2_formal_enemy_death is not None:
                    (
                        client1_formal_enemy_dead_start,
                        client1_formal_enemy_dead_later,
                        client1_formal_enemy_death_first_record,
                        client1_formal_enemy_death_later_record,
                    ) = client1_formal_enemy_death
                    (
                        client2_formal_enemy_dead_start,
                        client2_formal_enemy_dead_later,
                        client2_formal_enemy_death_first_record,
                        client2_formal_enemy_death_later_record,
                    ) = client2_formal_enemy_death
                    print(
                        "client1-formal-network-enemy-death-start: "
                        + client1_formal_enemy_death_first_record["_line"])
                    print(
                        "client1-formal-network-enemy-death-later: "
                        + client1_formal_enemy_death_later_record["_line"])
                    print(
                        "client2-formal-network-enemy-death-start: "
                        + client2_formal_enemy_death_first_record["_line"])
                    print(
                        "client2-formal-network-enemy-death-later: "
                        + client2_formal_enemy_death_later_record["_line"])
                    print(
                        "P6_FORMAL_NETWORK_ENEMY_SYNC_OK"
                        + f" client1ObservedFormalEnemyDeathStart={str(client1_formal_enemy_dead_start).lower()}"
                        + f" client1ObservedFormalEnemyDeathLater={str(client1_formal_enemy_dead_later).lower()}"
                        + f" client2ObservedFormalEnemyDeathStart={str(client2_formal_enemy_dead_start).lower()}"
                        + f" client2ObservedFormalEnemyDeathLater={str(client2_formal_enemy_dead_later).lower()}"
                        + " client1ObservedFormalEnemyDriver=suppressed"
                        + " client2ObservedFormalEnemyDriver=suppressed"
                    )

                if (
                    client1_enemy_attack_health is not None
                    and client2_enemy_attack_health is not None
                    and client1_formal_enemy_attack is not None
                    and client2_formal_enemy_attack is not None
                ):
                    (
                        client1_enemy_attack_drop,
                        client1_enemy_attack_start_health,
                        client1_enemy_attack_later_health,
                        client1_enemy_attack_first_record,
                        client1_enemy_attack_later_record,
                    ) = client1_enemy_attack_health
                    (
                        client2_enemy_attack_drop,
                        client2_enemy_attack_start_health,
                        client2_enemy_attack_later_health,
                        client2_enemy_attack_first_record,
                        client2_enemy_attack_later_record,
                    ) = client2_enemy_attack_health
                    (
                        client1_formal_enemy_attack_start,
                        client1_formal_enemy_attack_later,
                        client1_formal_enemy_attack_first_record,
                        client1_formal_enemy_attack_later_record,
                    ) = client1_formal_enemy_attack
                    (
                        client2_formal_enemy_attack_start,
                        client2_formal_enemy_attack_later,
                        client2_formal_enemy_attack_first_record,
                        client2_formal_enemy_attack_later_record,
                    ) = client2_formal_enemy_attack
                    print("client1-network-enemy-attack-health-start: " + client1_enemy_attack_first_record["_line"])
                    print("client1-network-enemy-attack-health-later: " + client1_enemy_attack_later_record["_line"])
                    print("client2-network-enemy-attack-health-start: " + client2_enemy_attack_first_record["_line"])
                    print("client2-network-enemy-attack-health-later: " + client2_enemy_attack_later_record["_line"])
                    print(
                        "client1-formal-network-enemy-attack-start: "
                        + client1_formal_enemy_attack_first_record["_line"])
                    print(
                        "client1-formal-network-enemy-attack-later: "
                        + client1_formal_enemy_attack_later_record["_line"])
                    print(
                        "client2-formal-network-enemy-attack-start: "
                        + client2_formal_enemy_attack_first_record["_line"])
                    print(
                        "client2-formal-network-enemy-attack-later: "
                        + client2_formal_enemy_attack_later_record["_line"])
                    print(
                        "P6_NETWORK_ENEMY_ATTACK_SYNC_OK"
                        + f" client1ObservedTargetRole={client1_enemy_attack_role}"
                        + f" client1ObservedTargetHealthStart={client1_enemy_attack_start_health}"
                        + f" client1ObservedTargetHealthLater={client1_enemy_attack_later_health}"
                        + f" client1ObservedTargetHealthDrop={client1_enemy_attack_drop}"
                        + f" client2ObservedTargetRole={client2_enemy_attack_role}"
                        + f" client2ObservedTargetHealthStart={client2_enemy_attack_start_health}"
                        + f" client2ObservedTargetHealthLater={client2_enemy_attack_later_health}"
                        + f" client2ObservedTargetHealthDrop={client2_enemy_attack_drop}"
                        + f" client1ObservedFormalEnemyAttackStart={str(client1_formal_enemy_attack_start).lower()}"
                        + f" client1ObservedFormalEnemyAttackLater={str(client1_formal_enemy_attack_later).lower()}"
                        + f" client2ObservedFormalEnemyAttackStart={str(client2_formal_enemy_attack_start).lower()}"
                        + f" client2ObservedFormalEnemyAttackLater={str(client2_formal_enemy_attack_later).lower()}"
                    )

        observed_remote_despawn = client1_despawn is not None
        observed_remote_movement = client2_remote_move is not None
        observed_health_sync = client2_target_health is not None
        observed_formal_attack_sync = client2_remote_formal_attack is not None
        observed_formal_hit_sync = client2_target_formal_hit is not None
        observed_death_sync = client2_target_death is not None
        observed_formal_death_sync = client2_target_formal_death is not None
        observed_network_enemy_sync = client1_enemy_death is not None and client2_enemy_death is not None
        observed_network_enemy_chase_sync = client1_enemy_chase is not None and client2_enemy_chase is not None
        observed_network_enemy_count = client1_enemy_count_spawn is not None and client2_enemy_count_spawn is not None
        observed_network_enemy_navmesh_chase = False
        observed_network_enemy_server_tick = False
        observed_network_enemy_target_switch = False
        observed_network_enemy_target_distribution = False
        observed_network_enemy_target_retention = False
        observed_formal_network_enemy_sync = (
            client1_formal_enemy_death is not None
            and client2_formal_enemy_death is not None
            and client1_formal_enemy_driver is not None
            and client2_formal_enemy_driver is not None
        )
        observed_network_enemy_attack_sync = (
            client1_enemy_attack_health is not None
            and client2_enemy_attack_health is not None
            and client1_formal_enemy_attack is not None
            and client2_formal_enemy_attack is not None
        )

        if args.require_network_enemy_navmesh_chase:
            server_navmesh_chase_line = require_server_network_enemy_navmesh_chase(server_log)
            observed_network_enemy_navmesh_chase = True
            print("server-network-enemy-navmesh-chase: " + server_navmesh_chase_line)
            print("P6_NETWORK_ENEMY_NAVMESH_CHASE_OK navMeshReady=true")

        if args.require_network_enemy_server_tick:
            (
                server_tick_line,
                server_tick_attack_count,
                target_switch_summary,
                target_distribution_summary,
                target_retention_summary,
            ) = require_server_network_enemy_server_tick(
                server_log,
                max(1, args.min_network_enemy_server_tick_attacks),
                args.require_network_enemy_target_switch,
                args.min_network_enemy_initial_target_attacks,
                args.require_network_enemy_target_distribution,
                args.min_network_enemy_count,
                args.min_network_enemy_target_distribution_targets,
                args.require_network_enemy_target_retention,
                args.min_network_enemy_target_retention_attacks)
            observed_network_enemy_server_tick = True
            print("server-network-enemy-server-tick: " + server_tick_line)
            print(
                "P6_NETWORK_ENEMY_SERVER_TICK_OK"
                + " navMeshReady=true"
                + f" serverTickAttackCount={server_tick_attack_count}"
            )
            if target_switch_summary is not None:
                observed_network_enemy_target_switch = True
                print("server-network-enemy-target-switch: " + target_switch_summary)
                print("P6_NETWORK_ENEMY_TARGET_SWITCH_OK " + target_switch_summary)
            if target_distribution_summary is not None:
                observed_network_enemy_target_distribution = True
                print("server-network-enemy-target-distribution: " + target_distribution_summary)
                print("P6_NETWORK_ENEMY_TARGET_DISTRIBUTION_OK " + target_distribution_summary)
            if target_retention_summary is not None:
                observed_network_enemy_target_retention = True
                print("server-network-enemy-target-retention: " + target_retention_summary)
                print("P6_NETWORK_ENEMY_TARGET_RETENTION_OK " + target_retention_summary)

        print(
            "P1.5_CLIENT_VISIBILITY_OK"
            + " mutual=true"
            + f" client1ObservedRemoteDespawn={str(observed_remote_despawn).lower()}"
            + f" client2ObservedRemoteMovement={str(observed_remote_movement).lower()}"
            + f" client2ObservedRemoteHealthSync={str(observed_health_sync).lower()}"
            + f" client2ObservedRemoteFormalAttackSync={str(observed_formal_attack_sync).lower()}"
            + f" client2ObservedLocalFormalHitSync={str(observed_formal_hit_sync).lower()}"
            + f" client2ObservedLocalDeathSync={str(observed_death_sync).lower()}"
            + f" client2ObservedLocalFormalDeathSync={str(observed_formal_death_sync).lower()}"
            + f" networkEnemySync={str(observed_network_enemy_sync).lower()}"
            + f" networkEnemyCount={str(observed_network_enemy_count).lower()}"
            + f" networkEnemyChaseSync={str(observed_network_enemy_chase_sync).lower()}"
            + f" networkEnemyNavMeshChaseSync={str(observed_network_enemy_navmesh_chase).lower()}"
            + f" networkEnemyServerTick={str(observed_network_enemy_server_tick).lower()}"
            + f" networkEnemyTargetSwitch={str(observed_network_enemy_target_switch).lower()}"
            + f" networkEnemyTargetDistribution={str(observed_network_enemy_target_distribution).lower()}"
            + f" networkEnemyTargetRetention={str(observed_network_enemy_target_retention).lower()}"
            + f" formalNetworkEnemySync={str(observed_formal_network_enemy_sync).lower()}"
            + f" networkEnemyAttackSync={str(observed_network_enemy_attack_sync).lower()}"
        )

        print(
            "P3_MULTIPLAYER_OK"
            + f" host={host}"
            + f" gamePort={args.game_port}"
            + f" healthPort={args.health_port}"
            + f" connected={connected_clients}"
            + f" spawned={spawned_players}"
            + " mutualVisibility=true"
            + f" clientDespawnObserved={str(observed_remote_despawn).lower()}"
            + f" remotePositionSync={str(observed_remote_movement).lower()}"
            + f" healthSync={str(observed_health_sync).lower()}"
            + f" deathSync={str(observed_death_sync).lower()}"
            + f" networkEnemyCount={str(observed_network_enemy_count).lower()}"
            + " disconnected=0"
        )

        if observed_death_sync:
            print(
                "P4_MULTIPLAYER_OK"
                + f" host={host}"
                + f" gamePort={args.game_port}"
                + f" healthPort={args.health_port}"
                + f" connected={connected_clients}"
                + f" spawned={spawned_players}"
                + " mutualVisibility=true"
                + f" clientDespawnObserved={str(observed_remote_despawn).lower()}"
                + f" remotePositionSync={str(observed_remote_movement).lower()}"
                + f" healthSync={str(observed_health_sync).lower()}"
                + f" deathSync={str(observed_death_sync).lower()}"
                + f" formalDeathSync={str(observed_formal_death_sync).lower()}"
                + f" formalAttackSync={str(observed_formal_attack_sync).lower()}"
                + f" formalHitSync={str(observed_formal_hit_sync).lower()}"
                + f" networkEnemySync={str(observed_network_enemy_sync).lower()}"
                + f" networkEnemyCount={str(observed_network_enemy_count).lower()}"
                + f" networkEnemyChaseSync={str(observed_network_enemy_chase_sync).lower()}"
                + f" networkEnemyNavMeshChaseSync={str(observed_network_enemy_navmesh_chase).lower()}"
                + f" networkEnemyServerTick={str(observed_network_enemy_server_tick).lower()}"
                + f" networkEnemyTargetSwitch={str(observed_network_enemy_target_switch).lower()}"
                + f" networkEnemyTargetDistribution={str(observed_network_enemy_target_distribution).lower()}"
                + f" networkEnemyTargetRetention={str(observed_network_enemy_target_retention).lower()}"
                + f" formalNetworkEnemySync={str(observed_formal_network_enemy_sync).lower()}"
                + f" networkEnemyAttackSync={str(observed_network_enemy_attack_sync).lower()}"
                + " disconnected=0"
            )

        if observed_network_enemy_sync or observed_network_enemy_server_tick or observed_network_enemy_target_distribution or observed_network_enemy_target_retention:
            print(
                "P6_MULTIPLAYER_OK"
                + f" host={host}"
                + f" gamePort={args.game_port}"
                + f" healthPort={args.health_port}"
                + f" connected={connected_clients}"
                + f" spawned={spawned_players}"
                + " mutualVisibility=true"
                + f" clientDespawnObserved={str(observed_remote_despawn).lower()}"
                + f" remotePositionSync={str(observed_remote_movement).lower()}"
                + f" healthSync={str(observed_health_sync).lower()}"
                + f" deathSync={str(observed_death_sync).lower()}"
                + f" formalDeathSync={str(observed_formal_death_sync).lower()}"
                + f" formalAttackSync={str(observed_formal_attack_sync).lower()}"
                + f" formalHitSync={str(observed_formal_hit_sync).lower()}"
                + f" networkEnemySync={str(observed_network_enemy_sync).lower()}"
                + f" networkEnemyCount={str(observed_network_enemy_count).lower()}"
                + f" networkEnemyChaseSync={str(observed_network_enemy_chase_sync).lower()}"
                + f" networkEnemyNavMeshChaseSync={str(observed_network_enemy_navmesh_chase).lower()}"
                + f" networkEnemyServerTick={str(observed_network_enemy_server_tick).lower()}"
                + f" networkEnemyTargetSwitch={str(observed_network_enemy_target_switch).lower()}"
                + f" networkEnemyTargetDistribution={str(observed_network_enemy_target_distribution).lower()}"
                + f" networkEnemyTargetRetention={str(observed_network_enemy_target_retention).lower()}"
                + f" formalNetworkEnemySync={str(observed_formal_network_enemy_sync).lower()}"
                + f" networkEnemyAttackSync={str(observed_network_enemy_attack_sync).lower()}"
                + " disconnected=0"
            )
    except Exception:
        print("\n--- server log tail ---", file=sys.stderr)
        print(tail_log(server_log), file=sys.stderr)
        for name, _process, log_path in client_processes:
            print(f"\n--- {name} log tail ---", file=sys.stderr)
            print(tail_log(log_path), file=sys.stderr)
        raise
    finally:
        for name, process, _log_path in client_processes:
            stop_process(process, name, args.stop_timeout)

        if not args.skip_server_start:
            stop_process(server_process, "server", args.stop_timeout)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--host", default="127.0.0.1", help="server address for clients")
    parser.add_argument(
        "--health-host",
        default="",
        help="health address; defaults to --host",
    )
    parser.add_argument(
        "--server-bind-address",
        default="",
        help="local server bind address; defaults to --host",
    )
    parser.add_argument("--game-port", type=int, default=7797, help="NGO/UTP game port")
    parser.add_argument("--health-port", type=int, default=7798, help="TCP health port")
    parser.add_argument(
        "--server-bin",
        default=str(DEFAULT_SERVER_BIN),
        help="local macOS server player binary",
    )
    parser.add_argument(
        "--client-bin",
        default=str(DEFAULT_CLIENT_BIN),
        help="local macOS client player binary",
    )
    parser.add_argument(
        "--skip-server-start",
        action="store_true",
        help="probe an already running local or ECS server",
    )
    parser.add_argument(
        "--network-player-prefab",
        default="",
        help="Resources path for the NGO player prefab used by local server and clients",
    )
    parser.add_argument(
        "--health-only",
        action="store_true",
        help="validate an already running server's P1.5 NGO health and exit",
    )
    parser.add_argument(
        "--server-quit-after-seconds",
        type=int,
        default=120,
        help="quit timer passed to a locally started server",
    )
    parser.add_argument(
        "--client-quit-after-seconds",
        type=int,
        default=25,
        help="legacy quit timer fallback for each batchmode client",
    )
    parser.add_argument(
        "--client1-quit-after-seconds",
        type=int,
        default=35,
        help="quit timer passed to the first batchmode client",
    )
    parser.add_argument(
        "--client2-quit-after-seconds",
        type=int,
        default=15,
        help="quit timer passed to the second batchmode client",
    )
    parser.add_argument(
        "--smoke-report-interval-seconds",
        type=int,
        default=1,
        help="client-side multiplayer smoke report interval",
    )
    parser.add_argument(
        "--skip-client-despawn-check",
        action="store_true",
        help="do not require client1 to observe client2 despawn",
    )
    parser.add_argument(
        "--skip-remote-movement-check",
        action="store_true",
        help="do not require client2 to observe client1 remote position changes",
    )
    parser.add_argument(
        "--skip-health-sync-check",
        action="store_true",
        help="do not require client2 to observe server-authoritative health changes",
    )
    parser.add_argument(
        "--require-death-sync",
        action="store_true",
        help="require client2 to observe its local avatar transition from alive to dead",
    )
    parser.add_argument(
        "--require-formal-attack-sync",
        action="store_true",
        help="require client2 to observe the remote formal PlayerAttackState transition",
    )
    parser.add_argument(
        "--require-formal-hit-sync",
        action="store_true",
        help="require client2 to observe its local formal PlayerHitState transition",
    )
    parser.add_argument(
        "--require-formal-death-sync",
        action="store_true",
        help="require client2 to observe its local formal PlayerDeathState transition",
    )
    parser.add_argument(
        "--skip-auto-formal-sync-requirements",
        action="store_true",
        help="do not auto-require formal player attack/hit/death sync for PF_NetworkPlayerCombatTest",
    )
    parser.add_argument(
        "--require-network-enemy-sync",
        action="store_true",
        help="require both clients to observe the server-spawned network enemy HP and death sync",
    )
    parser.add_argument(
        "--require-formal-network-enemy-sync",
        action="store_true",
        help="require both clients to observe formal enemy death and client-side driver suppression",
    )
    parser.add_argument(
        "--require-network-enemy-attack-sync",
        action="store_true",
        help="enable and require server-authored network enemy attack damage plus formal enemy attack presentation",
    )
    parser.add_argument(
        "--require-network-enemy-chase-sync",
        action="store_true",
        help="require both clients to observe the server-spawned network enemy move before the enemy attack smoke completes",
    )
    parser.add_argument(
        "--require-network-enemy-navmesh-chase",
        action="store_true",
        help="require the local server log to prove P6.9 EnemyBrain chase used a valid NavMeshAgent instead of fallback movement",
    )
    parser.add_argument(
        "--require-network-enemy-server-tick",
        action="store_true",
        help="require the local server log to prove P6.11 uses the non-smoke server enemy gameplay tick path",
    )
    parser.add_argument(
        "--min-network-enemy-server-tick-attacks",
        type=int,
        default=1,
        help="minimum Server tick enemy attack applied log lines required when --require-network-enemy-server-tick is set",
    )
    parser.add_argument(
        "--require-network-enemy-target-switch",
        action="store_true",
        help="require server tick enemy attacks to stay on the initial target until death, then switch to another live target",
    )
    parser.add_argument(
        "--require-network-enemy-target-distribution",
        action="store_true",
        help="require multiple server tick enemies to initially attack different network player targets",
    )
    parser.add_argument(
        "--require-network-enemy-target-retention",
        action="store_true",
        help="require multiple server tick enemies to retain distinct live targets for a sustained attack window",
    )
    parser.add_argument(
        "--min-network-enemy-initial-target-attacks",
        type=int,
        default=2,
        help="minimum applied attacks required on the initial target before a required server tick target switch",
    )
    parser.add_argument(
        "--network-enemy-server-tick-death-delay-seconds",
        type=int,
        default=None,
        help="local server override for the server tick enemy smoke death delay; defaults to 24 when target switch is required",
    )
    parser.add_argument(
        "--network-enemy-server-tick-damage",
        type=int,
        default=None,
        help="local server override for the network damage applied by server tick enemies",
    )
    parser.add_argument(
        "--network-enemy-count",
        type=int,
        default=None,
        help="local server override for how many network enemies to spawn",
    )
    parser.add_argument(
        "--min-network-enemy-count",
        type=int,
        default=1,
        help="minimum network enemies each client must observe before the smoke can pass",
    )
    parser.add_argument(
        "--min-network-enemy-target-distribution-targets",
        type=int,
        default=2,
        help="minimum distinct initial target owners required when --require-network-enemy-target-distribution is set",
    )
    parser.add_argument(
        "--min-network-enemy-target-retention-attacks",
        type=int,
        default=3,
        help="minimum same-target, live-target attacks each enemy must retain when --require-network-enemy-target-retention is set",
    )
    parser.add_argument(
        "--use-formal-network-enemy-attack-smoke",
        action="store_true",
        help="with --require-network-enemy-attack-sync, start the local server with the P6.5 formal EnemyAttackController-driven attack smoke",
    )
    parser.add_argument(
        "--use-brain-network-enemy-attack-smoke",
        action="store_true",
        help="with --require-network-enemy-attack-sync, start the local server with the P6.6 formal EnemyBrain-driven attack smoke",
    )
    parser.add_argument(
        "--use-brain-chase-network-enemy-attack-smoke",
        action="store_true",
        help="with --require-network-enemy-attack-sync, start the local server with the P6.8 formal EnemyBrain/EnemyMotor chase attack smoke",
    )
    parser.add_argument(
        "--min-remote-move-distance",
        type=float,
        default=0.25,
        help="minimum observed remote movement distance required in client logs",
    )
    parser.add_argument(
        "--smoke-move-duration-seconds",
        type=float,
        default=2.0,
        help="duration for client1 smoke move",
    )
    parser.add_argument(
        "--smoke-move-delay-seconds",
        type=float,
        default=4.0,
        help="delay before client1 starts smoke movement; default keeps attack at spawn position",
    )
    parser.add_argument(
        "--smoke-attack-id",
        default="Light_01",
        help="client1 smoke attack id; server resolves this against its local whitelist",
    )
    parser.add_argument(
        "--smoke-attack-damage-amount",
        type=int,
        default=9999,
        help="client1 smoke attack damage intent; server ignores this magnitude",
    )
    parser.add_argument(
        "--smoke-attack-delay-seconds",
        type=float,
        default=3.0,
        help="delay before client1 sends the smoke attack intent",
    )
    parser.add_argument(
        "--smoke-attack-count",
        type=int,
        default=1,
        help="number of client1 smoke attack intents to send",
    )
    parser.add_argument(
        "--smoke-attack-interval-seconds",
        type=float,
        default=0.75,
        help="interval between repeated client1 smoke attack intents",
    )
    parser.add_argument(
        "--min-remote-health-drop",
        type=int,
        default=1,
        help="minimum remote HP drop required in client2 logs",
    )
    parser.add_argument(
        "--min-network-enemy-health-drop",
        type=int,
        default=50,
        help="minimum server-spawned network enemy HP drop required in both client logs",
    )
    parser.add_argument(
        "--min-network-enemy-attack-health-drop",
        type=int,
        default=25,
        help="minimum player HP drop from the server-authored network enemy attack",
    )
    parser.add_argument(
        "--min-network-enemy-chase-distance",
        type=float,
        default=2.0,
        help="minimum network enemy movement distance required when --require-network-enemy-chase-sync is set",
    )
    parser.add_argument(
        "--server-log",
        default="/tmp/TY_NEW_p15_probe_server.log",
        help="server log path",
    )
    parser.add_argument(
        "--client1-log",
        default="/tmp/TY_NEW_p15_probe_client1.log",
        help="first client log path",
    )
    parser.add_argument(
        "--client2-log",
        default="/tmp/TY_NEW_p15_probe_client2.log",
        help="second client log path",
    )
    parser.add_argument("--socket-timeout", type=float, default=5.0)
    parser.add_argument("--startup-timeout", type=float, default=45.0)
    parser.add_argument("--connected-timeout", type=float, default=45.0)
    parser.add_argument("--disconnect-timeout", type=float, default=20.0)
    parser.add_argument("--client-exit-grace-seconds", type=float, default=15.0)
    parser.add_argument("--stop-timeout", type=float, default=8.0)
    args = parser.parse_args()

    if args.require_network_enemy_target_retention:
        args.require_network_enemy_target_distribution = True

    if args.require_network_enemy_target_distribution:
        args.require_network_enemy_server_tick = True
        args.min_network_enemy_count = max(2, args.min_network_enemy_count)
        args.min_network_enemy_server_tick_attacks = max(
            args.min_network_enemy_server_tick_attacks,
            args.min_network_enemy_count)

    if args.require_network_enemy_target_retention:
        args.min_network_enemy_server_tick_attacks = max(
            args.min_network_enemy_server_tick_attacks,
            args.min_network_enemy_count * max(1, args.min_network_enemy_target_retention_attacks))

    if args.require_network_enemy_target_switch:
        args.require_network_enemy_server_tick = True

    if args.require_network_enemy_server_tick:
        if not args.require_network_enemy_target_distribution:
            args.require_network_enemy_attack_sync = True
            args.require_network_enemy_chase_sync = True
        args.require_network_enemy_navmesh_chase = True

    if args.require_network_enemy_target_switch and args.network_enemy_server_tick_death_delay_seconds is None:
        args.network_enemy_server_tick_death_delay_seconds = 24

    args.min_network_enemy_count = max(1, args.min_network_enemy_count)
    if args.network_enemy_count is None and args.min_network_enemy_count > 1:
        args.network_enemy_count = args.min_network_enemy_count

    if args.require_network_enemy_navmesh_chase and not args.require_network_enemy_server_tick:
        args.require_network_enemy_attack_sync = True
        args.use_brain_chase_network_enemy_attack_smoke = True
        args.require_network_enemy_chase_sync = True

    if args.use_brain_chase_network_enemy_attack_smoke:
        args.require_network_enemy_chase_sync = True

    if args.require_network_enemy_chase_sync:
        args.require_network_enemy_sync = True

    try:
        if args.health_only:
            run_health_only_probe(args)
        else:
            run_probe(args)
    except Exception as exc:
        print(f"P1.5/P3/P3.5/P4 multiplayer probe failed: {exc}", file=sys.stderr)
        return 1

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
