#!/bin/sh
set -eu

usage() {
  cat >&2 <<'EOF'
Usage:
  Deploy/DedicatedServer/verify_p631_p6_closure.sh [--dry-run] <ssh-user> <host> [ssh-key]

Closes the P6 multiplayer spike without changing the remote service:
1. Requires the persistent three-enemy, damage-10, delay-90 baseline in both the unit file and effective ExecStart.
2. Requires no retention experiment lock and public health connected=0/spawned=0.
3. Runs the three-enemy, four-attack target-retention regression.
4. Proves MainPID, baseline arguments, lock state, and zero connections did not drift.

Environment overrides:
TY_NEW_BASE_ENEMY_COUNT, TY_NEW_BASE_TICK_DAMAGE,
TY_NEW_BASE_DEATH_DELAY_SECONDS, TY_NEW_RETENTION_ATTACKS,
TY_NEW_CLIENT1_SECONDS, TY_NEW_CLIENT2_SECONDS, TY_NEW_GAME_PORT,
TY_NEW_HEALTH_PORT, TY_NEW_HEALTH_STARTUP_TIMEOUT,
TY_NEW_EXPECTED_NETWORK_PLAYER_PREFAB, TY_NEW_SERVICE_NAME,
TY_NEW_REMOTE_SERVICE, TY_NEW_REMOTE_LOCK, TY_NEW_SSH_COMMAND,
TY_NEW_VERIFY_SCRIPT, and TY_NEW_PROBE_SCRIPT.
EOF
  exit 1
}

die() {
  echo "P6_CLOSURE_FAILED $*" >&2
  exit 1
}

DRY_RUN=0
while [ $# -gt 0 ]; do
  case "$1" in
    --dry-run)
      DRY_RUN=1
      shift
      ;;
    --)
      shift
      break
      ;;
    -*)
      die "Unknown option: $1"
      ;;
    *)
      break
      ;;
  esac
done

if [ $# -lt 2 ] || [ $# -gt 3 ]; then
  usage
fi

SSH_USER=$1
HOST=$2
SSH_KEY=${3:-}

case "$SSH_USER" in
  ''|-*|*[!A-Za-z0-9._-]*) die "SSH user contains unsupported characters: $SSH_USER" ;;
esac
case "$HOST" in
  ''|*[!A-Za-z0-9._:-]*) die "Host contains unsupported characters: $HOST" ;;
esac

PROJECT_ROOT=$(CDPATH= cd -- "$(dirname -- "$0")/../.." && pwd)
BASE_ENEMY_COUNT=${TY_NEW_BASE_ENEMY_COUNT:-3}
BASE_TICK_DAMAGE=${TY_NEW_BASE_TICK_DAMAGE:-10}
BASE_DEATH_DELAY_SECONDS=${TY_NEW_BASE_DEATH_DELAY_SECONDS:-90}
RETENTION_ATTACKS=${TY_NEW_RETENTION_ATTACKS:-4}
CLIENT1_SECONDS=${TY_NEW_CLIENT1_SECONDS:-100}
CLIENT2_SECONDS=${TY_NEW_CLIENT2_SECONDS:-90}
GAME_PORT=${TY_NEW_GAME_PORT:-7777}
HEALTH_PORT=${TY_NEW_HEALTH_PORT:-7778}
HEALTH_STARTUP_TIMEOUT=${TY_NEW_HEALTH_STARTUP_TIMEOUT:-10}
EXPECTED_NETWORK_PLAYER_PREFAB=${TY_NEW_EXPECTED_NETWORK_PLAYER_PREFAB:-Multiplayer/PF_NetworkPlayerCombatTest}
SERVICE_NAME=${TY_NEW_SERVICE_NAME:-ty-new-server.service}
REMOTE_SERVICE=${TY_NEW_REMOTE_SERVICE:-/etc/systemd/system/ty-new-server.service}
REMOTE_LOCK=${TY_NEW_REMOTE_LOCK:-/var/tmp/ty-new-server-retention.lock}
SSH_COMMAND=${TY_NEW_SSH_COMMAND:-ssh}
VERIFY_SCRIPT=${TY_NEW_VERIFY_SCRIPT:-$PROJECT_ROOT/Deploy/DedicatedServer/verify_p15_ecs_multiplayer.sh}
PROBE_SCRIPT=${TY_NEW_PROBE_SCRIPT:-$PROJECT_ROOT/Deploy/DedicatedServer/probe_p15_multiplayer.py}
REMOTE="$SSH_USER@$HOST"

