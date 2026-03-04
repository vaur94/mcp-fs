#!/usr/bin/env bash
set -euo pipefail

REPO="vaur94/mcp-fs"
PREFIX="${HOME}/.local/bin"
REQUESTED_VERSION=""
LOCAL_RELEASE_DIR=""

usage() {
  cat <<'USAGE'
Install latest (or specific) mcp-fs release binary with checksum verification.

Usage:
  ./install.sh [--prefix PATH] [--version X.Y.Z|vX.Y.Z] [--repo OWNER/REPO] [--local-release-dir DIR]
  ./install.sh --uninstall [--prefix PATH]

Options:
  --prefix PATH             Install directory (default: ~/.local/bin)
  --version VERSION         Install specific tag (default: latest)
  --repo OWNER/REPO         GitHub repository (default: vaur94/mcp-fs)
  --local-release-dir DIR   Use local release files instead of downloading
  --uninstall               Print uninstall command and exit
  -h, --help                Show this help

Security:
  Installation aborts unless SHA256 verification succeeds.
USAGE
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --prefix)
      PREFIX="$2"
      shift 2
      ;;
    --version)
      REQUESTED_VERSION="$2"
      shift 2
      ;;
    --repo)
      REPO="$2"
      shift 2
      ;;
    --local-release-dir)
      LOCAL_RELEASE_DIR="$2"
      shift 2
      ;;
    --uninstall)
      echo "Use ./uninstall.sh --prefix ${PREFIX}"
      exit 0
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

if ! command -v tar >/dev/null 2>&1; then
  echo "tar is required" >&2
  exit 1
fi

if [[ -z "${LOCAL_RELEASE_DIR}" ]] && ! command -v curl >/dev/null 2>&1; then
  echo "curl is required for remote install" >&2
  exit 1
fi

if command -v sha256sum >/dev/null 2>&1; then
  HASH_TOOL="sha256sum"
elif command -v shasum >/dev/null 2>&1; then
  HASH_TOOL="shasum -a 256"
else
  echo "sha256sum or shasum is required" >&2
  exit 1
fi

os_raw="$(uname -s)"
arch_raw="$(uname -m)"

case "${os_raw}" in
  Linux) os="linux" ;;
  Darwin) os="osx" ;;
  *)
    echo "Unsupported OS: ${os_raw}" >&2
    exit 1
    ;;
esac

case "${arch_raw}" in
  x86_64|amd64) arch="x64" ;;
  aarch64|arm64) arch="arm64" ;;
  *)
    echo "Unsupported architecture: ${arch_raw}" >&2
    exit 1
    ;;
esac

rid="${os}-${arch}"
archive_name="mcp-fs-${rid}.tar.gz"
checksum_name="SHA256SUMS"

normalize_tag() {
  local value="$1"
  if [[ -z "${value}" ]]; then
    echo "latest"
    return
  fi

  if [[ "${value}" =~ ^v ]]; then
    echo "${value}"
  else
    echo "v${value}"
  fi
}

tag="$(normalize_tag "${REQUESTED_VERSION}")"

tmp_dir="$(mktemp -d)"
cleanup() {
  rm -rf "${tmp_dir}"
}
trap cleanup EXIT

archive_path="${tmp_dir}/${archive_name}"
checksum_path="${tmp_dir}/${checksum_name}"

if [[ -n "${LOCAL_RELEASE_DIR}" ]]; then
  cp "${LOCAL_RELEASE_DIR}/${archive_name}" "${archive_path}"
  cp "${LOCAL_RELEASE_DIR}/${checksum_name}" "${checksum_path}"
else
  if [[ "${tag}" == "latest" ]]; then
    base_url="https://github.com/${REPO}/releases/latest/download"
  else
    base_url="https://github.com/${REPO}/releases/download/${tag}"
  fi

  echo "Downloading ${archive_name} and ${checksum_name} from ${REPO} (${tag})..."
  curl --fail --location --silent --show-error --output "${archive_path}" "${base_url}/${archive_name}"
  curl --fail --location --silent --show-error --output "${checksum_path}" "${base_url}/${checksum_name}"
fi

expected_hash="$(awk -v target="${archive_name}" '$2 == target { print $1 }' "${checksum_path}" | head -n1)"
if [[ -z "${expected_hash}" ]]; then
  echo "Could not find checksum entry for ${archive_name}" >&2
  exit 1
fi

if [[ "${HASH_TOOL}" == "sha256sum" ]]; then
  actual_hash="$(sha256sum "${archive_path}" | awk '{print $1}')"
else
  actual_hash="$(shasum -a 256 "${archive_path}" | awk '{print $1}')"
fi

if [[ "${expected_hash}" != "${actual_hash}" ]]; then
  echo "Checksum verification failed for ${archive_name}" >&2
  echo "Expected: ${expected_hash}" >&2
  echo "Actual:   ${actual_hash}" >&2
  exit 1
fi

echo "Checksum verified."

extract_dir="${tmp_dir}/extract"
mkdir -p "${extract_dir}"
tar -xzf "${archive_path}" -C "${extract_dir}"

source_binary="$(find "${extract_dir}" -type f -name 'McpFs' | head -n1 || true)"
if [[ -z "${source_binary}" ]]; then
  echo "Could not locate McpFs binary in archive" >&2
  exit 1
fi

mkdir -p "${PREFIX}"
install -m 0755 "${source_binary}" "${PREFIX}/mcp-fs"

echo "Installed: ${PREFIX}/mcp-fs"
echo "Run: ${PREFIX}/mcp-fs --help"
echo "Uninstall: ./uninstall.sh --prefix ${PREFIX}"
