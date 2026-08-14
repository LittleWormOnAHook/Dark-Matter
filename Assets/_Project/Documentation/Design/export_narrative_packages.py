#!/usr/bin/env python3
"""Export narrative package markdown to styled HTML and PDF (Edge headless)."""

from __future__ import annotations

import subprocess
import sys
import time
from pathlib import Path

import markdown

DESIGN_DIR = Path(__file__).resolve().parent
EXPORTS_DIR = DESIGN_DIR / "Exports"

CSS = """
body{font-family:Segoe UI,Helvetica,Arial,sans-serif;max-width:960px;margin:40px auto;padding:0 24px;line-height:1.45;color:#1a1a1a;font-size:11pt}
h1{font-size:1.8rem;border-bottom:2px solid #8F1E5E;padding-bottom:8px}
h2{font-size:1.35rem;margin-top:1.6em;color:#1C2A38}
h3{font-size:1.1rem;color:#2F2F2F}
table{border-collapse:collapse;width:100%;font-size:8.5pt;margin:12px 0;page-break-inside:auto}
tr{page-break-inside:avoid}
th,td{border:1px solid #4A4A5A;padding:5px 7px;vertical-align:top}
th{background:#2F2F2F;color:#EDE9E4}
code,pre{font-family:Consolas,monospace;font-size:9pt}
pre{background:#f4f4f4;padding:10px;overflow:auto}
blockquote{border-left:4px solid #C02E7A;margin-left:0;padding-left:12px;color:#4A4A5A}
hr{border:none;border-top:1px solid #8C7F75;margin:2em 0}
ul,ol{padding-left:1.3em}
@media print{body{margin:12px;max-width:none}}
"""

EDGE_PATHS = [
    Path(r"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe"),
    Path(r"C:\Program Files\Microsoft\Edge\Application\msedge.exe"),
]


def find_edge() -> Path | None:
    for path in EDGE_PATHS:
        if path.exists():
            return path
    return None


def md_to_html(md_path: Path) -> str:
    text = md_path.read_text(encoding="utf-8")
    body = markdown.markdown(
        text,
        extensions=["tables", "fenced_code", "sane_lists", "nl2br"],
    )
    title = md_path.stem.replace("_", " ")
    return (
        f"<!DOCTYPE html><html><head><meta charset=\"utf-8\">"
        f"<title>{title}</title><style>{CSS}</style></head><body>{body}</body></html>"
    )


def html_to_pdf(html_path: Path, pdf_path: Path) -> None:
    edge = find_edge()
    if edge is None:
        raise RuntimeError("Microsoft Edge not found for headless PDF export.")
    pdf_path.parent.mkdir(parents=True, exist_ok=True)
    html_uri = html_path.resolve().as_uri()
    pdf_target = str(pdf_path.resolve())
    cmd = [
        str(edge),
        "--headless",
        "--disable-gpu",
        "--no-pdf-header-footer",
        f"--print-to-pdf={pdf_target}",
        html_uri,
    ]
    result = subprocess.run(cmd, capture_output=True, text=True, timeout=120)
    for _ in range(20):
        if pdf_path.exists() and pdf_path.stat().st_size > 0:
            break
        time.sleep(0.5)
    if result.returncode != 0 or not pdf_path.exists():
        raise RuntimeError(
            f"PDF export failed for {html_path.name}: {result.stderr or result.stdout}"
        )


def export_file(stem: str) -> tuple[Path, Path]:
    md_path = DESIGN_DIR / f"{stem}.md"
    if not md_path.exists():
        raise FileNotFoundError(md_path)
    html_path = EXPORTS_DIR / f"{stem}.html"
    pdf_path = EXPORTS_DIR / f"{stem}.pdf"
    html_path.write_text(md_to_html(md_path), encoding="utf-8")
    html_to_pdf(html_path, pdf_path)
    return html_path, pdf_path


def main(argv: list[str]) -> int:
    stems = argv[1:] if len(argv) > 1 else [
        "Narrative_Package_V1_Ash_And_Signal",
        "Narrative_Package_V2_Colony_Horizon",
        "Narrative_Package_V3_Fracture_Compact",
        "Narrative_Package_V4_Crimson_Contract",
        "Narrative_Package_Compare_And_Pick",
    ]
    for stem in stems:
        html_path, pdf_path = export_file(stem)
        print(f"Exported {html_path.name} + {pdf_path.name}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv))
