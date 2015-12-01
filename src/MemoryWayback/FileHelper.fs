module MemoryWayback.FileHelper

open System.IO
open System
open ExifLib

let photoExtensions = [".jpg";".jpeg";".png";".bmp"]

let (|Photo|Video|) (ext:string) =
  if (List.contains ext photoExtensions) then
    Photo
  else
    Video

type FileHelper = {
  urlBuilder : string -> FileInfo -> string
  takenTime : FileInfo -> DateTime option
  fileFinder : string -> FileInfo list
}

module Internal =
  let urlBuilder (rootDir:string) (file:FileInfo) =
    file.FullName.Substring(rootDir.Length)
  let takenTime (file:FileInfo) : DateTime option =
    use reader = new ExifReader(file.FullName)
    let taken = DateTime.UtcNow
    if (reader.GetTagValue(ExifTags.DateTimeDigitized, ref taken)) then
      Some taken
    else
      None
  let fileFinder name =
    (new DirectoryInfo(name)).GetFiles()
    |> Array.toList

let realFileHelper = {
  urlBuilder = Internal.urlBuilder
  takenTime = Internal.takenTime
  fileFinder = Internal.fileFinder
}
