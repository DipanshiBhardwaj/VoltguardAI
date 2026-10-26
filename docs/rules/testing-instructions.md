# Testing Rules

Applies to: `*.Tests/**/*`

*Adapted for the Training App from the SES.AffiliateHub testing rules.*

## Stack & structure

> **Canonical naming & OOP rules live in `naming-and-oop-instructions.md`.** Test classes follow it too: one test class per class under test, named `{ClassUnderTest}Tests` (`CourseServiceTests`, `CoursesControllerTests`), in a folder structure mirroring the project under test.

- xUnit + Moq + FluentAssertions. Arrange-Act-Assert layout in every test.
- Test name format: `{Method}_{Scenario}_{ExpectedBehavior}`.
- One `*.Tests` project per layer being tested: `HAES.Portal.Training.Api.Tests`, `HAES.Portal.Training.BLL.Tests`, `HAES.Portal.Training.DAL.Tests` — grouped under one "Tests" solution folder in the IDE for a tidy view, but three distinct `.csproj` projects, per team decision.

## Controllers

- Every controller action gets its five tests immediately, not as a follow-up: happy path, not-found, invalid input, service throws → 500, and a `Mock.Verify` that the service was called with the expected arguments.
- Every course-scoped or student-scoped action additionally needs a test proving a `403`/`404` (per the endpoint's own convention) when the caller's role/assignment/ownership doesn't match — e.g. a Teacher not assigned to a course, or a Student requesting another student's `Enrollment`/`Receipt`/`Certificate`.

## Repositories

- Use EF Core InMemory with a fresh database name per test (never a shared/static instance).
- Every repository under test must include a test proving soft-deleted rows (`IsDeleted = true`) are excluded from `GetAllAsync`/equivalent query methods.
- Every repository whose entity is scoped to a `Course`/`Batch` must include a test proving a query scoped to one `Course`/`Batch` never returns another `Course`'s/`Batch`'s rows, alongside the existing soft-delete-exclusion test (this replaces the affiliate-hub's tenant-isolation test — see `dal-instructions.md`).

## External service connectors

*(Replaces the affiliate-hub "Network connectors" section — same rules, applied to this platform's real external services: Zoom, Razorpay, the quiz-generation LLM provider, and GitHub.)*

- No unit test may touch a real network, a real database, or a real Zoom/Razorpay/LLM-provider/GitHub account.
- Connector tests run against captured response fixtures (stored under `tests/Fixtures/<Service>/`, e.g. `tests/Fixtures/Zoom/`, `tests/Fixtures/Razorpay/`) via a mocked `HttpMessageHandler` — never a live HTTP call.
- Cover at minimum: happy path, empty/malformed payload, unknown/unrecognized status value, non-2xx response, timeout, and a `429`/rate-limit response triggering retry/backoff. For the Zoom and Razorpay webhook receivers specifically, also cover: an invalid/missing signature (must reject), and a duplicate delivery of the same event/payment id (must be a no-op the second time — idempotency).
- The two `IQuestionGenProvider` implementations (`LocalModelQuestionGenerator`, `ApiKeyQuestionGenerator`) each get their own fixture-backed test suite; a separate test proves the DI registration picks the correct implementation for each `QuizEngine:Mode` value.

## General

- No `Thread.Sleep`/`Task.Delay` in tests. No interdependent tests (each test must be runnable in isolation and in any order). No test without at least one assertion.
- Coverage targets: controllers 90% (minimum 80%), services 85% (minimum 75%), repositories 80% (minimum 70%).
