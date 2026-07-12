#!/bin/sh
set -eu

usage() {
  cat >&2 <<'EOF'
Usage:
  Deploy/DedicatedServer/verify_ecs_network_enemy_retention_with_temp_service.sh [options] <ssh-user> <host> [ssh-key]

Temporarily changes the ECS ty-new-server.service network-enemy parameters,
runs the public target-retention smoke, and restores the original service.

Options:
  --enemy-count N           Temporary enemy count (default: 4)
  --tick-damage N           Temporary server-tick damage (default: 5)
  --death-delay-seconds N   Temporary enemy death delay (default: 90)
  --retention-attacks N     Required retained attacks per enemy (default: 4)
  --rounds N                Verification rounds on the same temporary service (default: 1, max: 10)
  --client1-seconds N       First client lifetime (default: 100)
  --client2-seconds N       Second client lifetime (default: 90)
  --base-enemy-count N      Expected persistent enemy count (default: 3)
  --base-tick-damage N      Expected persistent tick damage (default: 10)
  --base-death-delay-seconds N
                            Expected persistent death delay (default: 90)
  --dry-run                 Validate and print configuration without connecting
  -h, --help                Show this help

The corresponding TY_NEW_TEMP_*, TY_NEW_BASE_*, TY_NEW_RETENTION_ATTACKS,
TY_NEW_ROUNDS,
TY_NEW_CLIENT1_SECONDS, and TY_NEW_CLIENT2_SECONDS environment variables can
also provide defaults. Command-line options take precedence.

Safety contract:
  - Refuses to modify a service that does not match the expected baseline.
  - Backs up the complete service before changing it.
  - Restores on success, verification failure, or a handled signal.
  - Requires public health after both the temporary restart and restoration.
EOF
}

die() {
  echo "$*" >&2
  exit 1
}

TEMP_ENEMY_COUNT=${TY_NEW_TEMP_ENEMY_COUNT:-4}
TEMP_TICK_DAMAGE=${TY_NEW_TEMP_TICK_DAMAGE:-5}
TEMP_DEATH_DELAY_SECONDS=${TY_NEW_TEMP_DEATH_DELAY_SECONDS:-90}
BASE_ENEMY_COUNT=${TY_NEW_BASE_ENEMY_COUNT:-3}
BASE_TICK_DAMAGE=${TY_NEW_BASE_TICK_DAMAGE:-10}
BASE_DEATH_DELAY_SECONDS=${TY_NEW_BASE_DEATH_DELAY_SECONDS:-90}
RETENTION_ATTACKS=${TY_NEW_RETENTION_ATTACKS:-4}
ROUNDS=${TY_NEW_ROUNDS:-1}
CLIENT1_SECONDS=${TY_NEW_CLIENT1_SECONDS:-100}
CLIENT2_SECONDS=${TY_NEW_CLIENT2_SECONDS:-90}
DRY_RUN=0

while [ $# -gt 0 ]; do
  case "$1" in
    --enemy-count)
      [ $# -ge 2 ] || die "Missing value for --enemy-count"
      TEMP_ENEMY_COUNT=$2
      shift 2
      ;;
    --tick-damage)
      [ $# -ge 2 ] || die "Missing value for --tick-damage"
      TEMP_TICK_DAMAGE=$2
      shift 2
      ;;
    --death-delay-seconds)
      [ $# -ge 2 ] || die "Missing value for --death-delay-seconds"
      TEMP_DEATH_DELAY_SECONDS=$2
      shift 2
      ;;
    --retention-attacks)
      [ $# -ge 2 ] || die "Missing value for --retention-attacks"
      RETENTION_ATTACKS=$2
      shift 2
      ;;
    --rounds)
      [ $# -ge 2 ] || die "Missing value for --rounds"
      ROUNDS=$2
      shift 2
      ;;
    --client1-seconds)
      [ $# -ge 2 ] || die "Missing value for --client1-seconds"
      CLIENT1_SECONDS=$2
      shift 2
      ;;
    --client2-seconds)
      [ $# -ge 2 ] || die "Missing value for --client2-seconds"
      CLIENT2_SECONDS=$2
      shift 2
      ;;
    --base-enemy-count)
      [ $# -ge 2 ] || die "Missing value for --base-enemy-count"
      BASE_ENEMY_COUNT=$2
      shift 2
      ;;
    --base-tick-damage)
      [ $# -ge 2 ] || die "Missing value for --base-tick-damage"
      BASE_TICK_DAMAGE=$2
      shift 2
      ;;
    --base-death-delay-seconds)
      [ $# -ge 2 ] || die "Missing value for --base-death-delay-seconds"
      BASE_DEATH_DELAY_SECONDS=$2
      shift 2
      ;;
    --dry-run)
      DRY_RUN=1
      shift
      ;;
    -h|--help)
      usage
      exit 0
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
  exit 1
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
REMOTE="$SSH_USER@$HOST"
PROJECT_ROOT=$(CDPATH= cd -- "$(dirname -- "$0")/../.." && pwd)
VERIFY_SCRIPT=${TY_NEW_VERIFY_SCRIPT:-$PROJECT_ROOT/Deploy/DedicatedServer/verify_p15_ecs_multiplayer.sh}
PROBE_SCRIPT=${TY_NEW_PROBE_SCRIPT:-$PROJECT_ROOT/Deploy/DedicatedServer/probe_p15_multiplayer.py}
SSH_COMMAND=${TY_NEW_SSH_COMMAND:-ssh}
SERVICE_NAME=ty-new-server.service
REMOTE_SERVICE=/etc/systemd/system/ty-new-server.service
LOCAL_LOCK_HOST=$(uname -n)
case "$LOCAL_LOCK_HOST" in
  ''|*[!A-Za-z0-9._-]*) LOCAL_LOCK_HOST=local ;;
