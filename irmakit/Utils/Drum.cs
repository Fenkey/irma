using System;
using System.Collections.Generic;

namespace IRMAKit.Utils
{
	public sealed class Drum : IDrum
	{
		private Random rand;

		private class DItem
		{
			public int Start;
			public int Next;
			public bool RoundEnd;
			public List<object> List;

			public DItem()
			{
				this.RoundEnd = false;
				this.Start = this.Next = 0;
				this.List = new List<object>();
			}

			public void Reset()
			{
				this.RoundEnd = false;
				this.Start = this.Next = 0;
			}
		}

		private Dictionary<string, DItem> map;

		public Drum()
		{
			this.rand = new Random();
			this.map = new Dictionary<string, DItem>();
		}

		public int Add(string key, object obj)
		{
			if (string.IsNullOrEmpty(key) || obj == null)
				return -1;
			if (!map.ContainsKey(key))
				map[key] = new DItem();
			map[key].List.Add(obj);
			return map[key].List.Count;
		}

		public object this[string key]
		{
			get {
				if (!map.ContainsKey(key))
					return null;
				DItem item = map[key];
				if (item.List.Count <= 0)
					return null;
				object o = item.List[item.Next++];
				if (item.Next >= item.List.Count)
					item.Next = 0;
				if (item.Next == item.Start) {
					item.RoundEnd = true;
					item.Start = item.Next = rand.Next(0, item.List.Count);
				} else
					item.RoundEnd = false;
				return o;
			}
		}

		public int Count(string key)
		{
			return map.ContainsKey(key) ? map[key].List.Count : 0;
		}

		public bool RoundEnd(string key)
		{
			return map.ContainsKey(key) ? map[key].RoundEnd : false;
		}

		public void Reset(string key)
		{
			if (map.ContainsKey(key))
				map[key].Reset();
		}

		public void ResetAll()
		{
			foreach (KeyValuePair<string, DItem> p in map)
				p.Value.Reset();
		}

		public bool Remove(string key)
		{
			if (string.IsNullOrEmpty(key))
				return false;
			return map.Remove(key);
		}

		public void RemoveAll()
		{
			map.Clear();
		}
	}
}
