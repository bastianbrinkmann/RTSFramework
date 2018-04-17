using System;
using System.Windows;
using RTSFramework.ViewModels.RequireUIServices;

namespace RTSFramework.GUI.RequireUIServices
{
	public class ApplicationUiExecutor : IApplicationUiExecutor
	{
		public void ExecuteOnUi(Action action)
		{
			Application.Current.Dispatcher.Invoke(action);
		}
	}
}