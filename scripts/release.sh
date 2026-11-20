#!/usr/bin/env bash
# Builds the mod and publishes it, either as a GitHub release or to the Steam Workshop.
# The build needs the game's sts2.dll and MegaDot, so this runs locally, not in CI.
# Two channels ship side by side. A manifest version like v1.2.3 is the stable channel, released from main to the
# dist/workshop item. A version like v1.2.3-beta.1 is the beta channel for the game's public-beta branch, released from
# the beta branch to the dist/workshop-beta item as a GitHub prerelease.
# Usage: scripts/release.sh [--mode github|workshop] [--dry-run] [--yes]
#   --mode     github (default): zip, tag the manifest version, and publish a GitHub release
#              workshop: refresh the channel's workshop content and upload it with dist/mod-uploader/ModUploader
#                        (workshop.json, image.png, and previews in the workspace are edited by hand, except
#                        changeNote, which is set from the changelog section)
#   --dry-run  build and package only; no tag, push, release, or upload
#   --yes      skip the confirmation prompt
set -euo pipefail

cd "$(dirname "$0")/.."
ROOT=$(pwd)

MOD=sts2_wayfinder_mod
MODE=github
DRY_RUN=0
ASSUME_YES=0
while [[ $# -gt 0 ]]; do
    case "$1" in
        --mode) [[ $# -ge 2 ]] || { echo "--mode needs a value" >&2; exit 2; }; MODE=$2; shift ;;
        --mode=*) MODE=${1#--mode=} ;;
        --dry-run) DRY_RUN=1 ;;
        --yes|-y) ASSUME_YES=1 ;;
        *) echo "Unknown option: $1" >&2; exit 2 ;;
    esac
    shift
done
[[ "$MODE" == "github" || "$MODE" == "workshop" ]] || { echo "Unknown mode: $MODE (use github or workshop)" >&2; exit 2; }

die() { echo "error: $*" >&2; exit 1; }

TOOLS=(git dotnet python3)
[[ "$MODE" == "github" ]] && TOOLS+=(gh)
for tool in "${TOOLS[@]}"; do
    command -v "$tool" >/dev/null || die "$tool is not installed"
done

VERSION=$(python3 -I -c 'import json,sys; print(json.load(open(sys.argv[1], encoding="utf-8-sig"))["version"])' "$MOD.json")
if [[ "$VERSION" =~ ^v[0-9]+\.[0-9]+\.[0-9]+-beta\.[0-9]+$ ]]; then
    CHANNEL=beta BRANCH=beta WORKSPACE="$ROOT/dist/workshop-beta"
elif [[ "$VERSION" =~ ^v[0-9]+\.[0-9]+\.[0-9]+$ ]]; then
    CHANNEL=stable BRANCH=main WORKSPACE="$ROOT/dist/workshop"
else
    die "manifest version '$VERSION' is not like v1.2.3 or v1.2.3-beta.1"
fi

UPLOADER_DIR="$ROOT/dist/mod-uploader"
if [[ "$MODE" == "workshop" && $DRY_RUN -eq 0 ]]; then
    [[ -x "$UPLOADER_DIR/ModUploader" ]] || die "$UPLOADER_DIR/ModUploader not found or not executable"
fi

if [[ $DRY_RUN -eq 0 ]]; then
    [[ "$(git branch --show-current)" == "$BRANCH" ]] || die "release $CHANNEL versions from $BRANCH"
    [[ -z "$(git status --porcelain)" ]] || die "working tree is not clean"
    git fetch --quiet origin "$BRANCH" --tags
    [[ "$(git rev-parse HEAD)" == "$(git rev-parse "origin/$BRANCH")" ]] || die "$BRANCH is not in sync with origin/$BRANCH"
    if [[ "$MODE" == "github" ]]; then
        git rev-parse -q --verify "refs/tags/$VERSION" >/dev/null && die "tag $VERSION already exists"
        gh auth status >/dev/null 2>&1 || die "gh is not logged in"
    fi
fi

