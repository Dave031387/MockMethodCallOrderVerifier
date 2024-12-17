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
> *The code examples contained in this document make use of C# 12 and .NET 8 features. Also, the xUnit test framework and Moq mocking framework are used.*

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
> *If the **maxCalls** parameter is omitted it will default to the maximum integer value. (2,147,483,647 on most modern PC's)*

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

### The ***GetCurrent*** Method
The ***GetCurrent*** method returns the current value from the sequence. An assertion failure will be triggered on any of the following conditions:

- The number of calls to ***GetCurrent*** exceeds the value that was given for the ***maxCalls*** parameter on the ***Sequence\<T>*** constructor.
- The ***maxCalls*** parameter on the ***Sequence\<T>*** constructor was set to some value less than 1.
- ***GetCurrent*** is called before calling the ***MoveNext*** or ***GetNext*** methods on the sequence.

> [!Note]
> *The reason for the third condition above is that the sequence starts out positioned just before the first value in the sequence. So, attempting
> to get the current value at that point is an invalid operation.*

Calling ***GetCurrent*** doesn't alter the current position in the sequence. Repeated calls to ***GetCurrent*** without any intervening calls to
***MoveNext*** or ***GetNext*** will always return the same value.

### The ***MoveNext*** Method
The ***MoveNext*** method advances the position in the sequence to the next value. Since the sequence starts out positioned before the first value, the
first call to ***MoveNext*** will position the sequence at the first value. If the current position happens to be at the last value in the sequence,
calling ***MoveNext*** does nothing and the sequence remains positioned on the last value.

> [!Important]
> *Sequences only move forward. There is no option for resetting the position of the sequence back to the first value or any prior value. Similarly,
> there is no option for skipping values moving forward in the sequence.*

### The ***GetNext*** Method
The ***GetNext*** method combines the functions of the ***MoveNext*** and ***GetCurrent*** methods. In fact, it simply makes a call to ***MoveNext***
followed by a call to ***GetCurrent*** and then returns the value that it receives from the call to ***GetCurrent***.

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
            .Callback(() => orderNumberSequence.MoveNext())
            .Verifiable(Times.AtMost(2));
        _ordersListMock
            .Setup(ordersList => ordersList.GetCurrentOrderNumber())
            .Returns(orderNumberSequence.GetCurrent())
            .Verifiable(Times.AtLeast(3));
        // Remainder of method omitted...
    }
}
```

In this example, the *GetNext* method call in the *Returns* method of the *GetCurrentOrderNumber* setup is replaced with a call to *GetCurrent*.
The call to the *MoveNext* method is then placed in the *Callback* method of the *GetNextOrder* setup. The first time that *GetNextOrder* is called
in the unit test, the *orderNumberSequence* will be positioned on the first value in the sequence. Then, when *GetCurrentOrderNumber* is called,
it will return the first value in the sequence. It will continue returning this value until *GetNextOrder* is called again, at which point calls to
*GetCurrentOrderNumber* will return the second value in the sequence. As in the first example, an assertion failure will be triggered if
*GetCurrent* is called more than 5 times on the sequence. Note that an assertion failure will also be triggered if the *GetCurrentOrderNumber* method
happens to get called before the *GetNextOrder* method is called, which is likely what we would want to happen anyway.

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
The ***MethodCallOrderVerifier*** class is used to keep track of the mock method calls that are made during a unit test, and then to verify
that those calls were made in the expected order.

### The Constructor
The class has only a default constructor, so creating an instance of the class is as simple as:

```csharp
MethodCallOrderVerifier verifier = new();
```

Typically you would create a new instance of the ***MethodCallOrderVerifier*** class in each unit test where you want to use it. However, you can
also create an instance of the class as a member of your unit test class and then reference that same member in each unit test where it is needed.
If you do this, though, you will need to call the ***Reset*** method on the ***MethodCallOrderVerifier*** class object at the start of each unit test to
reset the state of the object. Here's an example:

```csharp
public class MyUnitTestClass
{
    private readonly MethodCallOrderVerifier verifier = new();

