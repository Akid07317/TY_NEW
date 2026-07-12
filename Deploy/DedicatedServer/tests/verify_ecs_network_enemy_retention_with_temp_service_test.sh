#!/bin/sh
set -eu

PROJECT_ROOT=$(CDPATH= cd -- "$(dirname -- "$0")/../../.." && pwd)
SCRIPT="$PROJECT_ROOT/Deploy/DedicatedServer/verify_ecs_network_enemy_retention_with_temp_service.sh"
FAKE_SSH="$PROJECT_ROOT/Deploy/DedicatedServer/tests/fake_ssh.sh"
SUCCESS_COMMAND=/usr/bin/true
FAILURE_COMMAND=/usr/bin/false

fail() {
  echo "FAIL: $*" >&2
  exit 1
}

expect_contains() {
  OUTPUT=$1
  EXPECTED=$2
  case "$OUTPUT" in
    *"$EXPECTED"*) ;;
    *) fail "missing output marker: $EXPECTED" ;;
  esac
}

[ -x "$SCRIPT" ] || fail "script is not executable: $SCRIPT"
[ -x "$FAKE_SSH" ] || fail "fake SSH is not executable: $FAKE_SSH"
[ -x "$SUCCESS_COMMAND" ] || fail "missing success command: $SUCCESS_COMMAND"
[ -x "$FAILURE_COMMAND" ] || fail "missing failure command: $FAILURE_COMMAND"

SUCCESS_OUTPUT=$(
  TY_NEW_SSH_COMMAND="$FAKE_SSH" \
  TY_NEW_VERIFY_SCRIPT="$SUCCESS_COMMAND" \
  TY_NEW_PROBE_SCRIPT="$SUCCESS_COMMAND" \
    "$SCRIPT" \
      --enemy-count 5 \
      --tick-damage 4 \
      --death-delay-seconds 120 \
      --retention-attacks 6 \
      --rounds 2 \
      --client1-seconds 140 \
      --client2-seconds 120 \
      test-user example.invalid
)
expect_contains "$SUCCESS_OUTPUT" "TEMP_SERVICE_VERIFICATION_OK enemyCount=5 tickDamage=4 retainedAttacks=6"
expect_contains "$SUCCESS_OUTPUT" "TEMP_SERVICE_ROUND_OK round=1 rounds=2 pid=4242"
expect_contains "$SUCCESS_OUTPUT" "TEMP_SERVICE_ROUND_OK round=2 rounds=2 pid=4242"
expect_contains "$SUCCESS_OUTPUT" "TEMP_SERVICE_PID_OK pid=4242 round=2 checkpoint=after"
expect_contains "$SUCCESS_OUTPUT" "SERVICE_RESTORE_HEALTH_OK baselineEnemyCount=3 baselineTickDamage=10 baselineDeathDelaySeconds=90"
expect_contains "$SUCCESS_OUTPUT" "ECS_RETENTION_EXPERIMENT_OK temporaryEnemyCount=5 temporaryTickDamage=4 retainedAttacks=6 completedRounds=2 requestedRounds=2 temporaryServicePid=4242"

set +e
VERIFY_FAILURE_OUTPUT=$(
  TY_NEW_SSH_COMMAND="$FAKE_SSH" \
  TY_NEW_VERIFY_SCRIPT="$FAILURE_COMMAND" \
  TY_NEW_PROBE_SCRIPT="$SUCCESS_COMMAND" \
    "$SCRIPT" test-user example.invalid 2>&1
)
VERIFY_FAILURE_STATUS=$?
set -e
[ "$VERIFY_FAILURE_STATUS" -eq 1 ] || fail "verification failure returned $VERIFY_FAILURE_STATUS instead of 1"
expect_contains "$VERIFY_FAILURE_OUTPUT" "TEMP_SERVICE_VERIFICATION_FAILED status=1"
expect_contains "$VERIFY_FAILURE_OUTPUT" "SERVICE_RESTORE_HEALTH_OK"

set +e
INSTALL_FAILURE_OUTPUT=$(
  TY_NEW_FAKE_SSH_FAIL_INSTALL=1 \
  TY_NEW_SSH_COMMAND="$FAKE_SSH" \
  TY_NEW_VERIFY_SCRIPT="$SUCCESS_COMMAND" \
  TY_NEW_PROBE_SCRIPT="$SUCCESS_COMMAND" \
    "$SCRIPT" test-user example.invalid 2>&1
)
INSTALL_FAILURE_STATUS=$?
set -e
[ "$INSTALL_FAILURE_STATUS" -eq 42 ] || fail "install failure returned $INSTALL_FAILURE_STATUS instead of 42"
expect_contains "$INSTALL_FAILURE_OUTPUT" "SERVICE_RESTORE_RECOVERY_BEGIN priorStatus=42"
expect_contains "$INSTALL_FAILURE_OUTPUT" "SERVICE_RESTORE_RECOVERY_OK"

set +e
RESTORE_FAILURE_OUTPUT=$(
  TY_NEW_FAKE_SSH_FAIL_RESTORE=1 \
  TY_NEW_SSH_COMMAND="$FAKE_SSH" \
  TY_NEW_VERIFY_SCRIPT="$SUCCESS_COMMAND" \
  TY_NEW_PROBE_SCRIPT="$SUCCESS_COMMAND" \
    "$SCRIPT" test-user example.invalid 2>&1
)
RESTORE_FAILURE_STATUS=$?
set -e
[ "$RESTORE_FAILURE_STATUS" -eq 1 ] || fail "restore failure returned $RESTORE_FAILURE_STATUS instead of 1"
expect_contains "$RESTORE_FAILURE_OUTPUT" "SERVICE_RESTORE_RECOVERY_FAILED manual intervention required"

set +e
UNSAFE_USER_OUTPUT=$("$SCRIPT" --dry-run -- -V example.invalid 2>&1)
UNSAFE_USER_STATUS=$?
set -e
[ "$UNSAFE_USER_STATUS" -eq 1 ] || fail "unsafe SSH user returned $UNSAFE_USER_STATUS instead of 1"
expect_contains "$UNSAFE_USER_OUTPUT" "SSH user contains unsupported characters: -V"

set +e
UNSAFE_ROUNDS_OUTPUT=$("$SCRIPT" --dry-run --rounds 11 test-user example.invalid 2>&1)
UNSAFE_ROUNDS_STATUS=$?
set -e
[ "$UNSAFE_ROUNDS_STATUS" -eq 1 ] || fail "unsafe rounds returned $UNSAFE_ROUNDS_STATUS instead of 1"
expect_contains "$UNSAFE_ROUNDS_OUTPUT" "TY_NEW_ROUNDS must be at most 10."

echo "verify_ecs_network_enemy_retention_with_temp_service contract tests passed."
