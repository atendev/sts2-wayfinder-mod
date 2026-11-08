#!/usr/bin/env bash
# Builds the mod and publishes it, either as a GitHub release or to the Steam Workshop.
# The build needs the game's sts2.dll and MegaDot, so this runs locally, not in CI.
# Usage: scripts/release.sh [--mode github|workshop] [--dry-run] [--yes]
#   --mode     github (default): zip, tag the manifest version, and publish a GitHub release
#              workshop: refresh dist/workshop/content and upload it with dist/mod-uploader/ModUploader
#                        (workshop.json, image.png, and previews in dist/workshop are edited by hand)
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

UPLOADER_DIR="$ROOT/dist/mod-uploader"
WORKSPACE="$ROOT/dist/workshop"
if [[ "$MODE" == "workshop" && $DRY_RUN -eq 0 ]]; then
    [[ -x "$UPLOADER_DIR/ModUploader" ]] || die "$UPLOADER_DIR/ModUploader not found or not executable"
fi

VERSION=$(python3 -I -c 'import json,sys; print(json.load(open(sys.argv[1], encoding="utf-8-sig"))["version"])' "$MOD.json")
[[ "$VERSION" =~ ^v[0-9]+\.[0-9]+\.[0-9]+([-.][0-9A-Za-z.]+)?$ ]] || die "manifest version '$VERSION' is not like v1.2.3"

if [[ $DRY_RUN -eq 0 ]]; then
    [[ "$(git branch --show-current)" == "main" ]] || die "release from main"
    [[ -z "$(git status --porcelain)" ]] || die "working tree is not clean"
    git fetch --quiet origin main --tags
    [[ "$(git rev-parse HEAD)" == "$(git rev-parse origin/main)" ]] || die "main is not in sync with origin/main"
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

echo "Releasing $VERSION ($MODE)"
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
    [[ "$VERSION" == *-* ]] && PRERELEASE=(--prerelease)
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
