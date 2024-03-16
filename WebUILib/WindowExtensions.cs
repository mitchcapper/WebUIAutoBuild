using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.HighPerformance.Buffers;
using __IntPtr = global::System.IntPtr;
namespace WebUILib.Extensions {
	public class JavaScriptException : Exception {
		public JavaScriptException() { }

		public JavaScriptException(string? message) : base(message) { }

		public JavaScriptException(string? message, Exception? innerException) : base(message, innerException) { }

	}
	public static class WindowExtensions {

		/// <summary>
		/// Note for non-valuetype (string, numbers,etc) return types it expects the value to have been returned from javascript in json form (ie JSON.stringify({'my':'obj'});  if you want something to stringify for you see ScriptEvaluateMethod.
		/// </summary>
		/// <param name="javascript"></param>
		/// <param name="timeout">Note timeout has a resolution of 1 second</param>
		/// <param name="stringReturnsAreSerialized">When true even strings will be run through the deserializer so they must be quoted when returned, when null(default) will try to auto detect and strip off first pair of quotes (if present) when false nothing will be done to the result just returned bare.</param>
		/// <returns></returns>
		public async static Task<T?> ScriptEvaluate<T>(this Window window, string javascript, TimeSpan? timeout = null, bool? stringReturnsAreSerialized = null) {
			var time = timeout ?? window.config.ScriptEvaluationDefaultTimeout;
			var result = await window.BackgroundExecuteScript(javascript, time);
			if (stringReturnsAreSerialized != true && typeof(T) == typeof(string)) {
				if (stringReturnsAreSerialized != false && result is string s && s.Length > 2 && s.StartsWith("\"") && s.EndsWith("\"")) //try to autodetect when j
					return (T)(object)s.Substring(1, s.Length - 2);
				return (T)(object)result;
			}
			return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(result);
		}
		private unsafe static Task<string> BackgroundExecuteScript(this Window window, string javascript, TimeSpan timeout) {
			var it = Task.Run(() => {
				using var buffer = MemoryOwner<sbyte>.Allocate(window.config.ScriptEvaulationMaxReturnSize); // this is a memory pool, I promise:)
				var res = window.Script(javascript, (uint)Math.Round(timeout.TotalSeconds), buffer.Span);
				fixed (sbyte* ptr = buffer.Span) {
					var str = Encoding.UTF8.GetString((byte*)ptr, buffer.Span.IndexOf((sbyte)0));
					if (!res)
						throw new JavaScriptException(str);
					return str;
				}
			}
			);
			return it;
		}

		public static unsafe bool Script(this Window window, string javaScript, uint timeout_secs, Span<sbyte> buffer) {
			fixed (sbyte* ptr = buffer)
				return window.Script(javaScript, (UIntPtr)timeout_secs, ptr, (UIntPtr)buffer.Length);

		}
	}
}