validate_positive_uint() {
  NAME=$1
  VALUE=$2
  case "$VALUE" in
    ''|*[!0-9]*) die "$NAME must be a positive integer, got: $VALUE" ;;
  esac
  [ "$VALUE" -gt 0 ] || die "$NAME must be greater than zero, got: $VALUE"
}

validate_positive_uint TY_NEW_BASE_ENEMY_COUNT "$BASE_ENEMY_COUNT"
validate_positive_uint TY_NEW_BASE_TICK_DAMAGE "$BASE_TICK_DAMAGE"
validate_positive_uint TY_NEW_BASE_DEATH_DELAY_SECONDS "$BASE_DEATH_DELAY_SECONDS"
validate_positive_uint TY_NEW_RETENTION_ATTACKS "$RETENTION_ATTACKS"
validate_positive_uint TY_NEW_CLIENT1_SECONDS "$CLIENT1_SECONDS"
validate_positive_uint TY_NEW_CLIENT2_SECONDS "$CLIENT2_SECONDS"
validate_positive_uint TY_NEW_GAME_PORT "$GAME_PORT"
validate_positive_uint TY_NEW_HEALTH_PORT "$HEALTH_PORT"
validate_positive_uint TY_NEW_HEALTH_STARTUP_TIMEOUT "$HEALTH_STARTUP_TIMEOUT"

[ "$BASE_ENEMY_COUNT" -ge 2 ] || die "Baseline enemy count must be at least 2."
[ "$CLIENT1_SECONDS" -ge "$CLIENT2_SECONDS" ] || die "client1 seconds must be greater than or equal to client2 seconds."
[ "$GAME_PORT" -le 65535 ] || die "TY_NEW_GAME_PORT must be at most 65535."
[ "$HEALTH_PORT" -le 65535 ] || die "TY_NEW_HEALTH_PORT must be at most 65535."

if [ -n "$SSH_KEY" ] && [ ! -f "$SSH_KEY" ]; then
  die "SSH key does not exist: $SSH_KEY"
fi

[ -x "$VERIFY_SCRIPT" ] || die "Missing or non-executable ECS verify script: $VERIFY_SCRIPT"
[ -x "$PROBE_SCRIPT" ] || die "Missing or non-executable P1.5 probe script: $PROBE_SCRIPT"
command -v "$SSH_COMMAND" >/dev/null 2>&1 || die "Missing SSH command: $SSH_COMMAND"

echo "P6_CLOSURE_CONFIG remote=$REMOTE enemyCount=$BASE_ENEMY_COUNT tickDamage=$BASE_TICK_DAMAGE deathDelaySeconds=$BASE_DEATH_DELAY_SECONDS retentionAttacks=$RETENTION_ATTACKS client1Seconds=$CLIENT1_SECONDS client2Seconds=$CLIENT2_SECONDS"
if [ "$DRY_RUN" = "1" ]; then
  echo "P6_CLOSURE_DRY_RUN_OK no remote connection, verification, or service change was attempted."
  exit 0
fi

run_ssh() {
  if [ -n "$SSH_KEY" ]; then
    "$SSH_COMMAND" -i "$SSH_KEY" "$@"
  else
    "$SSH_COMMAND" "$@"
  fi
}

require_unique_exec_argument() {
  CURRENT_EXEC=$1
  EXPECTED_ARGUMENT=$2
  EXEC_CONTEXT=$3
  PADDED_EXEC=" $CURRENT_EXEC "
  OPTION_TOKEN=${EXPECTED_ARGUMENT%% *}
  PADDED_OPTION=" $OPTION_TOKEN "
  PADDED_ARGUMENT=" $EXPECTED_ARGUMENT "

  case "$PADDED_EXEC" in
    *"$PADDED_OPTION"*) ;;
    *) die "$EXEC_CONTEXT is missing required option: $OPTION_TOKEN" ;;
  esac
  AFTER_OPTION=${PADDED_EXEC#*"$PADDED_OPTION"}
  case "$AFTER_OPTION" in
    *"$PADDED_OPTION"*) die "$EXEC_CONTEXT contains duplicate option: $OPTION_TOKEN" ;;
  esac
  case "$PADDED_EXEC" in
    *"$PADDED_ARGUMENT"*) ;;
    *) die "$EXEC_CONTEXT is missing required argument: $EXPECTED_ARGUMENT" ;;
  esac
}