esac
LOCK_TOKEN="$(date +%s)-$$-$LOCAL_LOCK_HOST"
REMOTE_LOCK=/var/tmp/ty-new-server-retention.lock
REMOTE_LOCK_OWNER=$REMOTE_LOCK/owner
REMOTE_BACKUP=$REMOTE_LOCK/ty-new-server.service.backup
EXPECTED_NETWORK_PLAYER_PREFAB=Multiplayer/PF_NetworkPlayerCombatTest
GAME_PORT=${TY_NEW_GAME_PORT:-7777}
HEALTH_PORT=${TY_NEW_HEALTH_PORT:-7778}
HEALTH_STARTUP_TIMEOUT=${TY_NEW_HEALTH_STARTUP_TIMEOUT:-30}
ECS_SERVER_LOG_OVERRIDE=${TY_NEW_ECS_SERVER_LOG:-}
ECS_SERVER_TAIL_ERR_OVERRIDE=${TY_NEW_ECS_SERVER_TAIL_ERR:-}
RESTORE_NEEDED=0

validate_uint() {
  NAME=$1
  VALUE=$2
  case "$VALUE" in
    ''|*[!0-9]*)
      die "$NAME must be a non-negative integer, got: $VALUE"
      ;;
  esac
}

validate_positive_uint() {
  NAME=$1
  VALUE=$2
  validate_uint "$NAME" "$VALUE"
  [ "$VALUE" -gt 0 ] || die "$NAME must be greater than zero, got: $VALUE"
}

validate_positive_uint TY_NEW_TEMP_ENEMY_COUNT "$TEMP_ENEMY_COUNT"
validate_uint TY_NEW_TEMP_TICK_DAMAGE "$TEMP_TICK_DAMAGE"
validate_uint TY_NEW_TEMP_DEATH_DELAY_SECONDS "$TEMP_DEATH_DELAY_SECONDS"
validate_positive_uint TY_NEW_BASE_ENEMY_COUNT "$BASE_ENEMY_COUNT"
validate_uint TY_NEW_BASE_TICK_DAMAGE "$BASE_TICK_DAMAGE"
validate_uint TY_NEW_BASE_DEATH_DELAY_SECONDS "$BASE_DEATH_DELAY_SECONDS"
validate_positive_uint TY_NEW_RETENTION_ATTACKS "$RETENTION_ATTACKS"
validate_positive_uint TY_NEW_ROUNDS "$ROUNDS"
validate_positive_uint TY_NEW_CLIENT1_SECONDS "$CLIENT1_SECONDS"
validate_positive_uint TY_NEW_CLIENT2_SECONDS "$CLIENT2_SECONDS"
validate_positive_uint TY_NEW_GAME_PORT "$GAME_PORT"
validate_positive_uint TY_NEW_HEALTH_PORT "$HEALTH_PORT"
validate_positive_uint TY_NEW_HEALTH_STARTUP_TIMEOUT "$HEALTH_STARTUP_TIMEOUT"

[ "$TEMP_ENEMY_COUNT" -ge 2 ] || die "Temporary enemy count must be at least 2 for target-distribution verification."
[ "$CLIENT1_SECONDS" -ge "$CLIENT2_SECONDS" ] || die "client1 seconds must be greater than or equal to client2 seconds."
[ "$ROUNDS" -le 10 ] || die "TY_NEW_ROUNDS must be at most 10."
[ "$GAME_PORT" -le 65535 ] || die "TY_NEW_GAME_PORT must be at most 65535."
[ "$HEALTH_PORT" -le 65535 ] || die "TY_NEW_HEALTH_PORT must be at most 65535."

