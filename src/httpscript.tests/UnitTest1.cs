using System;
using Xunit;

namespace xtofs.httpscript.tests
{
    public class StringExtensionTests
    {
        [Theory]
        [InlineData("foobar", "foo", "bar")]
        [InlineData("bazbar", "foo", "bazbar")]
        public void StripPrefix(string input, string prefix, string expected)
        {
            var actual = input.StripPrefix(prefix);
            Assert.Equal(expected, actual);
        }
    }
}
