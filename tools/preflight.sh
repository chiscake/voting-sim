#!/usr/bin/env bash
# Preflight: fast repository checks that run without Unity.
#
# Usage:
#   tools/preflight.sh            # check what is staged (used by .githooks/pre-commit)
#   tools/preflight.sh --all      # check every tracked file (for CI or a manual run)
#
# Checks:
#   1. Every asset under Assets/ has its .meta, and every .meta has its asset.
#   2. Every scene listed in ProjectSettings/EditorBuildSettings.asset is in the repo.
#   3. ProjectSettings/ProjectVersion.txt matches EXPECTED_UNITY_VERSION.
#   4. No Unity cache or build folders are committed (Library/, Temp/, Logs/, ...).
#   5. Text files are valid UTF-8.
#   6. No leftover merge conflict markers.
#   7. Files larger than MAX_FILE_MB go through Git LFS.
#
# Checks 5-7 look only at added/modified files in staged mode, so old
# files do not block unrelated commits; --all checks everything.

set -u

EXPECTED_UNITY_VERSION="6000.6.6f1"
MAX_FILE_MB=50

MODE="staged"
[ "${1:-}" = "--all" ] && MODE="all"

cd "$(git rev-parse --show-toplevel)" || exit 2

errors=0
fail() { printf '  \033[31m✗\033[0m %s\n' "$1"; errors=$((errors + 1)); }
warn() { printf '  \033[33m!\033[0m %s\n' "$1"; }
section() { printf '\033[1m%s\033[0m\n' "$1"; }

# Read a file as it is in the index (what will be committed).
staged_cat() { git show ":$1" 2>/dev/null; }

# All tracked paths as they will be after this commit.
INDEX_FILES="$(git ls-files)"

if [ "$MODE" = "all" ]; then
    CHANGED_FILES="$INDEX_FILES"
else
    CHANGED_FILES="$(git diff --cached --name-only --diff-filter=ACMR)"
fi

# ---------------------------------------------------------------- 1. .meta
section "1. .meta files"
meta_errors=$(
    printf '%s\n' "$INDEX_FILES" | awk '
        /^Assets\// {
            # Unity ignores hidden files (.gitkeep) and names ending with ~,
            # but the folders holding them still need a .meta.
            hidden = ($0 ~ /\/\./ || $0 ~ /~(\/|$)/)
            if (!hidden && $0 ~ /\.meta$/) { meta[substr($0, 1, length($0) - 5)] = 1; next }
            if (!hidden) asset[$0] = 1
            p = $0
            while (match(p, /\/[^\/]*$/)) {
                p = substr(p, 1, RSTART - 1)
                if (p == "Assets") break
                if (p !~ /\/\./ && p !~ /~$/) asset[p] = 1
            }
        }
        END {
            for (a in asset) if (!(a in meta)) print "missing .meta: " a ".meta"
            for (m in meta) if (!(m in asset)) print "orphan\t" m ".meta"
        }' | sort
)
if [ -n "$meta_errors" ]; then
    while IFS= read -r line; do
        case "$line" in
            orphan*)
                m="${line#orphan	}"
                # Git cannot store empty folders, so a folder .meta without files is only a warning.
                if staged_cat "$m" | grep -q '^folderAsset: yes'; then
                    warn "empty folder, .meta has nothing to describe: $m (delete it or add files to the folder)"
                else
                    fail "orphan .meta (asset not in repo): $m"
                fi ;;
            *) fail "$line" ;;
        esac
    done <<< "$meta_errors"
fi

# ---------------------------------------------------- 2. Build Settings scenes
section "2. Build Settings scenes"
BUILD_SETTINGS="ProjectSettings/EditorBuildSettings.asset"
# In staged mode only when Build Settings or a scene is part of the commit,
# so a scene someone else forgot to push does not block unrelated commits.
check_scenes=1
if [ "$MODE" = "staged" ] && ! git diff --cached --name-only | grep -qE '(^ProjectSettings/EditorBuildSettings\.asset|\.unity)$'; then
    check_scenes=0
