#!/bin/sh
set -eu

ENEMY_COUNT=${TY_NEW_FAKE_P631_ENEMY_COUNT:-3}
TICK_DAMAGE=${TY_NEW_FAKE_P631_TICK_DAMAGE:-10}
DEATH_DELAY=${TY_NEW_FAKE_P631_DEATH_DELAY:-90}
PREFAB=${TY_NEW_FAKE_P631_PREFAB:-Multiplayer/PF_NetworkPlayerCombatTest}
PID_BEFORE=${TY_NEW_FAKE_P631_PID_BEFORE:-4242}
PID_AFTER=${TY_NEW_FAKE_P631_PID_AFTER:-$PID_BEFORE}
EXEC_START="/opt/ty-new-server/TYServer.x86_64 --network-player-prefab $PREFAB --network-enemy-count $ENEMY_COUNT --enable-network-enemy-server-tick --network-enemy-server-tick-damage $TICK_DAMAGE --network-enemy-server-tick-death-delay-seconds $DEATH_DELAY"

case "$*" in
  *--property=MainPID*)
    if [ -n "${TY_NEW_FAKE_P631_PID_STATE_FILE:-}" ]; then
      if [ -f "$TY_NEW_FAKE_P631_PID_STATE_FILE" ]; then
        echo "$PID_AFTER"
      else
        printf 'seen\n' > "$TY_NEW_FAKE_P631_PID_STATE_FILE"
        echo "$PID_BEFORE"
      fi
    else
      echo "$PID_BEFORE"
    fi
    ;;
  *--property=ExecStart*)
    echo "$EXEC_START"
    ;;
  *"sed -n"*ExecStart*)
    echo "$EXEC_START"
    ;;
  *"test ! -e"*)
    if [ "${TY_NEW_FAKE_P631_LOCK_EXISTS:-0}" = "1" ]; then
      exit 1
    fi
    ;;
  *)
    echo "Unexpected fake P6.31 SSH command: $*" >&2
    exit 44
    ;;
esac
