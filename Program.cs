using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

Option<string> projectOption = new("--project", "-p") {
	Description = "Name of the project or root namespace for creating the namespace",
	DefaultValueFactory = _ => FindNamespace()
};

Option<DirectoryInfo> outputOption = new("--output", "-o") {
	Description = "Output directory for generated files",
	DefaultValueFactory = _ => new DirectoryInfo("scripts/generated")
};

Option<string> namespaceOption = new("--namespace", "-n") {
	Description = "Namespace for generated files"
};

Option<bool> noActionsOption = new("--no-actions") {
	Description = "Disable generation of input actions"
};

Option<bool> noGroupsOption = new("--no-groups") {
	Description = "Disable generation of group names"
};

Option<bool> noLayersOption = new("--no-layers") {
	Description = "Disable generation of collision layers"
};

Option<string> actionsNameOption = new("--actions-name", "-a") {
	Description = "Class and file name for input actions",
	DefaultValueFactory = _ => "Actions"
};

Option<string> groupsNameOption = new("--groups-name", "-g") {
	Description = "Class and file name for group names",
	DefaultValueFactory = _ => "Groups"
};

Option<string> layersNameOption = new("--layers-name", "-l") {
	Description = "Class and file name for collision layers",
	DefaultValueFactory = _ => "CollisionLayers"
};

RootCommand rootCommand = new("Godot Constants Generator");
rootCommand.Options.Add(projectOption);
rootCommand.Options.Add(outputOption);
rootCommand.Options.Add(namespaceOption);
rootCommand.Options.Add(noActionsOption);
rootCommand.Options.Add(noGroupsOption);
rootCommand.Options.Add(noLayersOption);
rootCommand.Options.Add(actionsNameOption);
rootCommand.Options.Add(groupsNameOption);
rootCommand.Options.Add(layersNameOption);

ParseResult parseResult = rootCommand.Parse(args);

if (parseResult.Errors.Count > 0) {
	foreach (var error in parseResult.Errors)
		Console.Error.WriteLine(error.Message);
	return 1;
}

if (args.Contains("--help") || args.Contains("-h") || args.Contains("-?")) {
	rootCommand.Parse("-h").Invoke();
	return 0;
}

string project = parseResult.GetValue(projectOption);
DirectoryInfo output = parseResult.GetValue(outputOption);

string ns = parseResult.GetValue(namespaceOption);
bool noActions = parseResult.GetValue(noActionsOption);
bool noGroups = parseResult.GetValue(noGroupsOption);
bool noLayers = parseResult.GetValue(noLayersOption);
string actionsName = parseResult.GetValue(actionsNameOption);
string groupsName = parseResult.GetValue(groupsNameOption);
string layersName = parseResult.GetValue(layersNameOption);

string outputRelativePath = Path.GetRelativePath(
	Directory.GetCurrentDirectory(),
	output.FullName
);
string effectiveNamespace = ns ?? $"{project}.{outputRelativePath.Replace('/', '.').Replace('\\', '.')}";

Console.WriteLine($"Started generating files...");
const string godotProjectPath = "project.godot";

if (!File.Exists(godotProjectPath)) {
	Console.Error.WriteLine("project.godot not found.");
	return 1;
}

string[] lines = File.ReadAllLines(godotProjectPath);

bool inInput = false;
bool inLayers = false;
bool inGroups = false;

var inputActions = new List<string>();
var collisionLayers = new Dictionary<string, int>();
var groups = new List<string>();

foreach (string line in lines) {
	string trimmed = line.Trim();

	switch (trimmed) {
		case "[input]":
			inInput = true;
			inLayers = false;
			inGroups = false;
			continue;
		case "[layer_names]":
			inInput = false;
			inLayers = true;
			inGroups = false;
			continue;
		case "[global_group]":
			inInput = false;
			inLayers = false;
			inGroups = true;
			continue;
	}

	if (trimmed.StartsWith('[')) {
		inInput = false;
		inLayers = false;
		inGroups = false;
		continue;
	}

	if (inInput && line.Contains('=')) {
		string name = line.Split('=')[0].Trim();
		if (!String.IsNullOrWhiteSpace(name))
			inputActions.Add(name);
	}

	if (inLayers && line.Contains("d_physics/layer_")) {
		string[] parts = line.Split('=');
		string layerStr = parts[0].Split('_')[2].Trim();
		string name = parts[1].Trim().Trim('"');

		if (Int32.TryParse(layerStr, out int layerIndex))
			collisionLayers[name] = layerIndex - 1;
	}

	if (inGroups && line.Contains('=')) {
		string group = line.Split('=')[0].Trim();
		if (!String.IsNullOrWhiteSpace(group))
			groups.Add(group);
	}
}

