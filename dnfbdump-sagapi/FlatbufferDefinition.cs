using System.Text;
using dnlib.DotNet;

namespace DNFBDmp {
	public class FlatbufferDefinition {
		public string name;
		public TypeSig type;
		public HashSet<FlatbufferDefinition> dependencies;
		public bool isDone;
		public bool isArray;
		public bool isRootType;
		public string? data;

		public static Dictionary<string, FlatbufferDefinition> convTypes = new Dictionary<string, FlatbufferDefinition>();
		public static HashSet<string> names = new HashSet<string>();

		public static FlatbufferDefinition convert(TypeSig type, TypeResolver resolver) {
			type = type.RemovePinnedAndModifiers();
			string fullName = type.FullName;
			string name = Utils.cleanupClassName(fullName);
			FlatbufferDefinition? fbDef;
			
			if (convTypes.TryGetValue(fullName, out fbDef)) {
				if (fbDef.name != name)
					throw new Exception("Mismatched name for same typedef ??");
			} else {
				fbDef = new FlatbufferDefinition(name, type);
			}

			if (!fbDef.isDone && fbDef.data == null)
				fbDef.build(resolver);

			return fbDef;
		}

		private FlatbufferDefinition(string name, TypeSig type) {
			this.name = name;
			this.type = type;
			this.dependencies = new HashSet<FlatbufferDefinition>();
			this.data = null;
			this.isDone = false;
			this.isArray = false;
			this.isRootType = false;

			if (names.Contains(name))
				throw new Exception($"Duplicate class name: '{name}' ({type.FullName})");
			names.Add(name);

			convTypes.Add(type.FullName, this);
		}

		public string? getType() {
			if (!this.isDone) return null;
			if (this.isArray) return $"[{this.name}]";
			return this.name;
		}

		public string getFile() { return this.name + ".fbs"; }

		public bool writeToFile(string? path = null) {
			if (!this.isDone || this.data == null) return false;
			string outFile = getFile();
			if (path != null) outFile = Path.Combine(path, outFile);
			File.WriteAllText(outFile, this.data);
			return true;
		}

		private static string? getPrimitiveType(IType type) {
			switch (type.FullName) {
				case "System.Boolean": return "bool";
				case "System.Char":    return "uint16";
				case "System.SByte":   return "int8";
				case "System.Byte":    return "uint8";
				case "System.Int16":   return "int16";
				case "System.UInt16":  return "uint16";
				case "System.Int32":   return "int32";
				case "System.UInt32":  return "uint32";
				case "System.Int64":   return "int64";
				case "System.UInt64":  return "uint64";
				case "System.Single":  return "float";
				case "System.Double":  return "double";
				case "System.String":  return "string";
			}
			return null;
		}

		private static TypeSig? getArraySig(TypeSig sig) {
			string fullName = sig.FullName;
			if (sig.IsGenericInstanceType && (fullName.StartsWith("System.Collections.Generic.List") ||
				fullName.StartsWith("System.Collections.Generic.Stack") || fullName.StartsWith("System.Collections.Generic.HashSet"))) {
				GenericInstSig genericInstSig = sig.ToGenericInstSig();
				return genericInstSig.GenericArguments[0];
			} else if (sig.IsSZArray || sig.IsArray) {
				return sig.Next;
			}
			return null;
		}

		private static TypeSig handleGenericSig(TypeSig sig, IList<TypeSig>? genericArgs) {
			if (sig.IsGenericTypeParameter) {
				if (genericArgs == null) throw new Exception("Generic parameters null with generic argument");
				GenericVar genericVar = sig.ToGenericVar();
				sig = genericArgs[(int)genericVar.Number];
			} else if (sig.IsGenericMethodParameter) {
				throw new Exception("Generic Method param not supported");
			}
			return sig;
		}

