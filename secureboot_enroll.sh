#!/usr/bin/env bash
# secureboot_enroll.sh
# Enroll custom Secure Boot keys (PK, KEK, db) directly from Linux using efi-updatevar (no USB).
# Tested on Ubuntu/Debian-family distros.
# Usage:
#   sudo bash secureboot_enroll.sh
# Options:
#   --force   Overwrite existing key files in ./secureboot-keys
#
# This script:
#  1) Installs required packages (efitools, sbsigntool, openssl)
#  2) Generates PK, KEK, db keys and X.509 certs
#  3) Converts them to ESL
#  4) Produces AUTH files (signed updates) in correct trust order
#  5) Enrolls PK, KEK, db via efi-updatevar
#  6) Prints verification tips
#
# NOTE:
#  - Must be run on a machine in UEFI mode with Secure Boot disabled and Platform in Setup Mode.
#  - Once PK is enrolled, the platform exits Setup Mode.
#  - If efi-updatevar commands fail, your firmware may block OS-level updates; use KeyTool.efi instead.

set -euo pipefail

# --- helper ---
die() { echo "[ERROR] $*" >&2; exit 1; }
log() { echo -e "\n[INFO] $*"; }

[[ $EUID -eq 0 ]] || die "Run as root: sudo bash $0"

FORCE=0
if [[ \${1-} == "--force" ]]; then
  FORCE=1
fi

# --- prerequisites ---
log "Installing required packages (efitools, sbsigntool, openssl)..."
if command -v apt >/dev/null 2>&1; then
  apt update -y
  DEBIAN_FRONTEND=noninteractive apt install -y efitools sbsigntool openssl uuid-runtime
else
  log "apt not found. Please install equivalents for your distro: efitools, sbsigntool, openssl, uuid-runtime."
fi

# --- check UEFI ---
[[ -d /sys/firmware/efi ]] || die "System not booted in UEFI mode (no /sys/firmware/efi)."

# --- working dir ---
WORKDIR="${HOME}/secureboot-keys"
mkdir -p "$WORKDIR"
cd "$WORKDIR"

if [[ $FORCE -ne 1 ]]; then
  for f in PK.key PK.crt KEK.key KEK.crt db.key db.crt PK.esl KEK.esl db.esl PK.auth KEK.auth db.auth ; do
    if [[ -e "$f" ]]; then
      die "File $f already exists. Use --force to overwrite or remove it manually."
    fi
  done
fi

# --- generate keys/certs ---
log "Generating Platform Key (PK), Key Exchange Key (KEK), and Signature Database (db)..."
openssl req -new -x509 -newkey rsa:2048 -subj "/CN=My Platform Key/"   -keyout PK.key -out PK.crt -days 3650 -nodes -sha256

openssl req -new -x509 -newkey rsa:2048 -subj "/CN=My Key Exchange Key/"   -keyout KEK.key -out KEK.crt -days 3650 -nodes -sha256

openssl req -new -x509 -newkey rsa:2048 -subj "/CN=My Signature Database Key/"   -keyout db.key -out db.crt -days 3650 -nodes -sha256

# --- convert to ESL ---
log "Converting certificates to EFI Signature Lists (ESL)..."
cert-to-efi-sig-list -g "$(uuidgen)" PK.crt PK.esl
cert-to-efi-sig-list -g "$(uuidgen)" KEK.crt KEK.esl
cert-to-efi-sig-list -g "$(uuidgen)" db.crt db.esl

# --- create AUTH files ---
log "Creating signed AUTH updates (correct order: PK -> KEK -> db)..."
sign-efi-sig-list -k PK.key -c PK.crt PK PK.esl PK.auth
sign-efi-sig-list -k PK.key -c PK.crt KEK KEK.esl KEK.auth
sign-efi-sig-list -k KEK.key -c KEK.crt db db.esl db.auth

# --- enroll via efi-updatevar ---
log "Enrolling Platform Key (PK)..."
efi-updatevar -f PK.auth PK || die "Failed to enroll PK. Try KeyTool.efi if firmware blocks OS writes."

log "Enrolling Key Exchange Key (KEK)..."
efi-updatevar -f KEK.auth KEK || die "Failed to enroll KEK."

log "Enrolling Allow Database (db)..."
efi-updatevar -f db.auth db || die "Failed to enroll db."

log "Keys enrolled. Reboot into firmware setup to ENABLE Secure Boot."

# --- post info ---
cat <<'EOF'

Next steps:
  1) Reboot → enter BIOS/UEFI → Security → Secure Boot
  2) Confirm: Platform Mode = User Mode, Keys installed
  3) Turn Secure Boot ON, Save & Exit
  4) Back in Linux, verify:
       sudo mokutil --sb-state
       sudo efi-readvar
  5) If using custom kernels/bootloaders, sign them with db.key/db.crt (example):
       sudo sbsign --key db.key --cert db.crt --output /boot/efi/EFI/ubuntu/grubx64.efi.signed /boot/efi/EFI/ubuntu/grubx64.efi

Troubleshooting:
  - If efi-updatevar returns "Operation not permitted" or "Invalid parameter", your firmware may block OS-level updates.
    Use KeyTool.efi from efitools to enroll the .auth files in pre-boot.
  - Ensure you are in UEFI mode (/sys/firmware/efi exists).
  - Ensure Secure Boot is disabled and platform is in Setup Mode before running.
EOF

log "Done. Keys stored in: $WORKDIR"
