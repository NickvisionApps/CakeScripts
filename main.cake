#load local:?path=dotnet.cake
#load local:?path=gettext.cake
#load local:?path=packaging.cake

var target = Argument<string>("target");
var ui = Argument("ui", "");
var requiresUI = target switch
{
    "Clean" or "Build" or "Run" or "Publish" or "FlatpakSourcesGen" => true,
    _ => false
};
var projectSuffix = ui.ToLower() switch
{
    "gnome" => "GNOME",
    _ => ""
};
if (string.IsNullOrEmpty(projectSuffix) && requiresUI)
{
    throw new CakeException("Unknown UI. Possible values: gnome.");
}
var sep = System.IO.Path.DirectorySeparatorChar;

RunTarget(target);