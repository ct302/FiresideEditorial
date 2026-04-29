"""
Fireside Editorial — AI Article Generator
Generates a seasonal editorial article via OpenRouter and commits it to the repo.
Run manually or via GitHub Actions on a schedule.
"""
import json, os, re, sys, datetime, random
from pathlib import Path
import urllib.request, urllib.error

REPO_ROOT = Path(__file__).resolve().parent.parent
CONTENT_JSON = REPO_ROOT / "wwwroot" / "data" / "content.json"
ARTICLES_DIR = REPO_ROOT / "wwwroot" / "data" / "articles"

OPENROUTER_KEY = os.environ.get("OPENROUTER_API_KEY", "")
IMAGE_MODEL = os.environ.get("OPENROUTER_IMAGE_MODEL", "google/gemini-2.5-flash-image")

# Ordered fallback list — tries each until one works
# openrouter/free auto-routes to whatever's alive, so it goes first
MODEL_FALLBACKS = [
    os.environ.get("OPENROUTER_MODEL", "openrouter/free"),
    "google/gemma-4-31b-it:free",
    "google/gemma-4-26b-a4b-it:free",
    "inclusionai/ling-2.6-1t:free",
    "tencent/hy3-preview:free",
    "meta-llama/llama-3.3-70b-instruct:free",
]
MAX_RETRIES_PER_MODEL = 2  # retry 429s before moving to next model
RETRY_DELAY_SECS = 15      # wait between retries on rate-limit
MODEL = MODEL_FALLBACKS[0]  # default for logging

IMAGES_DIR = REPO_ROOT / "wwwroot" / "images" / "articles"

# Seasonal topic pools — the AI picks from these or riffs on them
TOPIC_POOLS = {
    "winter": [
        "The art of writing handwritten Christmas cards",
        "How to host a cookie decorating party",
        "Candle-making for the holiday table",
        "The history of Christmas crackers",
        "Building a winter reading nook",
        "Traditional mulled wine recipes from around Europe",
        "The magic of a winter morning walk",
        "Creating an heirloom advent calendar",
        "The lost art of Christmas letter writing",
        "How to throw a solstice gathering",
        "Preserving family recipes for future generations",
        "The perfect Christmas movie marathon playlist",
        "Making your own Christmas potpourri",
        "The tradition of Yule logs and hearth fires",
        "Winter foraging: pine, rosemary, and juniper",
        "How to start a holiday journaling practice",
        "The joy of secret Santa: a history",
        "Handmade gift wrapping techniques",
        "Creating a Christmas morning playlist",
        "The art of the holiday cheese board",
        "Snow globe collecting as a family tradition",
        "Planning a Boxing Day countryside walk",
    ],
}

CATEGORIES = [
    "The Bakery", "Evening Ritual", "Craft & Care", "The Forest",
    "Harmony", "Artisanal", "Nature", "Leisure", "Nostalgia",
    "Heritage", "Gatherings", "Homestead", "Storytelling",
]

def get_fallback_image() -> str:
    """When AI image generation fails, reuse a random existing local image.
    Guarantees the card always renders something instead of a dead URL."""
    if IMAGES_DIR.exists():
        existing = list(IMAGES_DIR.glob("*.png"))
        if existing:
            choice = random.choice(existing)
            print(f"  IMAGE FALLBACK USED: reusing {choice.name}", file=sys.stderr)
            return f"/images/articles/{choice.name}"
    print("  IMAGE FALLBACK USED: no local images found, using empty placeholder", file=sys.stderr)
    return "/images/articles/placeholder.png"

SYSTEM_PROMPT = """You are the voice of The Fireside Editorial — a warm, literary Christmas and winter traditions magazine. Your tone is:
- Intimate and nostalgic, like a letter from a well-read friend
- Sensory-rich: you describe smells, textures, sounds, temperatures
- Gently opinionated but never preachy
- You use short paragraphs, occasional em-dashes, and pull-quotes
- You write in second person sometimes ("you") to draw the reader in
- Articles are 400-600 words with 2-3 markdown ## subheadings
- Include one > blockquote somewhere in the article
- End with a reflective closing thought, never a call-to-action

You output ONLY a JSON object (no markdown fences, no preamble) with these exact keys:
{
  "title": "Article Title Here",
  "category": "One of the editorial categories",
  "description": "A 15-25 word teaser for the card grid",
  "ctaText": "A 2-3 word button label like 'Read the Ritual' or 'Explore the Craft'",
  "imageAlt": "Descriptive alt text for an editorial photo",
  "affiliateSearch": "3-5 word Amazon search term for related products",
  "affiliateLabel": "Shop [Product Type]",
  "markdown": "The full article body in markdown"
}"""


