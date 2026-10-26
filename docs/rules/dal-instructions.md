# Data Access (DAL) Rules

Applies to: `HAES.Portal.Training.DAL/**/*`

*Adapted for the Training App from the SES.AffiliateHub DAL rules. The biggest change from the original: this solution has no multi-tenancy (see "Deployment scope" below, replacing the original "Multi-tenancy" section) — everything else (BaseEntity, soft delete, column conventions, repository shape) carries over directly.*

## Entities

- Every entity inherits `BaseEntity`: `Id` (`Guid` PK, defaults to `NEWSEQUENTIALID()`), `CreatedOn`, `CreatedBy`, `UpdatedOn`, `UpdatedBy`, `Remarks` (max length 200), `IsDeleted`. Do not redeclare these per entity.
- Audit fields (`CreatedOn/By`, `UpdatedOn/By`) are set centrally by overriding `SaveChangesAsync()` — never assigned by hand in a BLL service or repository method.
- Do not add business logic to DAL; focus only on persistence, queries, and mapping. The `SessionProgress` unlock state machine, quiz sampling, attendance duration-threshold math, and certificate/receipt number generation all belong in BLL, not here.

## Deployment scope (no multi-tenancy in this version)

*(Replaces the affiliate-hub "Multi-tenancy" section. `architecture-plan.md` §0 is explicit: this version is one deployment per college/cohort — true multi-tenancy is a separate, later decision and is deliberately not built now.)*

- **Do not add a `TenantId` column, a tenant global query filter, or any tenant-scoping repository parameter.** There is no tenant concept in this schema.
- The **only** global query filter in this version is the soft-delete filter (`HasQueryFilter(e => !e.IsDeleted)` — see below).
- Keep aggregate boundaries clean anyway (`Course` → `Module` → `Session`; `Enrollment` → `SessionProgress`; `Batch` → `Session`, etc.) so that *if* multi-tenancy is decided later, a `TenantId` column and composite FKs can be layered on without a rewrite — but do not build that speculative structure now; it would just be dead weight and untested code paths.
- Role-based and assignment-based scoping — Admin sees every course; a Teacher is scoped to their `TrainerCourseAssignment` rows; a Student is scoped to their own `EnrollmentId`/`UserId` — is enforced in BLL/API from the authenticated principal (see `api-instructions.md`, `bll-instructions.md`), **not** a DAL-level tenant filter. A repository method still takes an explicit `courseId`/`enrollmentId`/`userId` parameter and scopes its query to it; it just isn't a *global* filter the way soft delete is.

## Soft delete

- Soft delete only: `IsDeleted = true`, enforced everywhere via an EF Core global query filter. No `DELETE` statement anywhere in application code, and never a data-repair `DELETE` inside an EF migration.
- Any unique index on a natural key must be filtered on `IsDeleted = 0`, so a soft-deleted row never blocks re-insertion of the same key (e.g. re-inviting a trainer by the same email after their account was deactivated).

## Column conventions

- Table names singular PascalCase (`Course`, `Session`, `Enrollment`, `QuestionBankItem`, `QuizAttempt`, `LabSubmission`, `Certificate`, `Receipt`, `AttendanceRecord`, `TrainerCourseAssignment`), not `Courses`/`courses`. Columns PascalCase. Foreign keys named `{ReferencedTable}Id` (`CourseId`, `SessionId`, `EnrollmentId`, `TrainerUserId`). All names in English.
- Enums are stored as strings — `.HasConversion<string>()` in the entity's configuration class, at length 50, never as an integer (e.g. `SessionProgressStatus`, `PaymentStatus`, `AttendanceSource`, `ValidationStatus`, `EnrollmentMode`). Number the underlying values `None = 0`, then from `5` in steps of `5` (`5, 10, 15, ...`), so a new state can be inserted later without renumbering existing ones.
- `DateTimeOffset` for any exact-moment column, named `*On` (e.g. `EnrolledOn`, `IssuedOn`, `ScheduledStartOn`, `ScheduledEndOn`, `RevokedOn`, `MarkedOn`, `JoinedOn`). `DateOnly` for calendar-day-only columns, named `*Day` (e.g. `CompletionDay`) — this is the grain reports (attendance/completion, payments/revenue) group and filter on. Never store a local/ambiguous timestamp in a `DateTimeOffset` column without an explicit, correct offset — this matters especially for anything downstream of Zoom webhook timestamps and Razorpay webhook timestamps.
- All string columns are VARCHAR (`.IsUnicode(false)`) with an explicit `HasMaxLength`, except columns holding free-text or names that cannot be guaranteed ASCII (student/trainer display names, course titles authored in a non-English script, override/revocation reason text) — those are `nvarchar`. Only a genuine JSON payload column (e.g. `QuestionBankItem.Options`, `LabAssignment.RubricJson`) may be `nvarchar(max)`/`varchar(max)`.
- Money is `decimal(18,6)` consistently — `Receipt.Amount`, `Course.OfflinePrice`/`OnlinePrice` — never a different precision for the same kind of quantity in different tables.
- Use right-sized integer types (`byte`/`tinyint` for small counters like `Course.TotalDays`, `short`/`smallint` for counts like `QuizAttempt.TotalQuestions`/`OrderIndex`, `int` for larger counters like `AttendanceRecord.DurationMinutes`) with an explicit `HasColumnType` where it matters.
- Every foreign key is indexed, with `Restrict` configured on delete — never `Cascade` (soft delete means the DB should never cascade a delete in the first place). This matters in particular for `Enrollment`→`Batch`→`Course` and `SessionProgress`→`Session`→`Module`→`Course`, where an accidental cascade would silently wipe a student's whole history.

## Naming conventions

> **Canonical naming & OOP rules live in `naming-and-oop-instructions.md`.** That file governs class/file/folder naming, the no-repetition rules, and the OOP/SOLID principles for the whole solution. The points below are the layer-specific additions to it.


- Use PascalCase for `DbContext`, entity classes, and repository classes.
- Use camelCase for parameters and local variables.
- Repository interfaces are prefixed with `I` (for example, `ICourseRepository`, `IEnrollmentRepository`, `IQuestionBankRepository`).

## Entity configuration

- One entity configuration class per entity, implementing `IEntityTypeConfiguration<T>`, under `Configurations/`. Avoid modifying scaffolded/generated files directly — extend via partial classes or configuration classes instead.

## Async access

- Use EF Core async APIs (`ToListAsync`, `FirstOrDefaultAsync`, `SaveChangesAsync`, etc.) for all database queries and commands, every one with a `CancellationToken cancellationToken = default` parameter that is propagated through.
- Do not block on async calls; always propagate async/await up the call stack.

## Repositories & queries

- Each repository encapsulates a clear aggregate or logical group of tables, with small, focused methods (`GetByIdAsync`, `GetAllAsync`, `AddAsync`, `UpdateAsync`, `DeleteAsync` meaning soft-delete).
- Prefer LINQ queries over raw SQL; use raw SQL only where measured as necessary, and encapsulate it safely.
- Keep repository methods small and focused; avoid mixing multiple unrelated queries in a single method.
- Every repository whose entity is scoped to a `Course` or `Batch` (e.g. `Session`, `SessionMaterial`, `QuestionBankItem`, `TrainerCourseAssignment`, `Batch` itself) needs a test proving a query scoped to one `Course`/`Batch` never returns another `Course`'s/`Batch`'s rows — this is this domain's equivalent of the affiliate-hub's tenant-isolation test, and matters just as much here since a Teacher's course-scoped access depends on it holding.
