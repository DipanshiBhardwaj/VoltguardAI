# Naming & OOP Conventions

**Applies to:** every C# file in the solution — `src/**/*.cs` and `tests/**/*.cs`.

This file is **canonical**. The per-layer files (`api-instructions.md`, `bll-instructions.md`, `common-instructions.md`, `dal-instructions.md`, `testing-instructions.md`) each have a short naming section; where any of them is less specific than this file, this file wins.

---

## 1. The rule, in one line

> **`{Thing}{Role}`** — what it is *about*, then what it *is*. The folder states the role once, the class states it once, and nothing beyond that is ever repeated.

Read any file name in this solution and you should know, without opening it, which layer it belongs to, what it operates on, and what kind of thing it is. `CourseService.cs` is the business logic for courses. `CourseRepository.cs` is the data access for courses. `CoursesController.cs` is the HTTP surface for courses. Nothing else needs to be said in the name.

## 2. Never repeat

These are the specific mistakes this convention exists to prevent. All are rejected in review.

| ✗ Wrong | ✓ Right | Why |
|---|---|---|
| `Services/CourseServices/CourseService.cs` | `Services/CourseService.cs` | The role is already in the folder and the suffix. Don't say it a third time. |
| `Repositories/Course/CourseRepository.cs` | `Repositories/CourseRepository.cs` | Don't create a folder per entity. The entity is already the class-name prefix. |
| `CourseServiceService`, `CourseRepositoryRepo` | `CourseService`, `CourseRepository` | One suffix per class, ever. |
| `BllCourseService`, `DalCourseRepository` | `CourseService`, `CourseRepository` | The project name already says the layer. Never put the layer in the class name. |
| `CourseServiceImpl`, `CourseRepositoryImplementation` | `CourseService`, `CourseRepository` | `Impl` is not a word. The interface is `ICourseService`; the class is `CourseService`. |
| `CourseEntity`, `CourseModel`, `CourseTable` | `Course` | Entities carry **no** suffix — see §4. |
| `CourseManager`, `CourseHelper`, `CourseUtils`, `CourseProcessor` | `CourseService` (or a named, specific class) | Vague role words are banned. If a class genuinely isn't a service, give it a suffix that says what it actually is (`CourseEnrollmentPolicy`, `CertificateNumberGenerator`) — never `Manager`/`Helper`/`Utils`/`Handler`-for-everything. |
| `GetData()`, `Process()`, `DoWork()`, `Handle()` | `GetCourseByIdAsync()`, `EnrollStudentAsync()` | The same rule applies to members, not just types. |

One more, easy to slip into: **do not repeat the entity inside its own member names.** Inside `Course`, the property is `Title`, not `CourseTitle`; inside `CourseService`, the method is `GetByIdAsync`, not `GetCourseByIdAsync` — the class already said "course". The entity name reappears only when the member genuinely refers to a *different* entity (`Enrollment.CourseId` is correct).

## 3. Per-layer map

Every folder below sits directly in its project root. Folder = plural role. Class = `{Entity}{Role}`. One public type per file, and the file name is exactly the type name.

