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

  let createNewMedia fh time (rootDir:MediaDirectory) (file:FileInfo) =
    let filetype =
      match file.Extension with
      | Photo -> MediaType.Photo
      | Video -> MediaType.Video
      | Other ext -> raise <| Exception(sprintf "unknown file type %s" ext)
    let taken = defaultArg (fh.takenTime file) DateTime.UtcNow
    let subPath = fh.transformPath rootDir file.FullName
    //printfn "%s" subPath
    {
      Id = -1
      Url = sprintf "/api/media/%s%s" rootDir.Mount subPath
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

  type RemoveOldHandler = DateTime -> IPersistence -> IPersistence
  type FileHandler = FileHelper -> DateTime -> MediaDirectory -> FileInfo -> IPersistence -> IPersistence
  let updateMedia ( (fileHandlerF:FileHandler), (removeOldF:RemoveOldHandler)) =
    fun time (fh:FileHelper) (dir:MediaDirectory) (per:IPersistence) ->
      let foldFile p file = fileHandlerF fh time dir file p
      fh.fileFinder dir
      |> List.fold foldFile per
      |> removeOldF time

  // PARTIALS
  let removeOldC = removeOld getOldMedias
  let fileUpdateC = fileUpdate (createNewMedia,matchExisting,itemUpdate)

let updateMedia : DateTime -> FileHelper -> MediaDirectory -> IPersistence -> IPersistence =
  Internal.updateMedia (Internal.fileUpdateC, Internal.removeOldC)