    [Fact]
    public void UnitTest1()
    {
        // Arrange
        verifier.Reset();
        // ... rest of arrange statements

        // Act
        // ... perform your test

        // Assert
        verifier.Verify();
        // ... rest of assertion statements
    }
}
```

Each additional unit test method in the unit test class would look similar to the one in the above example.

### The ***GetCallOrderAction*** Method
The ***MethodCallOrderVerifier*** class takes advantage of the *Callback* method of the Moq *Setup*. The ***GetCallOrderAction*** method
generates an *Action* that, when invoked by the *Callback* mechanism, records the details of the mock method call for later verification.

The ***GetCallOrderAction*** method has the following definition:

```csharp
public Action GetCallOrderAction(MethodCallToken methodCallToken,
                                 int callNumber = 0,
                                 Action? callbackAction = null);
```

The first parameter is required and must be a ***MethodCallToken***. Refer to the earlier section in this document for details on the
***MethodCallToken*** class.

The second parameter is an optional method call number. This parameter is useful for cases where the same mock method is called more than once
in a single unit test, but with different input parameters. Each Moq *Setup* for that mock method can specify a different call number to
distinguish between them. The default value for this parameter is 0 if the parameter isn't used. The specified value must not be less than zero.

> [!Important]
> *An assertion failure will be thrown when the call order action is invoked if the callNumber parameter happens to be less than zero.*

The third parameter is an optional callback action. The Moq *Callback* method is normally used to provide a means of interacting with the mock
method call in some way. For example, you could capture the values that get passed into the parameters on the mock method call. The callback
action parameter of the ***GetCallOrderAction*** gives you the means of providing your own action that will get invoked along with the action
that is returned from the ***GetCallOrderAction*** method.

To demonstrate the use of the ***GetCallOrderAction*** method, assume we have the following two classes:

```csharp
public class SimpleClass : ISimpleClass
{
    public SimpleClass() {}
    public void WriteMessage(string msg) { Console.WriteLine(msg); }
}

public class MyClass
{
    private readonly ISimpleClass _simpleClass;
    public MyClass(ISimpleClass _simpleClass) { _simpleClass = simpleClass; }
    public void DoSomething()
    {
        string statusText;
        // Some code that alters the value statusText
        // and calls methods on other classes.
        _simpleClass.WriteMessage(statusText);
    }
}
```

Assume we also have a *MethodCallTokens* class that contains the following line:

```csharp
public static readonly MethodCallToken SimpleClass_WriteMessage = new(nameof(SimpleClass_WriteMessage));
```

Now suppose we want to unit test the *DoSomething* method of the *MyClass* class and we are interested in logging the fact that the *WriteMessage*
method of the *SimpleClass* class was called. We are also interested in verifying the order that the *DoSomething* method gets called relative to
other method calls in the *DoSomething* class. The Moq *Setup* for this scenario might look something like this:

```csharp
public class MyUnitTests
{
    private readonly Mock<ISimpleClass> _simpleClassMock = new();

