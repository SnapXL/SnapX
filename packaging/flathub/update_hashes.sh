#!/usr/bin/env bash

SCRIPT_DIR=$(dirname "$(readlink -f "$0")")
JSON_FILE="$SCRIPT_DIR/sources.json"
NUGET_CACHE="$HOME/.nuget/packages"

if ! command -v jq &> /dev/null; then
    echo "Error: jq is required. Install with 'sudo apt install jq' or 'sudo dnf install jq'"
    exit 1
fi

echo "Updating hashes in $JSON_FILE..."

temp_output=$(mktemp)
echo "[" > "$temp_output"

items=$(jq -c '.[]' "$JSON_FILE")
item_count=$(echo "$items" | wc -l)
current=0

while IFS= read -r row; do
    current=$((current + 1))

    filename=$(echo "$row" | jq -r '."dest-filename"')
    url=$(echo "$row" | jq -r '.url')

    # URL format: .../v3-flatcontainer/asyncimageloader.avalonia/3.5.0/asyncimageloader.avalonia.3.5.0.nupkg
    package_name=$(echo "$url" | awk -F'/' '{print $(NF-2)}')
    version=$(echo "$url" | awk -F'/' '{print $(NF-1)}')
    local_path="$NUGET_CACHE/$package_name/$version/$filename"

    if [[ -f "$local_path" ]]; then
        echo "[$current/$item_count] Found local: $package_name ($version)"
        new_sha=$(sha512sum "$local_path" | cut -d' ' -f1)
    else
        echo "[$current/$item_count] Not in cache. Downloading: $url"
        new_sha=$(curl -sL "$url" | sha512sum | cut -d' ' -f1)
    fi

    updated_row=$(echo "$row" | jq --arg sha "$new_sha" '.sha512 = $sha')

    if [[ "$current" -eq "$item_count" ]]; then
        echo "$updated_row" >> "$temp_output"
    else
        echo "$updated_row," >> "$temp_output"
    fi

done <<< "$items"

echo "]" >> "$temp_output"

jq '.' "$temp_output" > "$JSON_FILE"
rm "$temp_output"

echo "-----------------------------------------------"
echo "Success! sources.json updated with local and remote hashes."