require_baseline_contract() {
  CURRENT_EXEC=$1
  EXEC_CONTEXT=$2
  require_unique_exec_argument "$CURRENT_EXEC" "--network-player-prefab $EXPECTED_NETWORK_PLAYER_PREFAB" "$EXEC_CONTEXT"
  require_unique_exec_argument "$CURRENT_EXEC" "--network-enemy-count $BASE_ENEMY_COUNT" "$EXEC_CONTEXT"
  require_unique_exec_argument "$CURRENT_EXEC" "--enable-network-enemy-server-tick" "$EXEC_CONTEXT"
  require_unique_exec_argument "$CURRENT_EXEC" "--network-enemy-server-tick-damage $BASE_TICK_DAMAGE" "$EXEC_CONTEXT"
  require_unique_exec_argument "$CURRENT_EXEC" "--network-enemy-server-tick-death-delay-seconds $BASE_DEATH_DELAY_SECONDS" "$EXEC_CONTEXT"
}

read_remote_service_pid() {
  PID_OUTPUT=$(run_ssh "$REMOTE" "sudo systemctl show --property=MainPID --value '$SERVICE_NAME'")
  case "$PID_OUTPUT" in
    ''|*[!0-9]*) die "Remote $SERVICE_NAME returned an invalid MainPID: $PID_OUTPUT" ;;
  esac
  [ "$PID_OUTPUT" -gt 0 ] || die "Remote $SERVICE_NAME is not running: MainPID=$PID_OUTPUT"
  echo "$PID_OUTPUT"
}

assert_baseline_contract() {
  CHECKPOINT=$1
  UNIT_FILE_EXEC=$(run_ssh "$REMOTE" "sudo sed -n 's/^ExecStart=//p' '$REMOTE_SERVICE' | tail -n 1")
  EFFECTIVE_EXEC=$(run_ssh "$REMOTE" "sudo systemctl show --property=ExecStart --value '$SERVICE_NAME'")
  require_baseline_contract "$UNIT_FILE_EXEC" "$CHECKPOINT unit file"
  require_baseline_contract "$EFFECTIVE_EXEC" "$CHECKPOINT effective service"
  echo "P6_CLOSURE_BASELINE_OK checkpoint=$CHECKPOINT enemyCount=$BASE_ENEMY_COUNT tickDamage=$BASE_TICK_DAMAGE deathDelaySeconds=$BASE_DEATH_DELAY_SECONDS"
}

assert_no_experiment_lock() {
  CHECKPOINT=$1
  if ! run_ssh "$REMOTE" "sudo test ! -e '$REMOTE_LOCK'"; then
    die "retention experiment lock exists at $REMOTE_LOCK checkpoint=$CHECKPOINT"
  fi
  echo "P6_CLOSURE_LOCK_CLEAR_OK checkpoint=$CHECKPOINT lock=$REMOTE_LOCK"
}

assert_zero_health() {
  CHECKPOINT=$1
  HEALTH_OUTPUT=$(
    "$PROBE_SCRIPT" \
      --health-only \
      --host "$HOST" \
      --game-port "$GAME_PORT" \
      --health-port "$HEALTH_PORT" \
      --startup-timeout "$HEALTH_STARTUP_TIMEOUT" \
      --socket-timeout 5
  )
  echo "$HEALTH_OUTPUT"
  case "$HEALTH_OUTPUT" in
    *"P1.5_HEALTH_OK "*" connected=0 spawned=0"*) ;;
    *) die "health did not prove connected=0 spawned=0 checkpoint=$CHECKPOINT" ;;
  esac
  echo "P6_CLOSURE_HEALTH_ZERO_OK checkpoint=$CHECKPOINT connected=0 spawned=0"
}

