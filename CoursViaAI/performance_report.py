import argparse
import csv
import html
import json
import math
import random
import statistics
import time
from collections import Counter, defaultdict
from pathlib import Path
from types import SimpleNamespace

from tokenizers import Tokenizer

import config
from evaluate import parse_data_from_prompt, parse_prompt_and_expected, read_examples
from generate import Generator, clean_text, extract_answer, required_fields, validate_output


PERFORMANCE_DIR = config.OUTPUTS_DIR / "performance"
CHARTS_DIR = PERFORMANCE_DIR / "charts"
PRESENTATION_DIR = config.OUTPUTS_DIR / "presentation"


COLORS = {
    "blue": "#2563eb",
    "green": "#16a34a",
    "orange": "#f97316",
    "red": "#dc2626",
    "purple": "#7c3aed",
    "cyan": "#0891b2",
    "gray": "#64748b",
    "dark": "#111827",
    "muted": "#e5e7eb",
    "paper": "#f8fafc",
}


def safe_div(numerator: float, denominator: float) -> float:
    return numerator / denominator if denominator else 0.0


def percent(numerator: float, denominator: float) -> float:
    return safe_div(numerator, denominator) * 100


def percentile(values: list[float], q: float) -> float:
    if not values:
        return 0.0

    ordered = sorted(values)
    index = (len(ordered) - 1) * q
    lower = math.floor(index)
    upper = math.ceil(index)

    if lower == upper:
        return ordered[int(index)]

    weight = index - lower
    return ordered[lower] * (1 - weight) + ordered[upper] * weight


def read_json(path: Path) -> dict:
    if not path.exists():
        return {}

    return json.loads(path.read_text(encoding="utf-8"))


def example_items(path: Path) -> list[str]:
    if not path.exists():
        return []

    raw = path.read_text(encoding="utf-8")

    return [
        item.strip() + "\n<|endoftext|>"
        for item in raw.split("<|endoftext|>")
        if "[GOREV]" in item and "[YANIT]" in item
    ]


def task_name(example: str) -> str:
    if "EGITMEN_KURS_ANALIZI" in example:
        return "Eğitmen analizi"

    if "OGRENCI_CALISMA_ONERISI" in example:
        return "Öğrenci önerisi"

    return "Bilinmeyen"


def dataset_stats(tokenizer: Tokenizer) -> dict:
    splits = {
        "train": config.TRAIN_FILE,
        "val": config.VAL_FILE,
        "test": config.TEST_FILE,
    }

    stats = {}

    for split, path in splits.items():
        examples = example_items(path)
        counter = Counter(task_name(example) for example in examples)
        token_lengths = [len(tokenizer.encode(example).ids) for example in examples]

        raw = path.read_text(encoding="utf-8") if path.exists() else ""
        token_count = len(tokenizer.encode(raw).ids) if raw else 0
        char_count = len(raw)

        stats[split] = {
            "file": str(path),
            "examples": len(examples),
            "tokens": token_count,
            "characters": char_count,
            "avg_tokens_per_example": round(safe_div(token_count, len(examples)), 2),
            "token_length": {
                "min": min(token_lengths) if token_lengths else 0,
                "median": round(statistics.median(token_lengths), 2) if token_lengths else 0,
                "p90": round(percentile(token_lengths, 0.90), 2),
                "max": max(token_lengths) if token_lengths else 0,
            },
            "tasks": dict(counter),
        }

    return stats


def model_config(summary: dict, tokenizer: Tokenizer) -> SimpleNamespace:
    summary_config = summary.get("config", {})

    return SimpleNamespace(
        VOCAB_SIZE=int(summary_config.get("VOCAB_SIZE", tokenizer.get_vocab_size())),
        BLOCK_SIZE=int(summary_config.get("BLOCK_SIZE", config.BLOCK_SIZE)),
        N_EMBD=int(summary_config.get("N_EMBD", config.N_EMBD)),
        N_HEAD=int(summary_config.get("N_HEAD", config.N_HEAD)),
        N_LAYER=int(summary_config.get("N_LAYER", config.N_LAYER)),
        DROPOUT=float(summary_config.get("DROPOUT", config.DROPOUT)),
    )


def parameter_breakdown(cfg: SimpleNamespace) -> dict:
    # model.py ile aynı mimari varsayımı:
    # bias=False Linear katmanlar, weight tying ve LayerNorm affine parametreleri.
    token_embedding = cfg.VOCAB_SIZE * cfg.N_EMBD
    position_embedding = cfg.BLOCK_SIZE * cfg.N_EMBD
    attention = cfg.N_LAYER * (
        cfg.N_EMBD * 3 * cfg.N_EMBD
        + cfg.N_EMBD * cfg.N_EMBD
    )
    mlp = cfg.N_LAYER * (
        cfg.N_EMBD * 4 * cfg.N_EMBD
        + 4 * cfg.N_EMBD * cfg.N_EMBD
    )
    layer_norm = cfg.N_LAYER * (4 * cfg.N_EMBD) + (2 * cfg.N_EMBD)

    items = {
        "Token embedding / LM head": token_embedding,
        "Position embedding": position_embedding,
        "Causal self-attention": attention,
        "Feed-forward MLP": mlp,
        "LayerNorm": layer_norm,
    }

    return {
        "items": items,
        "total": sum(items.values()),
        "estimated_fp32_mb": round(sum(items.values()) * 4 / (1024 * 1024), 2),
    }


def training_stats(summary: dict, cfg: SimpleNamespace) -> dict:
    best_val_loss = summary.get("best_val_loss")
    perplexity = math.exp(best_val_loss) if isinstance(best_val_loss, (int, float)) else None

    return {
        "best_val_loss": best_val_loss,
        "best_val_perplexity": perplexity,
        "max_iters": config.MAX_ITERS,
        "eval_interval": config.EVAL_INTERVAL,
        "batch_size": config.BATCH_SIZE,
        "grad_accum_steps": config.GRAD_ACCUM_STEPS,
        "effective_batch_size": config.BATCH_SIZE * config.GRAD_ACCUM_STEPS,
        "learning_rate": config.LEARNING_RATE,
        "weight_decay": config.WEIGHT_DECAY,
        "dropout": cfg.DROPOUT,
        "context_length": cfg.BLOCK_SIZE,
    }


