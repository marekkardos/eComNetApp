# Performance Review Skill

## Memory Allocation
- Avoid allocations in hot paths
- StringBuilder for string concatenation in loops
- ArrayPool/MemoryPool for temporary buffers
- Span<T> for slicing without allocation
- Object pooling for expensive-to-create objects

## Async Performance
- Don't await in tight loops unnecessarily
- Task.WhenAll for parallel independent operations
- ConfigureAwait(false) to avoid context capture
- ValueTask for frequently-sync-completing methods

## Caching
- Cache appropriate data (static, expensive to compute)
- Cache invalidation strategy
- Memory vs distributed cache decision
- Cache stampede prevention

## Database
- Index usage (check query plans)
- Pagination for large result sets
- Projection over full entity loading
- Connection pooling
- Batch operations over individual calls

## Network
- Response compression
- HTTP/2 multiplexing
- Connection reuse (HttpClient pooling)
- Appropriate timeouts

## Frontend Specific
- Bundle size analysis
- Lazy loading routes/components
- Virtual scrolling for long lists
- Image optimization
- Debounce user input events

## Measurement
- Don't optimize without profiling
- Identify actual bottlenecks first
- Benchmark before/after changes
