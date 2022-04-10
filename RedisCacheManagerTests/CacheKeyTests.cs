using System.Collections.Generic;
using AutoFixture;
using FluentAssertions;
using RedisCacheManager;
using Xunit;

namespace RedisCacheManagerTests
{
    public class CacheKeyTests
    {
        private readonly IFixture _fixture;

        public CacheKeyTests()
        {
            _fixture = new Fixture();
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void Constructor_SingleInvalidKey_IsInvalid(string key)
        {
            // Act
            var result = new CacheKey<TestClass>(key);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Key.Should().Be(key);
        }

        [Fact]
        public void Constructor_SingleValidKey_IsValidAndSetsKey()
        {
            // Arrange
            var key = _fixture.Create<string>();

            // Act
            var result = new CacheKey<TestClass>(key);

            // Assert
            result.IsValid.Should().BeTrue();
            result.Key.Should().Be($"TestClass-{key}");
        }

        [Fact]
        public void Constructor_NestedGenericTypeSingleValidKey_IsValidAndSetsKey()
        {
            // Arrange
            var key = _fixture.Create<string>();

            // Act
            var result = new CacheKey<IEnumerable<TestClass>>(key);

            // Assert
            result.IsValid.Should().BeTrue();
            result.Key.Should().Be($"IEnumerable`1<TestClass>-{key}");
        }

        [Fact]
        public void Constructor_NestedMultiGenericTypeSingleValidKey_IsValidAndSetsKey()
        {
            // Arrange
            var key = _fixture.Create<string>();

            // Act
            var result = new CacheKey<IDictionary<string, TestClass>>(key);

            // Assert
            result.IsValid.Should().BeTrue();
            result.Key.Should().Be($"IDictionary`2<String|TestClass>-{key}");
        }

        [Fact]
        public void Constructor_DeeplyNestedMultiGenericTypeSingleValidKey_IsValidAndSetsKey()
        {
            // Arrange
            var key = _fixture.Create<string>();

            // Act
            var result = new CacheKey<IDictionary<string, IEnumerable<TestClass>>>(key);

            // Assert
            result.IsValid.Should().BeTrue();
            result.Key.Should().Be($"IDictionary`2<String|IEnumerable`1<TestClass>>-{key}");
        }
    }
}
