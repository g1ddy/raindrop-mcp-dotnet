# Deterministic Collection Suggestions

## Purpose

The `suggest_collection_for_bookmark` workflow historically used MCP Sampling to ask the client model to rank collection titles. Sampling is deprecated as of MCP specification version 2026-07-28, varies in availability between clients, and makes ranking difficult to reproduce or evaluate.

This document describes the migration to a deterministic, server-owned classifier. The goal is to rank up to three collections from bookmark and library metadata, keep the existing user confirmation step, and avoid generative inference.

## Roadmap

### Phase 1: deterministic lexical and structured signals — implemented

Phase 1 builds a per-user in-memory index from the existing bookmark library and ranks collections using three explainable signals:

| Signal | Weight | Description |
| --- | ---: | --- |
| TF-IDF cosine similarity | 60% | Compares the query bookmark with a collection document assembled from its title, description, and historical bookmarks. |
| Tag Jaccard similarity | 20% | Measures overlap between the query's tags and the distinct tags historically used in a collection. |
| Domain affinity | 20% | Measures the fraction of bookmarks from the query domain that belong to a collection. |

Bookmark terms include the title, link, excerpt, note, type, domain, and normalized tags. Tokens are Unicode letter/number sequences, lower-cased invariantly, with one-character tokens omitted. Collection title and description supply useful cold-start terms even when a collection contains no bookmarks.

The index is built lazily on the first suggestion request, cached by Raindrop API token, and shared across concurrent requests. Failed builds are not cached. It is invalidated after successful collection mutations and after a bookmark is moved by the suggestion workflow. Index construction fetches bookmark pages of 50 items and stops at the end of the result set or if an API page repeats only bookmark IDs already observed.

Ranking is exact rather than approximate. Results are ordered by descending score and then collection ID, providing a stable tie-break. Zero-score candidates are omitted; the tool returns an unsuccessful result when no relevant collection can be found. The top three positive candidates continue through MCP Elicitation so that the user, not the classifier, selects the destination.

#### Phase 1 limitations

- The weights are initial defaults rather than values learned from evaluation data.
- Term frequency can over-emphasize repeated generic URL or text tokens.
- One aggregate document can blur collections containing several unrelated topics.
- The index is process-local and must be rebuilt after a restart.
- External bookmark moves do not immediately invalidate the index; a restart or a successful mutation through this server rebuilds it.
- Only top-level collections are candidates, preserving the previous tool behavior.

### Phase 2: optional vector embeddings — implemented

Add an optional `IEmbeddingGenerator<string, Embedding<float>>` implementation through `Microsoft.Extensions.AI`.

1. Format bookmark metadata into a stable canonical string.
2. Generate and normalize one embedding per historical bookmark.
3. Maintain one normalized centroid per collection.
4. Compute exact cosine similarity between a query vector and collection centroids.
5. Blend semantic similarity with the Phase 1 tag, domain, and lexical scores.
6. Pin the model identifier, revision, dimensions, tokenizer/input format, and normalization strategy in the index version.
7. Fall back to Phase 1 when no embedding generator is configured or embedding generation fails.

Before rollout, compare Phase 2 against the Phase 1 baseline. Do not retain embeddings unless top-three recall or accepted-suggestion accuracy improves materially. For privacy-sensitive deployments, evaluate a pinned local ONNX embedding model rather than transmitting bookmark content to a hosted provider.

### Phase 3: advanced retrieval and personalization

Add complexity only when measurements justify it:

- Use two to five centroids for large, heterogeneous collections.
- Optionally rerank using exact K-nearest-neighbor search over historical bookmark vectors.
- Return explanation data such as matching tags, domain history, and representative neighboring bookmarks.
- Persist a versioned index in SQLite or a binary cache.
- Incrementally update statistics and centroids on create, update, move, and delete events.
- Add approximate nearest-neighbor indexing only if exact search becomes a measured bottleneck.
- Learn or tune hybrid weights per user after enough confirmed choices are available.

## Evaluation plan

Treat the current collection of each historical bookmark as its label. When evaluating an item, exclude that item from the training index to prevent leakage.

Prefer a chronological split—older bookmarks for training and newer bookmarks for evaluation—to approximate future suggestions. Track:

- top-1 accuracy;
- top-3 recall;
- mean reciprocal rank;
- coverage above a confidence threshold;
- accuracy at accepted confidence;
- per-collection accuracy to identify large-collection bias;
- user acceptance rate for online suggestions.

Evaluate rules/domain affinity, TF-IDF, embeddings, and the hybrid independently. The more complex phase should be adopted only when it improves the measured result.

## Operational and privacy notes

The Phase 1 index remains in memory and does not add a new persistent copy of bookmark content. It is partitioned using the configured API token as the existing application cache key, but the token is not included in suggestion results. Future persisted or hosted embedding implementations must document retention, encryption, tenant isolation, provider data handling, and index deletion behavior.