if [ -n "$SSH_KEY" ] && [ ! -f "$SSH_KEY" ]; then
  die "SSH key does not exist: $SSH_KEY"
fi

[ -x "$VERIFY_SCRIPT" ] || die "Missing or non-executable ECS verify script: $VERIFY_SCRIPT"
[ -x "$PROBE_SCRIPT" ] || die "Missing or non-executable P1.5 probe script: $PROBE_SCRIPT"
command -v "$SSH_COMMAND" >/dev/null 2>&1 || die "Missing SSH command: $SSH_COMMAND"

print_configuration() {
  echo "ECS_RETENTION_EXPERIMENT_CONFIG remote=$REMOTE temporaryEnemyCount=$TEMP_ENEMY_COUNT temporaryTickDamage=$TEMP_TICK_DAMAGE temporaryDeathDelaySeconds=$TEMP_DEATH_DELAY_SECONDS retentionAttacks=$RETENTION_ATTACKS rounds=$ROUNDS client1Seconds=$CLIENT1_SECONDS client2Seconds=$CLIENT2_SECONDS baselineEnemyCount=$BASE_ENEMY_COUNT baselineTickDamage=$BASE_TICK_DAMAGE baselineDeathDelaySeconds=$BASE_DEATH_DELAY_SECONDS"
}

print_configuration
if [ "$DRY_RUN" = "1" ]; then
  echo "ECS_RETENTION_EXPERIMENT_DRY_RUN_OK no remote connection or service change was attempted."
  exit 0
fi

run_ssh() {
  if [ -n "$SSH_KEY" ]; then
    "$SSH_COMMAND" -i "$SSH_KEY" "$@"
  else
    "$SSH_COMMAND" "$@"
  fi
}

read_remote_service_pid() {
  PID_OUTPUT=$(run_ssh "$REMOTE" "sudo systemctl show --property=MainPID --value '$SERVICE_NAME'")
  case "$PID_OUTPUT" in
    ''|*[!0-9]*) die "Remote $SERVICE_NAME returned an invalid MainPID: $PID_OUTPUT" ;;
  esac
  [ "$PID_OUTPUT" -gt 0 ] || die "Remote $SERVICE_NAME is not running: MainPID=$PID_OUTPUT"
  echo "$PID_OUTPUT"
}

assert_temporary_service_pid() {
  ROUND_INDEX=$1
  CHECKPOINT=$2
  CURRENT_SERVICE_PID=$(read_remote_service_pid)
  if [ "$CURRENT_SERVICE_PID" != "$TEMP_SERVICE_PID" ]; then
    die "Temporary service restarted unexpectedly: expectedPid=$TEMP_SERVICE_PID actualPid=$CURRENT_SERVICE_PID round=$ROUND_INDEX checkpoint=$CHECKPOINT"
  fi
  echo "TEMP_SERVICE_PID_OK pid=$CURRENT_SERVICE_PID round=$ROUND_INDEX checkpoint=$CHECKPOINT"
}

wait_public_health() {
  "$PROBE_SCRIPT" \
    --health-only \
    --host "$HOST" \
    --game-port "$GAME_PORT" \
    --health-port "$HEALTH_PORT" \
    --startup-timeout "$HEALTH_STARTUP_TIMEOUT" \
    --socket-timeout 5
}

