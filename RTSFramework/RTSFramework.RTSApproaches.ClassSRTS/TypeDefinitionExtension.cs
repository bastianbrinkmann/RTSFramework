using System.Linq;
using Mono.Cecil;

namespace RTSFramework.RTSApproaches.Static
{
	public static class TypeDefinitionExtension
	{
		public static bool IsAnonymousType(this TypeDefinition type)
		{
			bool hasCompilerGeneratedAttribute = false;

			if (type.HasCustomAttributes)
			{
				hasCompilerGeneratedAttribute = type.CustomAttributes.Any(x => x.AttributeType.FullName == "System.Runtime.CompilerServices.CompilerGeneratedAttribute");
			}
			bool nameContainsAnonymousType = type.FullName.Contains("AnonymousType");

			bool isAnonymousType = hasCompilerGeneratedAttribute && nameContainsAnonymousType;

			return isAnonymousType;
		}
	}
}