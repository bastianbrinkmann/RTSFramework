using System;
using System.Collections.Generic;

namespace RTSFramework.Contracts.Models
{
	public interface IProgramModel
	{
        string VersionId { get; }

        string RootPath { get; }
	}
}