install_temporary_service() {
  echo "TEMP_SERVICE_INSTALL_BEGIN remote=$REMOTE enemyCount=$TEMP_ENEMY_COUNT tickDamage=$TEMP_TICK_DAMAGE deathDelaySeconds=$TEMP_DEATH_DELAY_SECONDS"
  run_ssh "$REMOTE" \
    "TEMP_ENEMY_COUNT=$TEMP_ENEMY_COUNT TEMP_TICK_DAMAGE=$TEMP_TICK_DAMAGE TEMP_DEATH_DELAY_SECONDS=$TEMP_DEATH_DELAY_SECONDS BASE_ENEMY_COUNT=$BASE_ENEMY_COUNT BASE_TICK_DAMAGE=$BASE_TICK_DAMAGE BASE_DEATH_DELAY_SECONDS=$BASE_DEATH_DELAY_SECONDS LOCK_TOKEN='$LOCK_TOKEN' REMOTE_LOCK='$REMOTE_LOCK' REMOTE_LOCK_OWNER='$REMOTE_LOCK_OWNER' REMOTE_BACKUP='$REMOTE_BACKUP' REMOTE_SERVICE='$REMOTE_SERVICE' SERVICE_NAME='$SERVICE_NAME' EXPECTED_NETWORK_PLAYER_PREFAB='$EXPECTED_NETWORK_PLAYER_PREFAB' sh -s" <<'REMOTE_SCRIPT'
set -eu

if ! sudo test -f "$REMOTE_SERVICE"; then
  echo "Missing remote service: $REMOTE_SERVICE" >&2
  exit 1
fi

require_unique_exec_argument() {
  EXPECTED_ARGUMENT=$1
  EXEC_CONTEXT=$2
  PADDED_EXEC=" $CURRENT_EXEC "
  OPTION_TOKEN=${EXPECTED_ARGUMENT%% *}
  PADDED_OPTION=" $OPTION_TOKEN "
  PADDED_ARGUMENT=" $EXPECTED_ARGUMENT "
  case "$PADDED_EXEC" in
    *"$PADDED_OPTION"*) ;;
    *)
      echo "$EXEC_CONTEXT is missing required option: $OPTION_TOKEN" >&2
      echo "$CURRENT_EXEC" >&2
      exit 1
      ;;
  esac
  AFTER_OPTION=${PADDED_EXEC#*"$PADDED_OPTION"}
  case "$AFTER_OPTION" in
    *"$PADDED_OPTION"*)
      echo "$EXEC_CONTEXT contains duplicate option: $OPTION_TOKEN" >&2
      echo "$CURRENT_EXEC" >&2
      exit 1
      ;;
  esac
  case "$PADDED_EXEC" in
    *"$PADDED_ARGUMENT"*) ;;
    *)
      echo "$EXEC_CONTEXT is missing required argument: $EXPECTED_ARGUMENT" >&2
      echo "$CURRENT_EXEC" >&2
      exit 1
      ;;
  esac

  AFTER_FIRST=${PADDED_EXEC#*"$PADDED_ARGUMENT"}
  case "$AFTER_FIRST" in
    *"$PADDED_ARGUMENT"*)
      echo "$EXEC_CONTEXT contains duplicate argument: $EXPECTED_ARGUMENT" >&2
      echo "$CURRENT_EXEC" >&2
      exit 1
      ;;
  esac
}

require_baseline_contract() {
  CURRENT_EXEC=$1
  EXEC_CONTEXT=$2
  require_unique_exec_argument "--network-player-prefab $EXPECTED_NETWORK_PLAYER_PREFAB" "$EXEC_CONTEXT"
  require_unique_exec_argument "--network-enemy-count $BASE_ENEMY_COUNT" "$EXEC_CONTEXT"
  require_unique_exec_argument "--enable-network-enemy-server-tick" "$EXEC_CONTEXT"
  require_unique_exec_argument "--network-enemy-server-tick-damage $BASE_TICK_DAMAGE" "$EXEC_CONTEXT"
  require_unique_exec_argument "--network-enemy-server-tick-death-delay-seconds $BASE_DEATH_DELAY_SECONDS" "$EXEC_CONTEXT"
}

require_temporary_contract() {
  CURRENT_EXEC=$1
  EXEC_CONTEXT=$2
  require_unique_exec_argument "--network-player-prefab $EXPECTED_NETWORK_PLAYER_PREFAB" "$EXEC_CONTEXT"
  require_unique_exec_argument "--network-enemy-count $TEMP_ENEMY_COUNT" "$EXEC_CONTEXT"
  require_unique_exec_argument "--enable-network-enemy-server-tick" "$EXEC_CONTEXT"
  require_unique_exec_argument "--network-enemy-server-tick-damage $TEMP_TICK_DAMAGE" "$EXEC_CONTEXT"
  require_unique_exec_argument "--network-enemy-server-tick-death-delay-seconds $TEMP_DEATH_DELAY_SECONDS" "$EXEC_CONTEXT"
}

UNIT_FILE_EXEC=$(sudo sed -n 's/^ExecStart=//p' "$REMOTE_SERVICE" | tail -n 1)
EFFECTIVE_EXEC=$(sudo systemctl show --property=ExecStart --value "$SERVICE_NAME")
require_baseline_contract "$UNIT_FILE_EXEC" "Baseline unit file"
require_baseline_contract "$EFFECTIVE_EXEC" "Effective baseline service"

if ! sudo sh -c '
set -eu
lock_path=$1
owner_path=$2
lock_token=$3
trap "" HUP INT TERM
if ! mkdir -m 700 "$lock_path"; then
  trap - HUP INT TERM
  exit 1
fi
if ! printf "%s\n" "$lock_token" > "$owner_path"; then
  rm -f "$owner_path"
  rmdir "$lock_path"
  trap - HUP INT TERM
  exit 1
