namespace ElevatorMaintenanceSystem.ViewModels;

public class ReportTicketRowViewModel
{
    public Guid TicketId { get; set; }

    public string Description { get; set; } = string.Empty;

    public string IssueType { get; set; } = string.Empty;

    public string Priority { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTime RequestedDate { get; set; }

    public string ElevatorName { get; set; } = string.Empty;

    public string AssignedWorkerName { get; set; } = string.Empty;
}
