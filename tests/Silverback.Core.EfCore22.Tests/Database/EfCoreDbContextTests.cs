using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Silverback.Database;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Core.EFCore22.TestTypes;
using Silverback.Tests.Core.EFCore22.TestTypes.Model;
using Xunit;

namespace Silverback.Tests.Core.EFCore22.Database
{
    public class EfCoreDbContextTests
    {
        private readonly TestDbContext _dbContext;
        private readonly EfCoreDbContext<TestDbContext> _efCoreDbContext;

        public EfCoreDbContextTests()
        {
            var dbOptions = new DbContextOptionsBuilder<TestDbContext>()
                .UseSqlite("DataSource=:memory:")
                .Options;

            _dbContext = new TestDbContext(dbOptions, Substitute.For<IPublisher>());
            _efCoreDbContext = new EfCoreDbContext<TestDbContext>(_dbContext);
        }

        [Fact]
        public void GetDbSet_SomeEntity_EfCoreDbSetIsReturned()
        {
            var dbSet = _efCoreDbContext.GetDbSet<Person>();

            dbSet.Should().NotBeNull();
            dbSet.Should().BeOfType<EfCoreDbSet<Person>>();
        }
    }
}