run_baseline_retention_verification() {
  export TY_NEW_REQUIRE_NETWORK_ENEMY_TARGET_RETENTION=1
  export TY_NEW_MIN_NETWORK_ENEMY_TARGET_RETENTION_ATTACKS="$RETENTION_ATTACKS"
  export TY_NEW_MIN_NETWORK_ENEMY_COUNT="$BASE_ENEMY_COUNT"
  export TY_NEW_CLIENT1_SECONDS="$CLIENT1_SECONDS"
  export TY_NEW_CLIENT2_SECONDS="$CLIENT2_SECONDS"
  export TY_NEW_NETWORK_PLAYER_PREFAB="${TY_NEW_NETWORK_PLAYER_PREFAB:-$EXPECTED_NETWORK_PLAYER_PREFAB}"
  export TY_NEW_SKIP_AUTO_FORMAL_SYNC_REQUIREMENTS="${TY_NEW_SKIP_AUTO_FORMAL_SYNC_REQUIREMENTS:-1}"
  export TY_NEW_SKIP_CLIENT_DESPAWN_CHECK="${TY_NEW_SKIP_CLIENT_DESPAWN_CHECK:-1}"
  export TY_NEW_SKIP_REMOTE_MOVEMENT_CHECK="${TY_NEW_SKIP_REMOTE_MOVEMENT_CHECK:-1}"
  export TY_NEW_SKIP_HEALTH_SYNC_CHECK="${TY_NEW_SKIP_HEALTH_SYNC_CHECK:-1}"
  export TY_NEW_REQUIRE_DEATH_SYNC="${TY_NEW_REQUIRE_DEATH_SYNC:-0}"
  export TY_NEW_ECS_SERVER_LOG="${TY_NEW_ECS_SERVER_LOG:-/tmp/TY_NEW_p631_p6_closure_server.log}"
  export TY_NEW_ECS_SERVER_TAIL_ERR="${TY_NEW_ECS_SERVER_TAIL_ERR:-/tmp/TY_NEW_p631_p6_closure_server_tail.err}"

  if [ -n "$SSH_KEY" ]; then
    "$VERIFY_SCRIPT" "$SSH_USER" "$HOST" "$SSH_KEY"
  else
    "$VERIFY_SCRIPT" "$SSH_USER" "$HOST"
  fi
}

assert_baseline_contract before
assert_no_experiment_lock before
assert_zero_health before
BASELINE_PID=$(read_remote_service_pid)
echo "P6_CLOSURE_PID_CAPTURED pid=$BASELINE_PID"

set +e
run_baseline_retention_verification
VERIFY_STATUS=$?
set -e
if [ "$VERIFY_STATUS" -ne 0 ]; then
  echo "P6_CLOSURE_VERIFICATION_FAILED status=$VERIFY_STATUS pid=$BASELINE_PID" >&2
  exit "$VERIFY_STATUS"
fi
echo "P6_CLOSURE_VERIFICATION_OK enemyCount=$BASE_ENEMY_COUNT tickDamage=$BASE_TICK_DAMAGE retainedAttacks=$RETENTION_ATTACKS"

assert_zero_health after
FINAL_PID=$(read_remote_service_pid)
if [ "$FINAL_PID" != "$BASELINE_PID" ]; then
  die "persistent service restarted during closure verification: expectedPid=$BASELINE_PID actualPid=$FINAL_PID"
fi
echo "P6_CLOSURE_PID_UNCHANGED_OK pid=$FINAL_PID"
assert_baseline_contract after
assert_no_experiment_lock after

echo "P6_CLOSURE_CAPABILITY_MATRIX stableBaseline=verified_three_enemy_damage10_delay90 stableRetention=verified_three_enemy_four_attacks fiveEnemyLowDamage=verified_by_p629 fiveEnemyDamage10=known_limit_p630 longDurationCapacity=out_of_scope_p7"
echo "P6_CLOSURE_OK p6Status=closed enemyCount=$BASE_ENEMY_COUNT tickDamage=$BASE_TICK_DAMAGE deathDelaySeconds=$BASE_DEATH_DELAY_SECONDS retainedAttacks=$RETENTION_ATTACKS pid=$FINAL_PID serviceRestarted=false connected=0 spawned=0"
