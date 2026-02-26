using System.Text;

namespace DNFBDmp {
	public class FBSBuilder {
		enum BuildingState { FINISHED = 0, BUILDING_TABLE, BUILDING_ENUM };
		private StringBuilder builder;
		private BuildingState state;
		private bool firstEnumDone;

		public FBSBuilder() {
			this.builder = new StringBuilder();
			this.state = BuildingState.FINISHED;
			this.firstEnumDone = false;
		}

		public FBSBuilder beginTable(string name) {
			this.state = BuildingState.BUILDING_TABLE;
			this.builder.AppendLine($"table {name} {{");
			return this;
		}

		public FBSBuilder addTableField(string name, string type) {
			this.builder.AppendLine($"\t{name}:{type};");
			return this;
		}

		public FBSBuilder addTableArrayField(string name, string type) {
			this.builder.AppendLine($"\t{name}:[{type}];");
			return this;
		}

		public FBSBuilder endTable() {
			this.state = BuildingState.FINISHED;
			this.builder.AppendLine("}\n");
			return this;
		}

		public FBSBuilder beginEnum(string name, string type) {
			this.state = BuildingState.BUILDING_ENUM;
			this.firstEnumDone = false;
			this.builder.AppendLine($"enum {name} : {type} {{");
			return this;
		}

		public FBSBuilder addEnumValue(string name, object? value=null) {
			if (this.firstEnumDone) this.builder.AppendLine(",");
			if (value != null) this.builder.Append($"\t{name} = {value}");
			else this.builder.Append($"\t{name}");
			if (!this.firstEnumDone) this.firstEnumDone = true;
			return this;
		}

		public FBSBuilder endEnum() {
			this.state = BuildingState.FINISHED;
			this.builder.AppendLine();
			this.builder.AppendLine("}");
			return this;
		}

		public string build() {
			return this.builder.ToString();
		}
	}
}