module MemoryWayback.FileHelper

open System.IO
open System
open ExifLib
open ExtCore.Collections
open ExtCore.Control
open System.Text.RegularExpressions
open System.Linq

type MediaDirectory = {
  Path : string
  Mount : string
}

let photoExtensions = [".jpg";".jpeg"] //;".png";".bmp"]
let videoExtensions = [".avi";".mov";".mpg";".mpeg";".mp4";".m4v"] //;".png";".bmp"]

let allValidExtensions = List.append photoExtensions videoExtensions

let (|Photo|Video|Other|) (ext:string) =
  if (List.contains ext photoExtensions) then
    Photo
  else if (List.contains ext videoExtensions) then
    Video
  else
    Other ext

type FileHelper = {
  urlBuilder : MediaDirectory -> FileInfo -> string
  takenTime : FileInfo -> DateTime option
  fileFinder : MediaDirectory -> FileInfo list
  transformPath : MediaDirectory -> string -> string
}

module Internal =
  let urlBuilder (rootDir:MediaDirectory) (file:FileInfo) =
    file.FullName.Substring(rootDir.Path.Length)
  let takenTime (file:FileInfo) : DateTime option =
    let exifRead (file:FileInfo) =
      try
        use reader = new ExifReader(file.FullName)
        let taken = DateTime.MinValue
        if (reader.GetTagValue(ExifTags.DateTimeOriginal, ref taken)) then
          if taken = DateTime.MinValue then
            None
          else
            Some taken
        else
          None
      with e ->
        None
    
    let fileNameTime (file:FileInfo) = 
      let m = Regex.Match(file.Name, "IMG_(\d\d\d\d)(\d\d)(\d\d)_(\d\d)(\d\d)(\d\d)\.")
      if m.Success then
        let times = 
          m.Groups.Cast<Group>()
          |> Seq.skip 1
          |> Seq.map (fun g -> System.Int32.Parse g.Value)
          |> Seq.toArray
        Some <| new DateTime(times.[0], times.[1], times.[2], times.[3], times.[4], times.[5])
      else
        None

    let fileStampRead (file:FileInfo) =
      if (file.CreationTime < file.LastWriteTime) then
        Some file.CreationTime
      else
        Some file.LastWriteTime

    let mpgRead (_:FileInfo) = None

    let finders = [ exifRead ; mpgRead ; fileNameTime ; fileStampRead ]
    List.fold (fun s fn -> Option.tryFillWith (fun _ -> fn file) s) None finders

  let rec fileFinder (name:MediaDirectory) =
    let di = new DirectoryInfo(name.Path)
    let lst = di.GetFiles() |> Array.toList
    let photos = lst |> List.filter (fun f -> List.contains f.Extension allValidExtensions)
    let subDirPhotos = 
      di.GetDirectories()
      |> Array.toList
      |> List.collect (fileFinder << (fun d -> { name with Path = d.FullName }))
    List.append photos subDirPhotos

  let transformPath (rootDir:MediaDirectory) fullPath = 
    let rootFh = FileInfo rootDir.Path
    String.substring rootFh.FullName.Length fullPath
    |> String.replace "\\" "/"
    |> String.replace "?" "%3F"
    |> Uri.EscapeUriString

let realFileHelper = {
  urlBuilder = Internal.urlBuilder
  takenTime = Internal.takenTime
  fileFinder = Internal.fileFinder
  transformPath = Internal.transformPath
}
