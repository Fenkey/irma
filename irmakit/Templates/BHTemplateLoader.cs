using System;
using System.IO;
using Commons.Collections;
using NVelocity.Runtime.Resource;
using NVelocity.Runtime.Resource.Loader;

namespace IRMAKit.Templates
{
	public class IRMATemplateLoader : ResourceLoader
	{
		public override void Init(ExtendedProperties prop)
		{
		}

		public override Stream GetResourceStream(string name)
		{
			return null;
		}

		public override long GetLastModified(Resource resource)
		{
			return 0;
		}

		public override bool IsSourceModified(Resource resource)
		{
			return false;
		}

		public virtual ITemplate GetTemplate(string name)
		{
			return null;
		}

		public virtual byte[] GetResource(string name, ref DateTime lastModify, bool isRaw)
		{
			return null;
		}
	}
}
