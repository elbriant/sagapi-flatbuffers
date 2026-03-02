using System.Text;
using dnlib.DotNet;

namespace DNFBDmp
{
	public class FlatbufferDefinition
	{
		public string name;
		public TypeSig type;
		public HashSet<FlatbufferDefinition> dependencies;
		public bool isDone;
		public bool isArray;
		public bool isRootType;
		public string? data;

		public static Dictionary<string, FlatbufferDefinition> convTypes = new Dictionary<string, FlatbufferDefinition>();
		public static HashSet<string> names = new HashSet<string>();

		public static FlatbufferDefinition convert(TypeSig type, TypeResolver resolver)
		{
			type = type.RemovePinnedAndModifiers();
			string fullName = type.FullName;
			string name = Utils.cleanupClassName(fullName);
			FlatbufferDefinition? fbDef;

			if (convTypes.TryGetValue(fullName, out fbDef))
			{
				if (fbDef.name != name)
					throw new Exception("Mismatched name for same typedef ??");
			}
			else
			{
				fbDef = new FlatbufferDefinition(name, type);
			}

			if (!fbDef.isDone && fbDef.data == null)
				fbDef.build(resolver);

			return fbDef;
		}

		private FlatbufferDefinition(string name, TypeSig type)
		{
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

		public string? getType()
		{
			if (!this.isDone) return null;
			if (this.isArray) return $"[{this.name}]";
			return this.name;
		}

		public string getFile() { return this.name + ".fbs"; }

		public bool writeToFile(string? path = null)
		{
			if (!this.isDone || this.data == null) return false;
			string outFile = getFile();
			if (path != null) outFile = Path.Combine(path, outFile);
			File.WriteAllText(outFile, this.data);
			return true;
		}

		private static string? getPrimitiveType(IType type)
		{
			// Modified to use same types as MooncellWiki
			switch (type.FullName)
			{
				case "System.Boolean": return "bool";
				case "System.Char": return "uint16"; // not used
				case "System.SByte": return "int8"; // not used
				case "System.Byte": return "ubyte";
				case "System.Int16": return "short";
				case "System.UInt16": return "uint16"; // not used
				case "System.Int32": return "int";
				case "System.UInt32": return "uint32"; // not used
				case "System.Int64": return "long";
				case "System.UInt64": return "uint64"; // not used
				case "System.Single": return "float";
				case "System.Double": return "double";
				case "System.String": return "string";
			}
			return null;
		}

		// Gets the TypeSig of the type inside the array if it's one
		private static TypeSig? getArraySig(TypeSig sig)
		{
			string fullName = sig.FullName;
			if (sig.IsGenericInstanceType && (fullName.StartsWith("System.Collections.Generic.List") ||
				fullName.StartsWith("System.Collections.Generic.Stack") || fullName.StartsWith("System.Collections.Generic.HashSet")))
			{
				GenericInstSig genericInstSig = sig.ToGenericInstSig();
				return genericInstSig.GenericArguments[0];
			}
			else if (sig.IsSZArray)
			{
				return sig.Next;
			}
			return null;
		}

		// Change out the generic type parameter with the real TypeSig if needed		
		private static TypeSig handleGenericSig(TypeSig sig, IList<TypeSig>? genericArgs)
		{
			if (sig.IsGenericTypeParameter)
			{
				if (genericArgs == null) throw new Exception("Generic parameters null with generic argument");
				GenericVar genericVar = sig.ToGenericVar();
				sig = genericArgs[(int)genericVar.Number];
			}
			else if (sig.IsGenericMethodParameter)
			{
				throw new Exception("Generic Method param not supported");
			}
			return sig;
		}

		// Find & return type of TypeSig, converts to FBDef if needed and adds to dependency
		private string? getType(TypeSig sig, TypeResolver resolver, IList<TypeSig>? genericArgs = null)
		{
			if (sig.FullName == "System.Int16[,]")
			{
				return "hg__internal__MapData";
			}

			// baking replacement for Torappu.Blackboard to Torappu.Blackboard/DataPäir
			if (sig.FullName == "Torappu.Blackboard")
			{
				TypeDef? classDataPair = resolver.Find("Torappu.Blackboard+DataPair", true);
				TypeSig? typeSigDataPair = new ClassSig(classDataPair);
				FlatbufferDefinition arrayFbDef = FlatbufferDefinition.convert(typeSigDataPair, resolver);
				if (arrayFbDef == null) return null;
				this.dependencies.Add(arrayFbDef);
				return $"[{arrayFbDef.getType()}]";
			}

			string? prim = getPrimitiveType(sig);
			if (prim != null) return prim;

			TypeSig? sigArray = getArraySig(sig);
			if (sigArray != null)
			{
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

		private static bool hasJsonIgnore(FieldDef field)
		{
			foreach (CustomAttribute ca in field.CustomAttributes)
			{
				string fullName = ca.TypeFullName;
				if (fullName.StartsWith("Newtonsoft.Json.JsonIgnoreAttribute")) return true;
			}
			return false;
		}

		private bool handleCustomFBS(TypeSig sig, IList<TypeSig>? genericArgs, TypeResolver resolver, FBSBuilder builder)
		{
			if (sig.FullName.StartsWith("System.Collections.Generic.Dictionary") || sig.FullName.StartsWith("Torappu.ListDict"))
			{
				if (genericArgs == null || genericArgs.Count != 2) throw new Exception("Bad dict generic args");

				string? keyType = getType(genericArgs[0], resolver);
				string? valueType = getType(genericArgs[1], resolver);

				if (keyType == null || valueType == null) throw new Exception("Couldn't get key or value type for dict");
				builder.beginTable(this.name);
				builder.addTableField("dict_key", keyType);
				builder.addTableField("dict_value", valueType);
				builder.endTable();

				this.isArray = true;
			}
			else if (sig.FullName.StartsWith("Newtonsoft.Json.Linq.JObject"))
			{
				builder.beginTable(this.name);
				builder.addTableField("jobj_bson", "string");
				builder.endTable();
			}
			else
			{
				return false;
			}
			return true;
		}

		public bool build(TypeResolver resolver)
		{
			if (this.data != null) return false;
			this.isDone = true;

			// Contains includes
			StringBuilder header = new StringBuilder();
			// Easier building of the table
			FBSBuilder fbBuilder = new FBSBuilder();

			TypeSig sig = this.type;
			// We want to use the type for the getArraySig before modifying handling the generic types
			// Because otherwise we won't get the generic arrays parameter
			TypeSig? arraySig = getArraySig(sig);
			// Generic args if we have some
			IList<TypeSig>? genericArgs = null;
			// We're a class by default
			this.isRootType = true;

			if (arraySig == null && sig.IsGenericInstanceType)
			{
				GenericInstSig genericSig = sig.ToGenericInstSig();
				genericArgs = genericSig.GenericArguments;
				sig = genericSig.GenericType;
			}

			if (arraySig != null)
			{
				// Nested single dim array
				string? type = getType(arraySig, resolver);
				if (type == null) throw new Exception("Can't find array type");
				fbBuilder.beginTable(this.name);
				fbBuilder.addTableArrayField("arr_values", type);
				fbBuilder.endTable();
			}
			else if (handleCustomFBS(sig, genericArgs, resolver, fbBuilder))
			{
				// Schemas with custom serializing functions
				// handled custom FBS
			}
			else if (sig.IsArray)
			{
				// empty for multi-dim
				// is already handled by 
				// hg_internal

			}
			else if (sig.IsTypeDefOrRef)
			{
				// Regular class, bulk of the classes
				// Get the TypeDef
				ITypeDefOrRef classSig = sig.ToTypeDefOrRef();
				TypeDef? def = resolver.Find(classSig);
				if (def == null) throw new Exception($"Couldn't find class ? {classSig.FullName}");

				if (def.IsEnum)
				{
					// Enum
					TypeSig enumSig = def.GetEnumUnderlyingType();
					if (!enumSig.IsPrimitive) throw new Exception("Enum of non primitve type");

					string? primType = getPrimitiveType(enumSig);
					if (primType == null || !(primType.StartsWith("int") || primType.StartsWith("uint") || primType == "ubyte"))
						throw new Exception($"Invalid primitive type for enum: {primType}");

					fbBuilder.beginEnum(this.name, primType);
					int nbFields = def.Fields.Count;
					bool hasZero = false;
					// ignore the first one as it is the actual value
					for (int i = 1; i < nbFields; i++)
					{
						FieldDef field = def.Fields[i];
						if (!field.HasConstant) throw new Exception("Enum's field without a value");
						fbBuilder.addEnumValue(field.Name, field.Constant.Value);
						if (Convert.ToInt32(field.Constant.Value) == 0) hasZero = true;
					}
					// Hack to fix enums not working when no zero is defined...
					// We can't know what the default value of the flatbuffer is
					// So just signal we got the default value
					if (!hasZero) fbBuilder.addEnumValue("ENUM_DEFAULT_VALUE", 0);
					fbBuilder.endEnum();
					this.isRootType = false;
				}
				else
				{
					// Normal class
					fbBuilder.beginTable(this.name);

					// --- Rescatar Enums y tipos de diccionarios heredados ---
					var allFields = new List<Tuple<FieldDef, IList<TypeSig>?>>();
					TypeDef? currentDef = def;
					IList<TypeSig>? currentGenericArgs = genericArgs;

					// 1. Escalar por la herencia, PERO deteniéndonos en la frontera de Unity o .NET
					while (currentDef != null)
					{
						string fName = currentDef.FullName;
						// Evitamos extraer los punteros de memoria internos del motor de Unity
						if (fName == "System.Object" || fName == "System.ValueType" || fName.StartsWith("UnityEngine.") || fName.StartsWith("System."))
						{
							break;
						}

						var fields = new List<Tuple<FieldDef, IList<TypeSig>?>>();
						foreach (FieldDef f in currentDef.Fields)
						{
							if (!f.IsStatic && !f.IsNotSerialized && !hasJsonIgnore(f))
							{
								fields.Add(new Tuple<FieldDef, IList<TypeSig>?>(f, currentGenericArgs));
							}
						}
						// Insertamos al inicio para respetar el orden de memoria de FlatBuffers
						allFields.InsertRange(0, fields);

						// 2. Resolver los argumentos genericos de la clase padre
						if (currentDef.BaseType != null)
						{
							TypeSig baseSig = currentDef.BaseType.ToTypeSig();
							if (baseSig.IsGenericInstanceType)
							{
								GenericInstSig genericInst = baseSig.ToGenericInstSig();
								var resolvedArgs = new List<TypeSig>();
								foreach (var arg in genericInst.GenericArguments)
								{
									try
									{
										resolvedArgs.Add(handleGenericSig(arg, currentGenericArgs));
									}
									catch
									{
										resolvedArgs.Add(arg);
									}
								}
								currentGenericArgs = resolvedArgs;
							}
							else
							{
								currentGenericArgs = null;
							}
							currentDef = currentDef.BaseType.ResolveTypeDef();
						}
						else
						{
							break;
						}
					}

					// 3. Procesar los campos con Diagnostico Exacto
					foreach (var tuple in allFields)
					{
						FieldDef field = tuple.Item1;
						IList<TypeSig>? fArgs = tuple.Item2;

						try
						{
							TypeSig fieldSig = handleGenericSig(field.FieldType, fArgs);

							// FlatBuffers no soporta Punteros ni variables abstractas
							if (fieldSig.IsPointer || fieldSig.IsByRef || fieldSig.IsGenericTypeParameter || fieldSig.IsGenericMethodParameter)
							{
								Console.WriteLine($"[INFO] Ignorando campo no-serializable: {field.Name} en {def.FullName}");
								continue;
							}

							string? type = getType(fieldSig, resolver, fArgs);
							if (type != null)
							{
								string cleanFieldName = field.Name;
								if (cleanFieldName.Contains("k__BackingField"))
								{
									cleanFieldName = cleanFieldName.Replace("<", "").Replace(">k__BackingField", "");
								}
								fbBuilder.addTableField(cleanFieldName, type);
							}
						}
						catch (Exception ex)
						{
							// En lugar de crashear, te decimos EXACTAMENTE que campo fallo para que lo evalues
							Console.WriteLine($"[ERROR] No se pudo procesar el campo '{field.Name}' en '{def.FullName}': {ex.Message}");
						}
					}
					// -------------------------------------------------------------

					fbBuilder.endTable();
				}
			}
			else
			{
				throw new Exception("Unhandled type");
			}

			// Process dependencies
			foreach (FlatbufferDefinition fb in this.dependencies)
				header.AppendLine($"include \"{fb.getFile()}\";");

			// Put together everything to make the FBS file
			this.data = header.ToString() + "\n" + fbBuilder.build();
			if (this.isRootType) this.data += $"\nroot_type {this.name};";

			// append hg_internal
			if (this.data.Contains("hg__internal__MapData"))
			{
				string mapDataDef = "table hg__internal__MapData {\n\trow_size:int;\n\tcolumn_size:int;\n\tmatrix_data:[short];\n}\n\n";
				this.data = mapDataDef + this.data;
			}
			// Cleanup because all my homies hate CRLF Windows dogshit
			this.data = this.data.Replace("\r\n", "\n");

			return true;
		}
	}
}