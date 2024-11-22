namespace MockMethodCallOrderVerifier;

/// <summary>
/// The <see cref="MethodCallToken" /> class is used by the <see cref="MethodCallOrderVerifier" />
/// class to represent a method call on a mock object.
/// </summary>
/// <param name="name">
/// A text string that uniquely identifies the method call.
/// <para>
/// For example: "ClassName1_MethodName1_AdditionalQualifiers"
/// </para>
/// </param>
public class MethodCallToken(string name) : IEquatable<MethodCallToken>
{
    private static int _counter;

    /// <summary>
    /// Gets the name of the method call.
    /// </summary>
    public string Name { get; private set; } = name;

    /// <summary>
    /// Gets the unique integer value assigned to this method call.
    /// </summary>
    public int Value { get; private set; } = NextNumber;

    private static int NextNumber => _counter++;

    /// <inheritdoc/>
    public bool Equals(MethodCallToken? other) => other is not null && Value == other.Value;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is MethodCallToken methodCall && Equals(methodCall);

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(Name, Value);
}
