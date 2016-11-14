module MemoryWayback.MediaLibraryUpdate

open System
open System.Net
open System.IO
open ExtCore.Control
open ExifLib
open MemoryWayback.FileHelper
open MemoryWayback.Persistence
open MemoryWayback.DbTypes

module Internal =
   // SIMPLE
  let itemUpdate (newValue:medias) existingItems (per:IPersistence) =
    match existingItems with
      | [] ->
        per.Insert({ newValue with Id = -1 })
      | itm :: _ ->
        per.Update({ newValue with Id = itm.Id })

  let matchExisting (newMedia:medias) (per:IPersistence) =
    per.Select(<@ fun (media:medias) ->
      media.Url = newMedia.Url
    @>)

  let createNewMedia fh time (MediaDirectory rootDir) (file:FileInfo) =
    let filetype =
      match file.Extension with
      | Photo -> MediaType.Photo
      | Video -> MediaType.Video
    let taken = defaultArg (fh.takenTime file) DateTime.UtcNow
    let rootFh = FileInfo(rootDir)
    let subPath = file.FullName.Substring(rootFh.FullName.Length)
    {
      Id = -1
      Url = sprintf "/media%s" <| Uri.EscapeUriString subPath
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
  let removeOld getOldF =
    fun (time:DateTime) (p:IPersistence) ->
      let old, newP = getOldF p time
      removeAll old newP

  let fileUpdate (makeNewF,matchF,updateF) =
    fun takenF time rootDir file (p:'p) ->
      let newMedia = makeNewF takenF time rootDir file
      let fn = state {
        let! items = matchF newMedia
        let! dbRec = updateF newMedia items
        return dbRec
      }
      State.execute fn p

  let updateMedia (fileHandlerF,removeOldF) =
    fun fh dir per ->
      let time = DateTime.UtcNow
      let foldFile p file = fileHandlerF fh time dir file p
      let files = fh.fileFinder dir
      files
      |> List.fold foldFile per
      |> removeOldF time

  // PARTIALS
  let removeOldC = removeOld getOldMedias
  let fileUpdateC = fileUpdate (createNewMedia,matchExisting,itemUpdate)

let updateMedia : FileHelper -> MediaDirectory -> IPersistence -> IPersistence =
  Internal.updateMedia (Internal.fileUpdateC, Internal.removeOldC)
