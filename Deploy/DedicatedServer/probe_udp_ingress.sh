#!/bin/sh
set -eu

usage() {
  cat >&2 <<'EOF'
Usage: Deploy/DedicatedServer/probe_udp_ingress.sh <ssh-user> <host> [ssh-key]

Starts a short tcpdump on the ECS host, sends one UDP datagram from this
machine to <host>:7777, and reports whether the packet reached the instance.
Set TY_NEW_UDP_PORT to override the port and TY_NEW_TCPDUMP_SECONDS to override
the capture window. Set TY_NEW_UDP_PROBE_PACKETS to send more probe packets.
EOF
  exit 1
}

if [ $# -lt 2 ] || [ $# -gt 3 ]; then
  usage
fi

SSH_USER=$1
HOST=$2
SSH_KEY=${3:-}
PORT=${TY_NEW_UDP_PORT:-7777}
CAPTURE_SECONDS=${TY_NEW_TCPDUMP_SECONDS:-10}
PROBE_PACKETS=${TY_NEW_UDP_PROBE_PACKETS:-5}
REMOTE="$SSH_USER@$HOST"

if ! command -v nc >/dev/null 2>&1; then
  echo "nc is required to send the UDP probe packet." >&2
  exit 1
fi

TMP_OUTPUT=$(mktemp "${TMPDIR:-/tmp}/ty-new-udp-ingress.XXXXXX")
cleanup() {
  rm -f "$TMP_OUTPUT"
}
trap cleanup EXIT

run_ssh() {
  if [ -n "$SSH_KEY" ]; then
    ssh -i "$SSH_KEY" "$@"
  else
    ssh "$@"
  fi
}

echo "Starting ECS tcpdump for udp port $PORT on $REMOTE"
run_ssh "$REMOTE" "sudo timeout '$CAPTURE_SECONDS' tcpdump -n -i any -c 1 udp port '$PORT'" >"$TMP_OUTPUT" 2>&1 &
TCPDUMP_PID=$!

sleep 2
PACKET_INDEX=1
while [ "$PACKET_INDEX" -le "$PROBE_PACKETS" ]; do
  printf 'ty-new-p15-udp-smoke-%s' "$PACKET_INDEX" | nc -u -w 1 "$HOST" "$PORT" || true
  PACKET_INDEX=$((PACKET_INDEX + 1))
  sleep 1
done

set +e
wait "$TCPDUMP_PID"
TCPDUMP_STATUS=$?
set -e

cat "$TMP_OUTPUT"

if [ "$TCPDUMP_STATUS" -eq 0 ] && grep -q " IP " "$TMP_OUTPUT"; then
  echo "P1.5_UDP_INGRESS_OK host=$HOST port=$PORT"
  exit 0
fi

PUBLIC_IPV4=${TY_NEW_PUBLIC_IPV4:-}
if [ -z "$PUBLIC_IPV4" ] && command -v curl >/dev/null 2>&1; then
  PUBLIC_IPV4=$(curl -4 -s --max-time 3 https://api.ipify.org || true)
fi

echo "P1.5_UDP_INGRESS_BLOCKED host=$HOST port=$PORT"
echo "If ECS ufw/iptables/nftables are open, check the Aliyun security group inbound UDP rule." >&2
if [ -n "$PUBLIC_IPV4" ]; then
  echo "Expected Aliyun rule: inbound UDP $PORT/$PORT source $PUBLIC_IPV4/32 on the ECS security group." >&2
else
  echo "Expected Aliyun rule: inbound UDP $PORT/$PORT source <your-current-public-ip>/32 on the ECS security group." >&2
fi
exit 1