fi
trap - HUP INT TERM
' sh "$REMOTE_LOCK" "$REMOTE_LOCK_OWNER" "$LOCK_TOKEN"; then
  echo "Refusing to start: another retention experiment lock exists at $REMOTE_LOCK" >&2
  if sudo test -f "$REMOTE_LOCK_OWNER"; then
    echo "Current lock owner: $(sudo cat "$REMOTE_LOCK_OWNER")" >&2
  fi
  exit 1
fi

LOCKED_UNIT_FILE_EXEC=$(sudo sed -n 's/^ExecStart=//p' "$REMOTE_SERVICE" | tail -n 1)
LOCKED_EFFECTIVE_EXEC=$(sudo systemctl show --property=ExecStart --value "$SERVICE_NAME")
require_baseline_contract "$LOCKED_UNIT_FILE_EXEC" "Locked baseline unit file"
require_baseline_contract "$LOCKED_EFFECTIVE_EXEC" "Locked effective baseline service"

sudo cp "$REMOTE_SERVICE" "$REMOTE_BACKUP"
TMP_SERVICE=$(mktemp /tmp/ty-new-server.service.retention.XXXXXX)
cleanup_temporary_file() {
  rm -f "$TMP_SERVICE"
}
trap cleanup_temporary_file EXIT
trap 'exit 130' INT
trap 'exit 143' TERM
trap 'exit 129' HUP

sudo sed \
  -e "s/--network-enemy-count $BASE_ENEMY_COUNT /--network-enemy-count $TEMP_ENEMY_COUNT /" \
  -e "s/--network-enemy-count $BASE_ENEMY_COUNT$/--network-enemy-count $TEMP_ENEMY_COUNT/" \
  -e "s/--network-enemy-server-tick-damage $BASE_TICK_DAMAGE /--network-enemy-server-tick-damage $TEMP_TICK_DAMAGE /" \
  -e "s/--network-enemy-server-tick-damage $BASE_TICK_DAMAGE$/--network-enemy-server-tick-damage $TEMP_TICK_DAMAGE/" \
  -e "s/--network-enemy-server-tick-death-delay-seconds $BASE_DEATH_DELAY_SECONDS /--network-enemy-server-tick-death-delay-seconds $TEMP_DEATH_DELAY_SECONDS /" \
  -e "s/--network-enemy-server-tick-death-delay-seconds $BASE_DEATH_DELAY_SECONDS$/--network-enemy-server-tick-death-delay-seconds $TEMP_DEATH_DELAY_SECONDS/" \
  "$REMOTE_SERVICE" > "$TMP_SERVICE"

TEMP_UNIT_FILE_EXEC=$(sed -n 's/^ExecStart=//p' "$TMP_SERVICE" | tail -n 1)
require_temporary_contract "$TEMP_UNIT_FILE_EXEC" "Temporary unit file"

sudo cp "$TMP_SERVICE" "$REMOTE_SERVICE"
sudo systemctl daemon-reload
sudo systemctl restart "$SERVICE_NAME"
sudo systemctl is-active --quiet "$SERVICE_NAME"
INSTALLED_UNIT_FILE_EXEC=$(sudo sed -n 's/^ExecStart=//p' "$REMOTE_SERVICE" | tail -n 1)
EFFECTIVE_TEMP_EXEC=$(sudo systemctl show --property=ExecStart --value "$SERVICE_NAME")
require_temporary_contract "$INSTALLED_UNIT_FILE_EXEC" "Installed temporary unit file"
require_temporary_contract "$EFFECTIVE_TEMP_EXEC" "Effective temporary service"
echo "TEMP_SERVICE_INSTALL_OK effectiveExecStart=$EFFECTIVE_TEMP_EXEC"
REMOTE_SCRIPT
}