| Project | Folder | Suffix | Example file | Contains |
|---|---|---|---|---|
| `...V1.Common` | `Dtos/` | `Dto` | `CourseDto.cs` | Data returned to callers |
| `...V1.Common` | `Dtos/` | `Request` | `CreateCourseRequest.cs` | Data accepted from callers |
| `...V1.Common` | `Enums/` | *(none)* | `SessionProgressStatus.cs` | Enums — named for what they describe, never `...Enum` |
| `...V1.Common` | `Constants/` | `Constants` | `AuthorizationPolicyConstants.cs` | Shared constant values |
| `...V1.Common` | `ExternalModels/Zoom/` | *(vendor's own shape)* | `ZoomRegistrantResponse.cs` | Third-party payload models, prefixed by vendor |
| `...V1.DAL` | `Entities/` | *(none)* | `Course.cs` | EF entities — see §4 |
| `...V1.DAL` | `Configurations/` | `Configuration` | `CourseConfiguration.cs` | `IEntityTypeConfiguration<Course>` |
| `...V1.DAL` | `Repositories/` | `Repository` | `CourseRepository.cs`, `ICourseRepository.cs` | Data access for one aggregate |
| `...V1.DAL` | *(root)* | `DbContext` | `TrainingDbContext.cs` | The context |
| `...V1.DAL` | `Migrations/` | *(EF-generated)* | — | Never hand-renamed |
| `...V1.BLL` | `Services/` | `Service` | `CourseService.cs`, `ICourseService.cs` | Business rules for one area |
| `...V1.BLL` | `Integrations/Zoom/` | `Client` | `ZoomClient.cs`, `IZoomClient.cs` | One connector per external service |
| `...V1.BLL` | `Jobs/` | `Job` | `ZoomRegistrationSyncJob.cs` | Hangfire background jobs |
| `...V1.BLL` | `Validators/` | `Validator` | `CreateCourseRequestValidator.cs` | Input validation |
| `...V1.API` | `Controllers/` | `Controller` | `CoursesController.cs` | HTTP endpoints |
| `...V1.API` | `Middleware/` | `Middleware` | `ExceptionHandlingMiddleware.cs` | Pipeline components |
| `...V1.API` | `Authorization/` | `Requirement` / `Handler` | `AssignedTrainerRequirement.cs` | Policy-based auth pieces |
| `...V1.API` | `Extensions/` | `Extensions` | `ServiceCollectionExtensions.cs` | DI registration extension methods |
| `*.Tests` | *(mirrors the project under test)* | `Tests` | `CourseServiceTests.cs` | Tests for exactly one class |

**The one place plural is used:** controllers. `CoursesController` serves `/api/v1/courses` — the class names the *collection of resources*, which is what REST addresses. Everywhere else the name is singular, because a service or repository acts on one entity type: `CourseService`, `CourseRepository`. (If uniformity is preferred over the .NET convention, switching to singular `CourseController` is a one-line change to this file and a rename — but pick one now and never mix.)

## 4. Entities have no suffix

An entity **is** the thing. `Course`, `Batch`, `Enrollment`, `Session`, `SessionProgress`, `QuizAttempt`, `LabSubmission`, `AttendanceRecord`, `Certificate`, `Receipt`, `TrainerCourseAssignment`.

Never `CourseEntity`, `CourseModel`, `CourseTable`, `tblCourse`, or `CourseDbo`. The `Entities/` folder and the `...V1.DAL` project already say what these are, and the DTO that shadows an entity is the one that carries a suffix (`CourseDto`) — so there is no ambiguity to disambiguate.

Entity property names follow the same no-repetition rule and the column conventions in `dal-instructions.md`: `*On` for timestamps (`EnrolledOn`, `IssuedOn`), `*Day` for calendar dates (`CompletionDay`), `*Id` for foreign keys (`CourseId`, `BatchId`).

## 5. Interfaces

- An interface is `I` + the implementation's exact name: `ICourseService` / `CourseService`, `IZoomClient` / `ZoomClient`.
- The interface lives in the same folder as its implementation, in its own file. No separate `Interfaces/` folder — that separates two things that are always read together.
- Where one interface has several implementations, each implementation names its *variant*, and the interface keeps the plain role name: `IQuestionGenProvider` → `LocalModelQuestionGenProvider`, `ApiKeyQuestionGenProvider`.

## 6. Members

- **Methods are verb-first and say what they do:** `GetByIdAsync`, `CreateAsync`, `EnrollStudentAsync`, `UnlockNextSessionAsync`, `IssueCertificateAsync`.
- **Repository methods describe the fetch; service methods describe the business action.** `CourseRepository.GetByBatchIdAsync` and `EnrollmentService.EnrollStudentAsync` — a service method called `InsertEnrollmentAsync` is a sign business logic has leaked into a data-access vocabulary.
- **Every async method ends in `Async`** and takes `CancellationToken cancellationToken = default`, propagated to every awaited call.
- **Booleans read as a question:** `IsDeleted`, `IsPassed`, `HasAttemptsRemaining`, `CanUnlockNextSession`. Never `Deleted`, `Flag`, `Status` for a boolean.
- **Collections are plural:** `Sessions`, `Enrollments`. Singular names for collections are always a bug in the making.
- **No abbreviations** other than the established ones (`Id`, `Url`, `Api`, `Dto`). Not `Crs`, `Enrl`, `Qz`, `Cnt`.

## 7. Request vs. response DTOs

The DTO name states its direction, so a reader knows which way the data flows:

- Inbound: `CreateCourseRequest`, `UpdateCourseRequest`, `EnrollStudentRequest`.
- Outbound: `CourseDto` (the full shape), `CourseListItemDto` (the trimmed list shape), `CertificateVerificationDto`.

Never reuse one type for both directions — an inbound type that also serves as a response drags server-only fields into the request contract.

---

# OOP principles this codebase follows

Naming makes the code readable; these make it changeable. They are not aspirational — a review rejects code that breaks them.

## Encapsulation — objects protect their own rules

- Entity state that has rules attached is not freely settable from outside. `SessionProgress` does not expose a public `Status` setter for anything to write; it exposes intention-revealing methods (`Unlock()`, `RecordQuizPass()`, `Fail()`) that enforce the legal transitions of the state machine. An illegal transition must be *impossible*, not merely *not currently done anywhere*.
- Collection navigation properties are exposed as read-only (`IReadOnlyCollection<Session>`) with an `Add...`/`Remove...` method that validates, rather than a settable `List<T>` any caller can mutate.
- No public mutable static state anywhere. Ever.
- Fields are private; anything public is a property.

## Abstraction — depend on the interface, not the thing

- Every service, repository and external connector is consumed through its interface. A class that constructs a dependency with `new` instead of receiving it through the constructor cannot be tested and is rejected.
- Constructor injection only. No service locator, no `IServiceProvider.GetService` inside business code, no property injection.
- An interface exposes what callers need, in callers' vocabulary — never the implementation's internals. `IZoomClient` speaks in our terms and maps Zoom's payload shapes inside the connector; Zoom's models never reach a service.

## Inheritance — sparingly, and only for genuine "is-a"

- The sanctioned base class is `BaseEntity` (Id, CreatedOn/By, UpdatedOn/By, Remarks, IsDeleted). Every entity inherits from it, because every entity genuinely *is* an auditable, soft-deletable record.
- Beyond that, **prefer composition to inheritance**. Do not build deep hierarchies to share a few methods — inject a collaborator instead. A base class exists to express a real is-a relationship, never to avoid retyping code.
- No inheritance across layers. A DTO never inherits from an entity; a service never inherits from a repository.
- Where a base class does exist, mark it `abstract` if it should never be instantiated, and seal classes not designed to be inherited from.

## Polymorphism — one interface, swappable behaviour

- The question-generation provider is the canonical example: `IQuestionGenProvider` with `LocalModelQuestionGenProvider` and `ApiKeyQuestionGenProvider` behind it, selected at DI-registration time by `QuizEngine:Mode`. Callers never branch on which provider is active — if a service contains `if (mode == "LocalModel")`, the polymorphism has been defeated and the code is wrong.
- The same shape applies anywhere a second implementation is plausible: payment provider, attendance source, notification channel. Branch once, at registration; never at every call site.

## SOLID, stated concretely for this codebase

- **Single responsibility.** A class has one reason to change. `EnrollmentService` handles enrollment; it does not also send emails, call Razorpay, and issue certificates. When a service grows past roughly 300 lines or its name needs an "and", split it.
- **Open/closed.** Adding a third question-gen provider, or a second payment gateway, means adding a class and one registration line — not editing a `switch` that every existing path runs through.
- **Liskov substitution.** Any implementation of an interface must be usable wherever that interface is expected, without the caller knowing which one it got. An implementation that throws `NotSupportedException` from an interface method is a sign the interface is wrong and needs splitting.
- **Interface segregation.** Small, purpose-built interfaces. A repository interface that has grown thirty methods so that four unrelated services can each use three of them should be several interfaces.
- **Dependency inversion.** BLL depends on `ICourseRepository`, not on `CourseRepository` or on EF Core. The project reference direction (`API → BLL → DAL → Common`) is the mechanical enforcement of this, and it is one-way: if you ever want to reverse an arrow, the design is wrong.

## DRY — one rule, one place

- A business rule exists in exactly one place: in BLL. The pass threshold, the 80% attendance gate, the seat-hold expiry, the quiz-retry limit — the Vue app and the API may each *also* check them for a better user experience, but the authoritative check is in the service and never duplicated as a second, independently-maintained copy of the logic.
- Shared constants (policy names, role names, claim types) live in `Common/Constants/`. Magic strings in more than one file are a bug.
- Duplicated *code* is worth removing; duplicated *coincidence* is not. Two methods that happen to look alike but change for different reasons should stay separate.

## Guard clauses and fail-fast

- Validate at the top of the method and return/throw immediately. No deeply nested `if` pyramids with the real work buried at the bottom.
- Constructors validate their arguments (`ArgumentNullException.ThrowIfNull`), so an object cannot exist in an invalid state.
- Never return `null` to mean "error". A not-found is an explicit result the caller handles; a failure is an exception mapped to `ProblemDetails` at the API boundary.

## Immutability where it costs nothing

- DTOs are immutable (`{ get; init; }`) — they are a snapshot of a contract, not a working object.
- Prefer `readonly` fields for injected dependencies.
- Value-like types (a money amount, a certificate number, a date range) are better as small immutable types than as loose primitives passed around — this is what stops a `BatchId` ever being passed where a `CourseId` was expected.
