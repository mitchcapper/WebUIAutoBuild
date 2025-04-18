using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WebUILib;

namespace ConsoleSampleApp {
	internal class WebUITestUsingClass : IWebUISvc {
		public WebUITestUsingClass() {

		}
		WebUILib.Window window;
		public async Task Start() {

			window = new();
			window.SetSize(640, 480);
			window.SetPosition(800, 50);

			window.SetRootFolder(@".");

			window.Show("index.html");
			window.Bind("TestButton", BoundFunc);
			window.Bind("MyTest_function", BoundFunc);
		}
		unsafe public Task<string> CallJS() {
			return Task.Run(() => {
				Span<byte> buffer = new byte[1024];
				fixed (byte* ptr = buffer) {

					var res = window.Script($"return SampleAsync();", 0, (sbyte*)ptr, (uint)buffer.Length);
					var msg = Encoding.UTF8.GetString((byte[])buffer.Slice(0, buffer.IndexOf((byte)0)).ToArray());
					return $"success: {res} msg: {msg}";

				}
			});
		}
		public bool IsValid => window.IsShown;
		public void Close() => window.Close();
		public async Task Wait() {
			await Task.Run(WebUI.webui.Wait);
		}
		private void BoundFunc(WebUIEvent evt) {
			var arg1 = evt.GetStringAt(0);
			var arg2 = evt.GetStringAt(1);
			var arg3 = evt.GetStringAt(2);
			var str = $"I am called: {arg1}({arg1.GetType()}) and {arg2}({arg2.GetType()}) {arg3}({arg3.GetType()})";
			var start = DateTime.Now;
			LogItem(str);
			//Thread.Sleep(TimeSpan.FromSeconds(10));
			evt.ReturnString($"Happy Days Str: {str} took: {(int)(DateTime.Now - start).TotalSeconds}");

		}

		private void LogItem(string str) => Console.WriteLine(str);
	}
}
