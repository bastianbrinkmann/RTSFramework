using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RTSFramework.Concrete.CSharp.MSTest.VsTest
{
	public class AsyncAutoResetEvent
	{
		private static readonly Task Completed = Task.FromResult(true);
		private readonly Queue<TaskCompletionSource<bool>> waits = new Queue<TaskCompletionSource<bool>>();
		private bool signaled;

		public Task WaitAsync(CancellationToken token = default(CancellationToken))
		{
			lock (waits)
			{
				token.Register(Set);
				if (signaled)
				{
					signaled = false;
					return Completed;
				}
				var tcs = new TaskCompletionSource<bool>();
				waits.Enqueue(tcs);
				return tcs.Task;
			}
		}

		public void Set()
		{
			TaskCompletionSource<bool> toRelease = null;

			lock (waits)
			{
				if (waits.Count > 0)
					toRelease = waits.Dequeue();
				else if (!signaled)
					signaled = true;
			}

			toRelease?.SetResult(true);
		}
	}
}