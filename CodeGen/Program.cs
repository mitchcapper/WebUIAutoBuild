using CppSharp;

namespace CodeGen {
	internal class Program {
		static void Main(string[] args) {
			ConsoleDriver.Run(new WebUIGeneratorClass());
			ConsoleDriver.Run(new WebUIGeneratorDirect());
		}
	}
}
