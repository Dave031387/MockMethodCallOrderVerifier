namespace MockMethodCallOrderVerifier;

using static MockMethodCallOrderVerifier.MethodCallTokens;

public class VerifierTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(0, 1)]
    [InlineData(0, -1)]
    [InlineData(1, 0)]
    [InlineData(1, 1)]
    [InlineData(1, -1)]
    [InlineData(-1, 0)]
    [InlineData(-1, 1)]
    [InlineData(-1, -1)]
    public void AllMethodsCalledInCorrectOrder_ShouldNotThrowException(int firstCallNumber1, int secondCallNumber1)
    {
        // Arrange
        MethodCallOrderVerifier verifier = new();
        int firstMethodCallNumber1 = GetMethodCallNumber(firstCallNumber1);
        int firstCallNumber2 = GetNextCallNumber(firstCallNumber1);
        int firstMethodCallNumber2 = GetMethodCallNumber(firstCallNumber2);
        int firstCallNumber3 = GetNextCallNumber(firstCallNumber2);
        int firstMethodCallNumber3 = GetMethodCallNumber(firstCallNumber3);
        int secondMethodCallNumber1 = GetMethodCallNumber(secondCallNumber1);
        int secondCallNumber2 = GetNextCallNumber(secondCallNumber1);
        int secondMethodCallNumber2 = GetMethodCallNumber(secondCallNumber2);
        RegisterMethodCall(verifier, MethodCall_1, firstMethodCallNumber1);
        RegisterMethodCall(verifier, MethodCall_2, secondMethodCallNumber1);
        RegisterMethodCall(verifier, MethodCall_3);
        RegisterMethodCall(verifier, MethodCall_2, secondMethodCallNumber2);
        RegisterMethodCall(verifier, MethodCall_1, firstMethodCallNumber2);
        RegisterMethodCall(verifier, MethodCall_4);
        RegisterMethodCall(verifier, MethodCall_1, firstMethodCallNumber3);
        verifier.DefineExpectedCallOrder(MethodCall_1, MethodCall_2, firstCallNumber1, secondCallNumber1);
        verifier.DefineExpectedCallOrder(MethodCall_2, MethodCall_3, secondCallNumber1);
        verifier.DefineExpectedCallOrder(MethodCall_3, MethodCall_2, 0, secondCallNumber2);
        verifier.DefineExpectedCallOrder(MethodCall_2, MethodCall_1, secondCallNumber2, firstCallNumber2);
        verifier.DefineExpectedCallOrder(MethodCall_1, MethodCall_4, firstCallNumber2);
        verifier.DefineExpectedCallOrder(MethodCall_4, MethodCall_1, 0, firstCallNumber3);
        Action action = verifier.Verify;

        // Act/Assert
        action
            .Should()
            .NotThrow();
    }

    [Fact]
    public void ExpectedFirstMethodCallComesAfterSecondMethodIsCalledTwice_ShouldThrowException()
    {
        // Arrange
        MethodCallOrderVerifier verifier = new();
        RegisterMethodCall(verifier, MethodCall_2);
        RegisterMethodCall(verifier, MethodCall_2);
        RegisterMethodCall(verifier, MethodCall_1);
        verifier.DefineExpectedCallOrder(MethodCall_1, MethodCall_2, 0, -1);
        verifier.DefineExpectedCallOrder(MethodCall_2, MethodCall_2, -1, -2);
        Action action = verifier.Verify;
        string expected = $"{MethodCall_1.MethodCallName} call sequence should be less than {GetMethodDisplayName(MethodCall_2.MethodCallName, -1)} call sequence on expected call order #1, but was 3 and 1, respectively.";

        // Act/Assert
        action
            .Should()
            .Throw<VerifierException>()
            .WithMessage(expected);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(-1)]
    public void ExpectedFirstMethodCallNotFound_ShouldThrowException(int callNumber)
    {
        // Arrange
        MethodCallOrderVerifier verifier = new();
        RegisterMethodCall(verifier, MethodCall_2);
        RegisterMethodCall(verifier, MethodCall_3);
        verifier.DefineExpectedCallOrder(MethodCall_1, MethodCall_2, callNumber);
        verifier.DefineExpectedCallOrder(MethodCall_2, MethodCall_3);
        Action action = verifier.Verify;
        string expected = $"The call sequence for {GetMethodDisplayName(MethodCall_1.MethodCallName, callNumber)} on expected call order #1 should be greater than 0, but was 0";

        // Act/Assert
        action
            .Should()
            .Throw<VerifierException>()
            .WithMessage(expected);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(-1)]
    public void ExpectedSecondMethodCallNotFound_ShouldThrowException(int callNumber)
    {
        // Arrange
        MethodCallOrderVerifier verifier = new();
        int methodCallNumber = GetMethodCallNumber(callNumber);
        RegisterMethodCall(verifier, MethodCall_1);
        RegisterMethodCall(verifier, MethodCall_2, methodCallNumber);
        verifier.DefineExpectedCallOrder(MethodCall_1, MethodCall_2, -1, callNumber);
        verifier.DefineExpectedCallOrder(MethodCall_2, MethodCall_1, callNumber, -2);
        Action action = verifier.Verify;
        string expected = $"The call sequence for {GetMethodDisplayName(MethodCall_1.MethodCallName, -2)} on expected call order #2 should be greater than 0, but was 0";

        // Act/Assert
        action
            .Should()
            .Throw<VerifierException>()
            .WithMessage(expected);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    public void FirstAndSecondCallNameAndNumberAreSame_ShouldThrowException(int callNumber)
    {
        // Arrange
        MethodCallOrderVerifier verifier = new();
        RegisterMethodCall(verifier, MethodCall_1, callNumber);
        RegisterMethodCall(verifier, MethodCall_1, callNumber);
        verifier.DefineExpectedCallOrder(MethodCall_1, MethodCall_1, callNumber, callNumber);
        Action action = verifier.Verify;
        string expected = $"The first and second call on expected call order #1 can't both be {MethodCall_1.MethodCallName} with call number {callNumber}";

        // Act/Assert
        action
            .Should()
            .Throw<VerifierException>()
            .WithMessage(expected);
    }

    [Fact]
    public void FirstAndSecondCallNameAreSameAndFirstCallNumberIsGreaterThanSecondCallNumber_ShouldNotThrowException()
    {
        // Arrange
        MethodCallOrderVerifier verifier = new();
        RegisterMethodCall(verifier, MethodCall_1);
        RegisterMethodCall(verifier, MethodCall_1);
        verifier.DefineExpectedCallOrder(MethodCall_1, MethodCall_1, -1, -2);
        Action action = verifier.Verify;

        // Act/Assert
        action
            .Should()
            .NotThrow();
    }

    [Fact]
    public void FirstAndSecondCallNameAreSameAndFirstCallNumberIsLessThanSecondCallNumber_ShouldThrowException()
    {
        // Arrange
        MethodCallOrderVerifier verifier = new();
        RegisterMethodCall(verifier, MethodCall_1);
        RegisterMethodCall(verifier, MethodCall_1);
        verifier.DefineExpectedCallOrder(MethodCall_1, MethodCall_1, -2, -1);
        Action action = verifier.Verify;
        string expected = $"{MethodCall_1.MethodCallName}[+2] can't come before {MethodCall_1.MethodCallName}[+1] on expected call order #1";

        // Act/Assert
        action
            .Should()
            .Throw<VerifierException>()
            .WithMessage(expected);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(0, 1)]
    [InlineData(0, -1)]
    [InlineData(1, 0)]
    [InlineData(1, 1)]
    [InlineData(1, -1)]
    [InlineData(-1, 0)]
    [InlineData(-1, 1)]
    [InlineData(-1, -1)]
    public void FirstAndSecondMethodCallsAreOutOfSequence_ShouldThrowException(int firstCallNumber, int secondCallNumber)
    {
        // Arrange
        MethodCallOrderVerifier verifier = new();
        int firstMethodCallNumber = GetMethodCallNumber(firstCallNumber);
        int secondMethodCallNumber = GetMethodCallNumber(secondCallNumber);
        RegisterMethodCall(verifier, MethodCall_2, secondMethodCallNumber);
        RegisterMethodCall(verifier, MethodCall_1, firstMethodCallNumber);
        verifier.DefineExpectedCallOrder(MethodCall_1, MethodCall_2, firstCallNumber, secondCallNumber);
        Action action = verifier.Verify;
        string expected = $"{GetMethodDisplayName(MethodCall_1.MethodCallName, firstCallNumber)} call sequence should be less than {GetMethodDisplayName(MethodCall_2.MethodCallName, secondCallNumber)} call sequence on expected call order #1, but was 2 and 1, respectively.";

        // Act/Assert
        action
            .Should()
            .Throw<VerifierException>()
            .WithMessage(expected);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(-1)]
    public void MethodCalledMultipleTimesAndSecondIsOutOfSequence_ShouldThrowException(int firstCallNumber)
    {
        // Arrange
        MethodCallOrderVerifier verifier = new();
        int firstMethodCallNumber = GetMethodCallNumber(firstCallNumber);
        int secondCallNumber = GetNextCallNumber(firstCallNumber);
        int secondMethodCallNumber = GetMethodCallNumber(secondCallNumber);
        RegisterMethodCall(verifier, MethodCall_1, firstMethodCallNumber);
        RegisterMethodCall(verifier, MethodCall_2);
        RegisterMethodCall(verifier, MethodCall_3);
        RegisterMethodCall(verifier, MethodCall_1, secondMethodCallNumber);
        verifier.DefineExpectedCallOrder(MethodCall_1, MethodCall_2, firstCallNumber);
        verifier.DefineExpectedCallOrder(MethodCall_2, MethodCall_1, 0, secondCallNumber);
        verifier.DefineExpectedCallOrder(MethodCall_1, MethodCall_3, secondCallNumber);
        Action action = verifier.Verify;
        string expected = $"{GetMethodDisplayName(MethodCall_1.MethodCallName, secondCallNumber)} call sequence should be less than {GetMethodDisplayName(MethodCall_3.MethodCallName, 0)} call sequence on expected call order #3, but was 4 and 3, respectively.";

        // Act/Assert
        action
            .Should()
            .Throw<VerifierException>()
            .WithMessage(expected);
    }

    [Theory]
    [InlineData(-1, 0, 2)]
    [InlineData(0, -2, 1)]
    public void MethodCallWithRelativeAndAbsoluteCallNumbers_ShouldThrowException(int firstCallNumber, int secondCallNumber, int counter)
    {
        // Arrange
        MethodCallOrderVerifier verifier = new();
        RegisterMethodCall(verifier, MethodCall_1);
        RegisterMethodCall(verifier, MethodCall_2);
        RegisterMethodCall(verifier, MethodCall_1);
        verifier.DefineExpectedCallOrder(MethodCall_1, MethodCall_2, firstCallNumber);
        verifier.DefineExpectedCallOrder(MethodCall_2, MethodCall_1, 0, secondCallNumber);
        Action action = verifier.Verify;
        string expected = $"All instances of {MethodCall_1.MethodCallName} should have a negative call number, but found 0 on expected call order #{counter}";

        // Act/Assert
        action
            .Should()
            .Throw<VerifierException>()
            .WithMessage(expected);
    }

    private static int GetMethodCallNumber(int callNumber) => callNumber < 0 ? 0 : callNumber;

    private static string GetMethodDisplayName(string methodName, int callNumber)
    {
        return callNumber == 0
            ? methodName
            : callNumber < 0
            ? $"{methodName}[+{-callNumber}]"
            : $"{methodName}[{callNumber}]";
    }

    private static int GetNextCallNumber(int firstCallNumber)
        => firstCallNumber < 0 ? firstCallNumber - 1 : firstCallNumber > 0 ? firstCallNumber + 1 : 0;

    private static void RegisterMethodCall(MethodCallOrderVerifier verifier, MethodCallToken methodCallToken, int callNumber = 0)
        => verifier.GetCallOrderAction(methodCallToken, callNumber).Invoke();
}