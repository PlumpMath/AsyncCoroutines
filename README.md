# AsyncCoroutines
This is an implementation of coroutines in C# using async. It allows you to use C# to write coroutines in the style of Python and EcmaScript 6. The C# async mechanism handles the transitions between the coroutine and the main routine.

To start a coroutine, call the static `Coroutine<TIn, TOut, TFinal>.Start` function and `await` the result. The `Start` function accepts an `async` coroutine function as an argument, and provides it with a Yield Services object. It runs the coroutine immediately, until it yields or exits. It returns the coroutine state that represents what the coroutine did, as an awaitable `Task<Coroutine<TIn, TOut, TFinal>.CoroutineState>` object.

The `CoroutineState` returned by `Start` can be any of three types -- one for when the coroutine yields, one for when it returns a value of type `TFinal`, or one for when it throws an exception. It has a property `CoroutineStateType`, the type of which is an enumeration of the same name, which can be used to determine what type it is. You can also use `is` but it's a bit less clunky to use the enumeration. There is also a `Visit` function which calls a different function depending on the type.

* `CoroutineStateType.Yielded` means you can cast the `CoroutineState` to type `CoroutineYielded` (actually `Coroutine<TIn, TOut, TFinal>.CoroutineYielded`, but I abbreviate here). The value yielded, of type `TOut`, is in the property `Value`. To continue the coroutine, you must call the `Send` function and pass a value of type `TIn`, or you can call `SendFault` to cause the `Yield` function in the coroutine to throw the exception you provide. The result of `Send` or `SendFault` is a `Task` which will return a new `CoroutineState`. **Note:** The `Send` or `SendFault` function can be called only once, after which that particular `CoroutineYielded` state is invalid and should be discarded. Use the new `CoroutineState` instead.
* `CoroutineStateType.Faulted` means the coroutine terminated with an exception. You can cast the `CoroutineState` to type `CoroutineFaulted` and retrieve the exception from the `Exception` property.
* `CoroutineStateType.Completed` means the coroutine completed, returning a value of type `TFinal`. You can cast the `CoroutineState` to type `CoroutineCompleted` and retrieve the return value from the `Value` property.

To write a coroutine, just write an `async` function that takes an `IYieldServices<TIn, TOut, TFinal>` as an argument and returns `Task<TFinal>`.

The Yield Services object provides an awaitable `Yield` function that the coroutine can call. The `Yield` function takes an argument of type `TOut` and returns a value of type `Task<TIn>`.

The Yield Services object also provides `YieldFrom`, which runs an inner coroutine as another `async` function. The inner coroutine can return a different `TFinal` type, but can yield on behalf of the outer coroutine; it has to yield the same `TOut` in exchange for the same `TIn`. If the inner coroutine terminates with an exception, then `YieldFrom` will throw that exception.

A test is included which demonstrates the implementation. Be sure and view the *output* of the test. (The proper way to do it would have been to collect the outputs into a list and then `Assert` that the list is as expected -- but there was a lot of output and I was lazy.)

This project is half of a constructive proof that async and coroutines are duals. (The other half would be an implementation of async in terms of coroutines; I think that's what Python 3.5 does.)

I've tried to implement this idea before, but this implementation is the first I've written that supports "yield from."
