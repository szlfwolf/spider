using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SpiderHost.script
{
	public class spider
	{
		public List<string> getUrls()
		{
			List<string> lst = new List<string>(); 

			Parallel.For(1, 2, (i =>
			{
				lst.Add("http://opp.hegc1024.com/pw/thread.php?fid=5&page=" + i);
			}));

			Parallel.For(1, 2, (i =>
			{
				lst.Add("http://opp.hegc1024.com/pw/thread.php?fid=22&page=" + i);
			}));

			return lst;
		}
		public override string ToString()
		{


			return base.ToString();
		}
	}
}
