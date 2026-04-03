using Microsoft.EntityFrameworkCore;
using StorageLens.Services.Orchestrator.Data;
using StorageLens.Services.Orchestrator.Models;

namespace StorageLens.Services.Orchestrator.Repositories;

public class WorkflowRepository(OrchestratorDbContext dbContext) : IWorkflowRepository
{
    public Task<WorkflowInstance?> GetByIdAsync(Guid id, bool includeSteps, CancellationToken cancellationToken)
    {
        var query = dbContext.WorkflowInstances.AsQueryable();

        if (includeSteps)
        {
            query = query.Include(x => x.Steps);
        }

        return query.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<WorkflowInstance?> GetByScanJobIdAsync(Guid scanJobId, CancellationToken cancellationToken)
    {
        return dbContext.WorkflowInstances
            .FirstOrDefaultAsync(x => x.ScanJobId == scanJobId, cancellationToken);
    }

    public async Task AddWorkflowAsync(WorkflowInstance workflow, CancellationToken cancellationToken)
    {
        dbContext.WorkflowInstances.Add(workflow);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public void AddStepExecution(WorkflowStepExecution step)
    {
        dbContext.WorkflowStepExecutions.Add(step);
    }

    public void AddError(WorkflowError error)
    {
        dbContext.WorkflowErrors.Add(error);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
