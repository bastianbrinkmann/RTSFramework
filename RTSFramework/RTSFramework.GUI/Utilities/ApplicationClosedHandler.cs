using System;
using System.Collections.Generic;
using RTSFramework.Contracts.Utilities;

namespace RTSFramework.GUI.Utilities
{
	public class ApplicationClosedHandler : IApplicationClosedHandler
	{
		private List<IDisposable> listeners = new List<IDisposable>();

		public void AddApplicationClosedListener(IDisposable listener)
		{
			listeners.Add(listener);
		}

		public void RemovedApplicationClosedListener(IDisposable listener)
		{
			listeners.Remove(listener);
		}

		public void ApplicationExiting()
		{
			listeners.ForEach(x => x.Dispose());
		}
	}
}