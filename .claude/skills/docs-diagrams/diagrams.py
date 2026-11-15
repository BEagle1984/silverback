"""Generates the documentation diagrams (docs/images/diagrams/*.svg).

Usage, from the repository root:
    python .claude/skills/docs-diagrams/diagrams.py [names...] [--out DIR] [--preview FILE]

Without names, every diagram is generated. --preview also writes an HTML page that shows the
generated diagrams on the site's dark and light backgrounds.
"""

import argparse
import os
import pathlib

FONT = 'Inter, "Segoe UI", system-ui, -apple-system, Roboto, sans-serif'

# One color per message, so that a message can be followed through the diagram. All are dark
# enough for a white label and work on both the light and the dark site theme.
COLORS = {1: "#3b82f6", 2: "#ea580c", 3: "#059669", 4: "#db2777"}
GRAY = "#64748b"  # Opaque payload (encrypted message)
RED = "#dc2626"  # Failure badge
ARROW = "#8b95a3"

STYLE = f"""    <style>
      .label {{ font: 600 12px {FONT}; letter-spacing: .09em; fill: #6b7685; }}
      .sub {{ font: 600 11px {FONT}; letter-spacing: .06em; fill: #6b7685; }}
      .num {{ font: 700 15px {FONT}; fill: #ffffff; }}
      .small {{ font: 700 11px {FONT}; fill: #ffffff; }}
      .title {{ font: 700 13px {FONT}; letter-spacing: .09em; fill: #3b82f6; }}
      .text {{ font: 600 14px {FONT}; fill: #7d8796; }}
      .card {{ fill: rgba(139, 149, 163, .10); stroke: rgba(139, 149, 163, .38); stroke-width: 1.25; }}
      .group {{ fill: rgba(139, 149, 163, .08); stroke: rgba(139, 149, 163, .55); stroke-width: 1.25; stroke-dasharray: 5 4; }}
      .slot {{ fill: none; stroke: rgba(139, 149, 163, .45); stroke-width: 1.25; stroke-dasharray: 4 4; }}
      .flow {{ fill: none; stroke: {ARROW}; stroke-width: 2; stroke-linecap: round; }}
    </style>
    <marker id="arrow" viewBox="0 0 10 10" refX="8" refY="5" markerWidth="8" markerHeight="8" orient="auto">
      <path d="M2 1.5 L8 5 L2 8.5" fill="none" stroke="{ARROW}" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round"/>
    </marker>"""

DIAGRAMS = {}


def diagram(name):
    """Registers a function that builds and returns the Svg for docs/images/diagrams/<name>.svg."""
    def register(func):
        DIAGRAMS[name] = func
        return func
    return register


class Svg:
    def __init__(self, w, h, title, desc):
        self.w, self.h, self.title, self.desc = w, h, title, desc
        self.items = []

    def add(self, s):
        self.items.append("  " + s)

    def comment(self, s):
        self.items.append("")
        self.add(f"<!-- {s} -->")

    def label(self, x, y, text, cls="label", anchor="middle"):
        a = f' text-anchor="{anchor}"' if anchor != "start" else ""
        self.add(f'<text class="{cls}" x="{x}" y="{y}"{a}>{text}</text>')

    def card(self, x, y, w, h, cls="card", rx=12):
        self.add(f'<rect class="{cls}" x="{x}" y="{y}" width="{w}" height="{h}" rx="{rx}"/>')

    def cell(self, x, y, n, w=38, h=38, text=None, cls="num", color=None):
        """A message: a colored rounded square (or bar) labeled with its number."""
        color = color or COLORS[n]
        text = str(n) if text is None else text
        t = f'<text class="{cls}" x="{x + w / 2:g}" y="{y + h / 2:g}" dy=".35em" text-anchor="middle">{text}</text>' if text else ""
        self.add(f'<rect x="{x}" y="{y}" width="{w}" height="{h}" rx="{8 if w >= 30 else 6}" fill="{color}"/>{t}')

    def slot(self, x, y, w=38, h=38):
        """An empty, dashed message slot (e.g. the end of the log, where new messages are appended)."""
        self.add(f'<rect class="slot" x="{x + .5}" y="{y + .5}" width="{w - 1}" height="{h - 1}" rx="8"/>')

    def lock(self, x, y):
        """An encrypted message: a gray cell with a padlock."""
        self.add(f'<rect x="{x}" y="{y}" width="38" height="38" rx="8" fill="{GRAY}"/>'
                 f'<g fill="none" stroke="#ffffff" stroke-width="2" stroke-linecap="round">'
                 f'<path d="M{x + 14.5} {y + 17.5} v-3 a4.5 4.5 0 0 1 9 0 v3"/></g>'
                 f'<rect x="{x + 11.5}" y="{y + 17.5}" width="15" height="11" rx="2.5" fill="#ffffff"/>')

    def arrow(self, x1, y1, x2, y2):
        self.add(f'<line class="flow" x1="{x1}" y1="{y1}" x2="{x2}" y2="{y2}" marker-end="url(#arrow)"/>')

    def path_arrow(self, d):
        self.add(f'<path class="flow" d="{d}" marker-end="url(#arrow)"/>')

    def render(self):
        head = (f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {self.w} {self.h}" width="{self.w}" height="{self.h}" '
                f'role="img" aria-labelledby="title desc">\n'
                f'  <title id="title">{self.title}</title>\n  <desc id="desc">{self.desc}</desc>\n'
                f'  <defs>\n{STYLE}\n  </defs>\n')
        body = "\n".join(self.items).lstrip("\n")
        return head + "\n" + body + "\n</svg>\n"