def classify_errors(errors: list[str]) -> Counter:
    counter = Counter()

    for error in errors:
        lower = error.lower()

        if "eksik" in lower or "başlık" in lower:
            counter["Format / başlık"] += 1
        elif "korun" in lower:
            counter["Veri kopyalama"] += 1
        elif "etiket" in lower or "sız" in lower:
            counter["Sistem etiketi sızıntısı"] += 1
        elif "kısa" in lower or "boş" in lower:
            counter["Kısa / boş çıktı"] += 1
        else:
            counter["Diğer"] += 1

    return counter


def run_blind_evaluation(num_tests: int, seed: int, max_new_tokens: int) -> dict:
    examples = read_examples(config.TEST_FILE)

    if not examples:
        return {
            "enabled": False,
            "reason": "Test setinde geçerli örnek bulunamadı.",
        }

    selected = random.Random(seed).sample(examples, min(num_tests, len(examples)))

    ai = Generator()

    total = len(selected)
    success = 0
    fallback_needed = 0
    error_breakdown = Counter()
    validation_pass_counts = Counter()
    per_task = defaultdict(lambda: {"total": 0, "success": 0, "fallback": 0})
    durations = []
    answer_char_lengths = []
    answer_token_lengths = []
    generated_token_counts = []
    field_total = 0
    field_missing = 0
    sample_failures = []

    print(f"Blind evaluation başlıyor: {total} örnek")

    for index, example in enumerate(selected, start=1):
        prompt, _ = parse_prompt_and_expected(example)
        mode, data = parse_data_from_prompt(prompt)
        task_label = "Eğitmen analizi" if mode == "egitmen" else "Öğrenci önerisi"

        start = time.perf_counter()
        raw = ai.generate_raw(
            prompt=prompt,
            max_new_tokens=max_new_tokens,
            temperature=0.0,
            top_k=0,
        )
        duration = time.perf_counter() - start
        durations.append(duration)

        answer = clean_text(extract_answer(raw))
        errors = validate_output(answer, mode, data)

        prompt_tokens = len(ai.encode(prompt))
        raw_tokens = len(ai.encode(raw))
        answer_tokens = len(ai.encode(answer)) if answer else 0
        generated_tokens = max(0, raw_tokens - prompt_tokens)

        answer_char_lengths.append(len(answer))
        answer_token_lengths.append(answer_tokens)
        generated_token_counts.append(generated_tokens)

        expected_fields = [field for field in required_fields(mode, data) if field]
        missing_fields = sum(1 for error in errors if error.startswith("Verilen alan korunmadı"))
        field_total += len(expected_fields)
        field_missing += missing_fields

        format_failed = any(error.startswith("Eksik başlık") for error in errors)
        copy_failed = missing_fields > 0
        leak_failed = any("etiketi" in error.lower() for error in errors)
        short_failed = any("kısa" in error.lower() or "boş" in error.lower() for error in errors)

        if not format_failed:
            validation_pass_counts["Başlık formatı"] += 1

        if not copy_failed:
            validation_pass_counts["Alan koruma"] += 1

        if not leak_failed:
            validation_pass_counts["Etiket sızıntısı yok"] += 1

        if not short_failed:
            validation_pass_counts["Yeterli uzunluk"] += 1

        per_task[task_label]["total"] += 1

        if errors:
            fallback_needed += 1
            per_task[task_label]["fallback"] += 1
            error_breakdown.update(classify_errors(errors))

            if len(sample_failures) < 3:
                sample_failures.append(
                    {
                        "task": task_label,
                        "errors": errors,
                        "answer_preview": answer[:700],
                    }
                )
        else:
            success += 1
            per_task[task_label]["success"] += 1

        print(f"[{index}/{total}] ölçüldü", end="\r")

    print()

    avg_duration = statistics.mean(durations) if durations else 0.0
    median_duration = statistics.median(durations) if durations else 0.0
    avg_generated_tokens = statistics.mean(generated_token_counts) if generated_token_counts else 0.0
    avg_tokens_per_second = safe_div(sum(generated_token_counts), sum(durations))

    scorecard_total = total
    validation_scorecard = {
        label: {
            "pass": validation_pass_counts[label],
            "fail": scorecard_total - validation_pass_counts[label],
            "pass_rate": round(percent(validation_pass_counts[label], scorecard_total), 2),
        }
        for label in [
            "Başlık formatı",
            "Alan koruma",
            "Etiket sızıntısı yok",
            "Yeterli uzunluk",
        ]
    }

    return {
        "enabled": True,
        "tests": total,
        "seed": seed,
        "max_new_tokens": max_new_tokens,
        "direct_valid_outputs": success,
        "fallback_needed": fallback_needed,
        "success_rate": round(percent(success, total), 2),
        "fallback_rate": round(percent(fallback_needed, total), 2),
        "error_breakdown": dict(error_breakdown),
        "per_task": {
            task: {
                **values,
                "success_rate": round(percent(values["success"], values["total"]), 2),
                "fallback_rate": round(percent(values["fallback"], values["total"]), 2),
            }
            for task, values in per_task.items()
        },
        "validation_scorecard": validation_scorecard,
        "field_copy_rate": {
            "total_fields": field_total,
            "missing_fields": field_missing,
            "preserved_fields": field_total - field_missing,
            "rate": round(percent(field_total - field_missing, field_total), 2),
        },
        "latency_seconds": {
            "avg": round(avg_duration, 3),
            "median": round(median_duration, 3),
            "min": round(min(durations), 3) if durations else 0.0,
            "max": round(max(durations), 3) if durations else 0.0,
        },
        "throughput": {
            "avg_generated_tokens": round(avg_generated_tokens, 2),
            "tokens_per_second": round(avg_tokens_per_second, 2),
        },
        "answer_length": {
            "characters": {
                "avg": round(statistics.mean(answer_char_lengths), 2) if answer_char_lengths else 0,
                "median": round(statistics.median(answer_char_lengths), 2) if answer_char_lengths else 0,
                "p90": round(percentile(answer_char_lengths, 0.90), 2),
            },
            "tokens": {
                "avg": round(statistics.mean(answer_token_lengths), 2) if answer_token_lengths else 0,
                "median": round(statistics.median(answer_token_lengths), 2) if answer_token_lengths else 0,
                "p90": round(percentile(answer_token_lengths, 0.90), 2),
            },
        },
        "sample_failures": sample_failures,
    }


