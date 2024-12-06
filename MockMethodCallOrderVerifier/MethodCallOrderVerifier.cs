namespace MockMethodCallOrderVerifier;

#if !DEBUG
using FluentAssertions;
#endif

using System;
using System.Collections.Generic;

/// <summary>
/// This class can be used to verify the order of method calls on mock objects in a unit test. It
/// was written to be used with the Moq mocking framework, but can be used with other frameworks
/// that provide a callback functionality on their Setup method and allow an action delegate to be
/// used for the mock method return value.
/// </summary>
public class MethodCallOrderVerifier
{
    /// <summary>
    /// A counter of the number of times the first method call of a <see cref="MethodCallOrder" />
    /// object has been matched.
    /// </summary>
    /// <remarks>
    /// The counter starts at a negative value and counts up to zero.
    /// </remarks>
    private int _firstCallCounter;

    /// <summary>
    /// The position within the <see cref="MethodCallList" /> where the mock method call matching
    /// the first method call in the <see cref="MethodCallOrder" /> object was found.
    /// </summary>
    private int _firstCallOrder;

    /// <summary>
    /// A counter of the number of times the second method call of a <see cref="MethodCallOrder" />
    /// object has been matched.
    /// </summary>
    /// <remarks>
    /// The counter starts at a negative value and counts up to zero.
    /// </remarks>
    private int _secondCallCounter;

    /// <summary>
    /// The position within the <see cref="MethodCallList" /> where the mock method call matching
    /// the second method call in the <see cref="MethodCallOrder" /> object was found.
    /// </summary>
    private int _secondCallOrder;

    /// <summary>
    /// Gets a list of <see cref="MethodCallOrder" /> objects that define the expected order of mock
    /// method calls.
    /// </summary>
    private List<MethodCallOrder> ExpectedOrderList { get; } = [];

    /// <summary>
    /// Gets a list of <see cref="MethodCall" /> records. The records will appear in the list in the
    /// order that each mock method was called during the unit test.
    /// </summary>
    private List<MethodCall> MethodCallList { get; } = [];

    /// <summary>
    /// Gets a list of <see cref="MethodCallToken" /> values for which at least one
    /// <see cref="MethodCallOrder" /> record specified a negative call number.
    /// </summary>
    private List<string> RelativeMethodCalls { get; } = [];

    /// <summary>
    /// This record defines the expected order of two mock method calls within a single unit test.
    /// </summary>
    /// <param name="FirstMethodCall">
    /// The <see cref="MethodCall" /> that should be invoked first in sequence.
    /// </param>
    /// <param name="SecondMethodCall">
    /// The <see cref="MethodCall" /> that should be invoked second in sequence.
    /// </param>
    private sealed record MethodCallOrder(MethodCall FirstMethodCall, MethodCall SecondMethodCall)
    {
        public string FirstMethodCallName => FirstMethodCall.MethodCallName;
        public int FirstMethodCallNumber => FirstMethodCall.MethodCallNumber;
        public MethodCallToken FirstMethodCallToken => FirstMethodCall.MethodCallToken;
        public string FirstDisplayName => FirstMethodCall.DisplayName;
        public string SecondMethodCallName => SecondMethodCall.MethodCallName;
        public int SecondMethodCallNumber => SecondMethodCall.MethodCallNumber;
        public MethodCallToken SecondMethodCallToken => SecondMethodCall.MethodCallToken;
        public string SecondDisplayName => SecondMethodCall.DisplayName;
    }

    /// <summary>
    /// This record represents a single mock method call during a unit test.
    /// </summary>
    /// <param name="MethodCallToken">
    /// The <see cref="MethodCallToken" /> that corresponds to the mock method being called in the
    /// unit test.
    /// </param>
    /// <param name="MethodCallNumber">
    /// If the mock method call represented by the <paramref name="MethodCallToken" /> is called
    /// more than once in the unit test with different parameter values, then each occurrence can be
    /// assigned a unique <paramref name="MethodCallNumber" /> to tell them apart. If not specified,
    /// this value defaults to 0.
    /// </param>
    private sealed record MethodCall(MethodCallToken MethodCallToken, int MethodCallNumber = 0)
    {
        /// <summary>
        /// Gets the display name for this method call.
        /// </summary>
        public string DisplayName => MethodCallNumber == 0
            ? MethodCallToken.MethodCallName
            : MethodCallNumber < 0
            ? $"{MethodCallToken.MethodCallName}[+{-MethodCallNumber}]"
            : $"{MethodCallToken.MethodCallName}[{MethodCallNumber}]";

        /// <summary>
        /// Gets the method call name for this method call.
        /// </summary>
        public string MethodCallName => MethodCallToken.MethodCallName;
    }

