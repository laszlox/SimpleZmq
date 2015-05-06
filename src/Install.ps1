param($installPath, $toolsPath, $package, $project)

write-host $package.Version

# setting the x86 and x64 native libzmq.dll's to be copied always into the output directory.
$project.ProjectItems.Item("SimpleZmq-" + $package.Version + ".0-x64").ProjectItems.Item("libzmq.dll").Properties.Item("CopyToOutputDirectory").Value = 1
$project.ProjectItems.Item("SimpleZmq-" + $package.Version + ".0-x86").ProjectItems.Item("libzmq.dll").Properties.Item("CopyToOutputDirectory").Value = 1