restore_remote_service() {
  echo "SERVICE_RESTORE_BEGIN remote=$REMOTE backup=$REMOTE_BACKUP"
  run_ssh "$REMOTE" \
    "BASE_ENEMY_COUNT=$BASE_ENEMY_COUNT BASE_TICK_DAMAGE=$BASE_TICK_DAMAGE BASE_DEATH_DELAY_SECONDS=$BASE_DEATH_DELAY_SECONDS LOCK_TOKEN='$LOCK_TOKEN' REMOTE_LOCK='$REMOTE_LOCK' REMOTE_LOCK_OWNER='$REMOTE_LOCK_OWNER' REMOTE_BACKUP='$REMOTE_BACKUP' REMOTE_SERVICE='$REMOTE_SERVICE' SERVICE_NAME='$SERVICE_NAME' EXPECTED_NETWORK_PLAYER_PREFAB='$EXPECTED_NETWORK_PLAYER_PREFAB' sh -s" <<'REMOTE_SCRIPT'
set -eu

if ! sudo test -f "$REMOTE_SERVICE"; then
  echo "Missing remote service during restore: $REMOTE_SERVICE" >&2
  exit 1
fi

LOCK_PRESENT=0
if sudo test -d "$REMOTE_LOCK"; then
  LOCK_PRESENT=1
  if ! sudo test -f "$REMOTE_LOCK_OWNER"; then
    echo "Refusing restore: lock has no owner file at $REMOTE_LOCK_OWNER" >&2
    exit 1
  fi
  ACTUAL_LOCK_TOKEN=$(sudo cat "$REMOTE_LOCK_OWNER")
  if [ "$ACTUAL_LOCK_TOKEN" != "$LOCK_TOKEN" ]; then
    echo "Refusing restore: lock belongs to token $ACTUAL_LOCK_TOKEN, not $LOCK_TOKEN" >&2
    exit 1
  fi
fi

if sudo test -f "$REMOTE_BACKUP"; then
  if [ "$LOCK_PRESENT" != "1" ]; then
    echo "Refusing restore: backup exists without its experiment lock." >&2
    exit 1
  fi
  sudo cp "$REMOTE_BACKUP" "$REMOTE_SERVICE"
  sudo systemctl daemon-reload
  sudo systemctl restart "$SERVICE_NAME"
  RESTORED_FROM_BACKUP=1
else
  RESTORED_FROM_BACKUP=0
fi

sudo systemctl is-active --quiet "$SERVICE_NAME"

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
    *)
      echo "$EXEC_CONTEXT is missing baseline option: $OPTION_TOKEN" >&2
      echo "$CURRENT_EXEC" >&2
      exit 1
      ;;
  esac
  AFTER_OPTION=${PADDED_EXEC#*"$PADDED_OPTION"}
  case "$AFTER_OPTION" in
    *"$PADDED_OPTION"*)
      echo "$EXEC_CONTEXT contains duplicate baseline option: $OPTION_TOKEN" >&2
      echo "$CURRENT_EXEC" >&2
      exit 1
      ;;
  esac
  case "$PADDED_EXEC" in
    *"$PADDED_ARGUMENT"*) ;;
    *)
      echo "$EXEC_CONTEXT is missing baseline argument: $EXPECTED_ARGUMENT" >&2
      echo "$CURRENT_EXEC" >&2
      exit 1
      ;;
  esac
  AFTER_FIRST=${PADDED_EXEC#*"$PADDED_ARGUMENT"}
  case "$AFTER_FIRST" in
    *"$PADDED_ARGUMENT"*)
      echo "$EXEC_CONTEXT contains duplicate baseline argument: $EXPECTED_ARGUMENT" >&2
      echo "$CURRENT_EXEC" >&2
      exit 1
      ;;
  esac
}

UNIT_FILE_EXEC=$(sudo sed -n 's/^ExecStart=//p' "$REMOTE_SERVICE" | tail -n 1)
EFFECTIVE_EXEC=$(sudo systemctl show --property=ExecStart --value "$SERVICE_NAME")

for EXPECTED_ARGUMENT in \
  "--network-player-prefab $EXPECTED_NETWORK_PLAYER_PREFAB" \
  "--network-enemy-count $BASE_ENEMY_COUNT" \
  "--enable-network-enemy-server-tick" \
  "--network-enemy-server-tick-damage $BASE_TICK_DAMAGE" \
  "--network-enemy-server-tick-death-delay-seconds $BASE_DEATH_DELAY_SECONDS"
do
  require_unique_exec_argument "$UNIT_FILE_EXEC" "$EXPECTED_ARGUMENT" "Restored unit file"
  require_unique_exec_argument "$EFFECTIVE_EXEC" "$EXPECTED_ARGUMENT" "Effective restored service"
done

if [ "$RESTORED_FROM_BACKUP" = "1" ]; then
  echo "SERVICE_RESTORE_ACTIVE effectiveExecStart=$EFFECTIVE_EXEC"
elif [ "$LOCK_PRESENT" = "1" ]; then
  echo "SERVICE_RESTORE_NOT_REQUIRED lockOwned=true baselineAlreadyActive=true effectiveExecStart=$EFFECTIVE_EXEC"
else
  echo "SERVICE_RESTORE_NOT_REQUIRED lockOwned=false baselineAlreadyActive=true effectiveExecStart=$EFFECTIVE_EXEC"
fi
REMOTE_SCRIPT
}

