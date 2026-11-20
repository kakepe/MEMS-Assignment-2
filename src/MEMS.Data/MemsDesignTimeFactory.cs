using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MEMS.Data;

public sealed class MemsDesignTimeFactory : IDesignTimeDbContextFactory<MemsDbContext>
{
    public MemsDbContext CreateDbContext(string[] args)
    {
        // Model/script generation needs no running server. No LocalDB or SQLite fallback.
        // An accidental update without an explicit connection cannot reach a local SQL instance.
        var connection = Environment.GetEnvironmentVariable("ConnectionStrings__mems")
            ?? "Server=design-only.invalid;Database=MEMSAssignment2;Integrated Security=true;Encrypt=true;Connect Timeout=1";
        return new(new DbContextOptionsBuilder<MemsDbContext>().UseSqlServer(connection).Options);
    }
}
