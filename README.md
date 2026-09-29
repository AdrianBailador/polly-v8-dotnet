# Polly v8 Rewrote Itself From Scratch. Here's What Actually Changed

Companion sample for the blog post [Polly v8 Rewrote Itself From Scratch. Here's What Actually Changed](https://adrianbailador.github.io/blog/72-polly-v8-dotnet/).

Three real resilience pipelines built on Polly v8's `ResiliencePipeline` model, each tested without waiting for real retry delays or real timeouts wherever that's actually possible.

## Structure

| File | What it shows |
|---|---|
| `src/RetryDemo.cs` | A retry pipeline wired to an injected `TimeProvider` |
| `tests/RetryDemoTests.cs` | Two seconds of simulated backoff, asserted in milliseconds of real test time via `FakeTimeProvider` |
| `src/CircuitBreakerDemo.cs` | A circuit breaker exposing `CircuitBreakerStateProvider` so state (`Closed` / `Open` / `HalfOpen`) is asserted directly |
| `tests/CircuitBreakerDemoTests.cs` | Opens the circuit after a failure threshold, then recovers to `Closed` after fast-forwarding past `BreakDuration` with no real waiting |
| `src/OrderingDemo.cs` | The same two strategies (Retry, Timeout), built in both orders |
| `tests/OrderingDemoTests.cs` | Proves the behavioral difference: per-attempt timeout budget vs. one deadline over the whole retry sequence |

## Running it

```bash
dotnet test
```

All 6 tests pass. Run five times in a row while writing this and the suite finished in 206-210ms every time.

## Requirements

- .NET 10 SDK

## Package versions used

- `Polly.Core` 8.8.0
- `Microsoft.Extensions.TimeProvider.Testing` 10.10.0 (namespace `Microsoft.Extensions.Time.Testing`, different from the package id)

## Notes

- `RetryDemoTests` and `CircuitBreakerDemoTests` use `FakeTimeProvider` throughout: no real `Task.Delay` longer than the 10ms yield needed to let a pipeline's continuation register its timer before the test fast-forwards past it.
- `OrderingDemoTests` deliberately does **not** use `FakeTimeProvider`. A timeout racing an in-flight operation is a genuinely hard thing to fake convincingly, because the operation's own completion has to be racing the *same* clock the timeout strategy reads. Those two tests use small real delays (tens of milliseconds) instead, which keeps the whole suite fast without pretending the timing is fully deterministic.

## License

MIT — do whatever you want with it.