def get_existing_slugs():
    """Get all existing article slugs to avoid duplicates."""
    return {f.stem for f in ARTICLES_DIR.glob("*.md")}


def slugify(title: str) -> str:
    slug = title.lower().strip()
    slug = re.sub(r"[^a-z0-9\s-]", "", slug)
    slug = re.sub(r"[\s-]+", "-", slug).strip("-")
    return slug[:60]


def pick_topic():
    existing = get_existing_slugs()
    pool = TOPIC_POOLS.get("winter", [])
    # Filter out topics whose slugified version already exists
    available = [t for t in pool if slugify(t) not in existing]
    if available:
        return random.choice(available)
    # If all pool topics used, ask AI to come up with one
    return "Write about a unique Christmas or winter tradition not commonly covered"


def call_openrouter(topic: str) -> dict:
    """Call OpenRouter API with model fallback + retry. Retries 429s before moving on."""
    global MODEL
    if not OPENROUTER_KEY:
        print("ERROR: OPENROUTER_API_KEY not set", file=sys.stderr)
        sys.exit(1)

    import time
    last_error = None

    for model_id in MODEL_FALLBACKS:
        MODEL = model_id
        print(f"  Trying model: {model_id}", file=sys.stderr)

        for attempt in range(1, MAX_RETRIES_PER_MODEL + 1):
            payload = json.dumps({
                "model": model_id,
                "max_tokens": 16000,
                "messages": [
                    {"role": "system", "content": SYSTEM_PROMPT},
                    {"role": "user", "content": f"Write a Fireside Editorial article about: {topic}\n\nUse one of these categories: {', '.join(CATEGORIES)}"}
                ]
            }).encode()

            req = urllib.request.Request(
                "https://openrouter.ai/api/v1/chat/completions",
                data=payload,
                headers={
                    "Authorization": f"Bearer {OPENROUTER_KEY}",
                    "Content-Type": "application/json",
                    "HTTP-Referer": "https://github.com/ct302/FiresideEditorial",
                    "X-Title": "Fireside Editorial Article Generator",
                },
            )

            try:
                with urllib.request.urlopen(req, timeout=120) as resp:
                    data = json.loads(resp.read())
                # Success — break out of both loops
                print(f"  Success with model: {model_id} (attempt {attempt})")
                break
            except urllib.error.HTTPError as e:
                body = e.read().decode() if e.fp else ""
                last_error = f"API error {e.code}: {body}"
                if e.code == 429 and attempt < MAX_RETRIES_PER_MODEL:
                    print(f"  Rate limited on {model_id} (attempt {attempt}), waiting {RETRY_DELAY_SECS}s...", file=sys.stderr)
                    time.sleep(RETRY_DELAY_SECS)
                    continue  # retry same model
                elif e.code == 404:
                    print(f"  Model {model_id} not found (404), skipping", file=sys.stderr)
                    break  # skip to next model, no point retrying
                else:
                    print(f"  Model {model_id} failed ({e.code}): {body[:200]}", file=sys.stderr)
                    break  # skip to next model
        else:
            continue  # inner loop exhausted retries, try next model
        break  # inner loop broke on success, exit outer loop
    else:
        # All models failed
        print(f"All {len(MODEL_FALLBACKS)} models failed. Last error: {last_error}", file=sys.stderr)
        sys.exit(1)

    # Handle different response formats across models
    choice = data.get("choices", [{}])[0]
    raw = choice.get("message", {}).get("content")

    # Some models use 'text' instead of 'content'
    if raw is None:
        raw = choice.get("text")

    # Last resort: check for content in the delta field (streaming leftovers)
    if raw is None:
        raw = choice.get("delta", {}).get("content")

    # Reasoning models may put everything in 'reasoning' if they run out of output tokens
    if raw is None:
        reasoning = choice.get("message", {}).get("reasoning", "")
        if reasoning:
            # Try to extract JSON from the reasoning field
            json_match = re.search(r'\{[^{}]*"title"[^{}]*"markdown".*?\}', reasoning, re.DOTALL)
            if json_match:
                raw = json_match.group(0)
            else:
                print(f"Model used all tokens for reasoning, no article produced. Try a non-reasoning model.", file=sys.stderr)
                print(f"Reasoning excerpt: {reasoning[:500]}", file=sys.stderr)
                sys.exit(1)

    if raw is None:
        print(f"No content in API response. Full response:\n{json.dumps(data, indent=2)[:1000]}", file=sys.stderr)
        sys.exit(1)

    # Strip markdown fences if the model added them
    raw = re.sub(r"^```json\s*", "", raw.strip())
    raw = re.sub(r"\s*```$", "", raw.strip())

    try:
        return json.loads(raw)
    except json.JSONDecodeError:
        # Try to repair truncated JSON — common when model runs out of tokens
        # If we have at least title and category, try to close the JSON
        repaired = raw.rstrip()
        if '"title"' in repaired and '"markdown"' not in repaired:
            # JSON was cut before the markdown field — add a placeholder
            if repaired.endswith('"'):
                repaired += ','
            elif not repaired.endswith(','):
                repaired += '",'
            repaired += '\n  "markdown": "Article content is being regenerated. Please check back soon."\n}'
            try:
                result = json.loads(repaired)
                print("  WARNING: Response was truncated, using placeholder article body", file=sys.stderr)
                return result
            except json.JSONDecodeError:
                pass
        elif '"markdown"' in repaired:
            # JSON was cut mid-markdown — try closing the string and object
            # Find the last complete sentence
            last_period = repaired.rfind('.')
            if last_period > repaired.rfind('"markdown"'):
                repaired = repaired[:last_period + 1] + '"\n}'
                try:
                    result = json.loads(repaired)
                    print("  WARNING: Markdown was truncated, using partial article", file=sys.stderr)
                    return result
                except json.JSONDecodeError:
                    pass

        print(f"Failed to parse AI response:\n{raw[:500]}", file=sys.stderr)
        sys.exit(1)


