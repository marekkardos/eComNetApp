# Entity Framework Core Review Skill

## Query Efficiency
- N+1 detection: loop + query = problem
- Include/ThenInclude for needed relations only
- Select projections over full entity loads
- AsNoTracking for read-only queries
- AsSplitQuery for complex includes if needed

## Performance Patterns
- Compiled queries for hot paths
- Bulk operations via EF Core extensions or raw SQL
- Pagination with Skip/Take (indexed column!)
- Avoid loading entire tables

## Tracking Behavior
- AsNoTracking by default for queries
- Explicit tracking when updates needed
- DetectChanges implications
- ChangeTracker.Clear() for batch operations

## Migrations & Schema
- Proper index definitions
- Foreign key constraints
- Appropriate column types
- Migration naming conventions

## Anti-patterns
- Lazy loading without awareness of N+1
- SaveChanges in loops
- DbContext as singleton
- Business logic in DbContext
- Raw SQL without parameterization

## Repository Pattern
- Generic repository often unnecessary with EF
- DbContext IS the Unit of Work
- Specification pattern for complex queries
- Don't abstract away LINQ unnecessarily
