---
title: Source Summaries Index
category: meta
created: {DATE}
updated: {DATE}
---

# Source Summaries

One page per ingested source (GDD chapter, meeting note, playtest report, reference deck). Each summary captures: provenance, a short abstract, key claims (link to [[claims]]), and which wiki pages were created/updated from it.

## Page filename

`<source-slug>.md` — kebab-case from the source's title or filename.

## Page template

```markdown
---
title: <Source title>
category: sources
tags: []
source_path: raw/gdd/<file>     # source-summary pages use this instead of the standard `sources:` list (they describe exactly one source)
source_type: gdd | spec | meeting | playtest | reference | technical
date: YYYY-MM-DD                # date OF the source, not ingest date
authors: [optional]
created: YYYY-MM-DD              # ingest date (same as `ingested` below)
ingested: YYYY-MM-DD
updated: YYYY-MM-DD
---

# <Source title>

## Abstract
2-5 sentences in your own words. No quotes longer than 15 words.

## Key claims
- [[claims#c-YYYYMMDD-01]] — short restatement
- [[claims#c-YYYYMMDD-02]] — short restatement

## Pages updated from this source
- [[systems/...]]
- [[entities/...]]
- [[decisions/...]]

## Open questions raised
- [[open-questions#q-YYYYMMDD-01]]

## Notes
Free text — gotchas, parts skipped, sections requiring re-read.
```