finalize_remote_restore() {
  run_ssh "$REMOTE" \
    "LOCK_TOKEN='$LOCK_TOKEN' REMOTE_LOCK='$REMOTE_LOCK' REMOTE_LOCK_OWNER='$REMOTE_LOCK_OWNER' REMOTE_BACKUP='$REMOTE_BACKUP' sh -s" <<'REMOTE_SCRIPT'
set -eu

if ! sudo test -d "$REMOTE_LOCK"; then
  echo "SERVICE_RESTORE_FINALIZE_NOT_REQUIRED no experiment lock exists."
  exit 0
fi

if ! sudo test -f "$REMOTE_LOCK_OWNER"; then
  echo "Refusing to finalize: lock has no owner file at $REMOTE_LOCK_OWNER" >&2
  exit 1
fi

ACTUAL_LOCK_TOKEN=$(sudo cat "$REMOTE_LOCK_OWNER")
if [ "$ACTUAL_LOCK_TOKEN" != "$LOCK_TOKEN" ]; then
  echo "Refusing to finalize: lock belongs to token $ACTUAL_LOCK_TOKEN, not $LOCK_TOKEN" >&2
  exit 1
fi

sudo rm -f "$REMOTE_BACKUP"
sudo rm -f "$REMOTE_LOCK_OWNER"
sudo rmdir "$REMOTE_LOCK"
echo "SERVICE_RESTORE_OK backupRemoved=$REMOTE_BACKUP lockReleased=$REMOTE_LOCK"
REMOTE_SCRIPT
}

restore_and_check_health() {
  restore_remote_service
  wait_public_health
  finalize_remote_restore
  RESTORE_NEEDED=0
  echo "SERVICE_RESTORE_HEALTH_OK baselineEnemyCount=$BASE_ENEMY_COUNT baselineTickDamage=$BASE_TICK_DAMAGE baselineDeathDelaySeconds=$BASE_DEATH_DELAY_SECONDS"
}

cleanup() {
  STATUS=$?
  trap - EXIT INT TERM HUP
  if [ "$RESTORE_NEEDED" = "1" ]; then
    echo "SERVICE_RESTORE_RECOVERY_BEGIN priorStatus=$STATUS" >&2
    if restore_remote_service && wait_public_health && finalize_remote_restore; then
      RESTORE_NEEDED=0
      echo "SERVICE_RESTORE_RECOVERY_OK" >&2
    else
      STATUS=1
      echo "SERVICE_RESTORE_RECOVERY_FAILED manual intervention required; backup=$REMOTE_BACKUP" >&2
    fi
  fi
  exit "$STATUS"
}

trap cleanup EXIT
trap 'exit 130' INT
trap 'exit 143' TERM
trap 'exit 129' HUP

echo "BASELINE_HEALTH_CHECK_BEGIN remote=$REMOTE baselineEnemyCount=$BASE_ENEMY_COUNT baselineTickDamage=$BASE_TICK_DAMAGE baselineDeathDelaySeconds=$BASE_DEATH_DELAY_SECONDS"
wait_public_health
echo "BASELINE_HEALTH_CHECK_OK remote=$REMOTE"

RESTORE_NEEDED=1
install_temporary_service
wait_public_health
echo "TEMP_SERVICE_HEALTH_OK enemyCount=$TEMP_ENEMY_COUNT tickDamage=$TEMP_TICK_DAMAGE deathDelaySeconds=$TEMP_DEATH_DELAY_SECONDS"
TEMP_SERVICE_PID=$(read_remote_service_pid)
echo "TEMP_SERVICE_PID_CAPTURED pid=$TEMP_SERVICE_PID rounds=$ROUNDS"

