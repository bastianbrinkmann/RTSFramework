using System;
using System.Windows;
using RTSFramework.ViewModels.RequireUIServices;

namespace RTSFramework.GUI.RequireUIServices
{
	public class ApplicationUiExecutor : IApplicationUiExecutor
	{
		public void ExecuteOnUI(Action action)
		{
			Application.Current.Dispatcher.Invoke(action);
		}
	}
}