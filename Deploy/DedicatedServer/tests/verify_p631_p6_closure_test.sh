#!/bin/sh
set -eu

PROJECT_ROOT=$(CDPATH= cd -- "$(dirname -- "$0")/../../.." && pwd)
SCRIPT="$PROJECT_ROOT/Deploy/DedicatedServer/verify_p631_p6_closure.sh"
FAKE_SSH="$PROJECT_ROOT/Deploy/DedicatedServer/tests/fake_p631_ssh.sh"
FAKE_PROBE="$PROJECT_ROOT/Deploy/DedicatedServer/tests/fake_p631_probe.sh"
SUCCESS_COMMAND=/usr/bin/true
FAILURE_COMMAND=/usr/bin/false
PID_STATE_FILE="/tmp/TY_NEW_p631_fake_pid_state.$$"

cleanup() {
  rm -f "$PID_STATE_FILE"
}
trap cleanup EXIT INT TERM HUP

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

expect_not_contains() {
  OUTPUT=$1
  UNEXPECTED=$2
  case "$OUTPUT" in
    *"$UNEXPECTED"*) fail "unexpected output marker: $UNEXPECTED" ;;
    *) ;;
  esac
}

[ -x "$SCRIPT" ] || fail "script is not executable: $SCRIPT"
[ -x "$FAKE_SSH" ] || fail "fake SSH is not executable: $FAKE_SSH"
[ -x "$FAKE_PROBE" ] || fail "fake probe is not executable: $FAKE_PROBE"

SUCCESS_OUTPUT=$(
  TY_NEW_SSH_COMMAND="$FAKE_SSH" \
  TY_NEW_VERIFY_SCRIPT="$SUCCESS_COMMAND" \
  TY_NEW_PROBE_SCRIPT="$FAKE_PROBE" \
    "$SCRIPT" test-user example.invalid
)
expect_contains "$SUCCESS_OUTPUT" "P6_CLOSURE_BASELINE_OK checkpoint=before enemyCount=3 tickDamage=10 deathDelaySeconds=90"
expect_contains "$SUCCESS_OUTPUT" "P6_CLOSURE_PID_CAPTURED pid=4242"
expect_contains "$SUCCESS_OUTPUT" "P6_CLOSURE_VERIFICATION_OK enemyCount=3 tickDamage=10 retainedAttacks=4"
expect_contains "$SUCCESS_OUTPUT" "P6_CLOSURE_PID_UNCHANGED_OK pid=4242"
expect_contains "$SUCCESS_OUTPUT" "P6_CLOSURE_CAPABILITY_MATRIX"
expect_contains "$SUCCESS_OUTPUT" "P6_CLOSURE_OK p6Status=closed"

set +e
BASELINE_FAILURE_OUTPUT=$(
  TY_NEW_FAKE_P631_ENEMY_COUNT=4 \
  TY_NEW_SSH_COMMAND="$FAKE_SSH" \
  TY_NEW_VERIFY_SCRIPT="$SUCCESS_COMMAND" \
  TY_NEW_PROBE_SCRIPT="$FAKE_PROBE" \
    "$SCRIPT" test-user example.invalid 2>&1
)
BASELINE_FAILURE_STATUS=$?
set -e
[ "$BASELINE_FAILURE_STATUS" -eq 1 ] || fail "baseline failure returned $BASELINE_FAILURE_STATUS instead of 1"
expect_contains "$BASELINE_FAILURE_OUTPUT" "P6_CLOSURE_FAILED before unit file is missing required argument: --network-enemy-count 3"
expect_not_contains "$BASELINE_FAILURE_OUTPUT" "P6_CLOSURE_OK"

