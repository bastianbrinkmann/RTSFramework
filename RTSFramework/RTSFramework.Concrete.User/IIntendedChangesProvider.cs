using System;
using System.Collections;
using System.Collections.Generic;

namespace RTSFramework.Concrete.User
{
	public interface IIntendedChangesProvider
	{
		IList<string> IntendedChanges { get; set; }
	}
}