		private string? getType(TypeSig sig, TypeResolver resolver, IList<TypeSig>? genericArgs = null) {
			if (sig.FullName == "System.Int16[,]") {
				return "hg__internal__MapData";
			}

			string? prim = getPrimitiveType(sig);
			if (prim != null) return prim;

			TypeSig? sigArray = getArraySig(sig);
			if (sigArray != null) {
				prim = getPrimitiveType(sigArray);
				if (prim != null) return $"[{prim}]";

				sigArray = handleGenericSig(sigArray, genericArgs);
				FlatbufferDefinition arrayFbDef = FlatbufferDefinition.convert(sigArray, resolver);
				if (arrayFbDef == null) return null;

				this.dependencies.Add(arrayFbDef);
				return $"[{arrayFbDef.getType()}]";
			}

			sig = handleGenericSig(sig, genericArgs);
			FlatbufferDefinition fbDef = FlatbufferDefinition.convert(sig, resolver);
			if (fbDef == null) return null;

			this.dependencies.Add(fbDef);
			return fbDef.getType();
		}

		private static bool hasJsonIgnore(FieldDef field) { 
			foreach (CustomAttribute ca in field.CustomAttributes) {
				string fullName = ca.TypeFullName;
				if (fullName.StartsWith("Newtonsoft.Json.JsonIgnoreAttribute")) return true;
			}
			return false;
		}

		private bool handleCustomFBS(TypeSig sig, IList<TypeSig>? genericArgs, TypeResolver resolver, FBSBuilder builder) {
			if (sig.FullName.StartsWith("System.Collections.Generic.Dictionary") || sig.FullName.StartsWith("Torappu.ListDict")) {
				if (genericArgs == null || genericArgs.Count != 2) throw new Exception("Bad dict generic args");

				string? keyType = getType(genericArgs[0], resolver);
				string? valueType = getType(genericArgs[1], resolver);

				if (keyType == null || valueType == null) throw new Exception("Couldn't get key or value type for dict");
				builder.beginTable(this.name);
				builder.addTableField("dict_key", keyType);
				builder.addTableField("dict_value", valueType);
				builder.endTable();

				this.isArray = true;
			} else if (sig.FullName.StartsWith("Newtonsoft.Json.Linq.JObject")) {
				builder.beginTable(this.name);
				builder.addTableField("jobj_bson", "string");
				builder.endTable();
			} else {
				return false;
			}
			return true;
		}

