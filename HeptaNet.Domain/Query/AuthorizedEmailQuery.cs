namespace HeptaNet.Domain.Query;

public class GetAuthorizedEmailQuery
{
    public string? Email { get; set; }
    public bool? IsImported { get; set; }
    public DateTime? SyncDateFrom { get; set; }
    public DateTime? SyncDateTo { get; set; }

    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? SortBy { get; set; } = "SyncId";
    public bool SortDescending { get; set; } = false;
}

public class AuthorizedEmailQueryResult
{
    public int SyncId { get; set; }
    public string Email { get; set; } = string.Empty;
    public bool IsImported { get; set; }
    public DateTime? SyncDate { get; set; }
}

