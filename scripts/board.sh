#!/usr/bin/env bash
# Project-board helper for the delivery-loop skills (plan-issue, start-issue, ship, …).
#
#   scripts/board.sh add <issue-number> [<Status>]   add the issue to the board (optionally set Status)
#   scripts/board.sh status <issue-number> <Status>  move an item already on the board
#   scripts/board.sh statuses                        list the Status options the board has
#
# Board: https://github.com/users/piotrkantorowicz/projects/3 (BOARD_OWNER / BOARD_NUMBER
# override it). Field and option ids are resolved at run time, so renaming a column on the
# board needs no change here. Talks GraphQL directly: the `gh project` subcommands insist
# on read:org + read:discussion scopes the board token does not need.
#
# Token: user-owned projects are not reachable with a fine-grained PAT, so the board uses
# its own classic token (scopes `repo` + `project`, nothing else) instead of gh's login:
#   BOARD_TOKEN env var, or one line in ${XDG_CONFIG_HOME:-~/.config}/home-system/board-token
# Only this script's gh calls see it. Without a usable token the script prints a warning
# and exits 0 so the skill flow is not blocked.
set -uo pipefail

token_file="${XDG_CONFIG_HOME:-$HOME/.config}/home-system/board-token"
if [[ -z "${BOARD_TOKEN:-}" && -r "$token_file" ]]; then
  BOARD_TOKEN=$(head -n1 "$token_file" | tr -d '[:space:]')
fi
[[ -n "${BOARD_TOKEN:-}" ]] && export GH_TOKEN="$BOARD_TOKEN"

owner=${BOARD_OWNER:-piotrkantorowicz}
number=${BOARD_NUMBER:-3}
repo=$(gh repo view --json nameWithOwner -q .nameWithOwner 2>/dev/null || echo "piotrkantorowicz/home-system")
repo_owner=${repo%/*}; repo_name=${repo#*/}

usage() { sed -n '2,7p' "$0" >&2; exit 64; }
warn_skip() { echo "board: $1 — skipping (set BOARD_TOKEN or write a classic PAT with scopes repo+project to $token_file)" >&2; exit 0; }
gql() { gh api graphql "$@" 2>&1; }

# Project id + Status field (id + options) in one query.
project_json=$(gql -F owner="$owner" -F number="$number" -f query='
  query($owner:String!,$number:Int!){ user(login:$owner){ projectV2(number:$number){
    id
    field(name:"Status"){ ... on ProjectV2SingleSelectField { id options{ id name } } } } } }')
project_id=$(jq -r '.data.user.projectV2.id // empty' <<< "$project_json")
[[ -n "$project_id" ]] || warn_skip "cannot read project $number of $owner ($(jq -r '.errors[0].message // .' <<< "$project_json" 2>/dev/null | head -c 120))"
status_field_id=$(jq -r '.data.user.projectV2.field.id' <<< "$project_json")

status_option_id() {  # case-insensitive lookup of a Status option name → id
  jq -r --arg s "$1" '.data.user.projectV2.field.options[]
    | select((.name | ascii_downcase) == ($s | ascii_downcase)) | .id' <<< "$project_json" | head -1
}
status_names() { jq -r '.data.user.projectV2.field.options[].name' <<< "$project_json"; }

issue_json() {  # node id of the issue + the id of its item on this board (if any)
  gql -F owner="$repo_owner" -F name="$repo_name" -F issue="$1" -f query='
    query($owner:String!,$name:String!,$issue:Int!){ repository(owner:$owner,name:$name){
      issue(number:$issue){ id projectItems(first:50){ nodes{ id project{ number } } } } } }' \
    | jq -c --argjson n "$number" '.data.repository.issue
        | { content: .id, item: ([.projectItems.nodes[] | select(.project.number == $n) | .id] | first) }' 2>/dev/null
}

set_status() {  # set_status <item-id> <status-name> <issue-number>
  local option; option=$(status_option_id "$2")
  [[ -n "$option" ]] || { echo "board: no Status option named '$2'. Have: $(status_names | paste -sd, -)" >&2; exit 1; }
  gql -F project="$project_id" -F item="$1" -F field="$status_field_id" -F option="$option" -f query='
    mutation($project:ID!,$item:ID!,$field:ID!,$option:String!){
      updateProjectV2ItemFieldValue(input:{projectId:$project,itemId:$item,fieldId:$field,
        value:{singleSelectOptionId:$option}}){ projectV2Item{ id } } }' >/dev/null \
    && echo "board: #$3 → $2"
}

case "${1:-}" in
  add)
    [[ -n "${2:-}" ]] || usage
    info=$(issue_json "$2"); content=$(jq -r '.content // empty' <<< "$info"); item=$(jq -r '.item // empty' <<< "$info")
    [[ -n "$content" ]] || { echo "board: issue #$2 not found in $repo" >&2; exit 1; }
    if [[ -z "$item" ]]; then
      item=$(gql -F project="$project_id" -F content="$content" -f query='
        mutation($project:ID!,$content:ID!){ addProjectV2ItemById(input:{projectId:$project,contentId:$content}){ item{ id } } }' \
        | jq -r '.data.addProjectV2ItemById.item.id // empty')
      [[ -n "$item" ]] || { echo "board: could not add #$2" >&2; exit 1; }
      echo "board: added #$2"
    fi
    [[ -n "${3:-}" ]] && set_status "$item" "$3" "$2"
    ;;
  status)
    [[ -n "${2:-}" && -n "${3:-}" ]] || usage
    item=$(issue_json "$2" | jq -r '.item // empty')
    [[ -n "$item" ]] || { echo "board: #$2 is not on the board — run: scripts/board.sh add $2 '$3'" >&2; exit 1; }
    set_status "$item" "$3" "$2"
    ;;
  statuses) status_names ;;
  *) usage ;;
esac
