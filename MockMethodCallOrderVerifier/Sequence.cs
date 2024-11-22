namespace MockMethodCallOrderVerifier;

#if !DEBUG
using FluentAssertions;
#endif

/// <summary>
/// The <see cref="Sequence{T}" /> class gives an alternative to the Moq SetupSequence method
/// for returning a series of values from a mock object.
/// </summary>
/// <typeparam name="T">
/// The class type or value type that is to be returned from this sequence.
/// </typeparam>
/// <param name="values">
/// The ordered set of values that will be returned from this sequence.
/// </param>
/// <param name="maxCalls">
/// The maximum number of calls permitted for this sequence.
/// </param>
public class Sequence<T>(T[] values, int maxCalls = int.MaxValue)
{
    private readonly int _maxCalls = maxCalls;
    private readonly int _maxIndex = values.Length - 1;
    private readonly T[] _values = values;
    private int _index = -1;
    private int _totalCalls;

    /// <summary>
    /// Get the current sequence value and stay positioned on that value.
    /// </summary>
    /// <returns>
    /// The current sequence value of type <typeparamref name="T" />.
    /// </returns>
    /// <exception cref="SequenceException" />
    public T Get()
    {
#if DEBUG
        if (_maxCalls < 1)
        {
            string msg = $"The max call count should be greater than zero, but was {_maxCalls}.";
            throw new SequenceException(msg);
        }
#else
        _maxCalls
            .Should()
            .BePositive("the max call count should be at least 1");
#endif

#if DEBUG
        if (_totalCalls >= _maxCalls)
        {
            string msg = $"Total calls for sequence should not be greater than {_maxCalls}, but was {_totalCalls + 1}.";
            throw new SequenceException(msg);
        }
#else
        _totalCalls
            .Should()
            .BeLessThan(_maxCalls, $"total calls on the sequence should be less than {_maxCalls}");
#endif

#if DEBUG
        if (_index < 0)
        {
            string msg = "The Get() method was called before positioning on the first value in the sequence.";
            throw new SequenceException(msg);
        }
#else
        _index
            .Should()
            .BeGreaterThan(-1, "the Next() method must be called before calling the Get() method for the first time");
#endif

        _totalCalls++;
        return _index < _maxIndex ? _values[_index] : _values[_maxIndex];
    }

    /// <summary>
    /// Position the sequence on the next value in the sequence and then return that value.
    /// </summary>
    /// <returns>
    /// The next sequence value of type <typeparamref name="T" />.
    /// </returns>
    /// <remarks>
    /// Calling <see cref="GetNext" /> has the same effect as calling <see cref="Next" />
    /// followed by <see cref="Get" />.
    /// </remarks>
    public T GetNext()
    {
        Next();
        return Get();
    }

    /// <summary>
    /// Position the sequence on the next value in the sequence.
    /// </summary>
    /// <remarks>
    /// Initially the sequence is positioned just before the first value in the sequence.
    /// Therefore, the first time <see cref="Next" /> is called the position will be moved to
    /// the first value in the sequence.
    /// </remarks>
    public void Next()
    {
        if (_index < _maxIndex)
        {
            _index++;
        }
    }
}