namespace MockMethodCallOrderVerifier;

/// <summary>
/// The <see cref="MethodCallToken" /> class is used by the <see cref="MethodCallOrderVerifier" />
/// class to represent a method call on a mock object.
/// </summary>
public class MethodCallToken : IEquatable<MethodCallToken>
{
    private static readonly List<string> _methodCallNames = [];
    private static int _counter;

    /// <summary>
    /// Create a new instance of the <see cref="MethodCallToken" /> class.
    /// </summary>
    /// <param name="methodCallName">
    /// A text string that uniquely identifies the method call.
    /// <para> For example: "ClassName1_MethodName1_AdditionalQualifiers" </para>
    /// </param>
    /// <exception cref="ArgumentException" />
    /// <exception cref="ArgumentNullException" />
    /// <exception cref="InvalidOperationException" />
    public MethodCallToken(string methodCallName)
    {
        ArgumentNullException.ThrowIfNull(methodCallName);

        if (string.IsNullOrWhiteSpace(methodCallName))
        {
            string msg = "Method call name must not be empty or whitespace.";
            throw new ArgumentException(msg);
        }

        if (_methodCallNames.Contains(methodCallName))
        {
            string msg = $"A method call token with the name \"{methodCallName}\" already exists.";
            throw new InvalidOperationException(msg);
        }

        MethodCallName = methodCallName;
        _methodCallNames.Add(methodCallName);
    }

    /// <summary>
    /// Gets the name of the method call.
    /// </summary>
    public string MethodCallName
    {
        get;
    }

    /// <summary>
    /// Gets the unique integer value assigned to this method call token.
    /// </summary>
    public int TokenID { get; } = ++_counter;

    /// <inheritdoc />
    public bool Equals(MethodCallToken? other) => other is not null && TokenID == other.TokenID;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is MethodCallToken methodCall && Equals(methodCall);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(MethodCallName, TokenID);

    internal static void Reset()
    {
        _counter = 0;
        _methodCallNames.Clear();
    }
}