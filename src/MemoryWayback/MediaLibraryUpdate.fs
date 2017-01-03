module MemoryWayback.MediaLibraryUpdate

open System
open System.Net
open System.IO
open ExtCore.Control
open ExifLib
open MemoryWayback.FileHelper
open MemoryWayback.Persistence
open MemoryWayback.DbTypes
open MemoryWayback.Types

module Internal =
   // SIMPLE
  let itemUpdate (newValue:medias) (existingItems:medias list) (per:IPersistence) =
    match existingItems with
      | [] ->
        per.Insert({ newValue with Id = -1 })
      | itm :: _ ->
        per.Update({ newValue with Id = itm.Id })

  let matchExisting (newMedia:medias) (per:IPersistence) =
    per.Select(<@ fun (media:medias) ->
      media.Url = newMedia.Url
    @>)


  let createNewMedia fh time (rootDir:media_directories) (file:FileInfo) =
    let filetype =
      match file.Extension with
      | Photo -> MediaType.Photo
      | Video -> MediaType.Video
      | Other ext -> raise <| Exception(sprintf "unknown file type %s" ext)
    let taken = defaultArg (fh.takenTime file) DateTime.UtcNow
    let md = Types.makeMediaDirectory rootDir
    let subPath = fh.transformPath md file.FullName
    //printfn "%s" subPath
    {
      Id = -1
      Url = sprintf "/api/media/%s%s" rootDir.Mount subPath
      Taken = taken
      Added = time
      Type = filetype
      MediaDirectoryId = rootDir.Id
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
    fun takenF time (rootDir:media_directories) file (p:'p) ->
      let newMedia = makeNewF takenF time rootDir file
      let fn = state {
        let! items = matchF newMedia
        let! dbRec = updateF newMedia items
        return dbRec
      }
      State.execute fn p

  type RemoveOldHandler = DateTime -> IPersistence -> IPersistence
  type FileHandler = FileHelper -> DateTime -> media_directories -> FileInfo -> IPersistence -> IPersistence
  let updateMedia ( (fileHandlerF:FileHandler), (removeOldF:RemoveOldHandler)) =
    fun time (fh:FileHelper) (dir:media_directories) (per:IPersistence) ->
      
      let foldFile p file = fileHandlerF fh time dir file p
      fh.fileFinder (makeMediaDirectory dir)
      |> List.fold foldFile per
      |> removeOldF time

  // PARTIALS
  let removeOldC = removeOld getOldMedias
  let fileUpdateC = fileUpdate (createNewMedia,matchExisting,itemUpdate)

let updateMedia : DateTime -> FileHelper -> media_directories -> IPersistence -> IPersistence =
  Internal.updateMedia (Internal.fileUpdateC, Internal.removeOldC)