def vertical_messages(s, x, y, ns, gap=5):
    """Card with the messages stacked vertically (84 wide). Returns the card height."""
    h = 28 + 38 * len(ns) + gap * (len(ns) - 1)
    s.card(x, y, 84, h)
    for i, n in enumerate(ns):
        s.cell(x + 23, y + 14 + i * (38 + gap), n)
    return h


def row_messages(s, x, y, ns, gap=10, slot=False, locked=False):
    """Card with the messages in a row (66 high). Returns the card width."""
    count = len(ns) + (1 if slot else 0)
    w = 32 + 38 * count + gap * (count - 1)
    s.card(x, y, w, 66)
    for i, n in enumerate(ns):
        cx = x + 16 + i * (38 + gap)
        s.lock(cx, y + 14) if locked else s.cell(cx, y + 14, n)
    if slot:
        s.slot(x + 16 + len(ns) * (38 + gap), y + 14)
    return w


@diagram("inbound-batch")
def inbound_batch():
    s = Svg(760, 244, "Batch processing",
            "A producer writes four messages to a topic. The consumer receives them grouped in two batches of two messages each.")
    s.label(66, 26, "PRODUCER"); s.label(307, 26, "TOPIC"); s.label(628, 26, "CONSUMER")
    s.comment("Producer")
    vertical_messages(s, 24, 40, [1, 2, 3, 4])
    s.arrow(120, 134, 164, 134)
    s.comment("Topic")
    row_messages(s, 176, 101, [1, 2, 3, 4], slot=True)
    s.arrow(450, 134, 508, 134)
    s.comment("Consumer")
    s.card(520, 40, 216, 188)
    for i, (a, b) in enumerate([(1, 2), (3, 4)]):
        gy = 54 + i * 90
        s.card(536, gy, 184, 70, cls="group", rx=10)
        s.add(f'<text class="sub" x="554" y="{gy + 35}" dy=".35em">BATCH {i + 1}</text>')
        s.cell(624, gy + 16, a); s.cell(668, gy + 16, b)
    return s


@diagram("inbound-stream")
def inbound_stream():
    s = Svg(780, 244, "Streaming",
            "A producer writes four messages to a topic. The consumer receives them one after the other through a single, unbounded stream.")
    s.label(66, 26, "PRODUCER"); s.label(307, 26, "TOPIC"); s.label(628, 26, "CONSUMER")
    s.comment("Producer")
    vertical_messages(s, 24, 40, [1, 2, 3, 4])
    s.arrow(120, 134, 164, 134)
    s.comment("Topic")
    row_messages(s, 176, 101, [1, 2, 3, 4], slot=True)
    s.arrow(450, 134, 488, 134)
    s.comment("Consumer")
    s.card(500, 40, 256, 188)
    s.card(516, 85, 224, 98, cls="group", rx=10)
    s.add('<text class="sub" x="532" y="104" dy=".35em">STREAM</text>')
    for i in range(4):
        s.cell(532 + i * 44, 126, i + 1, h=38)
    s.add(f'<g fill="{ARROW}"><circle cx="712" cy="145" r="2.5"/><circle cx="721" cy="145" r="2.5" opacity=".7"/>'
          '<circle cx="730" cy="145" r="2.5" opacity=".4"/></g>')
    return s


