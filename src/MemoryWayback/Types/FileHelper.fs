module MemoryWayback.FileHelperTypes

open System.IO
open System
open MemoryWayback.Types

type FileHelper = {
  urlBuilder : MediaDirectory -> FileInfo -> string
  takenTime : FileInfo -> DateTime option
  fileFinder : MediaDirectory -> FileInfo list
  transformPath : MediaDirectory -> string -> string
}
