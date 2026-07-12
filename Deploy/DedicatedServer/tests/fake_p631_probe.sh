#!/bin/sh
set -eu

if [ "${TY_NEW_FAKE_P631_PROBE_FAIL:-0}" = "1" ]; then
  echo "FAKE_P631_PROBE_FAILED" >&2
  exit 45
fi

CONNECTED=${TY_NEW_FAKE_P631_CONNECTED:-0}
SPAWNED=${TY_NEW_FAKE_P631_SPAWNED:-0}
echo "P1.5_HEALTH_OK host=example.invalid healthPort=7778 networkPort=7777 connected=$CONNECTED spawned=$SPAWNED"
