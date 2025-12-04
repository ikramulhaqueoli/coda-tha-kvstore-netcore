# KvStore

Key-value service built for the Coda take-home assignment. Part 1 delivers the
single-node API, while Part 2 introduces a multi-node router that scales reads
and writes horizontally.

## Project layout

- `KvStore.Api` – original HTTP API that keeps data in-memory. Handles
  optimistic locking, JSON patch semantics, and per-key serialization.
- `KvStore.Router` – stateless gateway that sits in front of the StatefulSet and
  proxies the public API. Requests are partitioned deterministically across
  pods and `GET /kv` returns NDJSON describing every key/node pair.
- `KvStore.Core`, `KvStore.Infrastructure` – shared domain logic plus the
  thread-safe in-memory repository.
- `KvStore.Tests`, `KvStore.E2eTest` – unit & end-to-end tests (E2E includes the
  required concurrent-counter scenario).

## Running locally

```bash
dotnet run --project KvStore.Api
dotnet run --project KvStore.Router
```

The router ships with two default configurations:

- `appsettings.Development.json` points at `localhost:7000` so you can run a
  single API instance on your machine.
- `appsettings.json` lists the five pod DNS names the StatefulSet produces in
  Kubernetes (`pod-0` … `pod-4`). Update the hostnames if you run in a different
  namespace or scale the StatefulSet beyond five replicas.

## Part 2 design (Router)

- **Deterministic sharding** – a SHA-256 hash of the key modulo the node count
  guarantees that every key is routed to the same pod (and different keys are
  spread evenly). Pods are described through configuration, so scaling out only
  requires updating the node list.
- **Optimistic forwarding** – the router mirrors the Part 1 API surface. `PUT`
  and `PATCH` send payloads to the selected pod and bubble up 400/404/409/500
  responses untouched, so existing clients keep their semantics.
- **Key aggregation** – `GET /kv` fans out to every pod, gathers its key list,
  and responds with NDJSON lines (`{"key":"foo","node":"pod-1"}`), as required.
- **Failure isolation** – if a pod is unreachable the router returns `503` and
  identifies the failed node to make debugging easier.

## Kubernetes deployment

StatefulSet (5 replicas) + headless service manifests live in
`deploy/k8s/part-2/KvStore-statefulset-spec.yaml`. The router deployment and
ClusterIP service are defined in
`deploy/k8s/part-2/KvStore-router-deployment.yaml`.

```
kubectl apply -f deploy/k8s/part-2/KvStore-statefulset-spec.yaml
kubectl apply -f deploy/k8s/part-2/KvStore-router-deployment.yaml
```

Build and publish the router image before applying the deployment, or update the
`image:` reference to the registry/tag you use.

## Testing

```
dotnet test
```

- `KvStore.E2eTest` contains high-concurrency coverage (500 parallel PUTs on the
  same key) along with the multi-key and happy-path flows.
- A dedicated test now verifies the single-node `GET /kv` list endpoint so the
  router can aggregate keys confidently.

To manually reproduce the “three clients incrementing the same counter to 300”
scenario described in the assignment
([PDF](file://_BE Engineering (2025) KV Store (5) (1) (1).pdf)), run the API and
issue `PATCH` requests from three concurrent shells (or use the E2E test which
exercises an even stricter parallel workload).