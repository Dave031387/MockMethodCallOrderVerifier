# Mock Method Call Order Verifier Class Library
## Overview
The ***Mock Method Call Order Verifier*** class library was created to provide a means of verifying the order that methods are called on mock objects
in a unit test. The class library was designed specifically to work with the ***Moq*** mocking framework, but it should work with any mocking framework
that provides a *Callback* method on the mock *Setup* or equivalent method.

This class library also provides a ***Sequence*** class object that can be used in place of the *SetupSequence* method of the ***Moq*** mocking
framework. Instead of specifying a string of *Returns* methods off the *SetupSequence* method, the ***Sequence*** class allows a sequence of values to be
returned to the single *Returns* method off the normal *Setup* method. This in turn allows the use of the *Callback* and *Verifiable* methods on the
sequence - something that isn't allowed on the *SetupSequence* method.

> [!Note]
> *The code examples contained in this document make use of C# 12 and .NET 8 features.*

## The *Sequence* Class
The ***Sequence*** class is a simple class that can be used to return a sequence of values in a unit test. The ***Sequence*** class object would
typically be used in the *Returns* method of a mock *Setup*.

### Constructor
The ***Sequence*** constructor definition looks like this:

```csharp
Sequence<T>(T[] values, int maxCalls = int.MaxValue);
```

The type parameter ***T*** can be any value type or reference type.

There is one required parameter (***values***) that provides an array of type ***T***. This array must contain the list of values to be returned from
the sequence, and the values must appear in the array in the order that they are to be returned.

There is one optional parameter (***maxCalls***) that can be used to set a limit on the number of values returned from the sequence. An assertion
failure will be triggered if more than the specified number of values are requested from the sequence.

