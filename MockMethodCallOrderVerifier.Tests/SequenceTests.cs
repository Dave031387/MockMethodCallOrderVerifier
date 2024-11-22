namespace MockMethodCallOrderVerifier;

public class SequenceTests
{
    [Fact]
    public void FirstTimeGetNextIsCalled_ShouldReturnFirstValueInSequence()
    {
        // Arrange
        Sequence<int> sequence = new([1, 2, 3]);
        int expected = 1;

        // Act
        int actual = sequence.GetNext();

        // Assert
        actual
            .Should()
            .Be(expected);
    }

    [Fact]
    public void GetCalledAfterCallingNext_ShouldReturnFirstValueInSequence()
    {
        // Arrange
        Sequence<int> sequence = new([1, 2, 3]);
        sequence.Next();
        int expected = 1;

        // Act
        int actual = sequence.Get();

        // Assert
        actual
            .Should()
            .Be(expected);
    }

    [Fact]
    public void GetCalledAfterCallingNextTwice_ShouldReturnSecondValueInSequence()
    {
        // Arrange
        Sequence<int> sequence = new([1, 2, 3]);
        sequence.Next();
        sequence.Next();
        int expected = 2;

        // Act
        int actual = sequence.Get();

        // Assert
        actual
            .Should()
            .Be(expected);
    }

    [Fact]
    public void GetCalledAfterPositionPastEndOfSequence_ShouldReturnLastValueInSequence()
    {
        // Arrange
        Sequence<int> sequence = new([1, 2, 3]);
        sequence.Next();
        sequence.Next();
        sequence.Next();
        sequence.Next();
        int expected = 3;

        // Act
        int actual = sequence.Get();

        // Assert
        actual
            .Should()
            .Be(expected);
    }

    [Fact]
    public void GetCalledBeforeCallingNext_ShouldThrowException()
    {
        // Arrange
        Sequence<int> sequence = new([1, 2, 3]);
        Action action = () => sequence.Get();
        string expected = "The Get() method was called before positioning on the first value in the sequence.";

        // Act/Assert
        action
            .Should()
            .ThrowExactly<SequenceException>()
            .WithMessage(expected);
    }

    [Fact]
    public void GetNextCalledAfterPositionPastEndOfSequence_ShouldReturnLastValueInSequence()
    {
        // Arrange
        Sequence<int> sequence = new([1, 2, 3]);
        sequence.Next();
        sequence.Next();
        sequence.Next();
        sequence.Next();
        int expected = 3;

        // Act
        int actual = sequence.GetNext();

        // Assert
        actual
            .Should()
            .Be(expected);
    }

    [Fact]
    public void MaxCallCountIsLessThanOne_ShouldThrowException()
    {
        // Arrange
        Sequence<int> sequence = new([1, 2, 3], 0);
        Action action = () => sequence.GetNext();
        string expected = "The max call count should be greater than zero, but was 0.";

        // Act/Assert
        action
            .Should()
            .ThrowExactly<SequenceException>()
            .WithMessage(expected);
    }

    [Fact]
    public void NumberOfGetsExceedsMaxAllowed_ShouldThrowException()
    {
        // Arrange
        Sequence<int> sequence = new([1, 2, 3], 3);
        _ = sequence.GetNext();
        _ = sequence.GetNext();
        _ = sequence.Get();
        Action action = () => sequence.Get();
        string expected = "Total calls for sequence should not be greater than 3, but was 4.";

        // Act/Assert
        action
            .Should()
            .ThrowExactly<SequenceException>()
            .WithMessage(expected);
    }

    [Fact]
    public void SecondTimeGetNextIsCalled_ShouldReturnSecondValueInSequence()
    {
        // Arrange
        Sequence<int> sequence = new([1, 2, 3]);
        _ = sequence.GetNext();
        int expected = 2;

        // Act
        int actual = sequence.GetNext();

        // Assert
        actual
            .Should()
            .Be(expected);
    }
}