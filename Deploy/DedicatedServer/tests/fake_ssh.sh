#!/bin/sh
set -eu

case "$*" in
  *--property=MainPID*)
    echo "4242"
    ;;
  *TEMP_ENEMY_COUNT=*)
    if [ "${TY_NEW_FAKE_SSH_FAIL_INSTALL:-0}" = "1" ]; then
      echo "FAKE_TEMP_SERVICE_INSTALL_FAILED" >&2
      exit 42
    fi
    echo "FAKE_TEMP_SERVICE_INSTALL_OK"
    ;;
  *BASE_ENEMY_COUNT=*)
    if [ "${TY_NEW_FAKE_SSH_FAIL_RESTORE:-0}" = "1" ]; then
      echo "FAKE_SERVICE_RESTORE_FAILED" >&2
      exit 43
    fi
    echo "FAKE_SERVICE_RESTORE_OK"
    ;;
  *)
    echo "FAKE_SERVICE_RESTORE_FINALIZE_OK"
    ;;
esac
