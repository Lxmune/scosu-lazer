# Sourced by build-portable.sh and the Makefile: sets up the local Linux .NET SDK in ../.lazer-build
# and defines sync_sources, which copies the working tree (uncommitted changes included) there.
#
# Builds run from ../.lazer-build/src rather than this tree, so bin/ and obj/ never land in the repo.

repo=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)
work=$(cd "$repo/.." && pwd)/.lazer-build

export DOTNET_ROOT=$work/dotnet
export DOTNET_CLI_HOME=$work/dotnet-home
export NUGET_PACKAGES=$work/nuget
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export MSBUILDDISABLENODEREUSE=1
if [[ ! -w ${XDG_RUNTIME_DIR:-/nonexistent} ]]; then
    export XDG_RUNTIME_DIR=${TMPDIR:-/tmp}/xdg
    mkdir -p "$XDG_RUNTIME_DIR"
fi

dotnet=$DOTNET_ROOT/dotnet
# One build node, no compiler server: worker nodes fail silently here with "Build FAILED, 0 errors".
dotnet_flags=(-m:1 -p:UseSharedCompilation=false)

if [[ ! -x $dotnet ]]; then
    echo "No .NET SDK in $DOTNET_ROOT: install it with" >&2
    echo "  $work/dotnet-install/dotnet-install.sh --channel 10.0 --install-dir $DOTNET_ROOT" >&2
    exit 1
fi

sync_sources() {
    echo "==> Copying sources to $work/src"
    mkdir -p "$work/src"
    # Drop the old sources (files deleted here would linger) but keep bin/ and obj/, so the build stays incremental.
    find "$work/src" -type f -not -path '*/bin/*' -not -path '*/obj/*' -delete
    # --ignore-failed-read: tracked files deleted but not committed yet are still listed.
    (cd "$repo" && git ls-files -co --exclude-standard -z | tar --ignore-failed-read --null -T - -cf - 2>/dev/null) | tar -xf - -C "$work/src"
}