fi
if [ "$check_scenes" = 1 ] && printf '%s\n' "$INDEX_FILES" | grep -qxF "$BUILD_SETTINGS"; then
    while IFS= read -r scene; do
        [ -z "$scene" ] && continue
        printf '%s\n' "$INDEX_FILES" | grep -qxF "$scene" \
            || fail "scene in Build Settings but not in repo: $scene"
    done <<< "$(staged_cat "$BUILD_SETTINGS" | sed -n 's/^ *path: *//p' | tr -d '\r')"
fi

# ------------------------------------------------------------ 3. Unity version
section "3. Unity version"
version="$(staged_cat ProjectSettings/ProjectVersion.txt | sed -n 's/^m_EditorVersion: *//p' | tr -d '\r')"
if [ "$version" != "$EXPECTED_UNITY_VERSION" ]; then
    fail "ProjectVersion.txt says '$version', expected '$EXPECTED_UNITY_VERSION' (change EXPECTED_UNITY_VERSION in tools/preflight.sh if the upgrade is intended)"
fi

# --------------------------------------------------- 4. Forbidden folders
section "4. Unity cache and build folders"
bad_paths="$(printf '%s\n' "$INDEX_FILES" \
    | grep -E '^(Library|Temp|Logs|UserSettings|obj|Build|Builds|MemoryCaptures)/' | head -20)"
if [ -n "$bad_paths" ]; then
    while IFS= read -r p; do fail "should not be committed: $p"; done <<< "$bad_paths"
fi

# Text-like files for checks 5 and 6.
TEXT_RE='\.(cs|shader|cginc|hlsl|compute|md|txt|json|asmdef|asmref|inputactions|uss|uxml|xml|yml|yaml|sh|editorconfig|gitattributes|gitignore|unity|prefab|asset|mat|meta|controller|anim|overrideController|physicMaterial|mixer|lighting)$'

# ------------------------------------------------------------- 5. UTF-8
section "5. UTF-8 encoding"
while IFS= read -r f; do
    [ -z "$f" ] && continue
    printf '%s' "$f" | grep -qE "$TEXT_RE" || continue
    if ! staged_cat "$f" | iconv -f UTF-8 -t UTF-8 >/dev/null 2>&1; then
        fail "not UTF-8 (re-save as UTF-8): $f"
    fi
done <<< "$CHANGED_FILES"

# -------------------------------------------------- 6. Conflict markers
section "6. Merge conflict markers"
while IFS= read -r f; do
    [ -z "$f" ] && continue
    printf '%s' "$f" | grep -qE "$TEXT_RE" || continue
    if staged_cat "$f" | grep -qE '^(<<<<<<< |>>>>>>> |=======$)'; then
        fail "conflict markers left in: $f"
    fi
done <<< "$CHANGED_FILES"

# ------------------------------------------------------- 7. Large files
section "7. Large files outside Git LFS"
max_bytes=$((MAX_FILE_MB * 1024 * 1024))
while IFS= read -r f; do
    [ -z "$f" ] && continue
    [ "$(git check-attr filter -- "$f" | sed 's/.*: //')" = "lfs" ] && continue
    size="$(git cat-file -s ":$f" 2>/dev/null || echo 0)"
    if [ "$size" -gt "$max_bytes" ]; then
        fail "$f is $((size / 1024 / 1024)) MB and not in Git LFS (add its extension to .gitattributes)"
    fi
done <<< "$CHANGED_FILES"

echo
if [ "$errors" -gt 0 ]; then
    printf '\033[31mPreflight failed: %d problem(s).\033[0m\n' "$errors"
    [ "$MODE" = "staged" ] && echo "Fix them and commit again (git commit --no-verify skips the check in an emergency)."
    exit 1
fi
printf '\033[32mPreflight passed.\033[0m\n'
