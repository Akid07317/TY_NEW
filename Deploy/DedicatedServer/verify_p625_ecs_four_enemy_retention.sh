#!/bin/sh
set -eu

usage() {
  cat >&2 <<'EOF'
Usage: Deploy/DedicatedServer/verify_p625_ecs_four_enemy_retention.sh <ssh-user> <host> [ssh-key]

Temporarily switches the ECS ty-new-server.service from the stable P6.23
three-enemy baseline to the P6.25 four-enemy retention smoke configuration,
runs the public multiplayer verification, then restores the original service.

Defaults:
  baseline enemy count: 3
  baseline tick damage: 10
  baseline death delay: 90
  smoke enemy count: 4
  smoke tick damage: 5
  smoke retained attacks per enemy: 4

Set TY_NEW_P625_ENEMY_COUNT, TY_NEW_P625_TICK_DAMAGE,
TY_NEW_P625_DEATH_DELAY_SECONDS, TY_NEW_P625_BASE_ENEMY_COUNT,
TY_NEW_P625_BASE_TICK_DAMAGE, or TY_NEW_P625_BASE_DEATH_DELAY_SECONDS
to override the service argument contract.
EOF
  exit 1
}

if [ $# -lt 2 ] || [ $# -gt 3 ]; then
  usage
fi

SSH_USER=$1
HOST=$2
SSH_KEY=${3:-}
REMOTE="$SSH_USER@$HOST"
PROJECT_ROOT=$(CDPATH= cd -- "$(dirname -- "$0")/../.." && pwd)
VERIFY_SCRIPT="$PROJECT_ROOT/Deploy/DedicatedServer/verify_p15_ecs_multiplayer.sh"
PROBE_SCRIPT="$PROJECT_ROOT/Deploy/DedicatedServer/probe_p15_multiplayer.py"
SERVICE_NAME=ty-new-server.service
REMOTE_SERVICE=/etc/systemd/system/ty-new-server.service
REMOTE_BACKUP="/tmp/ty-new-server.service.p625-backup.$$"
RESTORE_NEEDED=0

P625_ENEMY_COUNT=${TY_NEW_P625_ENEMY_COUNT:-4}
P625_TICK_DAMAGE=${TY_NEW_P625_TICK_DAMAGE:-5}
P625_DEATH_DELAY_SECONDS=${TY_NEW_P625_DEATH_DELAY_SECONDS:-90}
BASE_ENEMY_COUNT=${TY_NEW_P625_BASE_ENEMY_COUNT:-3}
BASE_TICK_DAMAGE=${TY_NEW_P625_BASE_TICK_DAMAGE:-10}
BASE_DEATH_DELAY_SECONDS=${TY_NEW_P625_BASE_DEATH_DELAY_SECONDS:-90}
RETENTION_ATTACKS=${TY_NEW_P625_RETENTION_ATTACKS:-4}
GAME_PORT=${TY_NEW_GAME_PORT:-7777}
HEALTH_PORT=${TY_NEW_HEALTH_PORT:-7778}

validate_uint() {
  NAME=$1
  VALUE=$2
  case "$VALUE" in
    ''|*[!0-9]*)
      echo "$NAME must be a non-negative integer, got: $VALUE" >&2
      exit 1
      ;;
  esac
}

validate_uint TY_NEW_P625_ENEMY_COUNT "$P625_ENEMY_COUNT"
validate_uint TY_NEW_P625_TICK_DAMAGE "$P625_TICK_DAMAGE"
validate_uint TY_NEW_P625_DEATH_DELAY_SECONDS "$P625_DEATH_DELAY_SECONDS"
validate_uint TY_NEW_P625_BASE_ENEMY_COUNT "$BASE_ENEMY_COUNT"
validate_uint TY_NEW_P625_BASE_TICK_DAMAGE "$BASE_TICK_DAMAGE"
validate_uint TY_NEW_P625_BASE_DEATH_DELAY_SECONDS "$BASE_DEATH_DELAY_SECONDS"
validate_uint TY_NEW_P625_RETENTION_ATTACKS "$RETENTION_ATTACKS"
validate_uint TY_NEW_GAME_PORT "$GAME_PORT"
validate_uint TY_NEW_HEALTH_PORT "$HEALTH_PORT"

if [ ! -f "$VERIFY_SCRIPT" ]; then
  echo "Missing ECS verify script: $VERIFY_SCRIPT" >&2
  exit 1