def generate_image(title: str, image_alt: str, slug: str) -> str:
    """Generate an editorial photo via Nano Banana and save as PNG. Returns relative URL path."""
    if not OPENROUTER_KEY:
        return get_fallback_image()

    IMAGES_DIR.mkdir(parents=True, exist_ok=True)

    prompt = (
        f"Generate a warm, editorial-style photograph for a winter traditions magazine article titled '{title}'. "
        f"Description: {image_alt}. "
        "Style: Cozy, sepia-toned, soft natural lighting, nostalgic warmth. "
        "No text, no watermarks, no logos. Photographic quality, shot on 35mm film aesthetic."
    )

    payload = json.dumps({
        "model": IMAGE_MODEL,
        "max_tokens": 4096,
        "modalities": ["image"],
        "messages": [
            {"role": "user", "content": prompt}
        ]
    }).encode()

    req = urllib.request.Request(
        "https://openrouter.ai/api/v1/chat/completions",
        data=payload,
        headers={
            "Authorization": f"Bearer {OPENROUTER_KEY}",
            "Content-Type": "application/json",
            "HTTP-Referer": "https://github.com/ct302/FiresideEditorial",
            "X-Title": "Fireside Editorial Image Generator",
        },
    )

    try:
        with urllib.request.urlopen(req, timeout=120) as resp:
            data = json.loads(resp.read())
    except urllib.error.HTTPError as e:
        body = e.read().decode() if e.fp else ""
        print(f"  Image API error {e.code}: {body[:200]}", file=sys.stderr)
        return get_fallback_image()
    except Exception as e:
        print(f"  Image generation failed: {e}", file=sys.stderr)
        return get_fallback_image()

    # Extract base64 image from response
    try:
        choice = data.get("choices", [{}])[0]
        message = choice.get("message", {})

        # Check for images array (OpenRouter image response format)
        images = message.get("images", [])
        if images:
            img_item = images[0]
            if isinstance(img_item, dict):
                b64_data = img_item.get("image_url", {}).get("url", "")
                if not b64_data:
                    b64_data = img_item.get("url", img_item.get("data", ""))
            else:
                b64_data = img_item
        else:
            # Some models embed base64 in content
            content_blocks = message.get("content", [])
            b64_data = None
            if isinstance(content_blocks, list):
                for block in content_blocks:
                    if isinstance(block, dict) and block.get("type") == "image_url":
                        url = block.get("image_url", {}).get("url", "")
                        if url.startswith("data:image"):
                            b64_data = url.split(",", 1)[1]
                            break
            elif isinstance(content_blocks, str) and content_blocks.startswith("data:image"):
                b64_data = content_blocks.split(",", 1)[1]

        if not b64_data:
            print("  No image data in response, using fallback", file=sys.stderr)
            return get_fallback_image()

        # Strip data URL prefix if present
        if isinstance(b64_data, str) and b64_data.startswith("data:image"):
            b64_data = b64_data.split(",", 1)[1]

        import base64
        img_bytes = base64.b64decode(b64_data)
        img_path = IMAGES_DIR / f"{slug}.png"
        img_path.write_bytes(img_bytes)
        print(f"  Generated image: {img_path.name} ({len(img_bytes) // 1024}KB)")
        return f"/images/articles/{slug}.png"

    except Exception as e:
        print(f"  Image extraction failed: {e}", file=sys.stderr)
        return get_fallback_image()


