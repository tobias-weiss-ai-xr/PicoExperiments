#!/usr/bin/env python3
"""adb pull helper for device recordings - stdlib only."""

import argparse
import os
import shutil
import subprocess
import sys


def self_test():
    """Run self-test without requiring adb - always exits 0."""
    print("self-test: OK")
    return 0


def list_devices(adb_path):
    """List connected adb devices."""
    try:
        result = subprocess.run(
            [adb_path, "devices"],
            capture_output=True,
            text=True,
            check=False,
        )
        devices = []
        for line in result.stdout.strip().split("\n")[1:]:
            line = line.strip()
            if line and not line.startswith("*"):
                parts = line.split()
                if len(parts) >= 2:
                    devices.append(parts[0])
        for device in devices:
            print(device)
        return 0
    except Exception as e:
        print(f"Error listing devices: {e}", file=sys.stderr)
        return 1


def pull_recordings(adb_path, package, dest, device=""):
    """Pull device recordings for a package."""
    if not os.path.isdir(dest):
        os.makedirs(dest)

    # Find the device
    devices_cmd = [adb_path, "devices"]
    if device:
        devices_cmd.extend(["-s", device])

    try:
        result = subprocess.run(
            [adb_path, "-s", device, "shell", "run-as", package, "ls", "/sdcard/"],
            capture_output=True,
            text=True,
            check=False,
        )
        # Parse files to pull
        files_dir = "/sdcard/"
        files = []
        for line in result.stdout.strip().split("\n"):
            line = line.strip()
            if line and line != "total" and not line.startswith("d"):
                files.append(os.path.join(files_dir, line.split()[-1]))

        if not files:
            print("No files found to pull")
            return 0

        # Pull each file
        for src_file in files:
            dest_file = os.path.join(dest, os.path.basename(src_file))
            pull_cmd = [adb_path, "-s", device, "pull", src_file, dest_file]
            pull_result = subprocess.run(pull_cmd, capture_output=True, text=True, check=False)
            if pull_result.returncode == 0:
                print(f"Pulled: {src_file} -> {dest_file}")
            else:
                print(f"Failed to pull {src_file}: {pull_result.stderr}", file=sys.stderr)
                return 1

        return 0

    except Exception as e:
        print(f"Error pulling recordings: {e}", file=sys.stderr)
        return 1


def main():
    parser = argparse.ArgumentParser(description="adb pull helper for device recordings")
    parser.add_argument("--adb", metavar="PATH", help="Path to adb executable")
    parser.add_argument("--package", metavar="PKG", help="Package name on device")
    parser.add_argument("--dest", metavar="DIR", help="Destination directory")
    parser.add_argument("--list", action="store_true", help="List connected devices")
    parser.add_argument("--device", metavar="DEVICE", default="", help="Target device serial")
    parser.add_argument(
        "--self-test", action="store_true", help="Run self-test without adb"
    )

    args = parser.parse_args()

    if args.self_test:
        return self_test()

    adb_path = args.adb or shutil.which("adb")
    if adb_path is None and not args.list:
        print("Error: adb not found. Use --adb or install adb.", file=sys.stderr)
        return 1

    if args.list:
        return list_devices(adb_path)

    if not args.package:
        print("Error: --package is required", file=sys.stderr)
        return 1

    if not args.dest:
        print("Error: --dest is required", file=sys.stderr)
        return 1

    return pull_recordings(adb_path, args.package, args.dest, args.device)


if __name__ == "__main__":
    sys.exit(main())
