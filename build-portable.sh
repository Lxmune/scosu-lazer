#!/usr/bin/env bash
# Builds a portable (self-contained, no installer) win-x64 zip of scosu from the working tree.
#
#   ./build-portable.sh [VERSION]      default version: newest tag
#
# Set SCOSU_SCORE_SIGNING_KEY to build the score signing key in, as the release workflow does; without it the build sends unsigned scores.
# The zip lands in ../.lazer-build/scosu-VERSION-win-x64-portable.zip.

set -euo pipefail
source "$(dirname "${BASH_SOURCE[0]}")/build-env.sh"

version=${1:-$(git -C "$repo" describe --tags --abbrev=0)}
# File versions can't have a suffix.
file_version=${version%%-*}
out=$work/publish-win-x64
zip_path=$work/scosu-$version-win-x64-portable.zip

if [[ -z ${SCOSU_SCORE_SIGNING_KEY:-} ]]; then
    echo "warning: SCOSU_SCORE_SIGNING_KEY is not set, this build won't sign score submissions" >&2
fi

sync_sources
rm -rf "$out" "$zip_path"

echo "==> Publishing $version for win-x64"
cd "$work/src"
"$dotnet" publish osu.Desktop -c Release -r win-x64 --self-contained \
    -p:Version="$version" -p:FileVersion="$file_version" -o "$out" "${dotnet_flags[@]}"

echo "==> Zipping"
(cd "$out" && zip -qr "$zip_path" .)
echo "Built $zip_path"
