var target = Argument<string>("target");
var ui = Argument("ui", "");
var sep = System.IO.Path.DirectorySeparatorChar;
// This list of suffixes is common for all Nickvision projects
var projectSuffix = ui.ToLower() switch
{
    "gnome" => "GNOME",
    _ => ""
};
// Only some tasks require to set a project to work with
var requiresUI = new string[] { "Clean", "Build", "Run", "Publish", "Flatpak" }.Any(target.Contains);
if ((string.IsNullOrEmpty(projectSuffix) || !projectsToBuild.Contains(projectSuffix))  && requiresUI)
{
    throw new CakeException($"Unknown UI. Possible values: {string.Join(", ", projectsToBuild)}.");
}
//Load tasks and run
#load local:?path=docs.cake
#load local:?path=dotnet.cake
#load local:?path=gettext.cake
#load local:?path=packaging.cake
RunTarget(target);