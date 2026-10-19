#!/usr/bin/env bash
# Strip AI attribution from a commit message file, in place (CLAUDE.md § Git Commit Policy).
# Human Co-Authored-By lines are kept. Usage: strip-ai-trailers.sh <commit-msg-file>
set -euo pipefail
ai='claude|anthropic|codex|openai|chatgpt|copilot|gemini|cursor'
sed -i -E "/^(Co-Authored-By:.*($ai)|($ai)-Session:|(🤖 )?Generated (with|by) .*($ai))/Id" "$1"