@diagram("chunk-basic")
def chunk_basic():
    chunk, chunk_gap, message_gap = 22, 3, 8
    chunks = {1: 4, 2: 2, 3: 3}  # Message number: chunks count
    s = Svg(702, 212, "Chunking",
            "The producer splits three large messages into chunks. The chunks are written to the topic and the consumer reassembles them into the original messages.")
    s.label(86, 26, "PRODUCER"); s.label(351, 26, "TOPIC"); s.label(615, 26, "CONSUMER")

    def whole_messages(x):
        s.card(x, 40, 125, 158)
        for i, n in enumerate(chunks):
            s.cell(x + 14, 54 + i * 46, n, w=chunks[n] * chunk + (chunks[n] - 1) * chunk_gap)

    s.comment("Producer")
    whole_messages(24)
    s.arrow(161, 119, 207, 119)
    s.comment("Topic")
    s.card(219, 86, 264, 66)
    cx = 235
    for n, k in chunks.items():
        for _ in range(k):
            s.cell(cx, 100, n, w=chunk, cls="small")
            cx += chunk + chunk_gap
        cx += message_gap - chunk_gap
    s.arrow(495, 119, 541, 119)
    s.comment("Consumer")
    whole_messages(553)
    return s


@diagram("encryption")
def encryption():
    s = Svg(522, 206, "End-to-end encryption",
            "The producer encrypts three messages, which are stored encrypted in the topic and decrypted by the consumer.")
    s.label(66, 26, "PRODUCER"); s.label(261, 26, "TOPIC"); s.label(456, 26, "CONSUMER")
    s.comment("Producer")
    vertical_messages(s, 24, 40, [1, 2, 3])
    s.arrow(120, 116, 166, 116)
    s.comment("Topic (encrypted)")
    row_messages(s, 178, 83, [1, 2, 3], locked=True)
    s.arrow(356, 116, 402, 116)
    s.comment("Consumer")
    vertical_messages(s, 414, 40, [1, 2, 3])
    return s


@diagram("outbound-outboxtable")
def outbound_outboxtable():
    s = Svg(698, 248, "Transactional outbox",
            "The producer stores three messages in the outbox table, in the same database transaction as the other changes. The outbox worker then produces them to the topic, from which the consumer reads them.")
    s.label(72, 26, "PRODUCER"); s.label(227, 26, "OUTBOX TABLE"); s.label(443, 26, "TOPIC"); s.label(632, 26, "CONSUMER")
    s.comment("Database transaction")
    s.card(16, 40, 280, 196, cls="group", rx=14)
    s.label(156, 224, "SAME DB TRANSACTION", cls="sub")
    s.comment("Producer")
    vertical_messages(s, 30, 54, [1, 2, 3])
    s.arrow(124, 130, 160, 130)
    s.comment("Outbox table")
    s.add('<path class="card" d="M172 74 a55 12 0 0 1 110 0 v108 a55 12 0 0 1 -110 0 z"/>')
    s.add('<ellipse cx="227" cy="74" rx="55" ry="12" fill="none" stroke="rgba(139, 149, 163, .38)" stroke-width="1.25"/>')
    for i, n in enumerate([1, 2, 3]):
        s.cell(192, 100 + i * 28, n, w=70, h=22, cls="small")
    s.label(328, 116, "worker", cls="sub")
    s.arrow(300, 130, 350, 130)
    s.comment("Topic")
    row_messages(s, 360, 97, [1, 2, 3])
    s.arrow(538, 130, 578, 130)
    s.comment("Consumer")
    vertical_messages(s, 590, 54, [1, 2, 3])
    return s


@diagram("outbound-routing")
def outbound_routing():
    s = Svg(654, 208, "Outbound routing",
            "The router sends message 1 to both topics, message 2 to the first topic and message 3 to the second one. Each consumer receives the messages of its topic.")
    s.label(66, 26, "PRODUCER"); s.label(379, 26, "TOPICS"); s.label(571, 26, "CONSUMERS")
    s.comment("Producer")
    vertical_messages(s, 24, 40, [1, 2, 3])
    s.arrow(118, 116, 146, 116)
    s.comment("Router")
    s.card(158, 86, 96, 60)
    s.label(206, 120, "ROUTER")
    for y in (73, 159):
        s.path_arrow(f"M264 116 C 288 116 286 {y} 308 {y}")
    s.comment("Topics and consumers")
    for (y, ns) in ((40, [1, 2]), (126, [1, 3])):
        row_messages(s, 320, y, ns)
        s.arrow(450, y + 33, 500, y + 33)
        row_messages(s, 512, y, ns)
    return s


