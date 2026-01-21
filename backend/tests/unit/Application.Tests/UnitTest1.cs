// namespace Application.Tests;

// public class UnitTest1
// {
//     [Fact]
//     public void Test1()
//     {

//     }
// }

using Xunit;
using Prime.Services;

namespace Prime.UnitTests.Services
{
    public class PrimeService_IsPrimeShould
    {
        [Fact]
        public void IsPrime_InputIs1_ReturnFalse()
        {
            var primeService = new PrimeService();
            bool result = primeService.IsPrime(1);

            Assert.False(result, "1 shouldn't be prime");
        }
    }
}