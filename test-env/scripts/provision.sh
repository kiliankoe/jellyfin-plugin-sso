#!/usr/bin/env bash
# Wrapper around SSO-Auth.TestEnv. Re-register the dex OIDC provider against a running stack.
set -euo pipefail
source "$(dirname "$0")/_lib.sh"
run_test_env_cli provision "$@"