    [Fact]
    public void UnitTest1()
    {
        // Arrange
        _simpleClassMock.Reset();
        MethodCallOrderVerifier verifier = new();
        MyClass myClass = new(_simpleClassMock.Object);
        void callbackAction() => Console.WriteLine("The SimpleClass.WriteMessage method was called.");
        _simpleClassMock
            .Setup(simpleClass => simpleClass.WriteMessage(It.IsAny<string>()))
            .Callback(verifier.GetCallOrderAction(SimpleClass_WriteMessage, 0, callbackAction))
            .Verifiable(Times.Once);

        // Act
        myClass.DoSomething("status");

        // Assert
        // ... assertions go here
    }
}
```

In this example, when *UnitTest1* is run and method *MyClass.DoSomething* is invoked, the following sequence of events will occur:

1. The *WriteMessage* method will get called on the mock *SimpleClass* object.
1. When *WriteMessage* is called, the *Callback* method on the *simpleClassMock* Moq *Setup* will get invoked.
1. The *Callback* method will in turn invoke the action that gets returned from the ***GetCallOrderAction*** method of the
   ***MethodCallOrderVerifier*** class instance. This will cause the verifier to save the ***MethodCallToken*** and call number
   so that the mock method call order can later be verified.
1. Finally, the *callbackAction* will be invoked and the message *"The SimpleClass.WriteMessage method was called."* will be written to the
   console.

> [!Important]
> *The **callbackAction** parameter on the **GetCallOrderAction** method can only be a parameterless action. The **MethodCallOrderVerifier**
> class is not capable of providing the same level of functionality as the Moq **Callback** method. Therefore, if you require access to the
> parameters of the mock method call, you will need to take a somewhat more complicated approach. (see following example)*

Suppose that instead of writing a message to the console we instead wanted to capture the value that was passed into the *msg* parameter of
the mock *WriteMessage* method call. We would then need to create a callback action with a string parameter. That callback action would need
to save the value of the string parameter. It would also need to invoke the ***GetCallOrderAction***. The previous example would look something
like the following after making these changes.

```csharp
public class MyUnitTests
{
    private readonly Mock<ISimpleClass> _simpleClassMock = new();

    [Fact]
    public void UnitTest1()
    {
        // Arrange
        _simpleClassMock.Reset();
        MethodCallOrderVerifier verifier = new();
        MyClass myClass = new(_simpleClassMock.Object);
        string msgSave;
        string expected = "Completed";
        void callbackAction(string msg)
        {
            msgSave = msg;
            verifier.GetCallOrderAction(SimpleClass_WriteMessage).Invoke();
        }
        _simpleClassMock
            .Setup(simpleClass => simpleClass.WriteMessage(It.IsAny<string>()))
            .Callback((Action<string>)callbackAction)
            .Verifiable(Times.Once);

        // Act
        myClass.DoSomething(expected);

        // Assert
        Assert.AreEqual(expected, msgSave, "The string passed to WriteMessage wasn't the expected value.");
        // ... additional assertions go here
    }
}
```

As you can see, this example is a bit more complicated than the previous example. This time when *UnitTest1* is run and method
*MyClass.DoSomething* is invoked, the following will happen:

1. The *WriteMessage* method will get called on the mock *SimpleClass* object.
1. When *WriteMessage* is called, the *Callback* method on the *simpleClassMock* Moq *Setup* will get invoked.
1. The *Callback* method will in turn invoke the *callbackAction* and pass in the string value that was passed into
   the *WriteMessage* method.
1. The *callbackAction*, when invoked, will:
   - Set the *msgSave* variable to the value that was passed into the *WriteMessage* method.
   - Invoke the ***GetCallOrderAction*** method of the ***MethodCallOrderVerifier*** class instance. This will cause the verifier
      to save the ***MethodCallToken*** and call number so that the mock method call order can later be verified.

> [!Note]
> *The previous two examples were just to illustrate the function of the **GetCallOrderAction** method. A real-life example would
> have used at least two Moq setups that make use of **GetCallOrderAction** in the Callback method. It would have also included
> at least one call to the **DefineExpectedCallOrder** method of the **MethodCallOrderVerifier** class instance.*

### The ***DefineExpectedCallOrder*** Method
As the name implies, the ***DefineExpectedCallOrder*** method is used to define the order that mock method calls should be made.
The method has the following definition:

```csharp
public void DefineExpectedCallOrder(MethodCallToken firstCallToken,
                                    MethodCallToken secondCallToken,
                                    int firstCallNumber = 0,
                                    int secondCallNumber = 0);
