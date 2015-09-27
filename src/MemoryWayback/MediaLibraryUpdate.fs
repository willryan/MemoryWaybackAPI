module MemoryWayback.MediaLibraryUpdate

open System
open MemoryWayback.Persistence

module Internal =
  let updateMedia dirFinder fileHandler removeOld dir (per:IPersistence) : IPersistence =
    let files = dirFinder dir
    files
    |> List.fold (fun p file ->
      fileHandler file p
    ) per
    |> removeOld