    /// <summary>
    /// Defines an expected method call order sequence to be verified later by calling the
    /// <see cref="Verify" /> method.
    /// </summary>
    /// <param name="firstCallToken">
    /// The <see cref="MethodCallToken" /> representing the first mock method to be called in
    /// sequence.
    /// </param>
    /// <param name="secondCallToken">
    /// The <see cref="MethodCallToken" /> representing the second mock method to be called in
    /// sequence.
    /// </param>
    /// <param name="firstCallNumber">
    /// Optional call number of the <paramref name="firstCallToken" /> method. (default is 0)
    /// </param>
    /// <param name="secondCallNumber">
    /// Optional call number of the <paramref name="secondCallToken" /> method. (default is 0)
    /// </param>
    /// <remarks>
    /// A negative value for either <paramref name="firstCallNumber" /> or
    /// <paramref name="secondCallNumber" /> indicates the specific method call number for the
    /// associated <paramref name="firstCallToken" /> or <paramref name="secondCallToken" />
    /// methods. For example, -1 refers to the first time the method is called, -2 the second time
    /// the method is called, and so on.
    /// <para>
    /// Positive numbers, on the other hand, are used only to distinguish between instances of a
    /// method that is called multiple times with different parameter values. For example, if
    /// MethodA is called three times with different parameter values each time, then a unique
    /// positive number should be assigned to each call.
    /// </para>
    /// </remarks>
    public void DefineExpectedCallOrder(MethodCallToken firstCallToken, MethodCallToken secondCallToken, int firstCallNumber = 0, int secondCallNumber = 0)
    {
        ArgumentNullException.ThrowIfNull(firstCallToken);
        ArgumentNullException.ThrowIfNull(secondCallToken);
        MethodCall firstMethodCall = new(firstCallToken, firstCallNumber);
        MethodCall secondMethodCall = new(secondCallToken, secondCallNumber);
        ExpectedOrderList.Add(new(firstMethodCall, secondMethodCall));

        if (firstCallNumber < 0 && !RelativeMethodCalls.Contains(firstCallToken.MethodCallName))
        {
            RelativeMethodCalls.Add(firstCallToken.MethodCallName);
        }

        if (secondCallNumber < 0 && !RelativeMethodCalls.Contains(secondCallToken.MethodCallName))
        {
            RelativeMethodCalls.Add(secondCallToken.MethodCallName);
        }
    }

    /// <summary>
    /// This method returns an <see langword="Action" /> that can be assigned to the Callback method
    /// of a Moq Setup. This <see langword="Action" /> saves a record of the mock method call for
    /// future verification.
    /// </summary>
    /// <param name="methodCallToken">
    /// The <see cref="MethodCallToken" /> representing the mock method being called.
    /// </param>
    /// <param name="callNumber">
    /// An optional call number used to distinguish the <paramref name="methodCallToken" /> if the
    /// associated mock method is called with different parameter values in two or more Moq Setups.
    /// </param>
    /// <param name="callbackAction">
    /// An optional action delegate that gets invoked when the returned call order action is
    /// invoked.
    /// </param>
    /// <returns>
    /// An <see langword="Action" /> that can be assigned to the Callback method of a Moq Setup.
    /// </returns>
    public Action GetCallOrderAction(MethodCallToken methodCallToken, int callNumber = 0, Action? callbackAction = null)
    {
        return () =>
        {
            callbackAction?.Invoke();
            MethodCallList.Add(new(methodCallToken, callNumber));
        };
    }

    /// <summary>
    /// This method resets the state of the <see cref="MethodCallOrderVerifier" /> class to its
    /// initial state.
    /// </summary>
    public void Reset()
    {
        MethodCallList.Clear();
        ExpectedOrderList.Clear();
        RelativeMethodCalls.Clear();
    }

    /// <summary>
    /// This method iterates through the list of expected mock method call order objects and
    /// verifies that each mock method was called in the expected order.
    /// <para>
    /// Any discrepancies between the expected and actual method call order will result in an
    /// assertion failure with an appropriate message identifying the cause of the failure.
    /// </para>
    /// </summary>
    public void Verify()
    {
        int counter = 0;

        foreach (MethodCallOrder expectedCallOrder in ExpectedOrderList)
        {
            _firstCallCounter = expectedCallOrder.FirstMethodCallNumber;
            _secondCallCounter = expectedCallOrder.SecondMethodCallNumber;
            _firstCallOrder = 0;
            _secondCallOrder = 0;
            counter++;
            string counterPhrase = $" on expected call order #{counter}";

            ValidateMethodCallOrderSetup(expectedCallOrder, counterPhrase);

            for (int i = 0; i < MethodCallList.Count; i++)
            {
                MethodCall methodCall = MethodCallList[i];

                IncrementMethodCallCounters(methodCall, expectedCallOrder);

                if (IsMatchForFirstMethodCall(methodCall, expectedCallOrder, i))
                {
                    continue;
                }

                if (IsMethodCallOrderMatchFound(methodCall, expectedCallOrder, i))
                {
                    break;
                }
            }

            AssertMethodCallOrderResults(expectedCallOrder, counterPhrase);
        }
    }