def update_content_json(card_entry: dict):
    """Append a new card to content.json."""
    with open(CONTENT_JSON, "r", encoding="utf-8") as f:
        content = json.load(f)
    content["cards"].append(card_entry)
    with open(CONTENT_JSON, "w", encoding="utf-8") as f:
        json.dump(content, f, indent=2, ensure_ascii=False)
    print(f"  Updated content.json ({len(content['cards'])} cards total)")


def main():
    print("=== Fireside Editorial — Article Generator ===")
    print(f"  Model: {MODEL}")
    print(f"  Date:  {datetime.date.today()}")

    topic = pick_topic()
    print(f"  Topic: {topic}")

    article = call_openrouter(topic)
    title = article.get("title", topic)
    slug = slugify(title)
    print(f"  Title: {title}")
    print(f"  Slug:  {slug}")

    # Avoid overwriting
    existing = get_existing_slugs()
    if slug in existing:
        slug = f"{slug}-{datetime.date.today().isoformat()}"
        print(f"  Slug collision, using: {slug}")

    # Write the markdown article
    md_path = ARTICLES_DIR / f"{slug}.md"
    md_path.write_text(article["markdown"], encoding="utf-8")
    print(f"  Wrote: {md_path.name}")

    # Generate editorial image (retry once if first attempt didn't produce a file)
    image_alt = article.get("imageAlt", f"Editorial photo for {title}")
    print("  Generating image via Nano Banana...")
    image_url = generate_image(title, image_alt, slug)
    expected_path = IMAGES_DIR / f"{slug}.png"
    if not expected_path.exists():
        print("  First image attempt failed — retrying once after 5s...", file=sys.stderr)
        import time
        time.sleep(5)
        image_url = generate_image(title, image_alt, slug)

    # Build the card entry for content.json
    amazon_tag = os.environ.get("AMAZON_TAG", "YOUR-TAG-20")
    search_term = article.get("affiliateSearch", title.lower())
    search_encoded = urllib.parse.quote_plus(search_term) if hasattr(urllib, 'parse') else search_term.replace(" ", "+")

    card = {
        "category": article.get("category", random.choice(CATEGORIES)),
        "title": title,
        "slug": slug,
        "description": article.get("description", "A new tradition from The Fireside Editorial."),
        "imageUrl": image_url,
        "imageAlt": image_alt,
        "ctaText": article.get("ctaText", "Read More"),
        "affiliateUrl": f"https://www.amazon.com/s?k={search_encoded}&tag={amazon_tag}",
        "affiliateLabel": article.get("affiliateLabel", "Shop Related"),
    }

    update_content_json(card)
    print("  Done! Article ready for the editorial.")


if __name__ == "__main__":
    import urllib.parse
    main()
