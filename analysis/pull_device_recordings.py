#!/usr/bin/env python3
"""Pull AoI recordings from a PICO device build.

The Unity app writes recordings to Application.persistentDataPath on device:
    /storage/emulated/0/Android/data/<package>/files/Recordings/

Usage:
    python analysis/pull_device_recordings.py --package <pkg>                # pull all
    python analysis/pull_device_recordings.py --package <pkg> --list         # preview
    python analysis/pull_device_recordings.py --package <pkg> --device <serial>
    python analysis/pull_device_recordings.py --self-test                    # no adb needed

stdlib only.
"""

import argparse
import os
import shutil
import subprocess
import sys

DEVICE_RECORDINGS_DIR = "/storage/emulated/0/Android/data/{package}/files/Recordings"


def run(adb, args):
    return subprocess.run([adb] + args, capture_output=True, text=True, check=False)


def dev_args(device, *rest):
    """adb -s <serial> <rest...>  (global -s must come before the command)"""
    return (["-s", device] if device else []) + list(rest)


def parse_devices(devices_out):
    """Parse `adb devices` stdout -> list of serials (state 'device' only)."""
    serials = []
    for line in devices_out.splitlines()[1:]:
        line = line.strip()
        if not line or line.startswith("*"):
            continue
        parts = line.split()
        if len(parts) >= 2 and parts[1] == "device":
            serials.append(parts[0])
    return serials


def parse_listing(ls_out):
    """Parse `ls -1 -p` output of the recordings dir -> CSV file names."""
    names = []
    for line in ls_out.splitlines():
        name = line.strip()
        if name and not name.endswith("/"):
            names.append(name)
    return sorted(names)


def list_recordings(adb, package, device):
    target = DEVICE_RECORDINGS_DIR.format(package=package)
    r = run(adb, dev_args(device, "shell", "ls", "-1", "-p", target))
    if r.returncode != 0:
        print(f"error listing {target}: {r.stderr.strip()}", file=sys.stderr)
        return None
    return parse_listing(r.stdout)


def pull_recordings(adb, package, dest, device):
    names = list_recordings(adb, package, device)
    if names is None:
        return 1
    if not names:
        print(f"no recordings found in {DEVICE_RECORDINGS_DIR.format(package=package)}")
        return 0
    os.makedirs(dest, exist_ok=True)
    failures = 0
    for name in names:
        src = DEVICE_RECORDINGS_DIR.format(package=package) + "/" + name
        r = run(adb, dev_args(device, "pull", src, os.path.join(dest, name)))
        if r.returncode == 0:
            print(f"pulled {name}")
        else:
            print(f"FAILED {name}: {r.stderr.strip()}", file=sys.stderr)
            failures += 1
    print(f"{len(names) - failures}/{len(names)} files -> {dest}")
    return 1 if failures else 0


def self_test():
    counts = {
        "List of devices attached\n"
        "ABC123\tdevice\n"
        "XYZ789\tunauthorized\n": ["ABC123"],
        "* daemon started successfully *\n": [],
        "": [],
    }
    for out, expected in counts.items():
        got = parse_devices(out)
        assert got == expected, f"parse_devices({out!r}) -> {got}"
    names = parse_listing("s1-aoi.csv\ns1-aoi-raw.csv\nsubdir/\n./\n../\n\n")
    assert names == ["s1-aoi-raw.csv", "s1-aoi.csv"], names
    assert "ABC" not in parse_devices("FOO\tno permissions")
    print("self-test: OK (device parsing, recording listing)")
    return 0


def main():
    p = argparse.ArgumentParser(description="Pull AoI recordings from a PICO device")
    p.add_argument("--adb", help="Path to adb (default: search PATH)")
    p.add_argument("--package", help="Android package name, e.g. com.example.app")
    p.add_argument("--dest", default="Recordings/device", help="Destination dir (default: Recordings/device)")
    p.add_argument("--device", help="Device serial when multiple are attached")
    p.add_argument("--list", action="store_true", help="Preview recordings without pulling")
    p.add_argument("--self-test", action="store_true", help="Offline self-test, no adb required")
    args = p.parse_args()

    if args.self_test:
        return self_test()

    adb = args.adb or shutil.which("adb")
    if adb is None:
        print("error: adb not found; pass --adb or add adb to PATH", file=sys.stderr)
        return 1

    r = run(adb, ["devices"])
    if r.returncode != 0:
        print(f"error running adb: {r.stderr.strip()}", file=sys.stderr)
        return 1
    serials = parse_devices(r.stdout)
    if not serials:
        print("no device connected (adb devices empty)")
        return 1
    if args.device and args.device not in serials:
        print(f"error: device {args.device!r} not connected; found: {', '.join(serials)}", file=sys.stderr)
        return 1
    device = args.device or (serials[0] if len(serials) == 1 else None)
    if device is None:
        print(f"multiple devices attached: {', '.join(serials)}; pass --device", file=sys.stderr)
        return 1

    if not args.package:
        p.error("--package is required")

    if args.list:
        names = list_recordings(adb, args.package, device)
        if names is None:
            return 1
        for n in names:
            print(n)
        print(f"{len(names)} file(s)")
        return 0

    return pull_recordings(adb, args.package, args.dest, device)


if __name__ == "__main__":
    sys.exit(main())
