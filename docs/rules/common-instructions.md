# Common Types & DTO Rules

Applies to: `HAES.Portal.Training.Common/**/*`

*Adapted for the Training App from the SES.AffiliateHub Common rules.*

## Purpose

- Provide shared DTOs, enums, and cross-cutting utilities used by API, BLL, and DAL (and mirrored, where useful, by the Vue frontend's own TypeScript types).
- Centralize contracts and shared types so they are consistent between layers — `CourseDto`, `BatchDto`, `SessionDto`, `EnrollmentDto`, `SessionProgressDto`, `QuizAttemptDto`, `LabSubmissionDto`, `AttendanceRecordDto`, `CertificateDto`, `ReceiptDto`, and the enums behind them (`SessionProgressStatus`, `PaymentStatus`, `EnrollmentMode`, `AttendanceSource`, `ValidationStatus`, `QuizEngineProvider`, etc.).

## Constraints

- Do not reference UI frameworks, ASP.NET-specific types, or EF Core types here.
- DTOs must be serialization-friendly and stable for API contracts (the Vue frontend and Swagger both depend on this shape staying predictable).
- Use enums and constants for well-known values shared across layers.

## Naming conventions

> **Canonical naming & OOP rules live in `naming-and-oop-instructions.md`.** That file governs class/file/folder naming, the no-repetition rules, and the OOP/SOLID principles for the whole solution. The points below are the layer-specific additions to it.


- Use PascalCase for DTOs, enums, and public properties.
- DTOs may use the `Dto` suffix when helpful (for example, `CourseDto`, `CertificateDto`).
- Use camelCase for parameters and local variables in helper methods.

## DTO shape

- Prefer immutable DTOs: `{ get; init; }` properties rather than mutable setters.
- Use nullable types for genuinely optional fields — never a sentinel value (empty string, `-1`, magic constant) to mean "absent" (e.g. `LiveClassInfo.RecordingBlobUrl` is `null` until the recording job populates it, never an empty string).
- Use `DateTimeOffset` for any point-in-time value (`EnrolledOn`, `IssuedOn`, `ScheduledStartOn`); use `DateOnly` for calendar-day-only values (`CompletionDay`) — this must match the entity's own `*On`/`*Day` distinction in DAL (see `dal-instructions.md`).
- XML doc comments on every public type and public member — these DTOs are the contract other layers (and Swagger, and the Vue frontend's generated/hand-mirrored types) read.

## Response shape

- There is no generic `ResponseModel`/envelope type in this solution. API responses are the typed DTO directly (via `ActionResult<T>`); do not add an `IsSuccess`/`Message`/`Data` wrapper type here.

## External service models

*(Replaces the affiliate-hub "External network models" section — same pattern, applied to this platform's actual external services rather than an affiliate network.)*

- Service-specific request/response shapes live under `ExternalModels/<Service>/` — for example `ExternalModels/Zoom/ZoomMeetingResponse.cs`, `ExternalModels/Zoom/ZoomRegistrantResponse.cs`, `ExternalModels/Razorpay/RazorpayOrderResponse.cs`, `ExternalModels/Razorpay/RazorpayWebhookPayload.cs`, `ExternalModels/LlmProvider/QuestionGenerationRequest.cs`, `ExternalModels/GitHub/GradingWorkflowResult.cs` — not mixed in with the canonical DTOs.
- These external models exist only to deserialize a service's raw request/response. They are mapped to canonical DTOs/domain models inside the BLL connector that owns that integration (`bll-instructions.md`) and must never be referenced outside of that integration's own code.

## Mapping & helpers

- Place mapping helpers and extension methods here if they are reused across multiple layers (e.g. a `SessionProgressStatus` → UI-friendly label mapper, if the frontend consumes it via a shared endpoint rather than duplicating the mapping in Vue).
- Keep helpers pure and side-effect free where possible to make them easy to test and reuse.