fi

if [ ! -f "$PROBE_SCRIPT" ]; then
  echo "Missing P1.5 probe script: $PROBE_SCRIPT" >&2
  exit 1
fi

run_ssh() {
  if [ -n "$SSH_KEY" ]; then
    ssh -i "$SSH_KEY" "$@"
  else
    ssh "$@"
  fi
}

wait_public_health() {
  "$PROBE_SCRIPT" \
    --health-only \
    --host "$HOST" \
    --game-port "$GAME_PORT" \
    --health-port "$HEALTH_PORT" \
    --startup-timeout 30 \
    --socket-timeout 5
}

install_temporary_service() {
  echo "Temporarily switching $REMOTE $SERVICE_NAME to $P625_ENEMY_COUNT enemies / tick damage $P625_TICK_DAMAGE"
  run_ssh "$REMOTE" \
    "P625_ENEMY_COUNT=$P625_ENEMY_COUNT P625_TICK_DAMAGE=$P625_TICK_DAMAGE P625_DEATH_DELAY_SECONDS=$P625_DEATH_DELAY_SECONDS BASE_ENEMY_COUNT=$BASE_ENEMY_COUNT BASE_TICK_DAMAGE=$BASE_TICK_DAMAGE BASE_DEATH_DELAY_SECONDS=$BASE_DEATH_DELAY_SECONDS REMOTE_BACKUP='$REMOTE_BACKUP' REMOTE_SERVICE='$REMOTE_SERVICE' SERVICE_NAME='$SERVICE_NAME' sh -s" <<'REMOTE_SCRIPT'
set -eu

if ! sudo test -f "$REMOTE_SERVICE"; then
  echo "Missing remote service: $REMOTE_SERVICE" >&2
  exit 1
fi

CURRENT_EXEC=$(sudo sed -n 's/^ExecStart=//p' "$REMOTE_SERVICE" | tail -n 1)
case "$CURRENT_EXEC" in
  *"--network-enemy-count $BASE_ENEMY_COUNT"*) ;;
  *)
    echo "Refusing to override service: expected baseline --network-enemy-count $BASE_ENEMY_COUNT." >&2
    echo "$CURRENT_EXEC" >&2
    exit 1
    ;;
esac
case "$CURRENT_EXEC" in
  *"--network-enemy-server-tick-damage $BASE_TICK_DAMAGE"*) ;;
  *)
    echo "Refusing to override service: expected baseline --network-enemy-server-tick-damage $BASE_TICK_DAMAGE." >&2
    echo "$CURRENT_EXEC" >&2
    exit 1
    ;;
esac
case "$CURRENT_EXEC" in
  *"--network-enemy-server-tick-death-delay-seconds $BASE_DEATH_DELAY_SECONDS"*) ;;
  *)
    echo "Refusing to override service: expected baseline --network-enemy-server-tick-death-delay-seconds $BASE_DEATH_DELAY_SECONDS." >&2
    echo "$CURRENT_EXEC" >&2
    exit 1
    ;;
esac

sudo cp "$REMOTE_SERVICE" "$REMOTE_BACKUP"
TMP_SERVICE=$(mktemp /tmp/ty-new-server.service.p625.XXXXXX)
sudo sed \
  -e "s/--network-enemy-count $BASE_ENEMY_COUNT/--network-enemy-count $P625_ENEMY_COUNT/" \
  -e "s/--network-enemy-server-tick-damage $BASE_TICK_DAMAGE/--network-enemy-server-tick-damage $P625_TICK_DAMAGE/" \
  -e "s/--network-enemy-server-tick-death-delay-seconds $BASE_DEATH_DELAY_SECONDS/--network-enemy-server-tick-death-delay-seconds $P625_DEATH_DELAY_SECONDS/" \
  "$REMOTE_SERVICE" > "$TMP_SERVICE"

if ! grep -q -- "--network-enemy-count $P625_ENEMY_COUNT" "$TMP_SERVICE"; then
  echo "Temporary service did not contain requested enemy count." >&2
  rm -f "$TMP_SERVICE"
  exit 1
fi
if ! grep -q -- "--network-enemy-server-tick-damage $P625_TICK_DAMAGE" "$TMP_SERVICE"; then
  echo "Temporary service did not contain requested tick damage." >&2
  rm -f "$TMP_SERVICE"
  exit 1
