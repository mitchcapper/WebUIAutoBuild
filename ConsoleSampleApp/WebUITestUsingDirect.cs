using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebUI;

namespace ConsoleSampleApp {
	internal class WebUITestUsingDirect : IWebUISvc {
		ulong window;
		public async Task Start() {
			window = webui.NewWindow();

			webui.SetSize(window,640, 480);
			webui.SetPosition(window,800, 50);
			webui.Show(window,"index.html");
			webui.InterfaceBind(window,"TestButton", BoundFunc);
			webui.InterfaceBind(window,"MyTest_function", BoundFunc);
		}


		unsafe public Task<string> CallJS() {
			return Task.Run(() => {
				Span<byte> buffer = new byte[1024];
				fixed (byte* ptr = buffer) {

					
					var res = webui.Script(window,$"return SampleAsync();", 0, (sbyte*)ptr, (uint)buffer.Length);
					var msg = Encoding.UTF8.GetString((byte[])buffer.Slice(0, buffer.IndexOf((byte)0)).ToArray());
					return $"success: {res} msg: {msg}";

				}
			});
		}
		public bool IsValid => webui.IsShown(window);
		public void Close() => webui.Close(window);
		public async Task Wait() {
			await Task.Run(WebUI.webui.Wait);
		}
		private void BoundFunc(ulong window, ulong event_type, string element, ulong event_number, ulong bind_id) {
			;
			var arg1 = webui.InterfaceGetStringAt(window,event_number,0);
			var arg2 = webui.InterfaceGetStringAt(window,event_number,1);
			var arg3 = webui.InterfaceGetStringAt(window,event_number,2);
			var str = $"I am called: {arg1}({arg1.GetType()}) and {arg2}({arg2.GetType()}) {arg3}({arg3.GetType()})";
			var start = DateTime.Now;
			LogItem(str);
			//Thread.Sleep(TimeSpan.FromSeconds(10));
			webui.InterfaceSetResponse(window,event_number,$"Happy Days Str: {str} took: {(int)(DateTime.Now - start).TotalSeconds}  for evt: {event_number}");

		}

		private void LogItem(string str) => Console.WriteLine(str);
	}
}
