namespace MockMethodCallOrderVerifier;

public class TokenTests
{
    [Fact]
    public void CompareMethodCallTokenToNullMethodCallToken_ShouldReturnFalse()
    {
        // Arrange
        MethodCallToken.Reset();
        MethodCallToken token1 = new("Method_1");
        MethodCallToken? token2 = null;

        // Act
        bool areEqual = token1.Equals(token2);

        // Assert
        areEqual
            .Should()
            .BeFalse();
    }

    [Fact]
    public void CompareMethodCallTokenToObjectThatIsMatchingMethodCallToken_ShouldReturnFalse()
    {
        // Arrange
        MethodCallToken.Reset();
        MethodCallToken token1 = new("Method_1");
        object object1 = token1;

        // Act
        bool areEqual = token1.Equals(object1);

        // Assert
        areEqual
            .Should()
            .BeTrue();
    }

    [Fact]
    public void CompareMethodCallTokenToObjectThatIsNonMatchingMethodCallToken_ShouldReturnFalse()
    {
        // Arrange
        MethodCallToken.Reset();
        MethodCallToken token1 = new("Method_1");
        MethodCallToken token2 = new("Method_2");
        object object1 = token2;

        // Act
        bool areEqual = token1.Equals(object1);

        // Assert
        areEqual
            .Should()
            .BeFalse();
    }

    [Fact]
    public void CompareMethodCallTokenToOtherObject_ShouldReturnFalse()
    {
        // Arrange
        MethodCallToken.Reset();
        MethodCallToken token1 = new("Method_1");
        object object1 = new();

        // Act
        bool areEqual = token1.Equals(object1);

        // Assert
        areEqual
            .Should()
            .BeFalse();
    }

    [Fact]
    public void CompareTwoEqualMethodCallTokens_ShouldReturnTrue()
    {
        // Arrange
        MethodCallToken.Reset();
        MethodCallToken token1 = new("Method_1");
        MethodCallToken token2 = token1;

        // Act
        bool areEqual = token1.Equals(token2);

        // Assert
        areEqual
            .Should()
            .BeTrue();
    }

    [Fact]
    public void CompareTwoUnequalMethodCallTokens_ShouldReturnFalse()
    {
        // Arrange
        MethodCallToken.Reset();
        MethodCallToken token1 = new("Method_1");
        MethodCallToken token2 = new("Method_2");

        // Act
        bool areEqual = token1.Equals(token2);

        // Assert
        areEqual
            .Should()
            .BeFalse();
    }

    [Fact]
    public void CreateMethodCallTokenWithDuplicateName_ShouldThrowException()
    {
        // Arrange
        MethodCallToken.Reset();
        string methodCallName = "Method_1";
        MethodCallToken token1 = new(methodCallName);
        Func<MethodCallToken> func = () => new(methodCallName);
        string expected = $"A method call token with the name \"{methodCallName}\" already exists.";

        // Act/Assert
        func
            .Should()
            .Throw<InvalidOperationException>()
            .WithMessage(expected);
    }

    [Fact]
    public void CreateMethodCallTokenWithEmptyMethodCallName_ShouldThrowException()
    {
        // Arrange
        MethodCallToken.Reset();
        Func<MethodCallToken> func = () => new(string.Empty);
        string expected = "Method call name must not be empty or whitespace.";

        // Act/Assert
        func
            .Should()
            .Throw<ArgumentException>()
            .WithMessage(expected);
    }

    [Fact]
    public void CreateMethodCallTokenWithNameThatIsOnlyWhitespace_ShouldThrowException()
    {
        // Arrange
        MethodCallToken.Reset();
        Func<MethodCallToken> func = static () => new(" \t\v");
        string expected = "Method call name must not be empty or whitespace.";

        // Act/Assert
        func
            .Should()
            .Throw<ArgumentException>()
            .WithMessage(expected);
    }

    [Fact]
    public void CreateMethodCallTokenWithNullMethodCallName_ShouldThrowException()
    {
        // Arrange
        MethodCallToken.Reset();
        Func<MethodCallToken> func = () => new(null!);

        // Act/Assert
        func
            .Should()
            .Throw<ArgumentNullException>();
    }

    [Fact]
    public void CreateMethodCallTokenWithValidMethodCallName_ShouldCreateToken()
    {
        // Arrange
        MethodCallToken.Reset();
        string methodCallName = "Method_1";
        int tokenID = 1;

        // Act
        MethodCallToken token = new(methodCallName);

        // Assert
        token
            .Should()
            .NotBeNull();
        token.MethodCallName
            .Should()
            .Be(methodCallName);
        token.TokenID
            .Should()
            .Be(tokenID);
    }

    [Fact]
    public void CreateMultipleTokens_EachTokenShouldHaveUniqueTokenID()
    {
        // Arrange
        MethodCallToken.Reset();
        string methodCallName1 = "Method_1";
        string methodCallName2 = "Method_2";
        string methodCallName3 = "Method_3";
        int tokenID1 = 1;
        int tokenID2 = 2;
        int tokenID3 = 3;

        // Act
        MethodCallToken token1 = new(methodCallName1);
        MethodCallToken token2 = new(methodCallName2);
        MethodCallToken token3 = new(methodCallName3);

        // Assert
        token1.MethodCallName
            .Should()
            .Be(methodCallName1);
        token1.TokenID
            .Should()
            .Be(tokenID1);
        token2.MethodCallName
            .Should()
            .Be(methodCallName2);
        token2.TokenID
            .Should()
            .Be(tokenID2);
        token3.MethodCallName
            .Should()
            .Be(methodCallName3);
        token3.TokenID
            .Should()
            .Be(tokenID3);
    }

    [Fact]
    public void GetHashCodeOfMultipleMethodCallTokens_ShouldReturnUniqueValues()
    {
        // Arrange
        MethodCallToken.Reset();
        MethodCallToken token1 = new("Method_1");
        MethodCallToken token2 = new("Method_2");
        MethodCallToken token3 = new("Method_3");

        // Act
        int hashCode1 = token1.GetHashCode();
        int hashCode2 = token2.GetHashCode();
        int hashCode3 = token3.GetHashCode();

        // Assert
        hashCode1
            .Should()
            .NotBe(hashCode2);
        hashCode2
            .Should()
            .NotBe(hashCode3);
        hashCode3
            .Should()
            .NotBe(hashCode1);
    }
}