fi

sudo cp "$TMP_SERVICE" "$REMOTE_SERVICE"
rm -f "$TMP_SERVICE"
sudo systemctl daemon-reload
sudo systemctl restart "$SERVICE_NAME"
sudo systemctl is-active --quiet "$SERVICE_NAME"
sudo systemctl status "$SERVICE_NAME" --no-pager
REMOTE_SCRIPT
}

restore_remote_service() {
  echo "Restoring $REMOTE $SERVICE_NAME from $REMOTE_BACKUP"
  run_ssh "$REMOTE" \
    "REMOTE_BACKUP='$REMOTE_BACKUP' REMOTE_SERVICE='$REMOTE_SERVICE' SERVICE_NAME='$SERVICE_NAME' sh -s" <<'REMOTE_SCRIPT'
set -eu

if ! sudo test -f "$REMOTE_BACKUP"; then
  echo "No remote backup found at $REMOTE_BACKUP; skipping restore." >&2
  exit 0
fi

sudo cp "$REMOTE_BACKUP" "$REMOTE_SERVICE"
sudo rm -f "$REMOTE_BACKUP"
sudo systemctl daemon-reload
sudo systemctl restart "$SERVICE_NAME"
sudo systemctl is-active --quiet "$SERVICE_NAME"
sudo systemctl status "$SERVICE_NAME" --no-pager
REMOTE_SCRIPT
}

cleanup() {
  STATUS=$?
  if [ "$RESTORE_NEEDED" = "1" ]; then
    if restore_remote_service; then
      RESTORE_NEEDED=0
    elif [ "$STATUS" -eq 0 ]; then
      STATUS=1
    fi
  fi
  exit "$STATUS"
}

trap cleanup EXIT INT TERM

RESTORE_NEEDED=1
install_temporary_service
wait_public_health

set +e
TY_NEW_REQUIRE_NETWORK_ENEMY_TARGET_RETENTION=1 \
TY_NEW_MIN_NETWORK_ENEMY_TARGET_RETENTION_ATTACKS="$RETENTION_ATTACKS" \
TY_NEW_MIN_NETWORK_ENEMY_COUNT="$P625_ENEMY_COUNT" \
TY_NEW_CLIENT1_SECONDS="${TY_NEW_CLIENT1_SECONDS:-100}" \
TY_NEW_CLIENT2_SECONDS="${TY_NEW_CLIENT2_SECONDS:-90}" \
TY_NEW_NETWORK_PLAYER_PREFAB="${TY_NEW_NETWORK_PLAYER_PREFAB:-Multiplayer/PF_NetworkPlayerCombatTest}" \
TY_NEW_SKIP_AUTO_FORMAL_SYNC_REQUIREMENTS="${TY_NEW_SKIP_AUTO_FORMAL_SYNC_REQUIREMENTS:-1}" \
TY_NEW_SKIP_CLIENT_DESPAWN_CHECK="${TY_NEW_SKIP_CLIENT_DESPAWN_CHECK:-1}" \
TY_NEW_SKIP_REMOTE_MOVEMENT_CHECK="${TY_NEW_SKIP_REMOTE_MOVEMENT_CHECK:-1}" \
TY_NEW_SKIP_HEALTH_SYNC_CHECK="${TY_NEW_SKIP_HEALTH_SYNC_CHECK:-1}" \
TY_NEW_REQUIRE_DEATH_SYNC="${TY_NEW_REQUIRE_DEATH_SYNC:-0}" \
TY_NEW_ECS_SERVER_LOG="${TY_NEW_ECS_SERVER_LOG:-/tmp/TY_NEW_p625_ecs_4enemy_retention_scripted.log}" \
TY_NEW_ECS_SERVER_TAIL_ERR="${TY_NEW_ECS_SERVER_TAIL_ERR:-/tmp/TY_NEW_p625_ecs_4enemy_retention_scripted_tail.err}" \
  "$VERIFY_SCRIPT" "$SSH_USER" "$HOST" ${SSH_KEY:+"$SSH_KEY"}
VERIFY_STATUS=$?
set -e

restore_remote_service
RESTORE_NEEDED=0
wait_public_health

if [ "$VERIFY_STATUS" -ne 0 ]; then
  exit "$VERIFY_STATUS"
fi

echo "P6.25 scripted ECS four-enemy retention verification passed and service was restored."
