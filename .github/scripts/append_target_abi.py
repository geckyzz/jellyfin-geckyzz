#!/usr/bin/env python3
"""Append targetAbi to the latest version entry in manifest.json."""
from pathlib import Path
import json
import re
import sys


def get_target_abi(csproj_path: Path) -> str:
    """Read the Jellyfin.Controller package version and return targetAbi."""
    content = csproj_path.read_text(encoding="utf-8")
    match = re.search(
        r'<PackageReference Include="Jellyfin\.Controller" Version="([^"]+)"', content
    )
    if not match:
        sys.exit("Error: Could not find Jellyfin.Controller version in csproj")
    version = match.group(1)
    # Ensure four parts (Major.Minor.Patch.Revision); pad with zeros if needed.
    parts = version.split(".")
    while len(parts) < 4:
        parts.append("0")
    return ".".join(parts[:4])


def main() -> None:
    repo_root = Path(__file__).resolve().parents[2]
    manifest_path = repo_root / "manifest.json"
    csproj_path = repo_root / "Jellyfin.Plugin.Geckyzz" / "Jellyfin.Plugin.Geckyzz.csproj"

    target_abi = get_target_abi(csproj_path)

    manifest = json.loads(manifest_path.read_text(encoding="utf-8"))

    plugin = manifest[0]
    versions = plugin.get("versions", [])
    if not versions:
        versions.append({"version": plugin.get("version", "0.0.0.0")})
        plugin["versions"] = versions

    latest = versions[0]
    if latest.get("targetAbi"):
        print(f"targetAbi already set for version {latest.get('version')}: {latest['targetAbi']}")
        return

    latest["targetAbi"] = target_abi
    manifest_path.write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
    print(f"Appended targetAbi to manifest: {target_abi}")


if __name__ == "__main__":
    main()