```

The first two parameters are required and must be ***MethodCallToken*** objects that represent mock method calls. It is expected
that the mock method represented by the *firstCallToken* parameter should be called before the mock method represented by the
*secondCallToken* parameter. The ***DefineExpectedCallOrder*** method doesn't verify that this is indeed the case. It only defines
what the expectations are. (See the section later that describes the ***Verify*** method.)

The two optional integer parameters determine how the *firstCallToken* and *secondCallToken* are matched by the ***Verify*** method.
These parameters are needed only if the mock methods represented by *firstCallToken* and/or *secondCallToken* are called more than
once during a unit test and you need to be able to match on a specific occurrence of the method call when the mock method calls for
the unit test are verified by the ***Verify*** method. There are three possible scenarios:

- If the call number parameter is greater than zero, then the ***MethodCallToken*** and call number specified in the
  ***DefineExpectedCallOrder*** statement must match the ***MethodCallToken*** and call number that were specified on the
  relevant ***GetCallOrderAction*** statement.
- If the call number is omitted or is specified as zero, then only the ***MethodCallToken*** on the ***DefineExpectedCallOrder***
  statement is matched against the ***MethodCallToken*** of the ***GetCallOrderAction*** statements. The call numbers specified on
  the ***GetCallOrderAction*** statements are ignored.
- If the call number parameter is less than zero, then the match is made on a specific occurrence of the mock method call determined
  by the absolute value of the call number value. (e.g., -1 matches only the first occurrence, -2 matches only the second, etc.)
  The call numbers specified on the ***GetCallOrderAction*** statements are ignored.

This will all probably be clearer with some examples. For the purposes of illustration we're going to assume that we are working on
a unit test that involves one or more mock objects and we want to validate that the mock methods are called in the correct order.
This implies that we have some Moq setups and we are making use of the ***GetCallOrderAction*** method in the Moq *Callback* on each
Moq *Setup*. To keep things generic and simple we're also going to assume the mock method names are *Method1*, *Method2*, *Method3*,
etc. We are also going to assume the ***MethodCallToken*** names match the mock method names. The following example is going to be
a bit contrived so that we can demonstrate all the features of the ***DefineExpectedCallOrder*** method.

Here is the list of Moq *Setup* statements we have defined in our unit test method:

```csharp
MethodCallOrderVerifier verifier = new();
mockClass.Setup(mock => mock.Method1("value1")).Callback(verifier.GetCallOrderAction(Method1, 1));
mockClass.Setup(mock => mock.Method1("value2")).Callback(verifier.GetCallOrderAction(Method1, 2));
mockClass.Setup(mock => mock.Method2()).Callback(verifier.GetCallOrderAction(Method2));
mockClass.Setup(mock => mock.Method3("value1")).Callback(verifier.GetCallOrderAction(Method3, 1));
mockClass.Setup(mock => mock.Method3("value2")).Callback(verifier.GetCallOrderAction(Method3, 2));
mockClass.Setup(mock => mock.Method4()).Callback(verifier.GetCallOrderAction(Method4));
```

Assume that during the unit test the methods on the mock object get called in the following order:

```csharp
mockClass.Object.Method4();
mockClass.Object.Method1("value1");
mockClass.Object.Method3();
mockClass.Object.Method2("value2");
mockClass.Object.Method4();
mockClass.Object.Method2("value1");
mockClass.Object.Method3();
mockClass.Object.Method1("value2");
```

Internally the *MethodCallOrderVerifier* object keeps track of the order of the mock method calls. Whenever a mock method is called its
*Callback* method in the *Setup* statement will invoke the ***GetCallOrderAction*** method, and this in turn will store the specified
***MethodCallToken*** and method call number in the internal list. So, after the above calls are made, the list will look like this:

```
#  Token    Method Call #
-  -------  -------------
1  Method4  0
2  Method1  1
3  Method3  0
4  Method2  2
5  Method4  0
6  Method2  1
7  Method3  0
8  Method1  2
```

Now, here are some sample ***DefineExpectedCallOrder*** statements. Each example is followed by an explanation of which of the
above mock method calls will be matched by the ***DefineExpectedCallOrder*** call.

```csharp
verifier.DefineExpectedCallOrder(Method3, Method4);
```

Since we didn't specify either the *firstCallNumber* or the *secondCallNumber* parameters in the above statement, these parameters will
be set to their default value of zero. The *firstCallToken* parameter will therefore match the first call to mock method *Method3*, which
is on line 3 above. The *secondCallToken* parameter will match the first *Method4* call that is made after that call, which is on line 5 above.

```csharp
verifier.DefineExpectedCallOrder(Method2, Method1);
```

As in the previous example, the *firstCallNumber* and *secondCallNumber* parameters will both default to zero. This means that the method call
numbers will be ignored when looking for a match and only the ***MethodCallToken*** values will be matched. Therefore, the *firstCallToken*
parameter will match the first call to *Method2*, which happens to be on line 4. The *secondCallToken* parameter will match the first
*Method1* call that is made after the first call to *Method2*. That *Method1* call is on line 8.

```csharp
verifier.DefineExpectedCallOrder(Method1, Method2, 1, 2);
```

In this example, the *firstCallNumber* is assigned a value of 1 and the *secondCallNumber* is assigned a value of 2. Therefore, the *Method1*
token will match the first *Method1* call having a method call number of 1, which is on line 2. The *Method2* token will match the first
*Method2* call having a method call number of 2 that is called after the matching *Method1* call. This is on line 4.

```csharp
verifier.DefineExpectedCallOrder(Method4, Method3, -2);
```

In this example, the *firstCallNumber* is assigned a value of -2 and the *secondCallNumber* defaults to 0. This means the *firstCallToken*
parameter will match the second call to *Method4*, which is on line 5, and the *secondCallToken* will match the first call to *Method3*
that happens after this, which is on line 7.

```csharp
verifier.DefineExpectedCallOrder(Method1, Method2, 1, -2);
```

Similar to one of the previous examples, the *firstCallToken* will match the first *Method1* call having a method call number of 1, which is on
line 2. This time, though, the *secondCallToken* (having a call number of -2) will match the second *Method2* call, which is on line 6.

### The ***Verify*** Method
The ***Verify*** method takes the list of mock method calls that were recorded by the ***GetCallOrderAction*** statements and compares the order
of the calls against the order that was defined by the ***DefineExpectedCallOrder*** statements. The method has the following definition:

```csharp
public void Verify();
```

Typically you would create an instance of the ***MethodCallOrderVerifier*** class at the top of a unit test method. After this you would have
some Moq *Setup* statements that reference the ***GetCallOrderAction*** method in their *Callback* methods. Then there would be one ore more
***DefineExpectedCallOrder*** statements. Somewhere near the bottom of the unit test method you would make a call to the ***Verify*** method.
Here's a simple example:

```csharp
[Fact]
public void UnitTest_001()
{
    // Arrange
    Mock<ISomeClass> mockObject = new();
    MyClass myClass = new(mockObject.Object);
    MethodCallOrderVerifier verifier = new();
    mockObject.Setup(mock => mock.Method1()).Callback(verifier.GetCallOrderAction(Method1));
    mockObject.Setup(mock => mock.Method2()).Callback(verifier.GetCallOrderAction(Method2));
    verifier.DefineExpectedCallOrder(Method1, Method2);
    // additional Arrange statements, if any...

    // Act
    myClass.DoSomething();

    // Assert
    verifier.Verify();
    // additional assertions, if any...
}
```

When ***Verify()*** is called in the above test a check will be made to see if *Method2* was indeed called after *Method1*. An appropriate
assertion failure will be thrown if this is not the case.

### The ***Reset*** Method
The ***Reset*** method was already described earlier. Basically, all this method does is to clear out the list of saved mock method calls
and restore the ***MethodCallOrderVerifier*** object back to its original state. This is primarily intended for those who would rather create
the ***MethodCallOrderVerifier*** object once at the unit test class level and then reference that object in each unit test method that needs it.
You would then need to call the ***Reset*** method at the beginning of each unit test method to reset the object's state. This must be done
before any ***GetCallOrderAction*** or ***DefineExpectedCallOrder*** statement is used in the unit test method.

The ***Reset*** method has the following definition:

```csharp
public void Reset();
```

Refer to the description of the ***MethodCallOrderVerifier*** class constructor for an example of how to use the ***Reset*** method.