set +e
LOCK_FAILURE_OUTPUT=$(
  TY_NEW_FAKE_P631_LOCK_EXISTS=1 \
  TY_NEW_SSH_COMMAND="$FAKE_SSH" \
  TY_NEW_VERIFY_SCRIPT="$SUCCESS_COMMAND" \
  TY_NEW_PROBE_SCRIPT="$FAKE_PROBE" \
    "$SCRIPT" test-user example.invalid 2>&1
)
LOCK_FAILURE_STATUS=$?
set -e
[ "$LOCK_FAILURE_STATUS" -eq 1 ] || fail "lock failure returned $LOCK_FAILURE_STATUS instead of 1"
expect_contains "$LOCK_FAILURE_OUTPUT" "retention experiment lock exists"

set +e
HEALTH_FAILURE_OUTPUT=$(
  TY_NEW_FAKE_P631_CONNECTED=1 \
  TY_NEW_SSH_COMMAND="$FAKE_SSH" \
  TY_NEW_VERIFY_SCRIPT="$SUCCESS_COMMAND" \
  TY_NEW_PROBE_SCRIPT="$FAKE_PROBE" \
    "$SCRIPT" test-user example.invalid 2>&1
)
HEALTH_FAILURE_STATUS=$?
set -e
[ "$HEALTH_FAILURE_STATUS" -eq 1 ] || fail "health failure returned $HEALTH_FAILURE_STATUS instead of 1"
expect_contains "$HEALTH_FAILURE_OUTPUT" "health did not prove connected=0 spawned=0 checkpoint=before"

set +e
VERIFY_FAILURE_OUTPUT=$(
  TY_NEW_SSH_COMMAND="$FAKE_SSH" \
  TY_NEW_VERIFY_SCRIPT="$FAILURE_COMMAND" \
  TY_NEW_PROBE_SCRIPT="$FAKE_PROBE" \
    "$SCRIPT" test-user example.invalid 2>&1
)
VERIFY_FAILURE_STATUS=$?
set -e
[ "$VERIFY_FAILURE_STATUS" -eq 1 ] || fail "verification failure returned $VERIFY_FAILURE_STATUS instead of 1"
expect_contains "$VERIFY_FAILURE_OUTPUT" "P6_CLOSURE_VERIFICATION_FAILED status=1 pid=4242"
expect_not_contains "$VERIFY_FAILURE_OUTPUT" "P6_CLOSURE_OK"

set +e
PID_DRIFT_OUTPUT=$(
  TY_NEW_FAKE_P631_PID_STATE_FILE="$PID_STATE_FILE" \
  TY_NEW_FAKE_P631_PID_AFTER=4343 \
  TY_NEW_SSH_COMMAND="$FAKE_SSH" \
  TY_NEW_VERIFY_SCRIPT="$SUCCESS_COMMAND" \
  TY_NEW_PROBE_SCRIPT="$FAKE_PROBE" \
    "$SCRIPT" test-user example.invalid 2>&1
)
PID_DRIFT_STATUS=$?
set -e
[ "$PID_DRIFT_STATUS" -eq 1 ] || fail "PID drift returned $PID_DRIFT_STATUS instead of 1"
expect_contains "$PID_DRIFT_OUTPUT" "persistent service restarted during closure verification: expectedPid=4242 actualPid=4343"
expect_not_contains "$PID_DRIFT_OUTPUT" "P6_CLOSURE_OK"

set +e
UNSAFE_USER_OUTPUT=$(
  TY_NEW_SSH_COMMAND="$FAKE_SSH" \
  TY_NEW_VERIFY_SCRIPT="$SUCCESS_COMMAND" \
  TY_NEW_PROBE_SCRIPT="$FAKE_PROBE" \
    "$SCRIPT" --dry-run -- -V example.invalid 2>&1
)
UNSAFE_USER_STATUS=$?
set -e
[ "$UNSAFE_USER_STATUS" -eq 1 ] || fail "unsafe user returned $UNSAFE_USER_STATUS instead of 1"
expect_contains "$UNSAFE_USER_OUTPUT" "SSH user contains unsupported characters: -V"

echo "verify_p631_p6_closure contract tests passed."
