module MemoryWayback.MediaLibraryUpdate

open System

module Internal =
  let updateMedia dirFinder fileHandler removeOld dir p =
    dirFinder dir
    |> List.fold (fun (p,file) ->
      fileHandler file p
    )
    |> removeOld

