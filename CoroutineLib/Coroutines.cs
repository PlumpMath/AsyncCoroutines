using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoroutineLib
{
    public enum CoroutineStateType
    {
        Completed,
        Faulted,
        Yielded
    }

    public interface IYieldService<TIn, TOut, TFinal>
    {
        Task<TIn> Yield(TOut value);

        Task<TResult> YieldFrom<TResult>(Func<IYieldService<TIn, TOut, TResult>, Task<TResult>> proc);
    }

    public static class Coroutine<TIn, TOut, TFinal>
    {
        public abstract class CoroutineState
        {
            protected readonly CoroutineStateType stateType;

            protected CoroutineState(CoroutineStateType stateType)
            {
                this.stateType = stateType;
            }

            public abstract TResult Visit<TResult>
            (
                Func<CoroutineCompleted, TResult> onCompleted,
                Func<CoroutineFaulted, TResult> onFaulted,
                Func<CoroutineYielded, TResult> onYielded
            );

            public CoroutineStateType CoroutineStateType { get { return stateType; } }
        }

        public sealed class CoroutineCompleted : CoroutineState
        {
            private TFinal value;

            public CoroutineCompleted(TFinal value) : base(CoroutineStateType.Completed)
            {
                this.value = value;
            }

            public TFinal Value { get { return value; } }

            public override TResult Visit<TResult>
            (
                Func<CoroutineCompleted, TResult> onCompleted,
                Func<CoroutineFaulted, TResult> onFaulted,
                Func<CoroutineYielded, TResult> onYielded
            )
            {
 	            return onCompleted(this);
            }
        }

        public sealed class CoroutineFaulted : CoroutineState
        {
            private Exception exc;

            public CoroutineFaulted(Exception exc) : base(CoroutineStateType.Faulted)
            {
                this.exc = exc;
            }

            public Exception Exception { get { return exc; } }

            public override TResult Visit<TResult>
            (
                Func<CoroutineCompleted, TResult> onCompleted,
                Func<CoroutineFaulted, TResult> onFaulted,
                Func<CoroutineYielded, TResult> onYielded
            )
            {
                return onFaulted(this);
            }
        }

        public abstract class CoroutineYielded : CoroutineState
        {
            protected CoroutineYielded() : base(CoroutineStateType.Yielded)
            {

            }

            public override TResult Visit<TResult>
            (
                Func<CoroutineCompleted, TResult> onCompleted,
                Func<CoroutineFaulted, TResult> onFaulted,
                Func<CoroutineYielded, TResult> onYielded
            )
            {
                return onYielded(this);
            }

            public abstract TOut Value { get; }

            public abstract Task<CoroutineState> Send(TIn value);

            public abstract Task<CoroutineState> SendFault(Exception exc);
        }

        private sealed class CoroutineYieldedImpl : CoroutineYielded
        {
            private TOut value;
            private TaskCompletionSource<SendMessage> k;
            private bool used;

            public CoroutineYieldedImpl(TOut value, TaskCompletionSource<SendMessage> k)
            {
                this.value = value;
                this.k = k;
                this.used = false;
            }

            public override TOut Value { get { return value; } }

            public override Task<CoroutineState> Send(TIn value)
            {
                if (!used)
                {
                    used = true;
                    TaskCompletionSource<CoroutineState> k2 = new TaskCompletionSource<CoroutineState>();
                    k.SetResult(new SendMessage() { value = value, exception = null, k = k2 });
                    return k2.Task;
                }
                else
                {
                    throw new InvalidOperationException("A result has already been sent to this coroutine state");
                }
            }

            public override Task<CoroutineState> SendFault(Exception exc)
            {
                if (exc == null) throw new ArgumentNullException("exc");
                if (!used)
                {
                    used = true;
                    TaskCompletionSource<CoroutineState> k2 = new TaskCompletionSource<CoroutineState>();
                    k.SetResult(new SendMessage() { value = default(TIn), exception = exc, k = k2, });
                    return k2.Task;
                }
                else
                {
                    throw new InvalidOperationException("A result has already been sent to this coroutine state");
                }
            }
        }

        private class SendMessage
        {
            public TIn value;
            public Exception exception;
            public TaskCompletionSource<CoroutineState> k;
        }

        private class YieldService : IYieldService<TIn, TOut, TFinal>
        {
            private TaskCompletionSource<CoroutineState> k;

            public YieldService()
            {
                this.k = new TaskCompletionSource<CoroutineState>();
            }
        
            public async Task<TIn> Yield(TOut value)
            {
                TaskCompletionSource<SendMessage> k2 = new TaskCompletionSource<SendMessage>();
 	            k.SetResult(new CoroutineYieldedImpl(value, k2));
                SendMessage incoming = await k2.Task;
                k = incoming.k;
                if (incoming.exception == null)
                {
                    return incoming.value;
                }
                else
                {
                    throw incoming.exception;
                }
            }

            public async Task<TResult> YieldFrom<TResult>(Func<IYieldService<TIn, TOut, TResult>, Task<TResult>> proc)
            {
 	            return await proc(new YieldFromService<TResult>(this));
            }

            private class YieldFromService<TResult> : IYieldService<TIn, TOut, TResult>
            {
                private YieldService parent;

                public YieldFromService(YieldService parent)
                {
                    this.parent = parent;
                }

                public Task<TIn> Yield(TOut value)
                {
                    return parent.Yield(value);
                }

                public async Task<TResult2> YieldFrom<TResult2>(Func<IYieldService<TIn, TOut, TResult2>, Task<TResult2>> proc)
                {
                    return await proc(new YieldFromService<TResult2>(parent));
                }
            }

            public Task<CoroutineState> Coroutine { get { return k.Task; } }

            public void Run(Func<IYieldService<TIn, TOut, TFinal>, Task<TFinal>> proc)
            {
                Task d = Task.Run
                (
                    async () =>
                    {
                        try
                        {
                            TFinal result = await proc(this);
                            k.SetResult(new CoroutineCompleted(result));
                        }
                        catch(Exception exc)
                        {
                            k.SetResult(new CoroutineFaulted(exc));
                        }
                    }
                );
            }
        }

        public static Task<CoroutineState> Start(Func<IYieldService<TIn, TOut, TFinal>, Task<TFinal>> proc)
        {
            YieldService y = new YieldService();
            y.Run(proc);
            return y.Coroutine;
        }
    }
}
