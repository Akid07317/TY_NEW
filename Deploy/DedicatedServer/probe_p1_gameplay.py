#!/usr/bin/env python3
"""Probe the TY_NEW P1 gameplay TCP line protocol."""

import argparse
import socket
import sys


def read_line(stream, label):
    line = stream.readline()
    if not line:
        raise RuntimeError(f"missing {label} response")

    return line.strip()


def require_contains(line, expected):
    if expected not in line:
        raise RuntimeError(f"expected {expected!r} in {line!r}")


def run_probe(host, port, player_name, timeout):
    with socket.create_connection((host, port), timeout) as sock:
        sock.settimeout(timeout)

        with sock.makefile("rw", encoding="utf-8", newline="\n") as stream:
            welcome = read_line(stream, "welcome")
            require_contains(welcome, "TY_NEW_GAME")
            require_contains(welcome, "protocol=1")
            print(welcome)

            stream.write(f"HELLO playerName={player_name}\n")
            stream.flush()
            joined = read_line(stream, "joined")
            require_contains(joined, "JOINED")
            require_contains(joined, f"playerName={player_name}")
            print(joined)

            stream.write("PING\n")
            stream.flush()
            pong = read_line(stream, "pong")
            require_contains(pong, "PONG")
            require_contains(pong, "joined=true")
            print(pong)

            stream.write("STATE\n")
            stream.flush()
            room = read_line(stream, "room state")
            require_contains(room, "ROOM")
            require_contains(room, "players=")
            print(room)

            stream.write("QUIT\n")
            stream.flush()
            bye = read_line(stream, "bye")
            require_contains(bye, "BYE")
            print(bye)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("host", help="server host or IP")
    parser.add_argument("--port", type=int, default=7777, help="gameplay TCP port")
    parser.add_argument("--player-name", default="CodexSmoke", help="HELLO playerName value")
    parser.add_argument("--timeout", type=float, default=5.0, help="socket timeout in seconds")
    args = parser.parse_args()

    try:
        run_probe(args.host, args.port, args.player_name, args.timeout)
    except Exception as exc:
        print(f"P1 gameplay probe failed: {exc}", file=sys.stderr)
        return 1

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
