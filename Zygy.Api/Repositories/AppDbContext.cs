using LinqToDB;
using LinqToDB.Data;
using Zygy.Api.Entities;

namespace Zygy.Api.Repositories;

public class AppDbContext(DataOptions<AppDbContext> options) : DataConnection(options.Options)
{
    public ITable<KeyValueEntity> KeyValue => this.GetTable<KeyValueEntity>();
}
