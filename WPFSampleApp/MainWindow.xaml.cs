using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using WebUILib;
using Windows.Win32;
using Windows.Win32.UI.Input.KeyboardAndMouse;


namespace WPFSampleApp {
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : System.Windows.Window {
		public MainWindow() {
			InitializeComponent();
			Loaded += MainWindow_Loaded;
			Closing += MainWindow_Closing;
		}

		private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
			window?.Close();
		}

		WebUILib.Window window;

		private void MainWindow_Loaded(object sender, RoutedEventArgs e) {

			window = new();
			window.SetSize(640,480);
			window.SetPosition(800,50);

			window.Show("index.html");
			window.Bind("MyTest_function",BoundFunc);
		}

		private async void BoundFunc(WebUIEvent evt) {
			var arg1 = evt.GetStringAt(0);
			var arg2 = evt.GetStringAt(1);
			var arg3 = evt.GetStringAt(2);
			var str = $"I am called: {arg1}({arg1.GetType()}) and {arg2}({arg2.GetType()}) {arg3}({arg3.GetType()})";
			LogItem(str);
			await Task.Delay(500);
		}


		private void JSClickedButton(String elem) {
			LogItem($"Got a click from JS for: {elem}");
		}

		private void LogItem(object val, [CallerArgumentExpression(nameof(val))] string expression = "unknown") {
			var valStr = val switch {
				String[] sarr => String.Join(", ", sarr),
				double or decimal or float or char or bool or int or string => val.ToString(),
				_ => "Serialized as: " + Newtonsoft.Json.JsonConvert.SerializeObject(val, Newtonsoft.Json.Formatting.None)
			};
			Dispatcher.BeginInvoke( () => 
			txtLog.Text += $"{expression} => {valStr} ({val.GetType()})\n"
			);
		}
		private async void btnTest1_Click(object sender, RoutedEventArgs e) {
			LogItem(await window.ScriptEvaluate<double>("return 5.143123;"));
			LogItem(await window.ScriptEvaluate<double>("return getDouble();"));
			LogItem(await window.ScriptEvaluate<String>("return getStrWArgs('arg1','arg2');"));
			//	LogItem(await window.ScriptEvaluateMethod<String>("getStrWArgs", "arg1", 1234.5678));
			//	LogItem(await window.ScriptEvaluateMethod<String[]>("getStrArr"));
			//	LogItem(await window.ScriptEvaluateMethod<ComplexObj>("getComplexObj"));
		}
		public class ComplexObj {
			public int id;
			public class ComplexObjSub {
				public string name;
				public string val;
			}
			public ComplexObjSub subObj;
			public object[] subArr;
		}
		private async void SendF12() {
			var mainWindHandle = Process.GetProcessById((int)window.GetChildProcessId).MainWindowHandle;//main process null?
			PInvoke.SetForegroundWindow(new(mainWindHandle));
			PInvoke.PostMessage(new(mainWindHandle), PInvoke.WM_KEYDOWN, new Windows.Win32.Foundation.WPARAM((nuint)VIRTUAL_KEY.VK_F12), default);
			await Task.Delay(50);
			PInvoke.PostMessage(new(mainWindHandle), PInvoke.WM_KEYUP, new Windows.Win32.Foundation.WPARAM((nuint)VIRTUAL_KEY.VK_F12), default);
		}

		private void btnDevTools_Click(object sender, RoutedEventArgs e) {
			SendF12();
		}
	}
}