    /// <summary>
    /// Assert that the expected mock method call order has been achieved by the unit test.
    /// </summary>
    /// <param name="methodCallOrder">
    /// The <see cref="MethodCallOrder" /> object defining the expected order of two mock method
    /// calls.
    /// </param>
    /// <param name="counterPhrase">
    /// A text string that identifies where the <paramref name="methodCallOrder" /> is located in
    /// the list of <see cref="MethodCallOrder" /> objects for the current unit test.
    /// </param>
    /// <exception cref="VerifierException" />
    private void AssertMethodCallOrderResults(MethodCallOrder methodCallOrder, string counterPhrase)
    {
#if DEBUG
        if (_firstCallOrder <= 0)
        {
            string msg = $"The call sequence for {methodCallOrder.FirstDisplayName}{counterPhrase} should be greater than 0, but was {_firstCallOrder}";
            throw new VerifierException(msg);
        }

        if (_secondCallOrder <= 0)
        {
            string msg = $"The call sequence for {methodCallOrder.SecondDisplayName}{counterPhrase} should be greater than 0, but was {_secondCallOrder}";
            throw new VerifierException(msg);
        }

        if (_firstCallOrder >= _secondCallOrder)
        {
            string msg = $"{methodCallOrder.FirstDisplayName} call sequence should be less than {methodCallOrder.SecondDisplayName} call sequence{counterPhrase}, but was {_firstCallOrder} and {_secondCallOrder}, respectively.";
            throw new VerifierException(msg);
        }
#else
            _firstCallOrder
                .Should()
                .BePositive($"the call sequence for {methodCallOrder.FirstDisplayName}{counterPhrase} should be greater than 0");
            _secondCallOrder
                .Should()
                .BePositive($"the call sequence for {methodCallOrder.SecondDisplayName}{counterPhrase} should be greater than 0");
            _secondCallOrder
                .Should()
                .BeGreaterThan(_firstCallOrder, $"{methodCallOrder.SecondDisplayName}{counterPhrase} should be called after {methodCallOrder.FirstDisplayName}");
#endif
    }

    /// <summary>
    /// Check to see if the given <see cref="MethodCall" /> matches either the first and/or the
    /// second <see cref="MethodCall" /> in the given <see cref="MethodCallOrder" /> object.
    /// Increment the appropriate call counter(s) if it does.
    /// </summary>
    /// <param name="methodCall">
    /// The <see cref="MethodCall" /> to be compared to the tokens defined in the given
    /// <see cref="MethodCallOrder" /> object.
    /// </param>
    /// <param name="methodCallOrder">
    /// A <see cref="MethodCallOrder" /> object that defines the order of two method calls.
    /// </param>
    private void IncrementMethodCallCounters(MethodCall methodCall, MethodCallOrder methodCallOrder)
    {
        if (_firstCallCounter < 0 && methodCall.MethodCallToken == methodCallOrder.FirstMethodCallToken)
        {
            _firstCallCounter++;
        }

        if (_secondCallCounter < 0 && methodCall.MethodCallToken == methodCallOrder.SecondMethodCallToken)
        {
            _secondCallCounter++;
        }
    }

