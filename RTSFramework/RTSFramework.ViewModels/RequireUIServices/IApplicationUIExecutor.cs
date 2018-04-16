using System;

namespace RTSFramework.ViewModels.RequireUIServices
{
	public interface IApplicationUiExecutor
	{
		void ExecuteOnUI(Action action);
	}
}