string outputDirectory = output.FullName;
Directory.CreateDirectory(outputDirectory);

// Generate Actions.cs
if (!noActions) {
	var actionBuilder = new StringBuilder();
	actionBuilder.AppendLine("using Godot;\n");
	actionBuilder.AppendLine($"namespace {effectiveNamespace};\n");
	actionBuilder.AppendLine($"public static class {actionsName} {{");
	foreach (string name in inputActions) {
		string sanitized = SanitizeName(name);
		actionBuilder.AppendLine($"\tpublic static readonly StringName {sanitized} = \"{name}\";");
	}
	actionBuilder.AppendLine("}");

	string actionPath = Path.Combine(outputDirectory, $"{actionsName}.cs");
	File.WriteAllText(actionPath, actionBuilder.ToString());
	Console.WriteLine($"Generated: {actionsName}.cs with {inputActions.Count} input actions");
}

// Generate CollisionLayers.cs
if (!noLayers) {
	var layersBuilder = new StringBuilder();
	layersBuilder.AppendLine($"namespace {effectiveNamespace};\n");
	layersBuilder.AppendLine($"public static class {layersName} {{");
	foreach ((string name, int index) in collisionLayers) {
		string sanitized = SanitizeName(name);
		layersBuilder.AppendLine($"\tpublic const uint {sanitized} = 1 << {index};");
	}
	layersBuilder.AppendLine("}");

	string layersPath = Path.Combine(outputDirectory, $"{layersName}.cs");
	File.WriteAllText(layersPath, layersBuilder.ToString());
	Console.WriteLine($"Generated: {layersName}.cs with {collisionLayers.Count} layer names");
}

// Generate Groups.cs
if (!noGroups) {
	var groupsBuilder = new StringBuilder();
	groupsBuilder.AppendLine("using Godot;\n");
	groupsBuilder.AppendLine($"namespace {effectiveNamespace};\n");
	groupsBuilder.AppendLine($"public static class {groupsName} {{");
	foreach (string name in groups) {
		string sanitized = SanitizeName(name);
		groupsBuilder.AppendLine($"\tpublic static readonly StringName {sanitized} = \"{name}\";");
	}
	groupsBuilder.AppendLine("}");

	string groupsPath = Path.Combine(outputDirectory, $"{groupsName}.cs");
	File.WriteAllText(groupsPath, groupsBuilder.ToString());
	Console.WriteLine($"Generated: {groupsName}.cs with {groups.Count} group names");
}

return 0;

// Helper: sanitize names for C# identifiers
static string SanitizeName(string raw) {
	if (String.IsNullOrEmpty(raw)) return "Unnamed";

	var sb = new StringBuilder();
	bool capitalizeNext = true;
	foreach (char c in raw) {
		if (Char.IsLetterOrDigit(c)) {
			sb.Append(capitalizeNext ? Char.ToUpper(c) : c);
			capitalizeNext = false;
		} else {
			capitalizeNext = true; // skip symbols/spaces/dashes
		}
	}

	string result = sb.ToString();
	if (Char.IsDigit(result[0])) result = "_" + result;
	return result;
}

string FindNamespace() {
	string csprojFile = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.csproj").FirstOrDefault();
	if (csprojFile == null) {
		Console.Error.WriteLine("No .csproj file found in the current directory. Please run from the project root or use the project name parameter.");
		Environment.Exit(1);
	}

	var doc = XDocument.Load(csprojFile);
	var nsElement = doc.Descendants("RootNamespace").FirstOrDefault();
	return nsElement != null ? nsElement.Value : Path.GetFileNameWithoutExtension(csprojFile);
}
