#!/bin/bash
# Renders the README captures (needs a GPU and the Editor closed).
#   Tools/readme-media/capture.sh <output-dir> [Menu|Level1AndEcho|Rewind|Level3]
# UNITY overrides the Editor path. Candidates are written to <output-dir>; review them before copying to docs/img/.
set -eu
root="$(cd "$(dirname "$0")/../.." && pwd)"
out="$(mkdir -p "$1" && cd "$1" && pwd)"
unity="${UNITY:-$HOME/Unity/Hub/Editor/6000.3.29f1/Editor/Unity}"
harness="$root/Assets/_Echoryx/Tests/PlayMode/Level/ZzReadmeCaptures.cs"
trap 'rm -f "$harness" "$harness.meta"' EXIT
cp "$root/Tools/readme-media/ReadmeCaptures.cs" "$harness"
CAPTURE_DIR="$out" "$unity" -batchmode -force-glcore -projectPath "$root" -runTests -testPlatform PlayMode \
  -testFilter "Echoryx.Level.PlayTests.ZzReadmeCaptures${2:+.$2}" \
  -testResults "$out/results.xml" -logFile "$out/unity.log"
