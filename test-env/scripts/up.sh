#!/usr/bin/env bash
# Wrapper around SSO-Auth.TestEnv. See `dotnet run --project test-env/SSO-Auth.TestEnv -- up --help`.
set -euo pipefail
source "$(dirname "$0")/_lib.sh"
run_test_env_cli up "$@"
