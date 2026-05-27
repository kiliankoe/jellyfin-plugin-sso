#!/usr/bin/env bash
# Wrapper around SSO-Auth.TestEnv. Pass --volumes / -v to also wipe .data/ and .publish/.
set -euo pipefail
source "$(dirname "$0")/_lib.sh"
run_test_env_cli down "$@"
