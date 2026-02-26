using dnlib.DotNet;

namespace DNFBDmp {
	public class TypeResolver {
		private List<ModuleDef> modules;

		public TypeResolver() {
			this.modules = new List<ModuleDef>();
		}

		public void add(ModuleDef module) {
			this.modules.Add(module);
		}

		public TypeDef? Find(string fullName, bool isReflectionName) {
			foreach (ModuleDef mod in this.modules) {
				TypeDef td = mod.Find(fullName, isReflectionName);
				if (td != null) return td;
			}
			return null;
		}

		public TypeDef? Find(TypeRef typeRef) {
			foreach (ModuleDef mod in this.modules) {
				TypeDef td = mod.Find(typeRef);
				if (td != null) return td;
			}
			return null;
		}

		public TypeDef? Find(ITypeDefOrRef typeRef) {
			foreach (ModuleDef mod in this.modules) {
				TypeDef td = mod.Find(typeRef);
				if (td != null) return td;
			}
			return null;
		}
	}
}