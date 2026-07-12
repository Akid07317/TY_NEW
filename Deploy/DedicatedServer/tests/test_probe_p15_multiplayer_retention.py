#!/usr/bin/env python3
"""Offline contracts for P6 server-tick target-retention diagnostics."""

from __future__ import annotations

import importlib.util
import unittest
from pathlib import Path


PROJECT_ROOT = Path(__file__).resolve().parents[3]
PROBE_PATH = PROJECT_ROOT / "Deploy/DedicatedServer/probe_p15_multiplayer.py"
SPEC = importlib.util.spec_from_file_location("probe_p15_multiplayer", PROBE_PATH)
if SPEC is None or SPEC.loader is None:
    raise RuntimeError(f"could not load probe module from {PROBE_PATH}")

PROBE = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(PROBE)


def attack_line(enemy_id: int, target_owner: int, previous_health: int, next_health: int) -> str:
    return (
        "[MultiplayerEnemy] Server tick enemy attack applied"
        + f" enemyId={enemy_id}"
        + f" targetOwner={target_owner}"
        + f" damage={previous_health - next_health}"
        + " formalDamage=0 attackId=ServerGameplayTickFallback"
        + f" health={previous_health}->{next_health}"
        + f" targetDead={str(next_health <= 0)}"
    )


def p630_damage_ten_lines() -> list[str]:
    enemy_sequence = [1, 2, 1, 2, 4, 1, 2, 4, 3, 1, 2, 4, 3, 5, 1, 2, 4, 3, 5, 1]
    target_by_enemy = {1: 1, 2: 2, 3: 2, 4: 1, 5: 2}
    health_by_target = {1: 100, 2: 100}
    lines: list[str] = []

    for enemy_id in enemy_sequence:
        target_owner = target_by_enemy[enemy_id]
        previous_health = health_by_target[target_owner]
        next_health = max(0, previous_health - 10)
        health_by_target[target_owner] = next_health
        lines.append(attack_line(enemy_id, target_owner, previous_health, next_health))

    return lines


class ServerTickTargetRetentionDiagnosticTests(unittest.TestCase):
    def test_damage_ten_failure_reports_health_budget_and_enemy_deficits(self) -> None:
        diagnostic = PROBE.summarize_server_tick_target_retention_failure(
            p630_damage_ten_lines(),
            min_enemy_count=5,
            min_retained_attacks=4,
        )

        self.assertIn(
            "classification=health_budget_exhausted_with_uneven_enemy_scheduling",
            diagnostic,
        )
        self.assertIn("observedAttacks=20 requiredAttacks=20", diagnostic)
        self.assertIn("enemyAttackCounts=1:6,2:5,3:3,4:4,5:2", diagnostic)
        self.assertIn("enemyAttackDeficits=3:1,5:2", diagnostic)
        self.assertIn("missingEnemyAttackSlots=3 excessEnemyAttacks=3", diagnostic)
        self.assertIn(
            "targetBudgets=1:hits=10/damage=100/health=100->0/dead=true/killedByEnemy=1"
            "|2:hits=10/damage=100/health=100->0/dead=true/killedByEnemy=5",
            diagnostic,
        )
        self.assertIn("allTargetsDead=true", diagnostic)
        self.assertIn("attackSequence=1:1>1:100->90", diagnostic)
        self.assertTrue(diagnostic.endswith("20:1>1:10->0:dead"))

    def test_retention_failure_includes_machine_readable_diagnostic_line(self) -> None:
        with self.assertRaises(PROBE.ProbeError) as context:
            PROBE.require_server_tick_target_retention(
                p630_damage_ten_lines(),
                min_enemy_count=5,
                min_target_count=2,
                min_retained_attacks=4,
            )

        message = str(context.exception)
        self.assertIn("enemyId=3 expected>=4 actual=3", message)
        self.assertIn("\nP6_NETWORK_ENEMY_TARGET_RETENTION_DIAGNOSTIC ", message)
        self.assertIn("enemyAttackDeficits=3:1,5:2", message)

    def test_balanced_low_damage_retention_still_passes(self) -> None:
        lines: list[str] = []
        health_by_target = {1: 100, 2: 100}
        target_by_enemy = {1: 1, 2: 2, 3: 2, 4: 1, 5: 2}

        for _ in range(4):
            for enemy_id in range(1, 6):
                target_owner = target_by_enemy[enemy_id]
                previous_health = health_by_target[target_owner]
                next_health = previous_health - 5
                health_by_target[target_owner] = next_health
                lines.append(attack_line(enemy_id, target_owner, previous_health, next_health))

        summary = PROBE.require_server_tick_target_retention(
            lines,
            min_enemy_count=5,
            min_target_count=2,
            min_retained_attacks=4,
        )

        self.assertIn("uniqueTargetCount=2", summary)
        self.assertIn("retainedAttackCounts=1:4,2:4,3:4,4:4,5:4", summary)


if __name__ == "__main__":
    unittest.main()
