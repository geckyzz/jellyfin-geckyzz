#!/usr/bin/env python3
"""Update manifest.json with a new plugin version entry."""

import json
import os

tag = os.environ["TAG"]
version = os.environ["VERSION"]
download_url = os.environ["DOWNLOAD_URL"]
manifest_path = os.environ.get("MANIFEST_PATH", "manifest.json")

with open(manifest_path) as f:
    manifest = json.load(f)

plugin = manifest[0]
plugin["versions"] = [v for v in plugin["versions"] if v.get("version") != version]
plugin["versions"].append(
    {
        "version": version,
        "downloadUrl": download_url,
    }
)
plugin["versions"].sort(
    key=lambda v: [int(x) for x in v["version"].split(".")],
    reverse=True,
)

with open(manifest_path, "w") as f:
    json.dump(manifest, f, indent=2)
    f.write("\n")

print(f"Updated {manifest_path} with version {version}")
