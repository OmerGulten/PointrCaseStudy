# Case Study Solution

This project provides a comprehensive solution for the Case Study, implementing a Clean Architecture with a focus on domain logic, concurrency handling, and robust integration testing.

## 1. Project Overview & Architecture

The solution is built using **.NET 8** and strictly adheres to the **Clean Architecture** principles, focusing on separating business logic from technical implementation details (Infrastructure).

* **`Pointr.API`**: The entry point, utilizing **Minimal APIs** for defining endpoints and handling HTTP concerns (serialization, response mapping).
* **`Pointr.Application`**: Contains the business logic interfaces (`IPageService`), core services (`PageService`), and use cases.
* **`Pointr.Domain`**: Defines the core entities (`Page`, `PageDraft`, `PagePublished`) and domain-specific configurations.
* **`Pointr.Infrastructure`**: Contains the persistence implementation using **EF Core** and **PostgreSQL** (`ApplicationDbContext`, `PageRepository`, `UnitOfWork`).
* **`Pointr.Tests`**: Integration tests using `xUnit` and `WebApplicationFactory`.

## 2. Technical Decisions & Features

The key architectural and technical decisions made during the implementation are summarized in the **`DECISIONS.md`** file, specifically covering:

* **Dependency Inversion Principle (DIP):** Strict adherence to separating domain logic from persistence details.
* **Concurrency Control:** Implementation of **Optimistic Concurrency** using a custom `RowVersion` field management for PostgreSQL and a **retry mechanism** to ensure atomicity.
* **Idempotency:** The `DELETE` endpoint is idempotent, ensuring repeat calls yield the same `204 NoContent` result without altering the final state.
* **Data Transfer Objects (DTOs):** DTOs are used at the API boundary to prevent serialization issues (circular references) and maintain API contract integrity.

## 3. Setup and Running Locally

### Prerequisites

* .NET 8 SDK
* Docker (Recommended for PostgreSQL setup) or a local PostgreSQL installation.

### 3.1. Database Setup (PostgreSQL)

The application and tests require a running PostgreSQL instance.

1.  **Run PostgreSQL via Docker (Recommended):**
    ```bash
    docker run --name casestudypointr -e POSTGRES_USER=postgrePointr -e POSTGRES_PASSWORD=postgrePointr123 -e POSTGRES_DB=CaseStudyPointr -p 5432:5432 -d postgres:latest
    ```
2.  **Connection Strings:**
    * **Main App:** Set the `ConnectionStrings:DefaultConnection` via **User Secrets** for the `Pointr.API` project.

### 3.2. Run the API

1.  Navigate to the solution root.
2.  Run the application:
    ```bash
    dotnet run --project Pointr.API
    ```
    The API will typically start on `http://localhost:5211` (or the configured port).

## 4. Testing

Integration tests cover the full stack, including database interactions and the critical concurrency logic.

1.  Ensure the PostgreSQL container is running.
2.  Run all tests from the solution root:
    ```bash
    dotnet test
    ```

> **Note on Concurrency Test:** The `ConcurrencyTest` is designed to aggressively test the retry and conflict logic, requiring a dedicated, non-in-memory database (PostgreSQL) to accurately validate the `409 Conflict` outcome.

---

**Thank you for reviewing my solution.**
