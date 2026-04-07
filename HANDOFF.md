# The Fireside Editorial — Handoff Document

> **For Claude:** Read this at the start of every new chat to get current project state. Update it as part of any significant change.

## What this is

A Christmas and winter traditions editorial magazine site. Q4-aligned content, monetized through stacked revenue streams (affiliate marketing → digital products → newsletter → display ads). Long-term goal: passive income, 1–2+ year horizon. Built in Blazor Web App (.NET 10) + SQLite/EF Core.

**Live URL:** https://firesideeditorial.azurewebsites.net
**Repo:** https://github.com/ct302/FiresideEditorial (public, `master` branch)
**Local path:** `C:\Users\heads\source\repos\FiresideEditorial`

## Current phase

**Phase 6 (Deploy) — ~95% complete, intentionally paused.**

Site is live on Azure F1 Free tier. Custom domain purchase deferred until CT is ready to upgrade to B1 Basic (~$13/mo) — F1 supports custom domain binding but **not** free managed SSL, so HTTPS on a custom domain requires the upgrade. Phase 7 (SEO/perf/a11y) and remaining revenue features (email drips, Advent countdown) still ahead.

## What's deployed and working on Azure

- Homepage with hero, snowflakes, navigation
- Journal index + all 20 articles (read from `.md` files via JsonContentService)
- Gift Guides (`/gift-guides/{slug}`), Recipes (`/recipes/{slug}` with JSON-LD + print), Shop (`/shop`)
- Search overlay (greps `.md` files at request time)
- Newsletter signup (Buttondown HTTP call)
- Affiliate components: `TopPicksBlock`, `ComparisonTable`
- Daily AI article generation pipeline (GitHub Actions, 3 AM EST)

## What's broken on Azure (works locally)

- **Admin panel CRUD** — depends on SQLite. Use local Visual Studio dev for content edits, then commit/push to deploy.
- **Tradition submissions** — currently `Console.WriteLine` instead of DB write.
- **Search performance** — greps files instead of indexed DB query. Fine at 20 articles, will need attention at 100+.

**Why:** Azure F1 Free Windows tier crashes the CLR worker when SQLitePCLRaw tries to load `e_sqlite3.dll` (32-bit native init bug). `Program.cs` wraps `EnsureCreatedAsync` in try/catch and registers `JsonContentService` instead of `EfContentService` so the request path never touches SQLite. Revert that one-line registration when on B1+.

## Architecture quick reference

**Stack:** Blazor Web App (.NET 10), SQLite + EF Core, Markdig for MD→HTML.

**Content data model:**
- `wwwroot/data/content.json` — card metadata only (title, slug, image, category, affiliate URL). No article body.
- `wwwroot/data/articles/{slug}.md` — individual article body. Markdig converts MD→HTML at runtime.
- To swap article content: edit the `.md` file. To swap title/image: edit `content.json`.
- `gift-guides.json`, `recipes.json`, `products.json` — separate data files for those sections.

**Services:**
- `IContentService` → `JsonContentService` (Azure) or `EfContentService` (local). Both implement `GetCardsAsync`, `GetCardBySlugAsync`, `SearchAsync`, `SubmitTraditionAsync`.
- `IGiftGuideService`, `IRecipeService`, `IShopService` — JSON-backed singletons.
- `INewsletterService` → `ButtondownNewsletterService` (HTTP).
- `AdminAuthService` — cookie auth, `admin` / `fireside2026`, 8-hour sessions, `/admin` route.

**Design tokens:** Primary `#002f19` (forest green), Secondary `#b51a1b` (Christmas red), Background `#fff8ef` (warm cream). Noto Serif headlines, Plus Jakarta Sans body. Form focus rings use secondary (red). Footer tree icon uses sage `#7ab590`.

**Design source of truth:** `C:\Users\heads\OneDrive\Desktop\the_fireside_editorial_gallery_updated\code.html` (NOT the PNG). Stitch project ID `3833952611689320369`.

## AI article pipeline

- **Trigger:** GitHub Actions cron, daily at 8 AM UTC (3 AM EST).
- **Models:** `stepfun/step-3.5-flash:free` (text), `google/gemini-2.5-flash-image` (Nano Banana, image).
- **Script:** `scripts/generate_article.py`. Picks unused topic from `TOPIC_POOLS`, generates article + metadata as JSON, writes `.md` + appends to `content.json`, generates image PNG, commits as "Fireside Bot".
- **Image fallback (hardened):** If image generation returns empty, retries once after 5s; if still fails, `get_fallback_image()` reuses a random existing PNG from `wwwroot/images/articles/` instead of an external URL. Logs `IMAGE FALLBACK USED:` line on failure.
- **DbSeeder:** Incremental slug-based sync (NOT empty-only guard) — runs on local startup to keep DB in sync with `content.json`.
- **Status:** 20 articles total (9 original + 11 AI-generated). Pipeline running cleanly since Mar 31.

## Azure resources

