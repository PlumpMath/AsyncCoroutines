using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using CoroutineLib;
using System.Linq;

namespace CoroutineTest
{
    [TestClass]
    public class UnitTest1
    {
        private async Task<string> CoroutineBuildString(int i, IYieldService<bool, string, string> yieldService)
        {
            await yieldService.Yield("Building string [" + i + "]");

            string str = "[" + i + "]:" + (char)((int)'A' + i);

            await yieldService.Yield("String built.");

            return str;
        }

        private async Task<bool> Coroutine1(IYieldService<bool, string, bool> yieldService)
        {
            foreach(int i in Enumerable.Range(0, 10))
            {
                string si = await yieldService.YieldFrom<string>(y2 => CoroutineBuildString(i, y2));

                await yieldService.Yield("Received completed string: " + si);
            }

            return true;
        }

        private async Task CoroutineTest1Async()
        {
            Coroutine<bool, string, bool>.CoroutineState st = await Coroutine<bool, string, bool>.Start(y => Coroutine1(y));

            while (st.CoroutineStateType == CoroutineStateType.Yielded)
            {
                Coroutine<bool, string, bool>.CoroutineYielded y = (Coroutine<bool, string, bool>.CoroutineYielded)st;
                System.Diagnostics.Debug.WriteLine("Received yielded message: " + y.Value);
                st = await y.Send(true);
            }

            if (st.CoroutineStateType == CoroutineStateType.Faulted)
            {
                Coroutine<bool, string, bool>.CoroutineFaulted f = (Coroutine<bool, string, bool>.CoroutineFaulted)st;
                throw f.Exception;
            }

            Assert.AreEqual(CoroutineStateType.Completed, st.CoroutineStateType);
            Assert.IsInstanceOfType(st, typeof(Coroutine<bool, string, bool>.CoroutineCompleted));
            Coroutine<bool, string, bool>.CoroutineCompleted c = (Coroutine<bool, string, bool>.CoroutineCompleted)st;
            Assert.AreEqual(true, c.Value);
        }

        [TestMethod]
        public void CoroutineTest1()
        {
            Task.Run(new Func<Task>(CoroutineTest1Async)).Wait();
        }
    }
}
