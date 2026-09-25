#!/usr/bin/env bash
# Compila el proyecto Unity en batchmode y corre la suite EditMode.
# Uso: tools/run-editmode-tests.sh
# Requiere el Editor pineado en Unity/ProjectSettings/ProjectVersion.txt y el proyecto cerrado en el Editor.
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PROJECT_PATH="$REPO_ROOT/Unity"
EDITOR_VERSION="$(awk '/m_EditorVersion:/ {print $2}' "$PROJECT_PATH/ProjectSettings/ProjectVersion.txt")"
OUTPUT_DIR="$PROJECT_PATH/Logs/TestRuns"
RESULTS_FILE="$OUTPUT_DIR/editmode-results.xml"
LOG_FILE="$OUTPUT_DIR/editmode.log"

case "$(uname -s)" in
    Darwin) DEFAULT_EDITOR="/Applications/Unity/Hub/Editor/$EDITOR_VERSION/Unity.app/Contents/MacOS/Unity" ;;
    Linux) DEFAULT_EDITOR="$HOME/Unity/Hub/Editor/$EDITOR_VERSION/Editor/Unity" ;;
    *) DEFAULT_EDITOR="/c/Program Files/Unity/Hub/Editor/$EDITOR_VERSION/Editor/Unity.exe" ;;
esac
UNITY_EDITOR="${UNITY_EDITOR:-$DEFAULT_EDITOR}"

if [[ ! -x "$UNITY_EDITOR" ]]; then
    echo "No se encontró Unity $EDITOR_VERSION en: $UNITY_EDITOR (definí UNITY_EDITOR)." >&2
    exit 2
fi

mkdir -p "$OUTPUT_DIR"
rm -f "$RESULTS_FILE"

set +e
"$UNITY_EDITOR" -batchmode -nographics \
    -projectPath "$PROJECT_PATH" \
    -runTests -testPlatform EditMode \
    -testResults "$RESULTS_FILE" \
    -logFile "$LOG_FILE"
EXIT_CODE=$?
set -e

if grep -q "error CS" "$LOG_FILE"; then
    echo "Errores de compilación:" >&2
    grep "error CS" "$LOG_FILE" | sort -u >&2
fi

if [[ -f "$RESULTS_FILE" ]]; then
    SUMMARY="$(grep -o '<test-run [^>]*>' "$RESULTS_FILE" | head -1)"
    echo "Resultado: $SUMMARY"
else
    echo "No se generó $RESULTS_FILE. Ver log: $LOG_FILE" >&2
fi

exit "$EXIT_CODE"
