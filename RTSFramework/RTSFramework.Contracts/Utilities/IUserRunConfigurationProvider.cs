﻿using System;
using System.Collections.Generic;
using RTSFramework.Contracts.Models;

namespace RTSFramework.Contracts.Utilities
{
	public interface IUserRunConfigurationProvider
	{
		IList<string> IntendedChanges { get; set; }

		double TimeLimit { get; set; }

		Func<ITestCase, bool> FilterFunction { get; set; }
	}
}