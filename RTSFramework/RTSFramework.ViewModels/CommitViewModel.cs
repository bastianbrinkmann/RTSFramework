using System;

namespace RTSFramework.ViewModels
{
	public class CommitViewModel
	{
		public string DisplayName => $"{WithMaxLength(Identifier, 5)}.. - {WithoutNewLine(WithMaxLength(Message, 50))}..\nby {Committer ?? ""}";

		private string WithoutNewLine(string value)
		{
			return value.TrimEnd('\n');
		}

		private string WithMaxLength(string value, int maxLength)
		{
			return value?.Substring(0, Math.Min(value.Length, maxLength)) ?? "";
		}


		public string Committer { get; set; }

		public string Identifier { get; set; }

		public string Message { get; set; }
	}
}