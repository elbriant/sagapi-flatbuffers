using System;
using System.IO;
using dnlib.DotNet;

namespace DNFBDmp {
	class MainClass {
		private static void Main(string[] args) {
			if (args.Length == 0) return;

			string outputFolder = args.Length >= 2 ? args[1] : @"output/";
			string inputFolder = args[0];

			ModuleContext modCtx = ModuleDef.CreateModuleContext();
			TypeResolver resolver = new TypeResolver();
			foreach (string file in Directory.GetFiles(inputFolder, "*.dll")) {
				resolver.add(ModuleDefMD.Load(file, modCtx));
			}

			TypeDef? fbLookupType = resolver.Find("Torappu.FlatBuffers.FlatLookupConverter", false);
			if (fbLookupType == null) return;

			foreach (MethodDef met in fbLookupType.Methods) {
				string orName = met.Name;
				if (!orName.StartsWith("Unpack_")) continue;
				orName = orName[7..];
				string name = orName.Replace("_", ".");

				if (name.StartsWith("Key.") || name.StartsWith("Value.")) continue;

				TypeDef? curType = null;
				do {
					curType = resolver.Find(name, true);
					if (curType != null) break;
					name = Utils.replaceLast(name, ".", "+");
				} while (name.IndexOf(".") >= 0);

				if (curType == null) continue;

				string qualName = Utils.cleanupClassName(curType.FullName);
				if (qualName != orName) continue;

				FlatbufferDefinition.convert(new ClassSig(curType).RemovePinnedAndModifiers(), resolver);
			}

			Directory.CreateDirectory(outputFolder);
			foreach (FlatbufferDefinition fbDef in FlatbufferDefinition.convTypes.Values)
				fbDef.writeToFile(outputFolder);
		}
	}
}