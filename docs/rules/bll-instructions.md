# Business Logic (BLL) Rules

Applies to: `HAES.Portal.Training.BLL/**/*`

*Adapted for the Training App from the SES.AffiliateHub BLL rules. The affiliate-network "ingest connector" pattern is retained structurally — it's a genuinely good fit — but repointed at the Training App's own external services: Zoom, Razorpay, the configurable quiz-generation LLM provider, and GitHub (lab/capstone auto-grading).*

## Responsibilities

- Contain all business rules, validations, and workflows for the Training App — course/module/session structure, enrollment + batch capacity, the day-by-day `SessionProgress` unlock state machine, quiz-bank generation and quiz-time sampling/scoring, GitHub lab/capstone grading, attendance recording and the 80% certificate gate, certificate issuance/QR/revocation, and receipts/reporting.
- Accept and return DTOs or domain models defined in `HAES.Portal.Training.Common`.
- Coordinate work across repositories from DAL but do not depend on UI frameworks or web-specific types.

## Naming conventions

> **Canonical naming & OOP rules live in `naming-and-oop-instructions.md`.** That file governs class/file/folder naming, the no-repetition rules, and the OOP/SOLID principles for the whole solution. The points below are the layer-specific additions to it.


- Use PascalCase for service classes, public methods, and properties (e.g. `IEnrollmentService`, `ISessionProgressService`, `IQuizEngineService`, `ICertificateService`, `IReceiptService`, `IAttendanceService`).
- Prefix interfaces with `I`.
- Use camelCase for method parameters and local variables.
- Use the `Async` suffix for async methods.

## Validation & async

- Enforce core business validation rules here, even if controllers or the Vue frontend already validate — e.g. the pass-threshold/attendance-threshold checks, seat-capacity/hold logic, quiz-retry rules, and certificate-revocation reason requirements all live in BLL, not the API layer.
- Centralize complex validation so higher layers (API, frontend) remain simple and consistent.
- Expose async methods for operations that hit DAL or external systems and use async/await end-to-end, with a `CancellationToken` parameter propagated all the way down. Never `.Result`, `.Wait()`, or `.GetAwaiter().GetResult()`.

## Dependencies & DI

- Depend only on DAL abstractions (interfaces) and Common types; do not reference UI or EF Core concrete types.
- All services should be registered in DI and injected via constructors; avoid manually instantiating dependencies with `new`.
- Design methods and services to be easily unit-testable with mocked DAL interfaces.

## External service integrations

*(Replaces the affiliate-hub "External network connectors (ingest)" section — same architectural pattern, applied to this platform's real external dependencies: Zoom, Razorpay, the quiz-generation LLM provider, and GitHub.)*

- One connector class per external service, behind its own interface — this interface is the only place that service's own vocabulary/API shape is allowed to leak into:
  - `IZoomClient` — meeting/recurring-meeting creation and updates, registrant registration, host start-URL lookup, cloud-recording pull (`architecture-plan.md` §3.6a, `batch-scheduling-and-zoom-provisioning.md`).
  - `IRazorpayClient` — order creation and webhook payload verification (`architecture-plan.md` §3.1).
  - `IQuestionGenProvider` — quiz-bank generation and the automated review pass, with **two DI-registered implementations selected by the `QuizEngine:Mode` appsettings switch** (`LocalModelQuestionGenerator` / `ApiKeyQuestionGenerator`, the latter parameterized for Anthropic or OpenRouter per `live-class-attendance-question-workflow.md` §6) — registration picks the implementation from configuration, never a hardcoded call, and there is no runtime auto-fallback between providers if the configured one is unreachable (retry-with-backoff instead, then flag the pool as needing attention).
  - `IGitHubGradingClient` — triggering the lab/capstone auto-grading workflow (GitHub Actions/webhook) and retrieving the code-review agent's structured result (`architecture-plan.md` §3.4/§3.5, `capstone-grading-research.md`).
- Use a typed `HttpClient` per connector, registered via DI with resilience (retry/backoff, timeout) configured at registration — never `new HttpClient()` inside a connector. The local-model quiz provider's client needs its own timeout/retry policy distinct from the hosted API-key provider's, since a slow/unreachable self-hosted inference server should degrade to a retried background job, not hang a Hangfire worker thread.
- Deserialize into that service's external models (`Common/ExternalModels/<Service>/`, e.g. `ExternalModels/Zoom/`, `ExternalModels/Razorpay/`, `ExternalModels/LlmProvider/`, `ExternalModels/GitHub/`) and map to canonical DTOs/domain models **inside the connector**. External models must never be returned from a connector method or leak into BLL services that consume it.
- All background-job orchestration — Hangfire jobs for Zoom registration-on-unlock, recording pull-and-rehost, quiz-bank generation/replenishment, report-card generation, certificate/receipt PDF rendering, and GitHub lab-grading triggers — lives here in BLL, scheduled and coordinated from one place, not scattered across controllers or a separate orchestrator service.
- Webhook receivers (Razorpay payment confirmation, Zoom `participant_joined`/`participant_left`/`meeting.ended`) must verify the sender's signature before processing, and must be idempotent (dedupe on the provider's event/payment id) so a retried delivery can never double-unlock a day, double-charge, double-register, or create a duplicate `Receipt`/`AttendanceRecord`.
- Logging: identifiers (UserId, CourseId, BatchId, SessionId, EnrollmentId), date ranges, and status codes only. Never log a Zoom/Razorpay/LLM-provider/GitHub API key or secret, a webhook signing secret, a URL containing a key/token, a JWT, an OTP code, or PII (student email, phone, payment details).
