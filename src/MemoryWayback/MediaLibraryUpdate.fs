module MemoryWayback.MediaLibraryUpdate

open System
open System.IO
open MemoryWayback.Persistence
open MemoryWayback.DbTypes
open ExtCore.Control

module Internal =
  let updateMedia dirFinder fileHandler removeOld dir (per:IPersistence) : IPersistence =
    let time = DateTime.UtcNow
    let files = dirFinder dir
    files
    |> List.fold (fun p file ->
      fileHandler time file p
    ) per
    |> removeOld time

  let itemUpdate (newValue:medias) existingItems (per:IPersistence) =
    match existingItems with
      | [] ->
        newValue.Id <- -1 // get a new id
        per.Insert(newValue)
      | itm :: _ ->
        newValue.Id <- itm.Id
        per.Update(newValue)

  let matchExisting (file:FileInfo) (per:IPersistence) =
    per.Select(<@ fun (media:medias) ->
      media.Url = file.FullName
    @>)

  let fileUpdate time (file:FileInfo) (p:IPersistence) : IPersistence =
    let newMedia =
      {
        Id = -1
        Url = file.FullName
        Taken = file.LastWriteTime
        Added = time
        Type = MediaType.Photo
      }

    let fn = state {
      let! items = matchExisting file
      return itemUpdate newMedia items
    }
    State.execute fn p

  let rec removeAll (recs:medias list) (p:IPersistence) =
    match recs with
    | recd :: tail ->
      let (_,p2) = p.Delete(recd)
      removeAll tail p2
    | [] -> p

  let getOldMedias (p:IPersistence) (t:DateTime) : (medias list * IPersistence) =
    p.Select(<@ fun (media:medias) ->
      media.Added < t
    @>)

  let removeOld (time:DateTime) (p:IPersistence) =
    let old, newP = getOldMedias p time
    removeAll old newP
