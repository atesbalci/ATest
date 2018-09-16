using System;
using Eto.Forms;

namespace ATest.Desktop
{
	class Program
	{
		[STAThread]
		static void Main(string[] args)
		{
			new Application(Eto.Platform.Detect).Run(new AtestForm());
		}
	}
}