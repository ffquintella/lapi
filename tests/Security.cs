using System;
using Xunit;
using lapi.Security;

namespace lapi.tests
{
    public class Security
    {
        [Fact]
        public void ApiKeyManagerRead()
        {
            var key = ApiKeyManager.FindBySecretKey("abc1234");

            Assert.NotNull(key);

            Assert.Equal("dev-local", key.keyID);
            Assert.Equal("127.0.0.1", key.authorizedIP);

        }
    }
}