run_retention_verification() {
  ROUND_INDEX=$1
  export TY_NEW_REQUIRE_NETWORK_ENEMY_TARGET_RETENTION=1
  export TY_NEW_MIN_NETWORK_ENEMY_TARGET_RETENTION_ATTACKS="$RETENTION_ATTACKS"
  export TY_NEW_MIN_NETWORK_ENEMY_COUNT="$TEMP_ENEMY_COUNT"
  export TY_NEW_CLIENT1_SECONDS="$CLIENT1_SECONDS"
  export TY_NEW_CLIENT2_SECONDS="$CLIENT2_SECONDS"
  export TY_NEW_NETWORK_PLAYER_PREFAB="${TY_NEW_NETWORK_PLAYER_PREFAB:-$EXPECTED_NETWORK_PLAYER_PREFAB}"
  export TY_NEW_SKIP_AUTO_FORMAL_SYNC_REQUIREMENTS="${TY_NEW_SKIP_AUTO_FORMAL_SYNC_REQUIREMENTS:-1}"
  export TY_NEW_SKIP_CLIENT_DESPAWN_CHECK="${TY_NEW_SKIP_CLIENT_DESPAWN_CHECK:-1}"
  export TY_NEW_SKIP_REMOTE_MOVEMENT_CHECK="${TY_NEW_SKIP_REMOTE_MOVEMENT_CHECK:-1}"
  export TY_NEW_SKIP_HEALTH_SYNC_CHECK="${TY_NEW_SKIP_HEALTH_SYNC_CHECK:-1}"
  export TY_NEW_REQUIRE_DEATH_SYNC="${TY_NEW_REQUIRE_DEATH_SYNC:-0}"
  if [ -n "$ECS_SERVER_LOG_OVERRIDE" ]; then
    export TY_NEW_ECS_SERVER_LOG="${ECS_SERVER_LOG_OVERRIDE}.round${ROUND_INDEX}"
  else
    export TY_NEW_ECS_SERVER_LOG="/tmp/TY_NEW_ecs_retention_${TEMP_ENEMY_COUNT}enemy_${TEMP_TICK_DAMAGE}damage_round${ROUND_INDEX}.log"
  fi
  if [ -n "$ECS_SERVER_TAIL_ERR_OVERRIDE" ]; then
    export TY_NEW_ECS_SERVER_TAIL_ERR="${ECS_SERVER_TAIL_ERR_OVERRIDE}.round${ROUND_INDEX}"
  else
    export TY_NEW_ECS_SERVER_TAIL_ERR="/tmp/TY_NEW_ecs_retention_${TEMP_ENEMY_COUNT}enemy_${TEMP_TICK_DAMAGE}damage_round${ROUND_INDEX}_tail.err"
  fi

  if [ -n "$SSH_KEY" ]; then
    "$VERIFY_SCRIPT" "$SSH_USER" "$HOST" "$SSH_KEY"
  else
    "$VERIFY_SCRIPT" "$SSH_USER" "$HOST"
  fi
}

VERIFY_STATUS=0
COMPLETED_ROUNDS=0
ROUND_INDEX=1
while [ "$ROUND_INDEX" -le "$ROUNDS" ]; do
  echo "TEMP_SERVICE_ROUND_BEGIN round=$ROUND_INDEX rounds=$ROUNDS retentionAttacks=$RETENTION_ATTACKS client1Seconds=$CLIENT1_SECONDS client2Seconds=$CLIENT2_SECONDS"
  assert_temporary_service_pid "$ROUND_INDEX" before
  set +e
  run_retention_verification "$ROUND_INDEX"
  ROUND_STATUS=$?
  set -e

  if [ "$ROUND_STATUS" -ne 0 ]; then
    VERIFY_STATUS=$ROUND_STATUS
    echo "TEMP_SERVICE_VERIFICATION_FAILED status=$ROUND_STATUS enemyCount=$TEMP_ENEMY_COUNT tickDamage=$TEMP_TICK_DAMAGE retainedAttacks=$RETENTION_ATTACKS round=$ROUND_INDEX rounds=$ROUNDS" >&2
    break
  fi

  echo "TEMP_SERVICE_VERIFICATION_OK enemyCount=$TEMP_ENEMY_COUNT tickDamage=$TEMP_TICK_DAMAGE retainedAttacks=$RETENTION_ATTACKS round=$ROUND_INDEX rounds=$ROUNDS"
  wait_public_health
  echo "TEMP_SERVICE_ROUND_HEALTH_OK round=$ROUND_INDEX rounds=$ROUNDS"
  assert_temporary_service_pid "$ROUND_INDEX" after
  COMPLETED_ROUNDS=$ROUND_INDEX
  echo "TEMP_SERVICE_ROUND_OK round=$ROUND_INDEX rounds=$ROUNDS pid=$TEMP_SERVICE_PID"
  ROUND_INDEX=$((ROUND_INDEX + 1))
done

restore_and_check_health

if [ "$VERIFY_STATUS" -ne 0 ]; then
  exit "$VERIFY_STATUS"
fi

echo "ECS_RETENTION_EXPERIMENT_OK temporaryEnemyCount=$TEMP_ENEMY_COUNT temporaryTickDamage=$TEMP_TICK_DAMAGE retainedAttacks=$RETENTION_ATTACKS completedRounds=$COMPLETED_ROUNDS requestedRounds=$ROUNDS temporaryServicePid=$TEMP_SERVICE_PID persistentEnemyCount=$BASE_ENEMY_COUNT persistentTickDamage=$BASE_TICK_DAMAGE persistentDeathDelaySeconds=$BASE_DEATH_DELAY_SECONDS"
