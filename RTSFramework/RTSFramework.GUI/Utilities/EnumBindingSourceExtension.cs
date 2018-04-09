using System;
using System.Windows.Markup;

namespace RTSFramework.GUI.Utilities
{
	public class EnumBindingSourceExtension : MarkupExtension
	{
		private Type enumType;
		public Type EnumType
		{
			get { return enumType; }
			set
			{
				if (value != enumType)
				{
					if (null != value)
					{
						Type underlyingType = Nullable.GetUnderlyingType(value) ?? value;
						if (!underlyingType.IsEnum)
							throw new ArgumentException("Type must be for an Enum.");
					}

					enumType = value;
				}
			}
		}

		public EnumBindingSourceExtension()
		{
			
		}

		public EnumBindingSourceExtension(Type enumType)
		{
			EnumType = enumType;
		}

		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			if (enumType == null)
				throw new InvalidOperationException("The EnumType must be specified.");

			Type actualEnumType = Nullable.GetUnderlyingType(enumType) ?? enumType;
			Array enumValues = Enum.GetValues(actualEnumType);

			if (actualEnumType == enumType)
				return enumValues;

			Array tempArray = Array.CreateInstance(actualEnumType, enumValues.Length + 1);
			enumValues.CopyTo(tempArray, 1);
			return tempArray;
		}
	}
}