> [!Note]
> *If the **maxCalls** parameter is omitted, it will default to the maximum integer value. (2,147,483,647 on most modern PC's)*

Here are a few examples of possible sequences that can be created:

```csharp
Sequence<int> intSequence = new([1, 2, 3, 4]);
Sequence<string> fruitSequence = new(["apple", "banana", "cherry"]);

Person alex = new() { FirstName = "Alex"; }
Person bob = new() { FirstName = "Bob"; }
Person charles = new() { FirstName = "Charles"; }
Person david = new() { FirstName = "David"; }
Person eugene = new() { FirstName = "Eugene"; }
Sequence<Person> personSequence = new[alex, bob, charles, david, eugene], 5);
```

### The ***Get*** Method
The ***Get*** method returns the current value from the sequence. An assertion failure will be triggered on any of the following conditions:

- The number of calls to ***Get*** exceeds the value that was given for the ***maxCalls*** parameter on the ***Sequence\<T>*** constructor.
- The ***maxCalls*** parameter on the ***Sequence\<T>*** constructor was set to some value less than 1.
- ***Get*** is called before calling the ***Next*** or ***GetNext*** methods on the sequence.

> [!Note]
> *The reason for the third condition above is that the sequence starts out positioned just before the first value in the sequence. So, attempting
> to get the current value at that point is an invalid operation.*

Calling ***Get*** doesn't alter the current position in the sequence. Repeated calls to ***Get*** without any intervening calls to ***Next*** or
***GetNext*** will always return the same value.

### The ***Next*** Method
The ***Next*** method advances the position in the sequence to the next value. Since the sequence starts out positioned before the first value, the
first call to ***Next*** will position the sequence at the first value. If the current position happens to be at the last value in the sequence,
calling ***Next*** does nothing and the sequence remains positioned on the last value.

> [!Important]
> *Sequences only move forward. There is no option for resetting the position of the sequence back to the first value or any prior value. Similarly,
> there is no option for skipping values moving forward in the sequence.*

### The ***GetNext*** Method
The ***GetNext*** method combines the functions of the ***Next*** and ***Get*** methods. In fact, it simply makes a call to ***Next*** followed
by a call to ***Get*** and then returns the value that it receives from the call to ***Get***.

### Using ***Sequence*** in a Moq Setup
Assume we have a *Billing* class that calls a method named *GetCurrentOrderNumber* on the *OrdersList* class. Also assume that the method returns the
current order number as an integer value. We are unit testing the *Billing* class and want to mock the call to the *GetCurrentOrderNumber* method.
Assume that we expect the first two calls to this method to return the same order number, and the third and subsequent calls return a different
order number. We expect that the method shouldn't be called more than 5 times during the unit test. The following code snippet demonstrates the
use of the ***Sequence*** class in this hypothetical scenario.

```csharp
using MockMethodCallOrderVerifier;
using Xunit;

public class BillingTests
{
    private readonly Mock<IOrdersList> _ordersListMock = new(MockBehavior.Strict);

    [Fact]
    public void BillingTest_0001()
    {
        int firstOrderNumber = 1111;
        int secondOrderNumber = 2222;
        int maxExpectedCalls = 5
        Sequence<int> orderNumberSequence = new([firstOrderNumber, firstOrderNumber, secondOrderNumber], maxExpectedCalls);
        _ordersListMock
            .Setup(ordersList => ordersList.GetCurrentOrderNumber())
            .Returns(orderNumberSequence.GetNext())
            .Verifiable(Times.AtLeast(3));
        // Remainder of method omitted...
    }
}
```

In the above example, the first two times that the mock *GetCurrentOrderNumber* method is called, the value 1111 will be return. On the third, fourth,
and fifth times the method is called, the value 2222 will be returned. If the method is called a sixth time in the same unit test, an assertion
failure will be triggered.

Suppose that the current order number changes only when the *GetNextOrder* method is called on the *OrdersList* class. We can alter the example above
to take this into account.

```csharp
using MockMethodCallOrderVerifier;
using Xunit;

public class BillingTests
{
    private readonly Mock<IOrdersList> _ordersListMock = new(MockBehavior.Strict);

    [Fact]
    public void BillingTest_0001()
    {
        int firstOrderNumber = 1111;
        int secondOrderNumber = 2222;
        int maxExpectedCalls = 5
        Sequence<int> orderNumberSequence = new([firstOrderNumber, secondOrderNumber], maxExpectedCalls);
        _ordersListMock
            .Setup(ordersList => ordersList.GetNextOrder())
            .Callback(() => orderNumberSequence.Next())
            .Verifiable(Times.AtMost(2));
        _ordersListMock
            .Setup(ordersList => ordersList.GetCurrentOrderNumber())
            .Returns(orderNumberSequence.Get())
            .Verifiable(Times.AtLeast(3));
        // Remainder of method omitted...
    }
}
```

In this example, the *GetNext* method call in the *Returns* method of the *GetCurrentOrderNumber* setup is replaced with a call to *Get*.
The call to the *Next* method is then placed in the *Callback* method of the *GetNextOrder* setup. The first time that *GetNextOrder* is called
in the unit test, the *orderNumberSequence* will be positioned on the first value in the sequence. Then, when *GetCurrentOrderNumber* is called,
it will return the first value in the sequence. It will continue returning this value until *GetNextOrder* is called again, at which point calls to
*GetCurrentOrderNumber* will return the second value in the sequence. As in the first example, an assertion failure will be triggered if *Get* is
called more than 5 times on the sequence. Note that an assertion failure will also be triggered if the *GetCurrentOrderNumber* method happens to
get called before the *GetNextOrder* method is called, which is likely what we would want to happen anyway.

## The ***MessageCallToken*** Class
The ***MessageCallToken*** class is used by the ***MethodCallOrderVerifier*** class to represent a specific mock method call.

### Constructor
The constructor of the ***MessageCallToken*** class looks like this:

```csharp
MethodCallToken(string methodCallName);
```

The single required parameter (*methodCallName*) is a string value that is used to identify the particular mock method call being represented.
A typical method call name could be of the form "className_methodName" or "className_propertyName". Sometimes a particular method call with
input parameters can be further distinguished by the types of values assigned to the parameters. In that case the method call name may be of
the form "className_methodName_distinguishingName".

Read/write properties in a mock setup can be set up as either a getter or a setter. Therefore, it may make sense to have two different method call
names for these properties to distinguish between the two. This can easily be accomplished by adding "Get" or "Set" in front of the property name
for getters and setters. For example, the AccountType property on the BankAccount class might use "BankAccount_GetAccountType" for the method call
name when the property is used as a getter, or "BankAccount_SetAccountType" when it is used as a setter. If so desired, you could also add a qualifying
name to the setter names to distinguish between different set values, such as "BankAccount_SetAccoutType_Savings" and
"BankAccount_SetAccountType_Checking".

> [!Note]
> *Obviously, these are just suggestions. You may use whatever naming scheme makes sense to you. The important thing is that each mock method call
> should have a unique name that you can easily tie back to the corresponding mock class and method/property name.*

Method call tokens are typically defined in a static class that is shared between all the unit test classes in a given unit test project. Here is an
example:

```csharp
using MockMethodCallOrderVerifier;

public static class MethodCallTokens
{
    public static readonly MethodCallToken Logger_Log = new(nameof(Logger_Log));
    public static readonly MethodCallToken BankAccount_Create_Checking = new(nameof(BankAccount_Create_Checking));
    public static readonly MethodCallToken BankAccount_Create_Savings = new(nameof(BankAccount_Create_Savings));
    public static readonly MethodCallToken ContactsList_RemoveContact = new(nameof(ContactList_RemoveContact));
}
```

The constructor of the ***MethodCallToken*** class will throw an exception on any of the following conditions:

- An *ArgumentNull* exception will be thrown if the *methodCallName* parameter is null.
- An *Argument* exception will be thrown if the *methodCallName* parameter is an empty string or only whitespace characters.
- An *InvalidOperation* exception will be thrown if you attempt to create a ***MethodCallToken*** with a *methodCallName* that matches any previously
  defined ***MethodCallToken***.

### Properties
The ***MethodCallToken*** class has two read-only properties:

- The ***MethodCallName*** property returns the method call name that was assigned when the ***MethodCallToken*** was created.
- The ***TokenID*** property returns a unique integer value. This integer value is assigned automatically when the ***MethodCallToken*** is created.

## The ***MethodCallOrderVerifier*** Class
