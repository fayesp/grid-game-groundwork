#!/usr/bin/env bash
# check-unity-script-refs.sh — debug utility
#
# Verifies that every m_Script GUID referenced by .unity/.prefab files
# resolves to a GUID declared by a .meta file in the project.
#
# Exit 0 = all references resolve (green).
# Exit 1 = one or more broken ("Missing Script") references (red).
#
# Usage: bash tools/check-unity-script-refs.sh
set -u
cd "$(dirname "$0")/.."

# GUIDs of Unity built-in package scripts (com.unity.ugui) that legitimately
# have no .meta inside the project — resolved from the editor installation.
# 4f231c4f… = StandaloneInputModule   76c392e4… = EventSystem
# dc42784c… = GraphicRaycaster        0cd44c10… = CanvasScaler
# 5f7201a1… = Text (Legacy UI)
KNOWN_PACKAGE_GUIDS="4f231c4fb786f3946a6b90b886c48677 76c392e42b5098c458856cdf6ecaaaa1 dc42784cf147c0c48a680349fa168899 0cd44c1031e13a943bb63640046fad76 5f7201a12d95ffc409449d95f23cf332"

guid_index=$(mktemp)
trap 'rm -f "$guid_index"' EXIT

# Index every GUID declared by project .meta files.
grep -rhoE '^guid: [0-9a-f]{32}' Assets --include='*.meta' \
  | awk '{print $2}' | sort -u > "$guid_index"

broken=0
while IFS=: read -r file lineno rest; do
  guid=$(echo "$rest" | grep -oE 'guid: [0-9a-f]{32}' | awk '{print $2}')
  case " $KNOWN_PACKAGE_GUIDS " in
    *" $guid "*) continue ;;
  esac
  if ! grep -qx "$guid" "$guid_index"; then
    echo "BROKEN  $file:$lineno  guid=$guid"
    broken=1
  fi
done < <(grep -rnE 'm_Script: \{fileID: 11500000, guid: [0-9a-f]{32}' Assets \
           --include='*.unity' --include='*.prefab')

if [ "$broken" -eq 0 ]; then
  echo "OK: all script references resolve."
fi
exit "$broken"