def svg_text(value) -> str:
    return html.escape(str(value), quote=True)


def write_svg(path: Path, content: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(content, encoding="utf-8")


def bar_chart_svg(title: str, bars: list[tuple[str, float, str]], unit: str = "") -> str:
    width = 980
    height = 520
    left = 230
    right = 70
    top = 90
    bar_h = 46
    gap = 24
    plot_w = width - left - right
    max_value = max([value for _, value, _ in bars] + [1])

    rows = []

    for i, (label, value, color) in enumerate(bars):
        y = top + i * (bar_h + gap)
        bar_w = plot_w * safe_div(value, max_value)
        rows.append(
            f"""
            <text x="36" y="{y + 30}" class="label">{svg_text(label)}</text>
            <rect x="{left}" y="{y}" width="{plot_w}" height="{bar_h}" rx="8" fill="#e5e7eb"/>
            <rect x="{left}" y="{y}" width="{bar_w:.1f}" height="{bar_h}" rx="8" fill="{color}"/>
            <text x="{left + bar_w + 12:.1f}" y="{y + 30}" class="value">{value:,.0f}{svg_text(unit)}</text>
            """
        )

    return f"""<svg xmlns="http://www.w3.org/2000/svg" width="{width}" height="{height}" viewBox="0 0 {width} {height}">
    <style>
        .title {{ font: 700 30px Arial, sans-serif; fill: {COLORS["dark"]}; }}
        .label {{ font: 600 18px Arial, sans-serif; fill: #334155; }}
        .value {{ font: 700 18px Arial, sans-serif; fill: {COLORS["dark"]}; }}
    </style>
    <rect width="100%" height="100%" rx="22" fill="{COLORS["paper"]}"/>
    <text x="36" y="48" class="title">{svg_text(title)}</text>
    {''.join(rows)}
</svg>"""


def success_gauge_svg(evaluation: dict) -> str:
    width = 980
    height = 520
    center_x = 250
    center_y = 270
    radius = 138
    circumference = 2 * math.pi * radius

    if not evaluation.get("enabled"):
        rate = 0.0
        details = "Blind test çalıştırılmadı"
    else:
        rate = float(evaluation.get("success_rate", 0.0))
        details = (
            f"{evaluation.get('direct_valid_outputs', 0)} geçerli / "
            f"{evaluation.get('tests', 0)} test"
        )

    dash = circumference * safe_div(rate, 100)

    return f"""<svg xmlns="http://www.w3.org/2000/svg" width="{width}" height="{height}" viewBox="0 0 {width} {height}">
    <style>
        .title {{ font: 700 30px Arial, sans-serif; fill: {COLORS["dark"]}; }}
        .big {{ font: 800 58px Arial, sans-serif; fill: {COLORS["dark"]}; }}
        .mid {{ font: 700 24px Arial, sans-serif; fill: #334155; }}
        .small {{ font: 500 18px Arial, sans-serif; fill: #64748b; }}
    </style>
    <rect width="100%" height="100%" rx="22" fill="{COLORS["paper"]}"/>
    <text x="36" y="48" class="title">Blind Test Başarı Oranı</text>
    <circle cx="{center_x}" cy="{center_y}" r="{radius}" fill="none" stroke="#e5e7eb" stroke-width="34"/>
    <circle cx="{center_x}" cy="{center_y}" r="{radius}" fill="none" stroke="{COLORS["green"]}" stroke-width="34"
        stroke-linecap="round" stroke-dasharray="{dash:.1f} {circumference:.1f}"
        transform="rotate(-90 {center_x} {center_y})"/>
    <text x="{center_x}" y="{center_y - 4}" text-anchor="middle" class="big">%{rate:.1f}</text>
    <text x="{center_x}" y="{center_y + 42}" text-anchor="middle" class="small">{svg_text(details)}</text>

    <text x="500" y="170" class="mid">Ölçüm neyi gösteriyor?</text>
    <text x="500" y="214" class="small">Model cevabı doğrudan kullanılabilir mi?</text>
    <text x="500" y="250" class="small">Başlık formatı korunuyor mu?</text>
    <text x="500" y="286" class="small">Kurs, bölüm ve ders adları kaymadan geliyor mu?</text>
    <text x="500" y="322" class="small">Sistem etiketi kullanıcıya sızıyor mu?</text>
    <text x="500" y="386" class="mid">Fallback oranı: %{float(evaluation.get("fallback_rate", 0.0)):.1f}</text>
</svg>"""


def dataset_chart_svg(dataset: dict) -> str:
    bars = []
    colors = [COLORS["blue"], COLORS["purple"], COLORS["orange"]]

    for color, split in zip(colors, ["train", "val", "test"]):
        bars.append((split.upper(), dataset.get(split, {}).get("examples", 0), color))

    return bar_chart_svg("Veri Seti Dağılımı", bars, " örnek")


def parameter_chart_svg(parameters: dict) -> str:
    color_cycle = [
        COLORS["blue"],
        COLORS["green"],
        COLORS["orange"],
        COLORS["purple"],
        COLORS["cyan"],
    ]

    bars = [
        (label, value / 1_000_000, color_cycle[i % len(color_cycle)])
        for i, (label, value) in enumerate(parameters["items"].items())
    ]

    return bar_chart_svg("Parametre Dağılımı", bars, "M")


def error_chart_svg(evaluation: dict) -> str:
    errors = evaluation.get("error_breakdown", {}) if evaluation.get("enabled") else {}

    if not errors:
        errors = {"Hata yok": 0}

    color_cycle = [
        COLORS["red"],
        COLORS["orange"],
        COLORS["purple"],
        COLORS["cyan"],
        COLORS["gray"],
    ]

    bars = [
        (label, value, color_cycle[i % len(color_cycle)])
        for i, (label, value) in enumerate(errors.items())
    ]

    return bar_chart_svg("Hata Kırılımı", bars, " adet")


def training_cards_svg(training: dict, cfg: SimpleNamespace) -> str:
    loss = training.get("best_val_loss")
    ppl = training.get("best_val_perplexity")

    cards = [
        ("Best Val Loss", f"{loss:.4f}" if isinstance(loss, (int, float)) else "-"),
        ("Perplexity", f"{ppl:.2f}" if isinstance(ppl, (int, float)) else "-"),
        ("Context", f"{cfg.BLOCK_SIZE} token"),
        ("Katman / Head", f"{cfg.N_LAYER} / {cfg.N_HEAD}"),
        ("Embedding", str(cfg.N_EMBD)),
        ("Effective Batch", str(training["effective_batch_size"])),
    ]

    width = 980
    height = 520
    rows = []

    for i, (label, value) in enumerate(cards):
        x = 42 + (i % 3) * 300
        y = 110 + (i // 3) * 165
        color = [COLORS["blue"], COLORS["green"], COLORS["orange"], COLORS["purple"], COLORS["cyan"], COLORS["red"]][i]

        rows.append(
            f"""
            <rect x="{x}" y="{y}" width="260" height="118" rx="16" fill="white" stroke="#dbe4ee"/>
            <rect x="{x}" y="{y}" width="10" height="118" rx="5" fill="{color}"/>
            <text x="{x + 28}" y="{y + 44}" class="label">{svg_text(label)}</text>
            <text x="{x + 28}" y="{y + 88}" class="value">{svg_text(value)}</text>
            """
        )

    return f"""<svg xmlns="http://www.w3.org/2000/svg" width="{width}" height="{height}" viewBox="0 0 {width} {height}">
    <style>
        .title {{ font: 700 30px Arial, sans-serif; fill: {COLORS["dark"]}; }}
        .label {{ font: 600 18px Arial, sans-serif; fill: #64748b; }}
        .value {{ font: 800 32px Arial, sans-serif; fill: {COLORS["dark"]}; }}
    </style>
    <rect width="100%" height="100%" rx="22" fill="{COLORS["paper"]}"/>
    <text x="36" y="50" class="title">Eğitim Özeti</text>
    {''.join(rows)}
</svg>"""


def architecture_svg(cfg: SimpleNamespace, parameters: dict) -> str:
    width = 1120
    height = 560
    boxes = [
        ("Etiketli Prompt", "Kurs, puan, bölüm, dersler", 36, 200, COLORS["blue"]),
        ("BPE Tokenizer", f"{cfg.VOCAB_SIZE} vocab", 246, 200, COLORS["cyan"]),
        ("Embedding", f"{cfg.N_EMBD} boyut + pozisyon", 456, 200, COLORS["green"]),
        ("Transformer Blokları", f"{cfg.N_LAYER} katman, {cfg.N_HEAD} attention head", 666, 200, COLORS["orange"]),
        ("LM Head", "Sonraki token tahmini", 876, 200, COLORS["purple"]),
    ]

    box_svg = []

    for title, subtitle, x, y, color in boxes:
        box_svg.append(
            f"""
            <rect x="{x}" y="{y}" width="180" height="116" rx="16" fill="white" stroke="#dbe4ee"/>
            <rect x="{x}" y="{y}" width="180" height="12" rx="6" fill="{color}"/>
            <text x="{x + 16}" y="{y + 48}" class="boxTitle">{svg_text(title)}</text>
            <foreignObject x="{x + 16}" y="{y + 62}" width="148" height="44">
                <div xmlns="http://www.w3.org/1999/xhtml" class="boxText">{svg_text(subtitle)}</div>
            </foreignObject>
            """
        )

    arrows = []
    for x in [216, 426, 636, 846]:
        arrows.append(
            f"""
            <line x1="{x}" y1="258" x2="{x + 28}" y2="258" stroke="#64748b" stroke-width="3"/>
            <polygon points="{x + 28},258 {x + 16},250 {x + 16},266" fill="#64748b"/>
            """
        )

    return f"""<svg xmlns="http://www.w3.org/2000/svg" width="{width}" height="{height}" viewBox="0 0 {width} {height}">
    <style>
        .title {{ font: 800 32px Arial, sans-serif; fill: {COLORS["dark"]}; }}
        .subtitle {{ font: 500 19px Arial, sans-serif; fill: #64748b; }}
        .boxTitle {{ font: 700 18px Arial, sans-serif; fill: {COLORS["dark"]}; }}
        .boxText {{ font: 500 15px Arial, sans-serif; color: #475569; line-height: 1.25; }}
        .metric {{ font: 800 26px Arial, sans-serif; fill: {COLORS["dark"]}; }}
        .metricLabel {{ font: 600 15px Arial, sans-serif; fill: #64748b; }}
    </style>
    <rect width="100%" height="100%" rx="22" fill="{COLORS["paper"]}"/>
    <text x="36" y="52" class="title">MiniCoursViaLLM V3 Mimari Akışı</text>
    <text x="36" y="84" class="subtitle">Decoder-only GPT tarzı küçük ve kontrollü öneri modeli</text>
    {''.join(box_svg)}
    {''.join(arrows)}
    <rect x="86" y="404" width="260" height="78" rx="16" fill="white" stroke="#dbe4ee"/>
    <text x="112" y="438" class="metric">{parameters["total"] / 1_000_000:.1f}M</text>
    <text x="112" y="464" class="metricLabel">yaklaşık parametre</text>
    <rect x="430" y="404" width="260" height="78" rx="16" fill="white" stroke="#dbe4ee"/>
    <text x="456" y="438" class="metric">{cfg.BLOCK_SIZE}</text>
    <text x="456" y="464" class="metricLabel">maksimum context token</text>
    <rect x="774" y="404" width="260" height="78" rx="16" fill="white" stroke="#dbe4ee"/>
    <text x="800" y="438" class="metric">Greedy</text>
    <text x="800" y="464" class="metricLabel">varsayılan deterministik decoding</text>
</svg>"""


def write_charts(metrics: dict) -> dict:
    CHARTS_DIR.mkdir(parents=True, exist_ok=True)

    charts = {
        "success": CHARTS_DIR / "success_gauge.svg",
        "errors": CHARTS_DIR / "error_breakdown.svg",
        "dataset": CHARTS_DIR / "dataset_distribution.svg",
        "parameters": CHARTS_DIR / "parameter_breakdown.svg",
        "training": CHARTS_DIR / "training_summary.svg",
        "architecture": CHARTS_DIR / "architecture.svg",
    }

    cfg = SimpleNamespace(**metrics["model"]["config"])

    write_svg(charts["success"], success_gauge_svg(metrics["evaluation"]))
    write_svg(charts["errors"], error_chart_svg(metrics["evaluation"]))
    write_svg(charts["dataset"], dataset_chart_svg(metrics["dataset"]))
    write_svg(charts["parameters"], parameter_chart_svg(metrics["model"]["parameters"]))
    write_svg(charts["training"], training_cards_svg(metrics["training"], cfg))
    write_svg(charts["architecture"], architecture_svg(cfg, metrics["model"]["parameters"]))

    return charts


def chart_link(name: str) -> str:
    return f"../performance/charts/{name}.svg"


def write_metrics_json(metrics: dict) -> None:
    PERFORMANCE_DIR.mkdir(parents=True, exist_ok=True)
    (PERFORMANCE_DIR / "metrics.json").write_text(
        json.dumps(metrics, ensure_ascii=False, indent=2),
        encoding="utf-8",
    )


def write_metrics_csv(metrics: dict) -> None:
    rows = [
        ("best_val_loss", metrics["training"].get("best_val_loss")),
        ("best_val_perplexity", metrics["training"].get("best_val_perplexity")),
        ("parameter_count", metrics["model"]["parameters"]["total"]),
        ("estimated_fp32_mb", metrics["model"]["parameters"]["estimated_fp32_mb"]),
        ("train_examples", metrics["dataset"]["train"]["examples"]),
        ("val_examples", metrics["dataset"]["val"]["examples"]),
        ("test_examples", metrics["dataset"]["test"]["examples"]),
    ]

    if metrics["evaluation"].get("enabled"):
        rows.extend(
            [
                ("blind_tests", metrics["evaluation"]["tests"]),
                ("direct_valid_outputs", metrics["evaluation"]["direct_valid_outputs"]),
                ("fallback_needed", metrics["evaluation"]["fallback_needed"]),
                ("success_rate", metrics["evaluation"]["success_rate"]),
                ("avg_latency_seconds", metrics["evaluation"]["latency_seconds"]["avg"]),
            ]
        )

    with (PERFORMANCE_DIR / "metric_summary.csv").open("w", encoding="utf-8", newline="") as file:
        writer = csv.writer(file)
        writer.writerow(["metric", "value"])
        writer.writerows(rows)


def write_markdown_presentation(metrics: dict) -> Path:
    cfg = metrics["model"]["config"]
    params = metrics["model"]["parameters"]
    training = metrics["training"]
    evaluation = metrics["evaluation"]

    success_text = (
        f"%{evaluation['success_rate']}"
        if evaluation.get("enabled")
        else "Blind test çalıştırılmadı"
    )

    content = f"""# MiniCoursViaLLM V3 Sunumu

Bitirme projesi AI performans ve mimari özeti

---

## 1. Projenin Amacı

- CoursVia içindeki kurs, sınav ve ders verilerine göre kontrollü öneri üretmek.
- Öğrenci için çalışma planı, eğitmen için kurs geliştirme analizi oluşturmak.
- Genel sohbet modeli değil; etiketli CoursVia verisiyle çalışan görev odaklı küçük dil modeli.

---

## 2. Veri Seti

![Veri seti dağılımı]({chart_link("dataset_distribution")})

- Veri `data_generator.py` ile sentetik ama CoursVia formatına uygun üretildi.
- Train/validation/test ayrımı: 80/10/10.
- Benzersiz CVX kodlarıyla kurs, bölüm ve ders adlarını birebir kopyalama davranışı güçlendirildi.

---

## 3. Prompt Formatı

```text
[GOREV] OGRENCI_CALISMA_ONERISI
[KURS] ...
[SINAV_PUANI] ...
[GECME_PUANI] ...
[ZORLANILAN_BOLUM] ...
[ZORLANILAN_DERSLER] ...
[YANIT]
```

Model, bu etiketli girdiden sonra başlıklı ve veri koruyan cevap üretir.

---

## 4. Model Mimarisi

![Mimari]({chart_link("architecture")})

- Mimari: decoder-only GPT benzeri Transformer.
- Katman sayısı: {cfg["N_LAYER"]}
- Attention head: {cfg["N_HEAD"]}
- Embedding boyutu: {cfg["N_EMBD"]}
- Context uzunluğu: {cfg["BLOCK_SIZE"]} token
- Vocab: {cfg["VOCAB_SIZE"]}

---

## 5. Parametre Dağılımı

![Parametre dağılımı]({chart_link("parameter_breakdown")})

- Yaklaşık parametre sayısı: {params["total"]:,}
- FP32 ağırlık boyutu tahmini: {params["estimated_fp32_mb"]} MB
- Token embedding ile output LM head aynı ağırlığı paylaşır.

---

## 6. Kullanılan Algoritmalar

- Tokenizer: BPE tabanlı tokenizer.
- Öğrenme hedefi: next-token prediction.
- Loss: cross entropy.
- Optimizasyon: AdamW.
- Stabilizasyon: dropout, gradient clipping, gradient accumulation.
- Üretim: greedy decoding, repetition penalty, isteğe bağlı temperature/top-k.

---

## 7. Eğitim Süreci

![Eğitim özeti]({chart_link("training_summary")})

- Maksimum iterasyon: {training["max_iters"]}
- Effective batch size: {training["effective_batch_size"]}
- Learning rate: {training["learning_rate"]}
- Weight decay: {training["weight_decay"]}
- Best validation loss: {training["best_val_loss"]:.4f}

---

## 8. Performans Ölçümü

![Başarı oranı]({chart_link("success_gauge")})

- Ölçüm tipi: blind test.
- Test girdileri modelin görmediği test setinden seçilir.
- Beklenen cevap doğrudan verilmez; model sıfırdan üretim yapar.
- Cevap başlık, alan koruma, sistem etiketi sızıntısı ve minimum uzunluk açısından doğrulanır.
- Doğrudan geçerli çıktı oranı: {success_text}

---

## 9. Hata Analizi

![Hata kırılımı]({chart_link("error_breakdown")})

Hatalar dört ana başlıkta kontrol edilir:

- Eksik başlık veya format bozulması.
- Kurs/bölüm/ders alanlarının korunmaması.
- Sistem etiketlerinin cevaba sızması.
- Çok kısa veya boş çıktı.

---

## 10. Güvenli Kullanım Katmanı

- Model cevabı `validate_output` ile kontrol edilir.
- Geçersiz cevapta kullanıcıya ham model çıktısı gösterilmez.
- `fallback_ogrenci` ve `fallback_egitmen` veri koruyan güvenli metin üretir.
- Bu yapı web/API entegrasyonunda hatalı AI cevabını kontrol altında tutar.

---

## 11. API Entegrasyon Akışı

1. Web/API tarafı öğrenci veya eğitmen verisini JSON olarak gönderir.
2. `generate.py` bu veriyi etiketli prompt formatına çevirir.
3. MiniCoursViaLLM cevap üretir.
4. Cevap temizlenir ve doğrulanır.
5. Başarılıysa model cevabı, başarısızsa fallback cevabı döner.

---

## 12. Sonuç

- Küçük ama görev odaklı bir Transformer modeli kuruldu.
- Eğitim verisi CoursVia senaryosuna özel tasarlandı.
- Performans sadece loss ile değil, gerçek kullanım kriterleriyle de ölçüldü.
- Fallback katmanı sayesinde proje web tarafında daha güvenli kullanılabilir.
"""

    PRESENTATION_DIR.mkdir(parents=True, exist_ok=True)
    path = PRESENTATION_DIR / "minicoursvia_sunum.md"
    path.write_text(content, encoding="utf-8")
    return path


def slide(title: str, body: str, accent: str = "blue") -> str:
    return f"""
    <section class="slide">
        <div class="accent {accent}"></div>
        <div class="slide-inner">
            <h2>{title}</h2>
            {body}
        </div>
    </section>
    """


def write_html_presentation(metrics: dict) -> Path:
    cfg = metrics["model"]["config"]
    params = metrics["model"]["parameters"]
    training = metrics["training"]
    evaluation = metrics["evaluation"]

    success_rate = (
        f"%{evaluation['success_rate']}"
        if evaluation.get("enabled")
        else "Blind test çalıştırılmadı"
    )

    slides = [
        """
        <section class="slide title-slide">
            <div class="title-copy">
                <p class="kicker">Bitirme Projesi AI Modülü</p>
                <h1>MiniCoursViaLLM V3</h1>
                <p class="lead">CoursVia için öğrenci çalışma önerisi ve eğitmen kurs analizi üreten görev odaklı küçük dil modeli.</p>
            </div>
            <img src="../performance/charts/architecture.svg" alt="MiniCoursViaLLM mimari akışı" />
        </section>
        """,
        slide(
            "Projenin Amacı",
            """
            <ul>
                <li>CoursVia eğitim verilerinden kişiselleştirilmiş öneri üretmek.</li>
                <li>Öğrenciye sınav sonucuna göre çalışma planı sunmak.</li>
                <li>Eğitmene kurs performansı üzerinden geliştirme aksiyonu önermek.</li>
                <li>Genel sohbet yerine kontrollü, formatı doğrulanabilir çıktı üretmek.</li>
            </ul>
            """,
            "green",
        ),
        slide(
            "Veri Seti",
            """
            <img class="chart" src="../performance/charts/dataset_distribution.svg" alt="Veri seti dağılımı" />
            <p>Veri seti, CoursVia kullanım senaryolarına uygun etiketli prompt ve yanıt çiftlerinden oluşturuldu.</p>
            """,
            "orange",
        ),
        slide(
            "Prompt Formatı",
            """
            <pre><code>[GOREV] OGRENCI_CALISMA_ONERISI
[KURS] Türkçe Dil Bilgisi
[SINAV_PUANI] 58
[GECME_PUANI] 70
[ZORLANILAN_BOLUM] Cümle Bilgisi
[ZORLANILAN_DERSLER] Cümlenin Ögeleri, Fiilimsi Türleri
[YANIT]</code></pre>
            <p>Bu yapı modelin girdiyi parçalara ayırmasını ve cevabı beklenen başlıklarda üretmesini kolaylaştırır.</p>
            """,
            "purple",
        ),
        slide(
            "Model Mimarisi",
            """
            <img class="chart wide" src="../performance/charts/architecture.svg" alt="Mimari" />
            <div class="metric-row">
                <span>Katman: <b>""" + str(cfg["N_LAYER"]) + """</b></span>
                <span>Head: <b>""" + str(cfg["N_HEAD"]) + """</b></span>
                <span>Embedding: <b>""" + str(cfg["N_EMBD"]) + """</b></span>
                <span>Context: <b>""" + str(cfg["BLOCK_SIZE"]) + """</b></span>
            </div>
            """,
            "blue",
        ),
        slide(
            "Parametre Dağılımı",
            f"""
            <img class="chart" src="../performance/charts/parameter_breakdown.svg" alt="Parametre dağılımı" />
            <p>Toplam yaklaşık <b>{params["total"]:,}</b> parametre bulunur. Weight tying ile token embedding ve LM head aynı ağırlığı paylaşır.</p>
            """,
            "cyan",
        ),
        slide(
            "Kullanılan Algoritmalar",
            """
            <ul>
                <li>BPE tokenizer ile metin token id dizisine çevrildi.</li>
                <li>Transformer, next-token prediction hedefiyle eğitildi.</li>
                <li>Cross entropy loss ve AdamW optimizer kullanıldı.</li>
                <li>Gradient accumulation, dropout ve gradient clipping eğitim stabilitesi için kullanıldı.</li>
                <li>Üretimde varsayılan olarak greedy decoding ve repetition penalty uygulandı.</li>
            </ul>
            """,
            "green",
        ),
        slide(
            "Eğitim Süreci",
            f"""
            <img class="chart" src="../performance/charts/training_summary.svg" alt="Eğitim özeti" />
            <p>Model {training["max_iters"]} iterasyonluk eğitim ayarıyla çalıştırıldı. Best validation loss değeri <b>{training["best_val_loss"]:.4f}</b>.</p>
            """,
            "orange",
        ),
        slide(
            "Performans Ölçümü",
            f"""
            <img class="chart" src="../performance/charts/success_gauge.svg" alt="Başarı grafiği" />
            <p>Blind testte model, test setinden gelen promptlara beklenen cevap verilmeden yanıt üretir. Doğrudan geçerli çıktı oranı: <b>{success_rate}</b>.</p>
            """,
            "purple",
        ),
        slide(
            "Hata Analizi",
            """
            <img class="chart" src="../performance/charts/error_breakdown.svg" alt="Hata kırılımı" />
            <p>Format, veri kopyalama, sistem etiketi sızıntısı ve kısa çıktı gibi kullanıcıya yansıyabilecek riskler ayrı ayrı ölçülür.</p>
            """,
            "red",
        ),
        slide(
            "Güvenli Kullanım",
            """
            <ul>
                <li>Ham model cevabı doğrudan kullanıcıya verilmez.</li>
                <li>Önce başlıklar, alan koruma ve sistem etiketi sızıntısı kontrol edilir.</li>
                <li>Hata varsa fallback metni üretilir.</li>
                <li>Bu katman, web/API tarafında daha kontrollü bir AI deneyimi sağlar.</li>
            </ul>
            """,
            "cyan",
        ),
        slide(
            "Sonuç",
            """
            <ul>
                <li>MiniCoursViaLLM, CoursVia'ya özel küçük bir Transformer modelidir.</li>
                <li>Model sadece loss ile değil, gerçek kullanım kurallarıyla da değerlendirildi.</li>
                <li>Performans grafikleri ve sunum çıktıları tekrar üretilebilir şekilde hazırlandı.</li>
            </ul>
            """,
            "green",
        ),
    ]

    content = f"""<!doctype html>
<html lang="tr">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>MiniCoursViaLLM V3 Sunumu</title>
    <style>
        :root {{
            --ink: #111827;
            --muted: #526070;
            --line: #dbe4ee;
            --bg: #eef3f8;
            --paper: #ffffff;
            --blue: #2563eb;
            --green: #16a34a;
            --orange: #f97316;
            --purple: #7c3aed;
            --cyan: #0891b2;
            --red: #dc2626;
        }}

        * {{ box-sizing: border-box; }}

        html {{
            scroll-snap-type: y mandatory;
        }}

        body {{
            margin: 0;
            font-family: Arial, Helvetica, sans-serif;
            color: var(--ink);
            background: var(--bg);
        }}

        .slide {{
            min-height: 100vh;
            scroll-snap-align: start;
            display: flex;
            align-items: center;
            justify-content: center;
            padding: 48px;
            position: relative;
            overflow: hidden;
        }}

        .slide-inner {{
            width: min(1180px, 100%);
            background: var(--paper);
            border: 1px solid var(--line);
            border-radius: 22px;
            padding: 44px;
            box-shadow: 0 24px 70px rgba(15, 23, 42, 0.10);
        }}

        .title-slide {{
            gap: 34px;
            align-items: stretch;
        }}

        .title-copy {{
            width: min(520px, 42vw);
            display: flex;
            flex-direction: column;
            justify-content: center;
        }}

        .title-slide img {{
            width: min(760px, 54vw);
            object-fit: contain;
        }}

        .kicker {{
            margin: 0 0 18px;
            font-size: 20px;
            font-weight: 700;
            color: var(--blue);
        }}

        h1 {{
            margin: 0;
            font-size: clamp(48px, 7vw, 96px);
            line-height: 1.02;
        }}

        h2 {{
            margin: 0 0 28px;
            font-size: clamp(34px, 4.5vw, 62px);
            line-height: 1.08;
        }}

        p, li {{
            font-size: clamp(20px, 2vw, 28px);
            line-height: 1.38;
            color: var(--muted);
        }}

        ul {{
            margin: 0;
            padding-left: 30px;
        }}

        li + li {{
            margin-top: 16px;
        }}

        .lead {{
            font-size: clamp(22px, 2.4vw, 34px);
            color: var(--muted);
        }}

        .chart {{
            width: min(960px, 100%);
            display: block;
            margin: 0 auto 28px;
        }}

        .wide {{
            width: min(1080px, 100%);
        }}

        pre {{
            margin: 0 0 24px;
            padding: 26px;
            background: #111827;
            color: #f8fafc;
            border-radius: 16px;
            overflow-x: auto;
            font-size: clamp(18px, 1.8vw, 25px);
            line-height: 1.35;
        }}

        .metric-row {{
            display: flex;
            flex-wrap: wrap;
            gap: 12px;
            margin-top: 10px;
        }}

        .metric-row span {{
            border: 1px solid var(--line);
            border-radius: 999px;
            padding: 12px 18px;
            font-size: 22px;
            color: var(--muted);
            background: #f8fafc;
        }}

        .accent {{
            position: absolute;
            left: 0;
            top: 0;
            width: 12px;
            height: 100%;
        }}

        .accent.blue {{ background: var(--blue); }}
        .accent.green {{ background: var(--green); }}
        .accent.orange {{ background: var(--orange); }}
        .accent.purple {{ background: var(--purple); }}
        .accent.cyan {{ background: var(--cyan); }}
        .accent.red {{ background: var(--red); }}

        @media (max-width: 900px) {{
            .slide {{
                padding: 24px;
            }}

            .slide-inner {{
                padding: 28px;
                border-radius: 16px;
            }}

            .title-slide {{
                flex-direction: column;
            }}

            .title-copy,
            .title-slide img {{
                width: 100%;
            }}
        }}

        @media print {{
            html {{
                scroll-snap-type: none;
            }}

            .slide {{
                min-height: 100vh;
                break-after: page;
                padding: 24px;
            }}

            .slide-inner {{
                box-shadow: none;
            }}
        }}
    </style>
</head>
<body>
    {''.join(slides)}
    <script>
        const slides = [...document.querySelectorAll('.slide')];
        document.addEventListener('keydown', event => {{
            const current = Math.round(window.scrollY / window.innerHeight);
            if (event.key === 'ArrowDown' || event.key === 'ArrowRight' || event.key === ' ') {{
                slides[Math.min(current + 1, slides.length - 1)].scrollIntoView({{ behavior: 'smooth' }});
            }}
            if (event.key === 'ArrowUp' || event.key === 'ArrowLeft') {{
                slides[Math.max(current - 1, 0)].scrollIntoView({{ behavior: 'smooth' }});
            }}
        }});
    </script>
</body>
</html>
"""

    PRESENTATION_DIR.mkdir(parents=True, exist_ok=True)
    path = PRESENTATION_DIR / "minicoursvia_sunum.html"
    path.write_text(content, encoding="utf-8")
    return path


def build_metrics(args) -> dict:
    if not config.TOKENIZER_FILE.exists():
        raise FileNotFoundError(f"Tokenizer dosyası bulunamadı: {config.TOKENIZER_FILE}")

    tokenizer = Tokenizer.from_file(str(config.TOKENIZER_FILE))
    summary = read_json(config.OUTPUTS_DIR / "train_summary.json")
    cfg = model_config(summary, tokenizer)
    params = parameter_breakdown(cfg)

    evaluation = (
        run_blind_evaluation(args.tests, args.seed, args.max_new_tokens)
        if args.tests > 0
        else {"enabled": False, "reason": "Komut --tests 0 ile çalıştırıldı."}
    )

    return {
        "created_at": time.strftime("%Y-%m-%d %H:%M:%S"),
        "dataset": dataset_stats(tokenizer),
        "model": {
            "config": vars(cfg),
            "parameters": params,
            "model_files": {
                "best": str(config.BEST_MODEL_FILE),
                "final": str(config.FINAL_MODEL_FILE),
                "tokenizer": str(config.TOKENIZER_FILE),
            },
        },
        "training": training_stats(summary, cfg),
        "evaluation": evaluation,
    }


def main() -> None:
    parser = argparse.ArgumentParser(
        description="MiniCoursViaLLM için grafiksel performans raporu ve sunum dosyaları üretir."
    )
    parser.add_argument("--tests", type=int, default=30, help="Blind testte kullanılacak örnek sayısı.")
    parser.add_argument("--seed", type=int, default=42, help="Tekrarlanabilir örnek seçimi için seed.")
    parser.add_argument(
        "--max-new-tokens",
        type=int,
        default=360,
        help="Her test örneği için üretilecek maksimum yeni token sayısı.",
    )

    args = parser.parse_args()

    PERFORMANCE_DIR.mkdir(parents=True, exist_ok=True)
    PRESENTATION_DIR.mkdir(parents=True, exist_ok=True)

    metrics = build_metrics(args)

    write_metrics_json(metrics)
    write_metrics_csv(metrics)
    charts = write_charts(metrics)
    markdown_path = write_markdown_presentation(metrics)
    html_path = write_html_presentation(metrics)

    print("=" * 70)
    print("MiniCoursVia performans paketi hazır")
    print("=" * 70)
    print(f"Metrikler : {PERFORMANCE_DIR / 'metrics.json'}")
    print(f"CSV       : {PERFORMANCE_DIR / 'metric_summary.csv'}")
    print(f"Grafikler : {CHARTS_DIR}")
    print(f"Sunum MD  : {markdown_path}")
    print(f"Sunum HTML: {html_path}")
    print("=" * 70)
    print("Üretilen grafikler:")

    for name, path in charts.items():
        print(f"- {name}: {path.name}")


if __name__ == "__main__":
    main()
