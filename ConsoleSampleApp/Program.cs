using System;
using System.Threading.Tasks;

public interface IWebUISvc {
	public void Close();
	public Task<string> CallJS();
	public Task Start();
	public bool IsValid { get; }
}
namespace ConsoleSampleApp {
	internal class Program {
		async static Task Main(string[] args) {
			var direct = args[0] == "--direct";
			IWebUISvc service = direct ? new WebUITestUsingDirect() : new WebUITestUsingClass();
			Console.WriteLine($"Using direct mode: {direct}");
			await service.Start();
			
			await Task.Delay(2000);// if we read isvalid too fast we will fail
			try {
				while (service.IsValid) {
					Console.WriteLine("Hit Key to call JS function");
					Console.ReadKey();
					Console.WriteLine();

					service.CallJS().ContinueWith((msg) => Console.WriteLine($"{DateTime.Now}: Got back: {msg.Result} "));
				}
			} finally {
				service.Close();
			}

			//await service.Wait();
		}


	}
}
