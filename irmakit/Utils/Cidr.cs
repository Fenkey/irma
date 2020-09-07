using System;
using System.Collections.Generic;
using IRMAKit.Log;

namespace IRMAKit.Utils
{
	public class Cidr : ICidr
	{
		private List<int[]> d = null;

		private bool CidrParse(string str, ref int[] cidr)
		{
			string[] s0 = str.Split(new char[] {'/'});
			cidr[4] = s0.Length == 2 ? int.Parse(s0[1]) : 32;
			if (cidr[4] < 0 || cidr[4] > 32)
				return false;
			cidr[5] = 0;

			string[] s1 = s0[0].Split(new char[] {'.'});
			if (s1.Length != 4)
				return false;
			cidr[0] = int.Parse(s1[0]);
			if (cidr[0] < 0)
				return false;
			cidr[1] = int.Parse(s1[1]);
			if (cidr[1] < 0)
				return false;
			cidr[2] = int.Parse(s1[2]);
			if (cidr[2] < 0)
				return false;
			cidr[3] = int.Parse(s1[3]);
			if (cidr[3] < 0)
				return false;

			if (cidr[4] == 0) {
				// 0.0.0.0/0, all inbound will be allowed.
				if (cidr[0] + cidr[1] + cidr[2] + cidr[3] != 0)
					return false;
				cidr[5] = 1;
			}
			return true;
		}

		private bool CidrHit(int[] cidr, int[] ip)
		{
			if (cidr[5] == 1)
				return true;
			int i, bits = cidr[4];
			for (i = 0; i < bits / 8; i++) {
				if (ip[i] != cidr[i])
					return false;
			}
			if (i >= 4)
				return true;
			int r = bits % 8;
			if (r > 0 && (cidr[i]>>(8-r))<<(8-r) != (ip[i]>>(8-r))<<(8-r))
				return false;
			return true;
		}

		public Cidr(string str)
		{
			if (string.IsNullOrEmpty(str))
				return;
			this.d = new List<int[]>();
			try {
				foreach (string p in str.Split(new char[] {',', ';', '`'})) {
					int[] cidr = new int[6];
					if (!CidrParse(p.Trim(), ref cidr)) {
						Logger.WARN("Kit - Invalid cidr: " + p);
						continue;
					}
					this.d.Add(cidr);
				}
			} catch (Exception e) {
				this.d = null;
				throw;
			}
		}

		public bool Hit(string ip)
		{
			if (d == null || d.Count <= 0 || string.IsNullOrEmpty(ip))
				return false;
			string[] s = ip.Split(new char[] {'.'});
			if (s.Length != 4)
				return false;
			int[] a = new int[] { int.Parse(s[0]), int.Parse(s[1]), int.Parse(s[2]), int.Parse(s[3]) };
			foreach (int[] cidr in d) {
				if (CidrHit(cidr, a))
					return true;
			}
			return false;
		}
	}
}
