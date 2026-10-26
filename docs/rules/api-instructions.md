# API (ASP.NET Core Web API) Rules

Applies to: `HAES.Portal.Training.API/**/*`

*Adapted for the Training App from the SES.AffiliateHub API rules. Anywhere the original referred to affiliate/tenant concepts, this version replaces them with the Training App's own roles (Admin / Teacher / Student) and course-scoping model, per `architecture-plan.md`.*

## Responsibilities & layering

- Keep controllers thin: parameter binding, basic validation, and delegating to BLL services.
- Use DTOs from `HAES.Portal.Training.Common` for all request/response models; never expose EF entities.
- Use attribute routing and versioned routes: `[Route("api/v1/[controller]")]`. Controllers are named in the plural and map to the platform's actual resources, e.g. `CoursesController`, `BatchesController`, `SessionsController`, `EnrollmentsController`, `QuizAttemptsController`, `LabSubmissionsController`, `AttendanceRecordsController`, `CertificatesController`, `ReceiptsController`, `TrainerAssignmentsController`.

## Naming conventions

> **Canonical naming & OOP rules live in `naming-and-oop-instructions.md`.** That file governs class/file/folder naming, the no-repetition rules, and the OOP/SOLID principles for the whole solution. The points below are the layer-specific additions to it.


- Follow Microsoft .NET naming guidelines:
  - PascalCase for controllers, methods, and public properties.
  - camelCase for local variables and parameters.
  - Interfaces prefixed with `I`.
- Controller classes end with `Controller`.
- DTO types end with `Dto` when appropriate.
- Async methods use the `Async` suffix.

## Response shape — no envelope

- Controller actions return `ActionResult<T>` with the DTO/data directly. There is no `ResponseModel`/`IsSuccess`/`Data` wrapper in this solution — flat, agent- and frontend-consumable JSON that the Vue app (and any future integration) can consume without unwrapping a generic envelope first.
- Every action declares its possible outcomes with `[ProducesResponseType]` per status code, has XML doc comments, and shows up correctly in Swagger/OpenAPI (`/swagger/v1/swagger.json`).
- Every action is `async`, uses the `Async` suffix, and takes a `CancellationToken cancellationToken = default` parameter that is propagated to every awaited call.

## Validation & errors

- Validate incoming requests using model validation (attributes, fluent validation, or BLL validation) before executing business logic.
- Never trust client data; always enforce server-side validation, even if the Vue frontend validates input (e.g. a GitHub repo URL format, a quiz submission's option indices, a batch's capacity numbers).
- Errors are RFC 7807 `ProblemDetails` (validation failures: `ValidationProblemDetails`), returned by global exception middleware — never a hand-built error shape in a controller, never a stack trace or raw exception message in the response body.
- The one exception, by design, is the public certificate-verification endpoint (`GET /api/v1/verify/{certificateNumber}`) and any other explicitly public endpoint: an unrecognized/malformed token still returns a plain, deliberate "not found" shape — never a fabricated success and never an internal error leaking implementation detail.

## Auth & role/course scoping

*(Replaces the affiliate-hub "Auth & tenancy" section — this solution is single-deployment, not multi-tenant, per `architecture-plan.md` §0/§6, so there is no `CompanyId`/tenant concept anywhere in the API.)*

- `[Authorize]` from the first commit on every controller/action — no endpoint ships without it, even during early development. Use ASP.NET Core **policy-based authorization** (e.g. `[Authorize(Policy = "Admin")]`, `[Authorize(Policy = "TeacherOrAdmin")]`, `[Authorize(Policy = "AssignedTrainerOrAdmin")]`) rather than ad hoc `Roles = "..."` string checks scattered across controllers, so the roles & permissions table in `architecture-plan.md` §4 maps 1:1 to enforceable policies.
- The public certificate-verification endpoint (`GET /api/v1/verify/{certificateNumber}`) and the public course catalog read endpoint are the only endpoints that may be anonymous (`[AllowAnonymous]`) — both are documented exceptions, not a default.
- Role (Admin / Teacher / Student) is resolved server-side from the authenticated principal's JWT claims (ASP.NET Core Identity + JWT, per `architecture-plan.md` §1) — never accept a role, "isAdmin" flag, or permission level as a request body/query field the caller controls.
- A Teacher's access to a course-scoped resource (material upload, batch scheduling, lab review, dashboard data) is resolved server-side via that trainer's current `TrainerCourseAssignment` rows (the `AssignedTrainerOrAdmin` policy) — never trust a client-supplied course list or assume a bare `Teacher` role is enough. Removing an assignment must revoke access on the very next request, not just in the UI.
- A Student's access to their own data (enrollment, quiz attempts, receipts, certificate) is resolved from `EnrollmentId`/`UserId` on the authenticated principal — a student must never be able to read another student's `SessionProgress`, `Receipt`, or `Certificate` row by guessing an id in the URL.

## Cross-cutting concerns

- Implement authentication, authorization, logging, and exception handling via middleware and filters, not per controller action.
- Log identifiers (UserId, CourseId, BatchId, SessionId, EnrollmentId), date ranges, and status codes for diagnostics — never log a credential, a JWT, a Razorpay/Zoom/LLM-provider API key, a URL containing a key/token, an OTP code, or PII (student email, phone, payment details).
