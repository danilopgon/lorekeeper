using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Api.Modules.Campaigns.Infrastructure;

public enum CampaignPersistenceError
{
    Unknown,
    InvalidName,
    NameConflict
}

public static class CampaignPersistenceErrors
{
    public static CampaignPersistenceError From(DbUpdateException exception) => From(exception.GetBaseException());

    public static CampaignPersistenceError From(Exception exception)
    {
        return exception switch
        {
            PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } => CampaignPersistenceError.NameConflict,
            PostgresException { SqlState: PostgresErrorCodes.CheckViolation } => CampaignPersistenceError.InvalidName,
            _ => CampaignPersistenceError.Unknown
        };
    }
}
