using LinqToDB;
using LinqToDB.Data;

namespace Zygy.Api.Repositories;

public class AppDbContext(DataOptions<AppDbContext> options) : DataConnection(options.Options)
{
}
