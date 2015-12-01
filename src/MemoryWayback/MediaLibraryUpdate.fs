module MemoryWayback.MediaLibraryUpdate

open System
open System.IO
open MemoryWayback.Persistence
open MemoryWayback.DbTypes
open ExtCore.Control
open ExifLib
open MemoryWayback.FileHelper

module Internal =
   // SIMPLE
  let itemUpdate (newValue:medias) existingItems (per:IPersistence) =
    match existingItems with
      | [] ->
        newValue.Id <- -1 // get a new id
        per.Insert(newValue)
      | itm :: _ ->
        newValue.Id <- itm.Id
        per.Update(newValue)

  let matchExisting (newMedia:medias) (per:IPersistence) =
    per.Select(<@ fun (media:medias) ->
      media.Url = newMedia.Url
    @>)

  let createNewMedia fh time (rootDir:string) (file:FileInfo) =
    let filetype =
      match file.Extension with
      | Photo -> MediaType.Photo
      | Video -> MediaType.Video
    let taken = defaultArg (fh.takenTime file) DateTime.UtcNow
    {
      Id = -1
      Url = file.FullName.Substring(rootDir.Length)
      Taken = taken
      Added = time
      Type = filetype
    }

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

   // COMPOSITE
  let removeOld getOldF (time:DateTime) (p:IPersistence) =
    let old, newP = getOldF p time
    removeAll old newP

  let fileUpdate makeNewF matchF updateF takenF time rootDir file (p:'p) : 'p =
    let newMedia = makeNewF takenF time rootDir file
    let fn = state {
      let! items = matchF newMedia
      let! dbRec = updateF newMedia items
      return dbRec
    }
    State.execute fn p

  let updateMedia fileHandlerF removeOldF fh dir per =
    let time = DateTime.UtcNow
    let files = fh.fileFinder dir
    files
    |> List.fold (fun p file ->
      fileHandlerF fh time dir file p
    ) per
    |> removeOldF time

  // PARTIALS
  let removeOldC = removeOld getOldMedias
  let fileUpdateC = fileUpdate createNewMedia matchExisting itemUpdate

let updateMedia : FileHelper -> string -> IPersistence -> IPersistence =
  Internal.updateMedia Internal.fileUpdateC Internal.removeOldC
