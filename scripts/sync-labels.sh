#!/usr/bin/env bash
# Sync the repository labels with .github/labels.json (name, colour, description).
#
#   scripts/sync-labels.sh            create / update every label in the file
#   scripts/sync-labels.sh --prune    also delete labels that are not in the file
#
# Idempotent. Renames are not expressed here — rename with `gh label edit <old> --name <new>`
# first (keeps the label on existing issues), then update the file.
set -euo pipefail
cd "$(git rev-parse --show-toplevel)"
prune=0; [[ "${1:-}" == "--prune" ]] && prune=1

jq -r '.[] | [.name, .color, .description] | @tsv' .github/labels.json \
| while IFS=$'\t' read -r name color description; do
    gh label create "$name" --color "$color" --description "$description" --force >/dev/null
    echo "synced  $name"
  done

if (( prune )); then
  comm -23 <(gh label list --limit 200 --json name -q '.[].name' | sort) \
           <(jq -r '.[].name' .github/labels.json | sort) \
  | while read -r name; do
      count=$(gh issue list --state all --limit 1 --label "$name" --json number -q length)
      if [[ "$count" != "0" ]]; then
        echo "keep    $name (still on issues — migrate them first)" >&2; continue
      fi
      gh label delete "$name" --yes >/dev/null && echo "deleted $name"
    done
fi
