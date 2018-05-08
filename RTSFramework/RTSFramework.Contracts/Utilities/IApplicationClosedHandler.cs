using System;

namespace RTSFramework.Contracts.Utilities
{
	public interface IApplicationClosedHandler
	{
		void AddApplicationClosedListener(IDisposable listener);

		void RemovedApplicationClosedListener(IDisposable listener);

		void ApplicationExiting();
	}
}