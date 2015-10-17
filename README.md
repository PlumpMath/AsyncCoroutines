# AsyncCoroutines
This is an implementation of coroutines in C# using async.

This project is half of a constructive proof that async and coroutines are duals. (The other half would be an implementation of async in terms of coroutines; I think that's what Python 3.5 does.) If you are trying to port Python or ES6 coroutines to C#, this project will make it easier. As for myself, I haven't really found a practical use for it yet...

A coroutine is just an `async` function that takes an `IYieldServices<TIn, TOut, TFinal>` as an argument and returns `Task<TFinal>`.

The Yield Services object provides an awaitable `Yield` function to the coroutine. The `Yield` function takes an argument of type `TOut` and returns a value of type `Task<TIn>`.

The Yield Services object also provides `YieldFrom`, which runs an "inner coroutine" as another `async` function. The inner coroutine can return a different `TFinal` type, but can yield on behalf of the outer coroutine; it has to yield the same `TOut` in exchange for the same `TIn`. If the inner coroutine terminates with an exception, then `YieldFrom` will throw that exception.

To *host* a coroutine, call the static `Coroutine<TIn, TOut, TFinal>.Start` function. It accepts the `async` coroutine function as an argument, and provides it with a Yield Services object. It returns a `Task<Coroutine<TIn, TOut, TFinal>.CoroutineState>` object.

The `CoroutineState` will be provided when the coroutine yields, or returns a value of type `TFinal`, or throws an exception. As such, it can be any of three types. It has a property `CoroutineStateType`, the type of which is an enumeration of the same name, which can be used to determine what type it is. You can also use `is` but it's a bit less clunky to use the enumeration.

* `CoroutineStateType.Yielded` means you can cast the `CoroutineState` to type `CoroutineYielded`. The value yielded, of type `TOut`, is in the property `Value`. To continue the coroutine, you must call the `Send` function and pass a value of type `TIn`, or you can call `SendFault` to cause the `Yield` function in the coroutine to throw the exception you provide. The result of `Send` or `SendFault` is a new `CoroutineState`. **Note:** The `Send` or `SendFault` function can be called only once, after which that particular `CoroutineYielded` state is invalid and should be discarded. Use the new `CoroutineState` instead.
* `CoroutineStateType.Faulted` means the coroutine terminated with an exception. You can cast the `CoroutineState` to type `CoroutineFaulted` and retrieve the exception from the `Exception` property.
* `CoroutineStateType.Completed` means the coroutine completed, returning a value of type `TFinal`. You can cast the `CoroutineState` to type `CoroutineCompleted` and retrieve the return value from the `Value` property.

A test is included which demonstrates the implementation. Be sure and view the "output" of the test. (The proper way to do it would have been to collect the outputs into a list and then `Assert` that the list is as expected -- but there was a lot of output and I was lazy.)

I've tried to implement this idea before, but this implementation is the first I've written that supports "yield from."