- **Resource group:** `fireside-rg`
- **App Service Plan:** `fireside-plan` — F1 Free, **Windows**, Central US (Linux F1 hits quota limits on new accounts)
- **Web App:** `firesideeditorial`
- **Subscription:** `Azure subscription 1` (`headshotsvol7@outlook.com`)
- **Deploy workflow:** `.github/workflows/deploy.yml` — self-contained `win-x86` publish (F1 = 32-bit only) → `azure/webapps-deploy@v3`
- **GitHub secrets set:** `AZURE_WEBAPP_PUBLISH_PROFILE`, `OPENROUTER_API_KEY`

## Active placeholders (need replacing before launch)

- `YOUR-TAG-20` — Amazon Associates tracking tag, throughout `content.json`
- `YOUR-PRODUCT-ID` — Gumroad product ID, in shop integration
- Buttondown API key — newsletter sends won't actually go through until set

All three are blocked on the same thing: domain purchase → account signups → real credentials.

## Next steps (when CT is ready to launch publicly)

1. **Upgrade `fireside-plan` from F1 to B1 Basic** (~$13/mo) — required for free managed SSL on a custom domain. Run: `az appservice plan update --resource-group fireside-rg --name fireside-plan --sku B1`
2. **Revert one line in `Program.cs`:** change `IContentService` registration back from `JsonContentService` → `EfContentService`. Admin panel + DB-backed search work again.
3. **Buy `firesideeditorial.com`** on Spaceship (~$8.88 first year, ~$11/yr renewal).
4. **Bind custom domain in Azure:** add hostname → CNAME + TXT verification → create managed cert (B1+ only) → bind SSL.
5. **Sign up for Amazon Associates** using the live domain → replace `YOUR-TAG-20` throughout `content.json`.
6. **Sign up for Gumroad** → create first digital product → replace `YOUR-PRODUCT-ID`.
7. **Set Buttondown API key** as appsettings/env var → newsletter goes live.
8. **Phase 7:** SEO meta + OG tags per page, JSON-LD, image optimization (WebP + lazy load), Lighthouse 90+, ARIA audit.
9. **Remaining revenue features:** email drip sequences (Buttondown), Advent Countdown interactive component.

## Hard-won gotchas (don't relearn these)

**Azure F1 Free tier:**
1. Windows F1 = 32-bit only. Publish self-contained as `win-x86`, not `win-x64`, or get HTTP 500.32 ANCM dll load error.
2. SCM basic publishing credentials policy is OFF by default. Fresh publish profiles get rejected as "invalid" until you run: `az resource update --resource-type basicPublishingCredentialsPolicies --set properties.allow=true` for both `scm` and `ftp` children of the web app.
3. SQLite native init crashes the CLR worker on F1+win-x86 (`SQLitePCL.Batteries_V2.Init` fails inside `e_sqlite3.dll`). Fall back to JSON/files or upgrade to B1.
4. Custom domain binding **does** work on F1 (Microsoft's older docs are wrong). But free managed SSL certificates do **not** — that requires B1+. Verified via `az webapp config ssl create`.
5. Linux F1 hits quota limits on new accounts. Stick with Windows.

**Local dev workflow:**
- CT runs project from Visual Studio, NOT `dotnet run` from CLI.
- Use `dotnet build` for verification only. Skip the build entirely if VS has the project open (go straight to git commit + push — the GitHub Actions deploy does its own publish).
- If CLI build fails with file lock errors: `taskkill /f /im FiresideEditorial.exe` + 3-second sleep first.

**Cmd quoting:** Spaces in `git commit -m "..."` messages get split into pathspec args by cmd. Use single-word commit messages (`UseJsonContentServiceOnAzure`) or run from PowerShell/git bash.

**`desktop-commander` patterns:** `edit_block` requires exact whitespace match — read target section first before editing. After Azure CLI install via winget, refresh PATH in same session: `$env:Path = [System.Environment]::GetEnvironmentVariable("Path","Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path","User")`.

## Monetization reality check

- Amazon Associates commissions are low (1–4%; a $30 product ≈ $0.90). Real money comes from stacking: digital products (100% margin) + affiliates + newsletter + eventual display ads.
- Q4 alignment is a strategic asset — the entire content library is relevant exactly when holiday purchase intent peaks.
- Mediavine qualification = 50k sessions/month, then ~$900–1,500/mo at that scale.
- Higher-margin affiliate opportunities (Etsy, direct brand partnerships) supplement Amazon's low rates.
- Family member's existing Amazon affiliate sales groups = early traffic channel for the 3-qualifying-sale Amazon Associates requirement.

## Working principles

- CT reviews structured plans and gives explicit greenlight before execution.
- CT is hands-off on development — Claude makes architectural decisions independently. CT intervenes on visual/UI issues.
- Minimal diffs only. Change only requested lines. Never rewrite what wasn't asked.
- Always `git commit` + `git push` after significant feature work. Use descriptive commit messages with phase/feature labels (subject to cmd quoting workaround above).
- Update this `HANDOFF.md` as part of any significant change so future sessions stay current.

---

**Last updated:** 2026-04-07 — End of Phase 6 deploy session. Site live on F1, paused before B1 upgrade.
