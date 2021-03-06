﻿using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace RTSFramework.Core.Utilities
{
	public static class ProcessExtensions
	{
		/// <summary>
		/// Waits asynchronously for the process to exit.
		/// https://stackoverflow.com/questions/470256/process-waitforexit-asynchronously
		/// </summary>
		/// <param name="process">The process to wait for cancellation.</param>
		/// <param name="cancellationToken">A cancellation token. If invoked, the task will return 
		/// immediately as canceled.</param>
		/// <returns>A Task representing waiting for the process to end.</returns>
		public static Task WaitForExitAsync(this Process process, CancellationToken cancellationToken)
		{
			var tcs = new TaskCompletionSource<object>();
			process.EnableRaisingEvents = true;
			process.Exited += (sender, args) => tcs.TrySetResult(null);
			cancellationToken.Register(tcs.SetCanceled);

			return tcs.Task;
		}
	}
}