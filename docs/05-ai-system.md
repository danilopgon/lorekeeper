# 05 — AI System

This document specifies Lorekeeper retrieval and generation. Open settings must be resolved at the relevant roadmap gate.

## Product capability

**Input:** [user data and trusted context]  
**Output:** [proposal/search result/structured result]  
**Human control:** [what the user reviews or decides]  
**Failure mode:** [safe behaviour when evidence/provider is unavailable]

## Provider architecture

Application code depends on project-owned ports. Infrastructure adapts `Microsoft.Extensions.AI`, OpenAI-compatible clients, provider-specific HTTP APIs or a measured local inference implementation behind the same ports.

```text
Use case
├── IChatModel
├── IEmbeddingGenerator
└── IReranker
        ↓
Infrastructure adapters
        ↓
[configured provider or justified local model]
```

Provider selection, model names, endpoints and credentials come from validated configuration. The domain does not know which provider is active.

Local-model libraries such as Hugging Face, SentenceTransformers and PyTorch are implementation options, not baseline dependencies. Prefer using them first in offline experiments; promote them to runtime only when evaluation demonstrates a useful quality/cost/latency trade-off.

## Retrieval pipeline

```text
[knowledge source]
    ↓ source reader
canonical documents
    ↓ semantic chunking
PostgreSQL
    ├── Full-Text Search
    └── pgvector
          ↓
Reciprocal Rank Fusion
    ↓ optional reranker
top K evidence
    ↓ context builder
LLM
    ↓ structured validation
grounded response + sources
```

## Ingestion rules

- Sources are adapters behind a common document contract.
- Preserve headings, paths, entity type, timestamps and ownership as metadata.
- Chunk on semantic boundaries before token-size fallback splitting.
- Track external ID, last-modified time and content hash for idempotent incremental ingestion.
- Rechunk and re-embed only changed documents.
- Deletion from a source has an explicit tombstone/removal policy.

## Retrieval rules

- Scope both lexical and vector candidates by explicit `campaignId` before ranking; the initial installation has one operator, not tenants.
- Run lexical and semantic retrieval independently.
- Fuse rankings with RRF; keep parameters explicit and testable.
- Reranking is optional behind `IReranker`; provider and local implementations must satisfy the same product contract.
- Return source identity and heading path with every selected chunk.
- If evidence is insufficient, say so; do not fill gaps with confident prose.

## Reranking experiments

Before adopting a reranker in the production path, compare at least the relevant retrieval variants on the same committed corpus:

1. lexical FTS;
2. vector retrieval;
3. FTS + vector + RRF;
4. RRF + candidate reranker.

A local cross-encoder implemented through SentenceTransformers/Hugging Face is a valid candidate when it helps answer a measured product question. PyTorch may be used for model loading, device selection, batching and inference; model training is not required by this architecture.

Record quality and operational impact together. At minimum consider MRR/nDCG, p50/p95 latency and resource/cost impact. A statistically or practically negligible ranking gain does not justify adding runtime complexity.

## Prompt and output rules

- Version prompts beside their owning capability.
- Separate system instructions, structured state, retrieved evidence and user input.
- Define context budget, truncation order and maximum output.
- Validate structured output before storage or return.
- Treat retrieved content as data, not instructions.
- Never log secrets or private prompt content by default.

## Evaluation dataset

Store versioned cases under `tests/evals/fixtures/`:

```yaml
id: case-001
question: "[question]"
expected_sources:
  - "[source-id]"
expected_facts:
  - "[fact]"
forbidden_facts:
  - "[unsupported claim]"
```

## Evaluation layers

| Layer | Measures | Suggested metrics |
| --- | --- | --- |
| Lexical/vector retrieval | Evidence found | Recall@K, MRR |
| Fusion/reranking | Ordering quality | nDCG@K, MRR |
| Context builder | Evidence density | Context precision/recall |
| Generation | Grounding and usefulness | Fact support, citation correctness, refusal quality |
| Runtime AI path | Operability | p50/p95 latency, provider/model cost, failure/fallback rate |

Retrieval evals are deterministic CI candidates. Model-graded generation evals run separately when cost or variance makes them unsuitable as a blocking gate.

## Python experimentation workbench

The product eval gate remains owned by the repository and must be reproducible without depending on ad-hoc notebooks. A Python workbench under `ai/` may complement it for ecosystem-specific experiments and analysis.

Suggested baseline if the workbench is introduced:

- `uv` for environment/dependency management;
- Pydantic for explicit experiment/config data;
- pytest for tooling tests;
- Hugging Face / SentenceTransformers for embedding or reranking experiments;
- PyTorch when required by local inference.

Python tooling should consume committed datasets and export reproducible results. It must not silently become a second implementation of the production retrieval policy.

## Observability and cost

Capture provider/model, latency, token or character usage, retrieved source IDs, retry/fallback path and validation failures. Do not store raw private content unless explicitly required and protected.

OpenTelemetry remains the system-wide observability baseline. An LLM-focused platform such as Langfuse may be added for AI-specific traces, prompt/model metadata, evaluation views and cost analysis when it materially improves inspection. It complements OpenTelemetry rather than replacing application tracing.

## Required decisions by block

- **03:** NaN adapter configuration, supported chat/embedding models, dimensions, budgets, timeouts, retries and failure behaviour; verify compatibility before locking settings.
- **04:** Notion root per campaign, supported block types, canonical schema, structural chunking limits, tables, source removal, idempotency and job lifecycle. Preserve knowledge status; unknown is not confirmed canon.
- **Before 05A:** fixed corpus and expected-source questions, including overlapping NPC names in different campaigns. Record baseline results before adding regression thresholds.
- **05A:** FTS language/configuration, ranking, snippet and locator contract, candidate limits.
- **05B:** vector representation/index compatibility, embedding model and candidate limits. Decide whether Python experiments are useful for comparing embedding alternatives; they are not required for the product path.
- **06:** RRF settings, reranker decision, context budget/truncation, citation schema, contradictory/insufficient evidence and response transport. Compare provider/local/no-reranker options with committed evaluation cases before adding runtime complexity. Define whether any structured current-state context is in scope rather than assuming all Lazy Lands features exist here.
- **07:** measured thresholds and tolerances; distinguish frozen reproducible fixtures from live-provider evals. Decide whether an AI-specific observability platform adds sufficient value beyond the OpenTelemetry baseline.

Campaign identity is preserved through all stages; final citation resolution must not fetch another campaign's document.

## Initial workspace/ingestion approach

Entry adapters now start with Markdown/text files and pasted text (04A), followed by Notion (04B). Both feed the canonical pipeline. Retrieval uses only published available source versions; failed/pending updates preserve the previous usable version. See [UX approach](12-workspace-and-ingestion-ux.md); define atomic publication and deletion races before implementation.

## Evidence presentation contract

The adopted design uses conditional canonical-fact, knowledge-asymmetry and information-limit blocks. Show canon or player/GM knowledge differences only when sources support them. Missing mentions do not prove ignorance. Never invent line/folio locators; emit only traceable source locations. Define the precise output schema before block 06; no mandatory three-part answer is implied.
