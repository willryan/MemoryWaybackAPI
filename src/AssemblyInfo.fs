namespace System
open System.Reflection

[<assembly: AssemblyTitleAttribute("MemoryWayback.Console")>]
[<assembly: AssemblyProductAttribute("MemoryWayback")>]
[<assembly: AssemblyDescriptionAttribute("browse through old memories (photos, videos)")>]
[<assembly: AssemblyVersionAttribute("1.0")>]
[<assembly: AssemblyFileVersionAttribute("1.0")>]
do ()

module internal AssemblyVersionInformation =
    let [<Literal>] Version = "1.0"
