﻿using System;

namespace RTSFramework.ViewModels.RequireUIServices
{
	public interface IApplicationUiExecutor
	{
		void ExecuteOnUi(Action action);
	}
}