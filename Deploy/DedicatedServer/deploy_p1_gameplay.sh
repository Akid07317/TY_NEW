#!/bin/sh
set -eu

usage() {
  cat >&2 <<'EOF'
Usage: Deploy/DedicatedServer/deploy_p1_gameplay.sh <ssh-user> <host> [ssh-key]

Uploads the selected Dedicated Server package, installs it under
/opt/ty-new-server, restarts ty-new-server.service, and runs local P1/P1.5
probes on the ECS host. Set TY_NEW_SERVER_PACKAGE to override the package path.
EOF
  exit 1
}

if [ $# -lt 2 ] || [ $# -gt 3 ]; then
  usage
fi

SSH_USER=$1
HOST=$2
SSH_KEY=${3:-}

PROJECT_ROOT=$(CDPATH= cd -- "$(dirname -- "$0")/../.." && pwd)
PACKAGE=${TY_NEW_SERVER_PACKAGE:-"$PROJECT_ROOT/Builds/DedicatedServer/TYServer-linux-x86_64-p3-attack.tar.gz"}
PACKAGE_NAME=$(basename "$PACKAGE")
SERVICE="$PROJECT_ROOT/Deploy/DedicatedServer/ty-new-server.service"
PROBE="$PROJECT_ROOT/Deploy/DedicatedServer/probe_p1_gameplay.py"
PROBE_P15="$PROJECT_ROOT/Deploy/DedicatedServer/probe_p15_multiplayer.py"
REMOTE="$SSH_USER@$HOST"

if [ ! -f "$PACKAGE" ]; then
  echo "Missing package: $PACKAGE" >&2
  exit 1
fi

if [ ! -f "$SERVICE" ]; then
  echo "Missing service file: $SERVICE" >&2
  exit 1
fi

if [ ! -f "$PROBE" ]; then
  echo "Missing probe script: $PROBE" >&2
  exit 1
fi

if [ ! -f "$PROBE_P15" ]; then
  echo "Missing P1.5 probe script: $PROBE_P15" >&2
  exit 1
fi

if ! command -v shasum >/dev/null 2>&1; then
  echo "shasum is required to calculate the package checksum." >&2
  exit 1
fi

PACKAGE_SHA256=$(shasum -a 256 "$PACKAGE" | awk '{print $1}')

run_scp() {
  if [ -n "$SSH_KEY" ]; then
    scp -i "$SSH_KEY" "$@"
  else
    scp "$@"
  fi
}

run_ssh() {
  if [ -n "$SSH_KEY" ]; then
    ssh -i "$SSH_KEY" "$@"
  else
    ssh "$@"
  fi
}

echo "Uploading Dedicated Server package and service to $REMOTE:/tmp"
echo "Local package SHA256: $PACKAGE_SHA256"
run_scp "$PACKAGE" "$SERVICE" "$PROBE" "$PROBE_P15" "$REMOTE:/tmp/"

echo "Installing and restarting ty-new-server.service on $HOST"
run_ssh "$REMOTE" "EXPECTED_SHA256='$PACKAGE_SHA256' PACKAGE_NAME='$PACKAGE_NAME' sh -s" <<'REMOTE_SCRIPT'
set -eu

REMOTE_PACKAGE="/tmp/$PACKAGE_NAME"
if ! command -v sha256sum >/dev/null 2>&1; then
  echo "sha256sum is required on the ECS host." >&2
  exit 1
fi

REMOTE_SHA256=$(sha256sum "$REMOTE_PACKAGE" | awk '{print $1}')
echo "Remote package SHA256: $REMOTE_SHA256"
if [ "$REMOTE_SHA256" != "$EXPECTED_SHA256" ]; then
  echo "Package checksum mismatch. Expected $EXPECTED_SHA256." >&2
  exit 1
fi

sudo mkdir -p /opt/ty-new-server /var/log/ty-new
sudo systemctl stop ty-new-server || true
sudo tar -xzf "$REMOTE_PACKAGE" -C /opt/ty-new-server
sudo find /opt/ty-new-server -name '._*' -type f -delete
sudo chmod +x /opt/ty-new-server/TYServer.x86_64
sudo chown -R tyserver:tyserver /opt/ty-new-server /var/log/ty-new
sudo cp /tmp/ty-new-server.service /etc/systemd/system/ty-new-server.service
sudo systemctl daemon-reload
sudo systemctl enable ty-new-server
sudo systemctl start ty-new-server
sudo systemctl status ty-new-server --no-pager

attempt=1
while [ "$attempt" -le 30 ]; do
  if python3 /tmp/probe_p15_multiplayer.py --health-only --host 127.0.0.1 --health-port 7778 --game-port 7777 --startup-timeout 2 --socket-timeout 2; then
    break
  fi

  sleep 1
  attempt=$((attempt + 1))
done

if [ "$attempt" -gt 30 ]; then
  echo "P1.5 NGO health probe did not become ready on 127.0.0.1:7778." >&2
  sudo systemctl status ty-new-server --no-pager >&2 || true
  tail -n 80 /var/log/ty-new/server.log >&2 || true
  exit 1
fi

if ! command -v ss >/dev/null 2>&1; then
  echo "ss is required on the ECS host to verify UDP 7777." >&2
  exit 1
fi

if ! sudo ss -lunp | awk '$4 ~ /:7777$/ { found=1 } END { exit found ? 0 : 1 }'; then
  echo "P1.5 NGO UDP listener was not found on port 7777." >&2
  sudo ss -lunp >&2 || true
  sudo systemctl status ty-new-server --no-pager >&2 || true
  tail -n 80 /var/log/ty-new/server.log >&2 || true
  exit 1
fi

attempt=1
while [ "$attempt" -le 30 ]; do
  if python3 /tmp/probe_p1_gameplay.py 127.0.0.1 --player-name ECSSmoke; then
    break
  fi

  sleep 1
  attempt=$((attempt + 1))
done

if [ "$attempt" -gt 30 ]; then
  echo "Gameplay probe did not become ready on 127.0.0.1:7777." >&2
  sudo systemctl status ty-new-server --no-pager >&2 || true
  tail -n 80 /var/log/ty-new/server.log >&2 || true
  exit 1
fi
REMOTE_SCRIPT

echo "Remote local health, UDP listener, and P1 TCP gameplay probes passed."
echo "If Aliyun security group allows TCP 7777, run:"
echo "python3 Deploy/DedicatedServer/probe_p1_gameplay.py $HOST --player-name PublicSmoke"
echo "For P1.5 NGO clients, also allow UDP 7777 from the playtest client IP, then run:"
echo "Deploy/DedicatedServer/probe_p15_multiplayer.py --skip-server-start --host $HOST --game-port 7777 --health-port 7778 --connected-timeout 60"
