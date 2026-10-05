#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
MANIFEST="$SCRIPT_DIR/manifest.json"
RESEND_BASE_URL="${RESEND_BASE_URL:-https://api.resend.com}"

DRY_RUN=false
ONLY_ALIAS=""

while [[ $# -gt 0 ]]; do
  case "$1" in
    --dry-run)
      DRY_RUN=true
      shift
      ;;
    --only)
      if [[ $# -lt 2 ]]; then
        echo "Error: --only requires an alias argument." >&2
        exit 1
      fi
      ONLY_ALIAS="$2"
      shift 2
      ;;
    *)
      echo "Unknown option: $1" >&2
      echo "Usage: $0 [--dry-run] [--only <alias>]" >&2
      exit 1
      ;;
  esac
done

if [[ -z "${RESEND_API_KEY:-}" ]]; then
  echo "Error: RESEND_API_KEY is not set. Export it before running this script." >&2
  exit 1
fi

check_placeholders() {
  local alias="$1"
  local html_file="$SCRIPT_DIR/html/${alias}.html"
  local text_file="$SCRIPT_DIR/text/${alias}.txt"
  local declared_keys
  declared_keys="$(jq -r --arg a "$alias" '.[] | select(.alias == $a) | .variables[].key' "$MANIFEST")"

  local errors=0

  while IFS= read -r key; do
    if [[ "$key" == *_TEXT ]]; then
      if ! grep -q "{{{${key}}}}" "$text_file" 2>/dev/null; then
        echo "  [check] $alias: declared variable $key missing from text/${alias}.txt" >&2
        errors=$((errors + 1))
      fi
    elif [[ "$key" == *_HTML ]]; then
      if ! grep -q "{{{${key}}}}" "$html_file" 2>/dev/null; then
        echo "  [check] $alias: declared variable $key missing from html/${alias}.html" >&2
        errors=$((errors + 1))
      fi
    else
      if ! grep -q "{{{${key}}}}" "$html_file" 2>/dev/null; then
        echo "  [check] $alias: declared variable $key missing from html/${alias}.html" >&2
        errors=$((errors + 1))
      fi
      if ! grep -q "{{{${key}}}}" "$text_file" 2>/dev/null; then
        echo "  [check] $alias: declared variable $key missing from text/${alias}.txt" >&2
        errors=$((errors + 1))
      fi
    fi
  done <<< "$declared_keys"

  while IFS= read -r placeholder; do
    if ! echo "$declared_keys" | grep -qx "$placeholder"; then
      echo "  [check] $alias: placeholder $placeholder in html/${alias}.html is not declared in manifest" >&2
      errors=$((errors + 1))
    fi
  done < <(grep -oE '\{\{\{[A-Z_]+\}\}\}' "$html_file" 2>/dev/null | tr -d '{}' | sort -u || true)

  while IFS= read -r placeholder; do
    if ! echo "$declared_keys" | grep -qx "$placeholder"; then
      echo "  [check] $alias: placeholder $placeholder in text/${alias}.txt is not declared in manifest" >&2
      errors=$((errors + 1))
    fi
  done < <(grep -oE '\{\{\{[A-Z_]+\}\}\}' "$text_file" 2>/dev/null | tr -d '{}' | sort -u || true)

  return "$errors"
}

api_call() {
  local method="$1"
  local path="$2"
  local body="${3:-}"

  local curl_args=(-s -w "\n%{http_code}" -X "$method" "${RESEND_BASE_URL}${path}" \
    -H "Authorization: Bearer ${RESEND_API_KEY}" \
    -H "Content-Type: application/json" \
    -H "Accept: application/json")

  if [[ -n "$body" ]]; then
    curl_args+=(-d "$body")
  fi

  local response
  response="$(curl "${curl_args[@]}")"
  local http_code
  http_code="$(echo "$response" | tail -1)"
  local body_out
  body_out="$(echo "$response" | sed '$d')"

  if [[ "$http_code" -lt 200 || "$http_code" -ge 300 ]]; then
    echo "Error: Resend $method $path failed: $http_code" >&2
    echo "$body_out" >&2
    exit 1
  fi

  echo "$body_out"
}

get_template_id_by_alias() {
  local alias="$1"
  local list_response
  list_response="$(api_call GET "/templates")"
  echo "$list_response" | jq -r --arg a "$alias" '.data[] | select(.alias == $a) | .id' | head -1
}

provision_template() {
  local alias="$1"
  local entry
  entry="$(jq -r --arg a "$alias" '.[] | select(.alias == $a)' "$MANIFEST")"

  if [[ -z "$entry" ]]; then
    echo "Error: alias '$alias' not found in manifest." >&2
    exit 1
  fi

  local name subject
  name="$(echo "$entry" | jq -r '.name')"
  subject="$(echo "$entry" | jq -r '.subject')"
  local variables
  variables="$(echo "$entry" | jq -c '.variables')"

  local html_file="$SCRIPT_DIR/html/${alias}.html"
  local text_file="$SCRIPT_DIR/text/${alias}.txt"

  if [[ ! -f "$html_file" ]]; then
    echo "Error: html/${alias}.html not found." >&2
    exit 1
  fi
  if [[ ! -f "$text_file" ]]; then
    echo "Error: text/${alias}.txt not found." >&2
    exit 1
  fi

  local body
  body="$(jq -n \
    --arg name "$name" \
    --arg alias "$alias" \
    --arg subject "$subject" \
    --rawfile html "$html_file" \
    --rawfile text "$text_file" \
    --argjson variables "$variables" \
    '{name: $name, alias: $alias, subject: $subject, html: $html, text: $text, variables: $variables}')"

  if [[ "$DRY_RUN" == "true" ]]; then
    echo "--- DRY RUN: $alias ---"
    echo "Body: $(echo "$body" | jq '{name,alias,subject,variables} + {html: (.html | .[0:80] + "..."), text: (.text | .[0:80] + "...")}')"
    echo ""
    return
  fi

  local existing_id
  existing_id="$(get_template_id_by_alias "$alias")"

  local action
  if [[ -z "$existing_id" ]]; then
    api_call POST "/templates" "$body" > /dev/null
    existing_id="$(get_template_id_by_alias "$alias")"
    action="created"
  else
    api_call PATCH "/templates/${existing_id}" "$body" > /dev/null
    action="updated"
  fi

  api_call POST "/templates/${existing_id}/publish" > /dev/null

  echo "$alias: $action, published"
}

check_failed=0
aliases=()

if [[ -n "$ONLY_ALIAS" ]]; then
  aliases=("$ONLY_ALIAS")
else
  while IFS= read -r a; do
    aliases+=("$a")
  done < <(jq -r '.[].alias' "$MANIFEST")
fi

echo "Checking placeholder consistency..."
for alias in "${aliases[@]}"; do
  if ! check_placeholders "$alias"; then
    check_failed=1
  fi
done

if [[ $check_failed -ne 0 ]]; then
  echo "Placeholder check failed. Fix the issues above before provisioning." >&2
  exit 1
fi
echo "Placeholder check passed."
echo ""

for alias in "${aliases[@]}"; do
  provision_template "$alias"
done