		public bool build(TypeResolver resolver) {
			if (this.data != null) return false;
			this.isDone = true;

			StringBuilder header = new StringBuilder();
			FBSBuilder fbBuilder = new FBSBuilder();
			TypeSig sig = this.type;
			TypeSig? arraySig = getArraySig(sig);
			IList<TypeSig>? genericArgs = null;
			this.isRootType = true;

			if (arraySig == null && sig.IsGenericInstanceType) {
				GenericInstSig genericSig = sig.ToGenericInstSig();
				genericArgs = genericSig.GenericArguments;
				sig = genericSig.GenericType;
			}

			if (arraySig != null) {
				string? type = getType(arraySig, resolver);
				if (type == null) throw new Exception("Can't find array type");
				fbBuilder.beginTable(this.name);
				fbBuilder.addTableArrayField("arr_values", type);
				fbBuilder.endTable();
			} else if (handleCustomFBS(sig, genericArgs, resolver, fbBuilder)) {
				// handled custom FBS
			} else if (sig.IsArray) {
				// empty for multi-dim
			} else if (sig.IsTypeDefOrRef) {
				ITypeDefOrRef classSig = sig.ToTypeDefOrRef();
				TypeDef? def = resolver.Find(classSig);
				if (def == null) throw new Exception($"Couldn't find class ? {classSig.FullName}");

				if (def.IsEnum) {
					TypeSig enumSig = def.GetEnumUnderlyingType();
					if (!enumSig.IsPrimitive) throw new Exception("Enum of non primitve type");

					string? primType = getPrimitiveType(enumSig);
					if (primType == null || !(primType.StartsWith("int") || primType.StartsWith("uint")))
						throw new Exception($"Invalid primitive type for enum: {primType}");

					fbBuilder.beginEnum(this.name, primType);
					int nbFields = def.Fields.Count;
					bool hasZero = false;
					for (int i = 1; i < nbFields; i++) {
						FieldDef field = def.Fields[i];
						if (!field.HasConstant) throw new Exception("Enum's field without a value");
						fbBuilder.addEnumValue(field.Name, field.Constant.Value);
						if (Convert.ToInt32(field.Constant.Value) == 0) hasZero = true;
					}
					if (!hasZero) fbBuilder.addEnumValue("ENUM_DEFAULT_VALUE", 0);
					fbBuilder.endEnum();
					this.isRootType = false;
				} else {
					fbBuilder.beginTable(this.name);

					// --- PARCHE NATIVO: Rescatar Enums y tipos de diccionarios heredados ---
					var allFields = new List<Tuple<FieldDef, IList<TypeSig>?>>();
					TypeDef? currentDef = def;
					IList<TypeSig>? currentGenericArgs = genericArgs;

					// 1. Escalar por el árbol de herencia recolectando campos con su contexto exacto
					while (currentDef != null && currentDef.FullName != "System.Object" && currentDef.FullName != "System.ValueType") {
						var fields = new List<Tuple<FieldDef, IList<TypeSig>?>>();
						foreach (FieldDef f in currentDef.Fields) {
							if (!f.IsStatic && !f.IsNotSerialized && !hasJsonIgnore(f)) {
								fields.Add(new Tuple<FieldDef, IList<TypeSig>?>(f, currentGenericArgs));
							}
						}
						// Insertamos al inicio para respetar el orden de memoria de FlatBuffers
						allFields.InsertRange(0, fields);

						// 2. Al subir a la clase padre, resolvemos sus argumentos genéricos
						if (currentDef.BaseType != null) {
							TypeSig baseSig = currentDef.BaseType.ToTypeSig();
							if (baseSig.IsGenericInstanceType) {
								GenericInstSig genericInst = baseSig.ToGenericInstSig();
								var resolvedArgs = new List<TypeSig>();
								foreach (var arg in genericInst.GenericArguments) {
									// Traducimos el tipo genérico para que C# sepa qué es 'T'
									resolvedArgs.Add(handleGenericSig(arg, currentGenericArgs));
								}
								currentGenericArgs = resolvedArgs;
							} else {
								// Si el padre no es genérico, vaciamos los argumentos
								currentGenericArgs = null;
							}
							currentDef = currentDef.BaseType.ResolveTypeDef();
						} else {
							break;
						}
					}

					// 3. Procesar los campos con su contexto genérico perfecto (Sin ocultar errores)
					foreach (var tuple in allFields) {
						FieldDef field = tuple.Item1;
						IList<TypeSig>? fArgs = tuple.Item2;
						
						TypeSig fieldSig = handleGenericSig(field.FieldType, fArgs);
						string? type = getType(fieldSig, resolver, fArgs);
						
						if (type == null) throw new Exception($"Can't find type for field '{field.Name}' in '{def.FullName}'");
						
						string cleanFieldName = field.Name;
						if (cleanFieldName.Contains("k__BackingField")) {
							cleanFieldName = cleanFieldName.Replace("<", "").Replace(">k__BackingField", "");
						}
						fbBuilder.addTableField(cleanFieldName, type);
					}
					// -------------------------------------------------------------
					
					fbBuilder.endTable();
				}
			} else {
				throw new Exception("Unhandled type");
			}

			foreach (FlatbufferDefinition fb in this.dependencies)
				header.AppendLine($"include \"{fb.getFile()}\";");

			this.data = header.ToString() + "\n" + fbBuilder.build();
			if (this.isRootType) this.data += $"\nroot_type {this.name};";
			if (this.data.Contains("hg__internal__MapData")) {
				string mapDataDef = "table hg__internal__MapData {\n\trow_size:int32;\n\tcolumn_size:int32;\n\tmatrix_data:[int16];\n}\n\n";
				this.data = mapDataDef + this.data;
			}
			this.data = this.data.Replace("\r\n", "\n");

			return true;
		}
	}
}