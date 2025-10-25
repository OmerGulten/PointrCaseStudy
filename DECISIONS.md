1. Architecture & Design
**DIP: IUnitOfWork and IPageRepository interfaces reside in the Application layer, ensuring independence from Infrastructure.
**DTO: API responses utilize DTOs to prevent circular reference errors during serialization and decouple the external contract from domain entities.
**Concerns: The API layer translates service exceptions (KeyNotFoundException, DbConflictException) into HTTP results (404, 409, etc.).

2. Concurrency & Data Integrity
**Atomicity: Archiving and Publishing are executed within a single database transaction with IUnitOfWork.
**Concurrency: A retry loop attempts the operation once after a DbUpdateConcurrencyException (returns 409 conflict).

3. Testing
**Strategy: Integrated Testing (WebApplicationFactory) is used to validate the entire API-to-DB pipeline. A dedicated PostgreSQL database ensures the concurrency mechanism works reliably.