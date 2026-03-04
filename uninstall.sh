#!/usr/bin/env bash
set -euo pipefail

PREFIX="${HOME}/.local/bin"

usage() {
  cat <<'USAGE'
Uninstall mcp-fs binary.

Usage:
  ./uninstall.sh [--prefix PATH]
USAGE
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --prefix)
      PREFIX="$2"
      shift 2
      ;;
    -h|--help)
      usage
      exit 0
      ;;
    *)
      echo "Unknown argument: $1" >&2
      usage >&2
      exit 1
      ;;
  esac
done

target="${PREFIX}/mcp-fs"
if [[ -f "${target}" ]]; then
  rm -f "${target}"
  echo "Removed ${target}"
else
  echo "Not installed at ${target}"
fi