@diagram("kafka-partitions")
def kafka_partitions():
    s = Svg(534, 240, "Parallel processing of the Kafka partitions",
            "A topic with three partitions. The consumer processes the partitions concurrently, while the messages of each partition are processed sequentially.")
    s.label(122, 26, "TOPIC"); s.label(411, 26, "CONSUMER")
    s.comment("Topic")
    s.card(24, 40, 196, 186)
    s.comment("Consumer")
    s.card(312, 40, 198, 186)
    for p, n in enumerate([1, 2, 3]):  # The color identifies the partition here, the label is the message
        y = 54 + p * 56
        s.add(f'<text class="sub" x="40" y="{y + 23}" dy=".35em">P{p}</text>')
        for i in range(3):
            s.cell(72 + i * 48, y + 4, n, text=str(i + 1))
        s.arrow(232, y + 23, 300, y + 23)
        s.card(326, y, 170, 46, cls="group", rx=10)
        for i in range(3):
            s.cell(336 + i * 56, y + 4, n, text=str(i + 1))
            if i < 2:
                cx = 336 + i * 56 + 38 + 9
                s.add(f'<path d="M{cx - 2.5} {y + 18} L{cx + 2.5} {y + 23} L{cx - 2.5} {y + 28}" fill="none" stroke="{ARROW}" '
                      'stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round"/>')
    return s


@diagram("error-policies-chain")
def error_policies_chain():
    s = Svg(704, 140, "Chained error policies",
            "A failed message is retried 3 times every second, then 5 more times every 30 seconds, and finally moved to the dead-letter topic.")
    s.comment("Failed message")
    s.cell(24, 51, 1)
    s.add(f'<circle cx="62" cy="51" r="9" fill="{RED}"/>'
          '<path d="M58.5 47.5 L65.5 54.5 M65.5 47.5 L58.5 54.5" stroke="#ffffff" stroke-width="2" stroke-linecap="round"/>')
    s.arrow(80, 70, 108, 70)
    for i, (title, text, sub) in enumerate([("RETRY", "3× · every 1s", "FAILURES 1–3"),
                                            ("RETRY", "5× · every 30s", "FAILURES 4–8"),
                                            ("MOVE", "to dead-letter", "9TH FAILURE")]):
        x = 120 + i * 200
        s.comment(f"Policy {i + 1}")
        s.card(x, 24, 160, 92)
        s.label(x + 80, 52, title, cls="title")
        s.label(x + 80, 77, text, cls="text")
        s.label(x + 80, 101, sub, cls="sub")
        if i < 2:
            s.arrow(x + 172, 70, x + 188, 70)
    return s


PREVIEW = """<!doctype html>
<html><head><meta charset="utf-8"><title>Diagrams preview</title><style>
body {{ margin: 0; display: flex; font-family: {font}; }}
.theme {{ flex: 1; padding: 20px; }}
.dark {{ background: #0d1117; }} .light {{ background: #ffffff; }}
figure {{ margin: 0 0 18px; }}
img {{ max-width: 100%; height: auto; padding: 12px; border-radius: 10px; }}
.dark img {{ background: #161b22; border: 1px solid #2a313b; }}
.light img {{ background: #f6f8fb; border: 1px solid #e3e7ed; }}
figcaption {{ font-size: 12px; color: #888; }}
</style></head><body>
<div class="theme dark">{figures}</div>
<div class="theme light">{figures}</div>
</body></html>
"""


def main():
    repo = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", ".."))
    parser = argparse.ArgumentParser(description="Generate the documentation diagrams.")
    parser.add_argument("names", nargs="*", help=f"Diagrams to generate (default: all): {', '.join(DIAGRAMS)}")
    parser.add_argument("--out", default=os.path.join(repo, "docs", "images", "diagrams"), help="Output directory")
    parser.add_argument("--preview", help="Also write an HTML page showing the diagrams in both site themes")
    args = parser.parse_args()

    unknown = [n for n in args.names if n not in DIAGRAMS]
    if unknown:
        parser.error(f"unknown diagram(s): {', '.join(unknown)}")

    os.makedirs(args.out, exist_ok=True)
    paths = []
    for name in args.names or DIAGRAMS:
        path = os.path.join(args.out, name + ".svg")
        with open(path, "w", newline="\r\n", encoding="utf-8") as f:  # The repository uses CRLF
            f.write(DIAGRAMS[name]().render())
        paths.append(path)
        print(path)

    if args.preview:
        figures = "".join(
            f'<figure><img src="{pathlib.Path(p).resolve().as_uri()}"><figcaption>{os.path.basename(p)}</figcaption></figure>'
            for p in paths)
        with open(args.preview, "w", encoding="utf-8") as f:
            f.write(PREVIEW.format(font=FONT, figures=figures))
        print(args.preview)


if __name__ == "__main__":
    main()
