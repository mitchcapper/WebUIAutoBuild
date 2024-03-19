using CppSharp;
using CppSharp.AST;
using CppSharp.AST.Extensions;
using CppSharp.Generators.CSharp;
using CppSharp.Passes;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;


namespace CodeGen {
	internal class WebUIGeneratorDirect : ILibrary {

		public void Setup(Driver driver) {

			var options = driver.Options;

			options.GeneratorKind = CppSharp.Generators.GeneratorKind.CSharp;
			var module = options.AddModule("WebUI");
			var ourInfo = new FileInfo(Assembly.GetExecutingAssembly().FullName);
			var projectRootDiir = ourInfo.Directory;
			while (projectRootDiir != null && projectRootDiir.Name != "CodeGen")
				projectRootDiir = projectRootDiir.Parent;
			if (projectRootDiir == null)
				throw new Exception("Couldn't find project root");

			module.IncludeDirs.Add(ourInfo.DirectoryName);
			module.IncludeDirs.Add(Path.Combine(ourInfo.DirectoryName, "upstream"));
			module.Headers.Add("webui.h");
			module.SharedLibraryName = "webui-2.dll";
			module.OutputNamespace="WebUI";
			options.Verbose = true;
			options.OutputDir = Path.Combine(projectRootDiir.FullName, @"../Generated/").Replace("\\","/");
			var parserOptions = driver.ParserOptions;
			parserOptions.LanguageVersion = CppSharp.Parser.LanguageVersion.C99;//important for proper import generation
		}

		public void SetupPasses(Driver driver) {
			driver.Context.TranslationUnitPasses.RemovePrefix("WEBUI_EVENT_");
			driver.Context.TranslationUnitPasses.RemovePrefix("webui_");
		}

		public void Preprocess(Driver driver, ASTContext ctx) {}

		

		public void Postprocess(Driver driver, ASTContext ctx) {

			ctx.SetNameOfEnumWithName("Events", "EventType");
			ctx.SetClassBindName("EventT", "Event");
			ctx.SetClassBindName("webui", "Window");

		}

	}

}
