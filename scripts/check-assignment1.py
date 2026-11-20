"""Read-only content fingerprint of the complete reference folder; writes only in Assignment 2."""
import hashlib
import json
from pathlib import Path
import sys

workspace = Path(__file__).resolve().parents[1]
reference = workspace.parent / "MEMS-Assignment-1"
manifest = workspace / ".local" / "assignment1-baseline.json"

def fingerprint():
    result = {}
    for path in sorted(reference.rglob("*")):
        if path.is_file():
            digest = hashlib.sha256()
            with path.open("rb") as source:
                for block in iter(lambda: source.read(1024 * 1024), b""):
                    digest.update(block)
            stat = path.stat()
            result[str(path.relative_to(reference))] = [stat.st_size, stat.st_mtime_ns, digest.hexdigest()]
    return result

current = fingerprint()
if sys.argv[1:] == ["baseline"]:
    if manifest.exists():
        raise SystemExit("Baseline already exists; refusing to overwrite.")
    manifest.parent.mkdir(exist_ok=True)
    manifest.write_text(json.dumps(current), encoding="utf-8")
    print(f"Baseline: {len(current)} files, {sum(v[0] for v in current.values())} bytes.")
elif sys.argv[1:] == ["verify"]:
    previous = json.loads(manifest.read_text(encoding="utf-8"))
    changed = [key for key in previous.keys() | current.keys() if previous.get(key) != current.get(key)]
    if changed:
        print(f"FAIL: {len(changed)} added, removed, or changed files.")
        raise SystemExit(1)
    print(f"PASS: all {len(current)} Assignment 1 files match baseline (SHA-256, length, last-write time).")
else:
    raise SystemExit("Use baseline or verify.")
