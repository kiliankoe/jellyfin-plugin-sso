#!/usr/bin/env bash
# Wrapper around SSO-Auth.TestEnv. Republish + restart Jellyfin.
set -euo pipefail
source "$(dirname "$0")/_lib.sh"
run_test_env_cli reload "$@"
