# Memory Leak Examples in .NET

Practical demonstrations of common memory leak patterns in .NET and how to fix them. Source code lives in `MemoryLeakExamples.cs`.

## Scenarios and Fixes

### 1) Event handler leak
- **Problem:** Subscribers register to publisher events and never unsubscribe, so the publisher holds strong references and prevents GC.
- **Fixes:** Implement `IDisposable` and unsubscribe; use `WeakEventManager` in WPF; prefer local event scopes with `using` or `try/finally` unsubscription.

### 2) Static collection leak
- **Problem:** Static lists/dictionaries grow forever, keeping all items alive for the app lifetime.
- **Fixes:** Add eviction (LRU/TTL), cap collection size, clear on lifecycle boundaries, avoid static when instance lifetime is sufficient.

### 3) Timer not disposed
- **Problem:** `Timer` holds references to the owning object; without disposing, the owner stays alive and callbacks keep firing.
- **Fixes:** Stop and dispose timers (implement `IDisposable`); use `using` or `await using` with `PeriodicTimer`; unregister callbacks before dispose.

### 4) Closure capturing large objects
- **Problem:** Lambdas capture large objects, so delegates keep them alive even after the outer scope ends.
- **Fixes:** Avoid capturing large state; pass minimal arguments into the lambda; null out or dispose captured resources after use.

### 5) UI service subscriptions (WPF/WinForms)
- **Problem:** Windows or views subscribe to long-lived services and never unsubscribe, so closed windows remain in memory.
- **Fixes:** Unsubscribe on close/unload; use weak events; centralize subscription management (e.g., `CompositeDisposable`).

## Quick Checklist
- Unsubscribe from events on dispose/close.
- Dispose timers, streams, and any `IDisposable` resources.
- Avoid unbounded static caches; enforce eviction.
- Be mindful of closures capturing large objects.
- Prefer weak events for long-lived publisher/short-lived subscriber pairs.
