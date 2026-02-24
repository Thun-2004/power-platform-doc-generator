using backend.Application.DTO;

namespace backend.Application.Interfaces;

public interface IJobStatusService
{
    JobStatusResult GetJobStatus(string jobId);
}