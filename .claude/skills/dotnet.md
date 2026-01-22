# .NET 8 Review Skill

## Async/Await
- Async all the way (no .Result, .Wait(), .GetAwaiter().GetResult())
- ConfigureAwait(false) in library code
- CancellationToken propagation through call chain
- ValueTask for hot paths with sync completion
- Avoid async void except event handlers

## Null Safety
- Nullable reference types enabled and respected
- Appropriate use of null-conditional (?.) and null-coalescing (??)
- Required keyword for non-nullable properties
- Guard clauses vs ArgumentNullException.ThrowIfNull

## Resource Management
- IDisposable/IAsyncDisposable implementation
- Using statements/declarations
- HttpClient via IHttpClientFactory
- No manual new HttpClient()

## Collections
- IEnumerable vs ICollection vs IList intent
- ReadOnlyCollection for immutable returns
- Span<T>/Memory<T> for performance-critical paths
- Array.Empty<T>() vs new T[0]

## Modern C# Features
- Records for DTOs/value objects
- Pattern matching where clearer
- Primary constructors appropriately
- File-scoped namespaces
