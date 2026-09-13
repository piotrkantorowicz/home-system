#!/usr/bin/env bash
# Project-board helper for the delivery-loop skills (plan-issue, start-issue, ship, …).
#
#   scripts/board.sh add <issue-number> [<Status>]   add the issue to the board (optionally set Status)
#   scripts/board.sh status <issue-number> <Status>  move an item already on the board
#   scripts/board.sh statuses                        list the Status options the board has
#
# Board: https://github.com/users/piotrkantorowicz/projects/3 (BOARD_OWNER / BOARD_NUMBER
# override it). Field and option ids are resolved at run time, so renaming a column on the
# board needs no change here. Needs a gh token with the `project` scope — without it the
# script prints a warning and exits 0 so the skill flow is not blocked.
set -uo pipefail

owner=${BOARD_OWNER:-piotrkantorowicz}
number=${BOARD_NUMBER:-3}
repo=$(gh repo view --json nameWithOwner -q .nameWithOwner 2>/dev/null || echo "piotrkantorowicz/home-system")

usage() { sed -n '2,10p' "$0" >&2; exit 64; }
warn_skip() { echo "board: $1 — skipping (grant the token the 'project' scope: gh auth refresh -s project)" >&2; exit 0; }

project_json=$(gh project view "$number" --owner "$owner" --format json 2>/dev/null) || warn_skip "cannot read project $number of $owner"
project_id=$(jq -r .id <<< "$project_json")

status_field() {  # prints "<field-id> <option-id>" for a status name (case-insensitive)
  gh project field-list "$number" --owner "$owner" --format json \
    | jq -r --arg s "$1" '.fields[] | select(.name == "Status") | .id as $f
        | .options[] | select((.name | ascii_downcase) == ($s | ascii_downcase)) | "\($f) \(.id)"'
}

item_id() {  # item id for an issue number, or empty
  gh project item-list "$number" --owner "$owner" --format json -L 1000 \
    | jq -r --argjson n "$1" '.items[] | select(.content.number == $n and .content.repository == "'"$repo"'") | .id' | head -1
}

set_status() {
  local item=$1 status=$2 ids
  ids=$(status_field "$status")
  [[ -n "$ids" ]] || { echo "board: no Status option named '$status'. Have: $(gh project field-list "$number" --owner "$owner" --format json | jq -r '.fields[] | select(.name=="Status") | .options[].name' | paste -sd, -)" >&2; exit 1; }
  gh project item-edit --project-id "$project_id" --id "$item" \
    --field-id "${ids% *}" --single-select-option-id "${ids#* }" >/dev/null \
    && echo "board: #${3:-?} → $status"
}

case "${1:-}" in
  add)
    [[ -n "${2:-}" ]] || usage
    item=$(item_id "$2")
    if [[ -z "$item" ]]; then
      url="https://github.com/$repo/issues/$2"
      item=$(gh project item-add "$number" --owner "$owner" --url "$url" --format json | jq -r .id) || exit 1
      echo "board: added #$2"
    fi
    [[ -n "${3:-}" ]] && set_status "$item" "$3" "$2"
    ;;
  status)
    [[ -n "${2:-}" && -n "${3:-}" ]] || usage
    item=$(item_id "$2")
    [[ -n "$item" ]] || { echo "board: #$2 is not on the board — run: scripts/board.sh add $2 '$3'" >&2; exit 1; }
    set_status "$item" "$3" "$2"
    ;;
  statuses)
    gh project field-list "$number" --owner "$owner" --format json | jq -r '.fields[] | select(.name=="Status") | .options[].name'
    ;;
  *) usage ;;
esac
