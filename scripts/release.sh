#!/usr/bin/env bash
# Builds the release zip, tags the version from the manifest, and publishes a GitHub release.
# The build needs the game's sts2.dll and MegaDot, so this runs locally, not in CI.
# Usage: scripts/release.sh [--dry-run] [--yes]
#   --dry-run  build and zip only; no tag, push, or release
#   --yes      skip the confirmation prompt
set -euo pipefail

cd "$(dirname "$0")/.."

MOD=sts2_wayfinder_mod
DRY_RUN=0
ASSUME_YES=0
for arg in "$@"; do
    case "$arg" in
        --dry-run) DRY_RUN=1 ;;
        --yes|-y) ASSUME_YES=1 ;;
        *) echo "Unknown option: $arg" >&2; exit 2 ;;
    esac
done

die() { echo "error: $*" >&2; exit 1; }

for tool in git dotnet gh python3; do
    command -v "$tool" >/dev/null || die "$tool is not installed"
done

VERSION=$(python3 -I -c 'import json,sys; print(json.load(open(sys.argv[1], encoding="utf-8-sig"))["version"])' "$MOD.json")
[[ "$VERSION" =~ ^v[0-9]+\.[0-9]+\.[0-9]+([-.][0-9A-Za-z.]+)?$ ]] || die "manifest version '$VERSION' is not like v1.2.3"

if [[ $DRY_RUN -eq 0 ]]; then
    [[ "$(git branch --show-current)" == "main" ]] || die "release from main"
    [[ -z "$(git status --porcelain)" ]] || die "working tree is not clean"
    git fetch --quiet origin main --tags
    [[ "$(git rev-parse HEAD)" == "$(git rev-parse origin/main)" ]] || die "main is not in sync with origin/main"
    git rev-parse -q --verify "refs/tags/$VERSION" >/dev/null && die "tag $VERSION already exists"
    gh auth status >/dev/null 2>&1 || die "gh is not logged in"
fi

NOTES=$(awk -v h="## $VERSION" '
    $0 == h { on = 1; next }
    on && /^## / { exit }
    on { print }
' docs/changelog.md | sed -e :a -e '/^\n*$/{$d;N;ba' -e '}')
[[ -n "$NOTES" ]] || die "docs/changelog.md has no '## $VERSION' section"

echo "Releasing $VERSION"
echo "$NOTES"
echo

if [[ $DRY_RUN -eq 0 && $ASSUME_YES -eq 0 ]]; then
    read -r -p "Tag, push, and publish $VERSION? [y/N] " reply
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
