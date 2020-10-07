using System;
using System.Collections.Generic;

namespace IRMAKit.Templates
{
	public interface ITemplateManager
	{
		/// <summary>
		/// RenderBytes
		/// </summary>
		byte[] RenderBytes(string tplName, Dictionary<string, object> variables);

		/// <summary>
		/// RenderBytes
		/// </summary>
		byte[] RenderBytes(string tplName);

		/// <summary>
		/// Render
		/// </summary>
		string Render(string tplName, Dictionary<string, object> variables);

		/// <summary>
		/// Render
		/// </summary>
		string Render(string tplName);
	}
}
