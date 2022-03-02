using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhetos.Dom.DefaultConcepts;
using System.Linq;

namespace CommonConcepts.Test
{
    [TestClass]
    public class ComputedFromSelfExtensionTest
    {
        [TestMethod]
        public void RecomputeAfterInsert()
        {
            using (var scope = TestScope.Create())
            {
                var repository = scope.Resolve<Common.DomRepository>();
                var artist = new TestComputedFromSelfExtension.Artist()
                {
                    Name = "a2",
                    ToursRevenue = 1000
                };

                repository.TestComputedFromSelfExtension.Artist.Insert(new[] { artist });
                var computedArtist = repository.TestComputedFromSelfExtension.Artist
                    .Query(item => item.ID == artist.ID)
                    .SingleOrDefault();

                decimal expectedTotalRevenue = 1000;
                Assert.AreEqual(expectedTotalRevenue, computedArtist.TotalRevenue);
            }
        }
    }
}