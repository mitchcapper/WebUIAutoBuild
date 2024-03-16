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
	internal class WebUIGenerator : ILibrary {

		public void Setup(Driver driver) {

			var options = driver.Options;

			options.GeneratorKind = CppSharp.Generators.GeneratorKind.CSharp;
			var module = options.AddModule("WebUILib");
			var ourInfo = new FileInfo(Assembly.GetExecutingAssembly().FullName);
			var projectRootDiir = ourInfo.Directory;
			while (projectRootDiir != null && projectRootDiir.Name != "CodeGen")
				projectRootDiir = projectRootDiir.Parent;
			if (projectRootDiir == null)
				throw new Exception("Couldn't find project root");

			module.IncludeDirs.Add(ourInfo.DirectoryName);
			module.IncludeDirs.Add(Path.Combine(ourInfo.DirectoryName, "upstream"));
			module.Headers.Add("webui-stub.h");
			module.Headers.Add("webui.h");
			module.SharedLibraryName = "webui-2.dll";
			module.OutputNamespace="WebUILib";
			options.Verbose = true;
			options.OutputDir = Path.Combine(projectRootDiir.FullName, @"../Generated/").Replace("\\","/");
			var parserOptions = driver.ParserOptions;
			parserOptions.LanguageVersion = CppSharp.Parser.LanguageVersion.C99;//important for proper import generation
		}

		public void SetupPasses(Driver driver) {
			driver.Context.TranslationUnitPasses.RemovePrefix("WEBUI_EVENT_");

			driver.Context.TranslationUnitPasses.AddPass(new DelegatesPass());


			driver.Context.TranslationUnitPasses.AddPass(new CheckStaticClass());
			driver.Context.TranslationUnitPasses.AddPass(new FunctionToStaticMethodPass());
			driver.Context.TranslationUnitPasses.Passes.Insert(0, new FunctionToInstanceMethodPass());
			driver.Context.TranslationUnitPasses.AddPass(new MethodsToPrivatePass("webui", "webui_new_window", "_destroy"));
		}
		delegate void ActionRef<T>(ref T item);

		private FunctionType GetFunctionTypeFromPointerParam(Parameter param) => (param.QualifiedType.Type as PointerType).QualifiedPointee.Type as FunctionType;
		private void SetIsConstOnType(ITypedDecl type) => SetQualifiersOnType(type, (ref TypeQualifiers quals) => quals.IsConst = true);
		private void SetQualifiersOnType(ITypedDecl type, ActionRef<TypeQualifiers> SetQuals) {
			if (type.QualifiedType.Type is PointerType pt) {
				SetQuals(ref pt.QualifiedPointee.Qualifiers);
				return;
			}
			var quals = type.QualifiedType;
			SetQuals(ref quals.Qualifiers);
			type.QualifiedType = quals;
		}
		public void SetFieldTypeOnClass(Class c, String fieldName, QualifiedType type) {
			c.Fields.Single(a => a.Name == fieldName).QualifiedType = type;
			c.Layout.Fields.Single(a => a.Name == fieldName).QualifiedType = type;
		}
		public void Preprocess(Driver driver, ASTContext ctx) {

			var WebEventClass = ctx.FindClass("webui_event_t").Single();
			var WebUIClass = ctx.FindClass("webui").Single();
			var evtTypeEnum = ctx.FindEnum("webui_events").Single();

			var ptrToWebUIClass = new QualifiedType(new PointerType(ctx.FindTypedef("webui").Single().QualifiedType));
			var ptrToWebEvtClass = new QualifiedType(new PointerType(ctx.FindTypedef("webui_event_t").Single().QualifiedType));
			var webBind = ctx.FindFunction("webui_bind").Single();
			var webInterfaceBind = ctx.FindFunction("webui_interface_bind").Single();
			var webNewWindow = ctx.FindFunction("webui_new_window").Single();
			webNewWindow.ReturnType = ptrToWebUIClass;
			SetIsConstOnType(GetFunctionTypeFromPointerParam(webInterfaceBind.Parameters.Single(a => a.Name == "func")).Parameters[2]);

			webBind.Parameters.Single(a => a.Name == "func").QualifiedType = new QualifiedType(new TypedefType(ctx.FindTypedef("WebEventHandler").Single()));

			//SelfGenerateConstructor(WebUIClass, webNewWindow);//could not get it to generate the constructor correctly

			SetFieldTypeOnClass(WebEventClass, "window", ptrToWebUIClass);
			SetFieldTypeOnClass(WebEventClass, "event_type", new QualifiedType(new TagType() { Declaration = evtTypeEnum }));
			SetIsConstOnType(WebEventClass.Fields.Single(a => a.Name == "element"));

			ctx.SetNameOfEnumWithName("webui_events", "EventType");


			foreach (var translationUnit in ctx.TranslationUnits) {
				foreach (var function in translationUnit.Functions) {
					var parameter = function.Parameters.FirstOrDefault();
					if (parameter == default)
						continue;

					if (parameter.Name == "window")
						parameter.QualifiedType = ptrToWebUIClass;
					if (parameter.Name == "e" && parameter.Type is PointerType && parameter.Type.ToString() == ptrToWebEvtClass.Type.ToString()) { //its a pointer to the typdef not a tag to the class need to update for instancer to catch, we rename it as well to make sure it goes onto our Event class
						parameter.QualifiedType = ptrToWebEvtClass;
						if (function.Name.StartsWith("webui_"))
							function.Name = "webui_event_t_" + function.Name.Substring("webui_".Length);
					}


				}
			}

		}

		private void SelfGenerateConstructor(Class webUIClass, Function webNewWindow) {
			//Attempt to automatically add the constructor, shows up but tries to either pass an instance version to the call or with static type static accesses the instance items.  Doesn't assign to the __instance either.

			Method method = new Method {
				Namespace = webUIClass,
				OriginalNamespace = webNewWindow.Namespace,
				Name = ".ctor",
				OriginalName = webNewWindow.OriginalName,
				Mangled = webNewWindow.Mangled,
				Access = AccessSpecifier.Public,
				Kind = CXXMethodKind.Constructor,
				ReturnType = webNewWindow.ReturnType,
				CallingConvention = webNewWindow.CallingConvention,
				IsVariadic = webNewWindow.IsVariadic,
				IsInline = webNewWindow.IsInline,
				//IsDefaultConstructor=true,
				Conversion = MethodConversionKind.FunctionToStaticMethod
			};
			method.ReturnType = new QualifiedType(new BuiltinType(PrimitiveType.Void));
			webUIClass.Methods.Add(method);

		}

		public void Postprocess(Driver driver, ASTContext ctx) {

			ctx.SetClassBindName("WebuiEventT", "WebUIEvent");
			ctx.SetClassBindName("Webui", "Window");

		}

	}

	internal class MethodsToPrivatePass : TranslationUnitPass {

		private readonly string className;
		private readonly string[] methodNames;

		public MethodsToPrivatePass(string className, params string[] methodNames) {
			this.className = className;
			this.methodNames = methodNames;
		}

		public override bool VisitClassDecl(Class cls) {
			if (AlreadyVisited(cls)) {
				return false;
			}
			if (cls.Name == className) {
				foreach (var name in methodNames) {
					var toFix = cls.Methods.Where(a => a.Name == name);
					if (!toFix.Any())
						throw new Exception($"Unable to find expect method: {name} on class: {className} to private");
					foreach (var meth in toFix) {
						meth.Access = AccessSpecifier.Private;
						meth.GenerationKind = GenerationKind.Generate;//force to generate to avoid private stripping
						Diagnostics.Debug($"{className}::{name} converted to private access");
					}
				}

			}
			return base.VisitClassDecl(cls);
		}

	}
}
