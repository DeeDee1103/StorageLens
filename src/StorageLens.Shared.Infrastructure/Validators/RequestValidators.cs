using FluentValidation;
using StorageLens.Shared.Contracts;

namespace StorageLens.Shared.Infrastructure.Validators;

// ── Locations ─────────────────────────────────────────────────────────────────

public class CreateStorageLocationRequestValidator : AbstractValidator<CreateStorageLocationRequest>
{
    public CreateStorageLocationRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(256).WithMessage("Name must not exceed 256 characters.");

        RuleFor(x => x.Path)
            .NotEmpty().WithMessage("Path is required.")
            .MaximumLength(1024).WithMessage("Path must not exceed 1024 characters.");
    }
}

public class UpdateStorageLocationRequestValidator : AbstractValidator<UpdateStorageLocationRequest>
{
    public UpdateStorageLocationRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(256).WithMessage("Name must not exceed 256 characters.");

        RuleFor(x => x.Path)
            .NotEmpty().WithMessage("Path is required.")
            .MaximumLength(1024).WithMessage("Path must not exceed 1024 characters.");
    }
}

// ── ScanJobs ──────────────────────────────────────────────────────────────────

public class CreateScanJobRequestValidator : AbstractValidator<CreateScanJobRequest>
{
    public CreateScanJobRequestValidator()
    {
        RuleFor(x => x.StorageLocationId)
            .NotEmpty().WithMessage("StorageLocationId is required.");

        RuleFor(x => x.Notes)
            .MaximumLength(2048).WithMessage("Notes must not exceed 2048 characters.")
            .When(x => x.Notes is not null);
    }
}

public class UpdateScanJobStatusRequestValidator : AbstractValidator<UpdateScanJobStatusRequest>
{
    public UpdateScanJobStatusRequestValidator()
    {
        RuleFor(x => x.CurrentStage)
            .NotEmpty().WithMessage("CurrentStage is required.")
            .MaximumLength(128).WithMessage("CurrentStage must not exceed 128 characters.");
    }
}

// ── Scanner ───────────────────────────────────────────────────────────────────

public class ScannerExecuteRequestValidator : AbstractValidator<ScannerExecuteRequest>
{
    public ScannerExecuteRequestValidator()
    {
        RuleFor(x => x.StorageLocationId)
            .NotEmpty().WithMessage("StorageLocationId is required.");

        RuleFor(x => x.ScanJobId)
            .NotEmpty().WithMessage("ScanJobId is required.");

        RuleFor(x => x.CorrelationId)
            .NotEmpty().WithMessage("CorrelationId is required.")
            .MaximumLength(64).WithMessage("CorrelationId must not exceed 64 characters.");

        RuleFor(x => x.RootPath)
            .NotEmpty().WithMessage("RootPath is required.")
            .MaximumLength(1024).WithMessage("RootPath must not exceed 1024 characters.");

        RuleFor(x => x.BatchSize)
            .InclusiveBetween(1, 5000).WithMessage("BatchSize must be between 1 and 5000.");

        RuleFor(x => x.ProgressUpdateInterval)
            .GreaterThan(0).WithMessage("ProgressUpdateInterval must be greater than 0.");
    }
}

// ── FileInventory ─────────────────────────────────────────────────────────────

public class FileMetadataBatchIngestRequestValidator : AbstractValidator<FileMetadataBatchIngestRequest>
{
    public FileMetadataBatchIngestRequestValidator()
    {
        RuleFor(x => x.StorageLocationId)
            .NotEmpty().WithMessage("StorageLocationId is required.");

        RuleFor(x => x.ScanJobId)
            .NotEmpty().WithMessage("ScanJobId is required.");

        RuleFor(x => x.CorrelationId)
            .NotEmpty().WithMessage("CorrelationId is required.")
            .MaximumLength(64).WithMessage("CorrelationId must not exceed 64 characters.");

        RuleFor(x => x.Files)
            .NotNull().WithMessage("Files list is required.")
            .Must(f => f.Count > 0).WithMessage("Files list must not be empty.")
            .Must(f => f.Count <= 10_000).WithMessage("Batch size must not exceed 10,000 files.");
    }
}

public class MarkDuplicateHashesRequestValidator : AbstractValidator<MarkDuplicateHashesRequest>
{
    public MarkDuplicateHashesRequestValidator()
    {
        RuleFor(x => x.ScanJobId)
            .NotEmpty().WithMessage("ScanJobId is required.");

        RuleFor(x => x.CorrelationId)
            .NotEmpty().WithMessage("CorrelationId is required.");

        RuleFor(x => x.Hashes)
            .NotNull().WithMessage("Hashes list is required.")
            .Must(h => h.Count > 0).WithMessage("Hashes list must not be empty.");
    }
}
