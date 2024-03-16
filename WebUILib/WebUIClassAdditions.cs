using System;
using System.Collections.Concurrent;
using __IntPtr = global::System.IntPtr;

namespace WebUILib {

	public partial class Window {
		public Window() {
			__Instance = new __IntPtr(__Internal.WebuiNewWindow());
			__RecordNativeToManagedMapping(__Instance, this);

		}
		partial void DisposePartial(bool disposing) {
			Destroy();
		}


		public ulong Bind(string element, WrappedWebEventHandler func) { //could switch to a compiled expression rather
			WebEventHandler customDel = (__IntPtr evt) => func(WebUIEvent.__GetOrCreateInstance(evt));
			knownDels[element] = customDel;
			return Bind(element, customDel);
		}
		private ConcurrentDictionary<string, WebEventHandler> knownDels = new();
		public delegate void WrappedWebEventHandler(WebUIEvent evt);


		public class WindowConfig {
			public int ScriptEvaulationMaxReturnSize = 1024 * 1024 * 8;
			public TimeSpan ScriptEvaluationDefaultTimeout = TimeSpan.FromSeconds(15);

		}
		public WindowConfig config = new();







	}
}
