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
        public string FirstMethodCallName = FirstMethodCall.MethodCallName;
        public int FirstMethodCallNumber = FirstMethodCall.MethodCallNumber;
        public MethodCallToken FirstMethodCallToken = FirstMethodCall.MethodCallToken;
        public string FirstDisplayName = FirstMethodCall.DisplayName;
        public string SecondMethodCallName = SecondMethodCall.MethodCallName;
        public int SecondMethodCallNumber = SecondMethodCall.MethodCallNumber;
        public MethodCallToken SecondMethodCallToken = SecondMethodCall.MethodCallToken;
        public string SecondDisplayName = SecondMethodCall.DisplayName;
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
        public string DisplayName = MethodCallNumber == 0
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
    /// associated <paramref name="firstCallToken" /> or <paramref name="secondCallToken" /> methods. For
    /// example, -1 refers to the first time the method is called, -2 the second time the method is
    /// called, and so on.
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
    /// <returns>
    /// An <see langword="Action" /> that can be assigned to the Callback method of a Moq Setup.
    /// </returns>
    public Action GetCallOrderAction(MethodCallToken methodCallToken, int callNumber = 0)
    {
        return () => MethodCallList.Add(new(methodCallToken, callNumber));
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

        foreach (MethodCallOrder expectedOrder in ExpectedOrderList)
        {
            int firstCallCounter = expectedOrder.FirstMethodCallNumber;
            int secondCallCounter = expectedOrder.SecondMethodCallNumber;
            int firstCallOrder = 0;
            int secondCallOrder = 0;
            counter++;
            string counterPhrase = $" on expected call order #{counter}";

#if DEBUG
            if (expectedOrder.FirstMethodCall == expectedOrder.SecondMethodCall)
            {
                string msg = $"The first and second call{counterPhrase} can't both be {expectedOrder.FirstMethodCallName} with call number {expectedOrder.FirstMethodCallNumber}";
                throw new VerifierException(msg);
            }
#else
            expectedOrder.FirstCall
                .Should()
                .NotBe(expectedOrder.SecondCall, $"the first and second method calls must not both be {expectedOrder.FirstCallName}{counterPhrase}");
#endif

            if (RelativeMethodCalls.Contains(expectedOrder.FirstMethodCallName))
            {
#if DEBUG
                if (expectedOrder.FirstMethodCallNumber >= 0)
                {
                    string msg = $"All instances of {expectedOrder.FirstMethodCallName} should have a negative call number, but found {expectedOrder.FirstMethodCallNumber}{counterPhrase}";
                    throw new VerifierException(msg);
                }
#else
                expectedOrder.FirstCallNumber
                    .Should()
                    .BeNegative($"{expectedOrder.FirstCallName}{counterPhrase} must specify a negative call number if any other instances do");
#endif
            }

            if (RelativeMethodCalls.Contains(expectedOrder.SecondMethodCallName))
            {
#if DEBUG
                if (expectedOrder.SecondMethodCallNumber >= 0)
                {
                    string msg = $"All instances of {expectedOrder.SecondMethodCallName} should have a negative call number, but found {expectedOrder.SecondMethodCallNumber}{counterPhrase}";
                    throw new VerifierException(msg);
                }
#else
                expectedOrder.SecondCallNumber
                    .Should()
                    .BeNegative($"{expectedOrder.SecondCallName}{counterPhrase} must specify a negative call number if any other instances do");
#endif
            }

            if (expectedOrder.FirstMethodCallToken == expectedOrder.SecondMethodCallToken && expectedOrder.FirstMethodCallNumber < 0)
            {
#if DEBUG
                if (expectedOrder.FirstMethodCallNumber < expectedOrder.SecondMethodCallNumber)
                {
                    string msg = $"{expectedOrder.FirstDisplayName} can't come before {expectedOrder.SecondDisplayName}{counterPhrase}";
                    throw new VerifierException(msg);
                }
#else
                expectedOrder.FirstCallNumber
                    .Should()
                    .BeGreaterThan(expectedOrder.SecondCallNumber, $"{expectedOrder.SecondDisplayName} can't come before {expectedOrder.FirstDisplayName}{counterPhrase}");
#endif
            }

            for (int i = 0; i < MethodCallList.Count; i++)
            {
                MethodCall methodCall = MethodCallList[i];
                MethodCallToken methodCallToken = methodCall.MethodCallToken;
                int methodCallNumber = methodCall.MethodCallNumber;

                if (methodCallToken == expectedOrder.FirstMethodCallToken)
                {
                    firstCallCounter++;
                }

                if (methodCallToken == expectedOrder.SecondMethodCallToken)
                {
                    secondCallCounter++;
                }

                if (firstCallOrder == 0 && methodCallToken == expectedOrder.FirstMethodCallToken)
                {
                    if (firstCallCounter < 0)
                    {
                        continue;
                    }

                    if (expectedOrder.FirstMethodCallNumber < 0 || methodCallNumber == expectedOrder.FirstMethodCallNumber)
                    {
                        firstCallOrder = i + 1;
                        continue;
                    }
                }

                if (methodCallToken == expectedOrder.SecondMethodCallToken)
                {
                    if (expectedOrder.SecondMethodCallNumber < 0 && secondCallOrder > 0)
                    {
                        continue;
                    }

                    if (secondCallCounter < 0)
                    {
                        continue;
                    }

                    if (expectedOrder.SecondMethodCallNumber < 0 || methodCallNumber == expectedOrder.SecondMethodCallNumber)
                    {
                        secondCallOrder = i + 1;

                        if (firstCallOrder > 0)
                        {
                            break;
                        }
                    }
                }
            }

#if DEBUG
            if (firstCallOrder <= 0)
            {
                string msg = $"The call sequence for {expectedOrder.FirstDisplayName}{counterPhrase} should be greater than 0, but was {firstCallOrder}";
                throw new VerifierException(msg);
            }

            if (secondCallOrder <= 0)
            {
                string msg = $"The call sequence for {expectedOrder.SecondDisplayName}{counterPhrase} should be greater than 0, but was {secondCallOrder}";
                throw new VerifierException(msg);
            }

            if (firstCallOrder >= secondCallOrder)
            {
                string msg = $"{expectedOrder.FirstDisplayName} call sequence should be less than {expectedOrder.SecondDisplayName} call sequence{counterPhrase}, but was {firstCallOrder} and {secondCallOrder}, respectively.";
                throw new VerifierException(msg);
            }
#else
            firstCallOrder
                .Should()
                .BePositive($"the call sequence for {expectedOrder.FirstDisplayName}{counterPhrase} should be greater than 0");
            secondCallOrder
                .Should()
                .BePositive($"the call sequence for {expectedOrder.SecondDisplayName}{counterPhrase} should be greater than 0");
            secondCallOrder
                .Should()
                .BeGreaterThan(firstCallOrder, $"{expectedOrder.SecondDisplayName}{counterPhrase} should be called after {expectedOrder.FirstDisplayName}");
#endif
        }
    }
}