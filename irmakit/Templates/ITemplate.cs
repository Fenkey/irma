using System;

namespace IRMAKit.Templates
{
	public interface ITemplate
	{
		/// <summary>
		/// Template content
		/// </summary>
		byte[] Content { get; }

		/// <summary>
		/// The last modify time
		/// </summary>
		DateTime LastModify { get; }

		/// <summary>
		/// The last reload time
		/// </summary>
		DateTime LastTime { get; }
	}
}
