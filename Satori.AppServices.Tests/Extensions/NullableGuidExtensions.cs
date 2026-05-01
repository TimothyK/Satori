using Shouldly;

namespace Satori.AppServices.Tests.Extensions
{
    internal static class NullableGuidExtensions
    {
        public static void ShouldBe(this Guid actual, Guid? expected)
        {
            if (expected == null)
            {
                ShouldBeTestExtensions.ShouldBe(actual, Guid.Empty);
                return;
            }
            ShouldBeTestExtensions.ShouldBe(actual, expected.Value);
        }

        extension(IEnumerable<Guid> actual)
        {
            public void ShouldContain(Guid? expected, string message)
            {
                if (expected == null)
                {
                    ShouldBeEnumerableTestExtensions.ShouldContain(actual, Guid.Empty, message);
                    return;
                }
                ShouldBeEnumerableTestExtensions.ShouldContain(actual, expected.Value, message);
            }

            public bool Contains(Guid? value)
            {
                return Enumerable.Contains(actual, value ?? Guid.Empty);
            }
        }
    }
}
