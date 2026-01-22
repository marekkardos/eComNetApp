# REST API Design Review Skill

## HTTP Methods
- GET: read (idempotent, cacheable)
- POST: create or actions
- PUT: full replace (idempotent)
- PATCH: partial update
- DELETE: remove (idempotent)

## Status Codes
- 200 OK: success with body
- 201 Created: resource created (with Location header)
- 204 No Content: success, no body
- 400 Bad Request: client error, validation failure
- 401 Unauthorized: authentication required
- 403 Forbidden: authenticated but not allowed
- 404 Not Found: resource doesn't exist
- 409 Conflict: state conflict
- 422 Unprocessable Entity: semantic error
- 500 Internal Server Error: server fault

## Response Format
- Consistent envelope (or no envelope, but consistent)
- Error responses include code, message, details
- Pagination metadata (total, page, pageSize)
- HATEOAS links if applicable

## Naming Conventions
- Plural nouns for collections (/users not /user)
- Nested resources for relationships (/users/{id}/orders)
- Query params for filtering, sorting, pagination
- No verbs in URLs (POST /users not POST /createUser)

## Versioning
- URL path (/v1/users) - most common
- Header (Accept-Version) - cleaner but less visible
- Consistent across all endpoints

## Documentation
- OpenAPI/Swagger accuracy
- Examples for complex payloads
- Error response documentation
- Authentication requirements clear