NOTES=$(awk -v h="## $VERSION" '
    $0 == h { on = 1; next }
    on && /^## / { exit }
    on { print }
' docs/changelog.md | sed -e :a -e '/^\n*$/{$d;N;ba' -e '}')
[[ -n "$NOTES" ]] || die "docs/changelog.md has no '## $VERSION' section"

echo "Releasing $VERSION ($CHANNEL, $MODE)"
echo "$NOTES"
echo

if [[ $DRY_RUN -eq 0 && $ASSUME_YES -eq 0 ]]; then
    if [[ "$MODE" == "github" ]]; then
        read -r -p "Tag, push, and publish $VERSION? [y/N] " reply
    else
        read -r -p "Upload $VERSION to the Steam Workshop? [y/N] " reply
    fi
    [[ "$reply" =~ ^[Yy]$ ]] || die "aborted"
fi

# The build compiles against the installed game, so the installed Steam branch has to match the channel.
STS2_PATH=$(dotnet msbuild "$MOD.csproj" -getProperty:Sts2Path)
ACF="$STS2_PATH/../../appmanifest_2868840.acf"
if [[ -f "$ACF" ]]; then
    INSTALLED_BRANCH=$(awk '/"MountedConfig"/ { on = 1 } on && /"BetaKey"/ { gsub(/"/, "", $2); print $2; exit }' "$ACF")
    # Steam records the default branch as "public", or leaves BetaKey out.
    [[ "${INSTALLED_BRANCH:-public}" == "public" ]] && INSTALLED_BRANCH=public
    [[ "$CHANNEL" == "beta" ]] && WANT_BRANCH=public-beta || WANT_BRANCH=public
    [[ "$INSTALLED_BRANCH" == "$WANT_BRANCH" ]] \
        || die "the game is installed from branch '$INSTALLED_BRANCH', but $CHANNEL builds need '$WANT_BRANCH'. Switch branches in Steam first."
else
    echo "warning: $ACF not found, so the installed game branch can't be checked" >&2
fi

echo "== Tests"
dotnet test tests/OrderingTests

echo "== Build"
dotnet publish "$MOD.csproj" -c Release

MODS_PATH=$(dotnet msbuild "$MOD.csproj" -getProperty:ModsPath)
OUT="${MODS_PATH%/}/$MOD"
for ext in dll json pck; do
    f="$OUT/$MOD.$ext"
    [[ -f "$f" ]] || die "missing build output $f"
done

if [[ "$MODE" == "github" ]]; then
    echo "== Zip"
    ZIP="dist/$MOD-$VERSION.zip"
    mkdir -p dist
    rm -f "$ZIP"
    python3 -I - "$OUT" "$MOD" "$ZIP" <<'PY'
import sys, zipfile
out, mod, zip_path = sys.argv[1:4]
docs = (("LICENSE", "LICENSE"), ("README.md", "README.md"), ("docs/changelog.md", "CHANGELOG.md"))
with zipfile.ZipFile(zip_path, "w", zipfile.ZIP_DEFLATED) as z:
    for ext in ("dll", "json", "pck"):
        z.write(f"{out}/{mod}.{ext}", f"{mod}/{mod}.{ext}")
    for src, name in docs:
        z.write(src, f"{mod}/{name}")
PY
    python3 -I -m zipfile -l "$ZIP"

    if [[ $DRY_RUN -eq 1 ]]; then
        echo "Dry run done: $ZIP"
        exit 0
    fi

    echo "== Publish"
    git tag -a "$VERSION" -m "$VERSION"
    git push origin "$VERSION"
    PRERELEASE=()
    [[ "$CHANNEL" == "beta" ]] && PRERELEASE=(--prerelease --latest=false)
    gh release create "$VERSION" "$ZIP" --title "$VERSION" --notes "$NOTES" --verify-tag "${PRERELEASE[@]}"
    echo "Released $VERSION"
    exit 0
fi

echo "== Stage workshop content"
[[ -f "$WORKSPACE/workshop.json" ]] || die "no workshop workspace at $WORKSPACE; create one with: (cd $UPLOADER_DIR && ./ModUploader new -w $WORKSPACE)"
CONTENT="$WORKSPACE/content/$MOD"
mkdir -p "$WORKSPACE/content"
rm -rf "$CONTENT"
mkdir -p "$CONTENT"
for ext in dll json pck; do
    cp "$OUT/$MOD.$ext" "$CONTENT/"
done
cp LICENSE README.md "$CONTENT/"
cp docs/changelog.md "$CONTENT/CHANGELOG.md"
python3 -I - "$WORKSPACE/workshop.json" "$VERSION" "$NOTES" <<'PY'
import json, sys
path, version, notes = sys.argv[1:4]
with open(path, encoding="utf-8") as f:
    workshop = json.load(f)
workshop["changeNote"] = f"{version}\n\n{notes}"
with open(path, "w", encoding="utf-8") as f:
    json.dump(workshop, f, indent=4, ensure_ascii=False)
PY
echo "Staged $CONTENT"
if [[ -f "$WORKSPACE/mod_id.txt" ]]; then
    echo "Updating Workshop item $(<"$WORKSPACE/mod_id.txt")"
else
    echo "No mod_id.txt yet: this upload creates a new Workshop item. Back up $WORKSPACE/mod_id.txt afterward."
fi

if [[ $DRY_RUN -eq 1 ]]; then
    echo "Dry run done: $WORKSPACE"
    exit 0
fi

echo "== Upload"
# Run from the uploader's folder so it finds libsteam_api.so and steam_appid.txt. Steam must be running.
(cd "$UPLOADER_DIR" && ./ModUploader upload -w "$WORKSPACE")
echo "Uploaded $VERSION to the Steam Workshop"
