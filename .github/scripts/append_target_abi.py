#!/usr/bin/env python3
"""Append targetAbi to the latest version entry in manifest.json."""
import json
import os
import re
import sys

def get_target_abi(csproj_path):
    """Read the Jellyfin.Controller package version and return targetAbi."""
    with open(csproj_path) as f:
        content = f.read()
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

def main():
    manifest_path = os.path.join(os.getcwd(), "manifest.json")
    csproj_path = os.path.join(
        os.getcwd(), "Jellyfin.Plugin.Geckyzz", "Jellyfin.Plugin.Geckyzz.csproj"
    )
    target_abi = get_target_abi(csproj_path)

    with open(manifest_path) as f:
        manifest = json.load(f)

    plugin = manifest[0]
    versions = plugin.get("versions", [])
    if not versions:
        versions.append({"version": plugin.get("version", "0.0.0.0")})
        plugin["versions"] = versions

    latest = versions[0]
    latest["targetAbi"] = target_abi

    with open(manifest_path, "w") as f:
        json.dump(manifest, f, indent=2)
        f.write("\n")

    print(f"Appended targetAbi to manifest: {target_abi}")

if __name__ == "__main__":
    main()