    /// <summary>
    /// Compares the given <see cref="MethodCall" /> against the first method call defined in the
    /// given <see cref="MethodCallOrder" /> object.
    /// </summary>
    /// <param name="methodCall">
    /// The <see cref="MethodCall" /> to be compared against the <paramref name="methodCallOrder" />
    /// object.
    /// </param>
    /// <param name="methodCallOrder">
    /// The <see cref="MethodCallOrder" /> object that the <paramref name="methodCall" /> object is
    /// being compared to.
    /// </param>
    /// <param name="position">
    /// An integer representing the current position in the <see cref="MethodCallList" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> if the given <see cref="MethodCall" /> is a match for the first
    /// method call defined in the given <see cref="MethodCallOrder" /> object. Otherwise, returns
    /// <see langword="false" />.
    /// </returns>
    private bool IsMatchForFirstMethodCall(MethodCall methodCall, MethodCallOrder methodCallOrder, int position)
    {
        if (_firstCallOrder == 0 && methodCall.MethodCallToken == methodCallOrder.FirstMethodCallToken)
        {
            if (_firstCallCounter < 0)
            {
                return true;
            }

            if (methodCallOrder.FirstMethodCallNumber < 0 || methodCall.MethodCallNumber == methodCallOrder.FirstMethodCallNumber)
            {
                _firstCallOrder = position + 1;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Compares the given <see cref="MethodCall" /> against the second method call defined in the
    /// given <see cref="MethodCallOrder" /> object.
    /// </summary>
    /// <param name="methodCall">
    /// The <see cref="MethodCall" /> to be compared against the <paramref name="methodCallOrder" />
    /// object.
    /// </param>
    /// <param name="methodCallOrder">
    /// The <see cref="MethodCallOrder" /> object that the <paramref name="methodCall" /> object is
    /// being compared to.
    /// </param>
    /// <param name="position">
    /// An integer representing the current position in the <see cref="MethodCallList" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> if the given <see cref="MethodCall" /> is a match for the second
    /// method call defined in the given <see cref="MethodCallOrder" /> object and the first method
    /// call has previously been matched. Otherwise, returns <see langword="false" />.
    /// </returns>
    private bool IsMethodCallOrderMatchFound(MethodCall methodCall, MethodCallOrder methodCallOrder, int position)
    {
        if (methodCall.MethodCallToken == methodCallOrder.SecondMethodCallToken)
        {
            if (methodCallOrder.SecondMethodCallNumber < 0 && _secondCallOrder > 0)
            {
                return false;
            }

            if (_secondCallCounter < 0)
            {
                return false;
            }

            if (methodCallOrder.SecondMethodCallNumber < 0 || methodCall.MethodCallNumber == methodCallOrder.SecondMethodCallNumber)
            {
                _secondCallOrder = position + 1;

                if (_firstCallOrder > 0)
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Validate that the given <see cref="MethodCallOrder" /> is logically consistent.
    /// </summary>
    /// <param name="methodCallOrder">
    /// The <see cref="MethodCallOrder" /> to be validated.
    /// </param>
    /// <param name="counterPhrase">
    /// A text string that identifies where the <paramref name="methodCallOrder" /> is located in
    /// the list of <see cref="MethodCallOrder" /> objects for the current unit test.
    /// </param>
    /// <exception cref="VerifierException" />
    private void ValidateMethodCallOrderSetup(MethodCallOrder methodCallOrder, string counterPhrase)
    {
#if DEBUG
        if (methodCallOrder.FirstMethodCall == methodCallOrder.SecondMethodCall)
        {
            string msg = $"The first and second call{counterPhrase} can't both be {methodCallOrder.FirstMethodCallName} with call number {methodCallOrder.FirstMethodCallNumber}";
            throw new VerifierException(msg);
        }
#else
            expectedOrder.FirstCall
                .Should()
                .NotBe(expectedOrder.SecondCall, $"the first and second method calls must not both be {expectedOrder.FirstCallName}{counterPhrase}");
#endif

        if (RelativeMethodCalls.Contains(methodCallOrder.FirstMethodCallName))
        {
#if DEBUG
            if (methodCallOrder.FirstMethodCallNumber >= 0)
            {
                string msg = $"All instances of {methodCallOrder.FirstMethodCallName} should have a negative call number, but found {methodCallOrder.FirstMethodCallNumber}{counterPhrase}";
                throw new VerifierException(msg);
            }
#else
                expectedOrder.FirstCallNumber
                    .Should()
                    .BeNegative($"{expectedOrder.FirstCallName}{counterPhrase} must specify a negative call number if any other instances do");
#endif
        }

        if (RelativeMethodCalls.Contains(methodCallOrder.SecondMethodCallName))
        {
#if DEBUG
            if (methodCallOrder.SecondMethodCallNumber >= 0)
            {
                string msg = $"All instances of {methodCallOrder.SecondMethodCallName} should have a negative call number, but found {methodCallOrder.SecondMethodCallNumber}{counterPhrase}";
                throw new VerifierException(msg);
            }
#else
                expectedOrder.SecondCallNumber
                    .Should()
                    .BeNegative($"{expectedOrder.SecondCallName}{counterPhrase} must specify a negative call number if any other instances do");
#endif
        }

        if (methodCallOrder.FirstMethodCallToken == methodCallOrder.SecondMethodCallToken && methodCallOrder.FirstMethodCallNumber < 0)
        {
#if DEBUG
            if (methodCallOrder.FirstMethodCallNumber < methodCallOrder.SecondMethodCallNumber)
            {
                string msg = $"{methodCallOrder.FirstDisplayName} can't come before {methodCallOrder.SecondDisplayName}{counterPhrase}";
                throw new VerifierException(msg);
            }
#else
                expectedOrder.FirstCallNumber
                    .Should()
                    .BeGreaterThan(expectedOrder.SecondCallNumber, $"{expectedOrder.SecondDisplayName} can't come before {expectedOrder.FirstDisplayName}{counterPhrase}");
#endif
        }
    }
}