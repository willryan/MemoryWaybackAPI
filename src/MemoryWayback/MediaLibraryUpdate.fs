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
  type UpdateMedia = medias -> medias list -> StateFunc<IPersistence,medias>
  let itemUpdate : UpdateMedia = fun (newValue:medias) (existingItems:medias list) (per:IPersistence) ->
    match existingItems with
      | [] ->
        per.Insert({ newValue with Id = -1 })
      | itm :: _ ->
        per.Update({ newValue with Id = itm.Id })

  type MatchMedia = medias -> StateFunc<IPersistence,medias list>
  let matchExisting : MatchMedia = fun (newMedia:medias) (per:IPersistence) ->
    per.Select(<@ fun (media:medias) -> media.Url = newMedia.Url @>)

  type CreateMedia = FileHelper -> DateTime -> media_directories -> FileInfo -> medias
  let createNewMedia : CreateMedia = fun (fh:FileHelper) time (rootDir:media_directories) (file:FileInfo) ->
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

  type RemoveAllMedia = medias list -> IPersistence -> IPersistence
  let rec removeAll (recs:medias list) (p:IPersistence) =
    match recs with
    | recd :: tail ->
      let (_,p2) = p.Delete(recd)
      removeAll tail p2
    | [] -> p

  type GetOldMedia = IPersistence -> DateTime -> medias list * IPersistence
  let getOldMedias : GetOldMedia = fun (p:IPersistence) (t:DateTime) ->
    p.Select(<@ fun (media:medias) -> media.Added < t @>)

   // COMPOSITE
  let removeOld (getOldF:GetOldMedia) (time:DateTime) (p:IPersistence) =
    getOldF p time ||> removeAll 

  let fileUpdate ((makeNewF:CreateMedia),(matchF:MatchMedia),(updateF:UpdateMedia)) =
    fun fileHelper time (rootDir:media_directories) (file:FileInfo) (p:IPersistence) ->
      let newMedia = makeNewF fileHelper time rootDir file
      let fn = state {
        let! items = matchF newMedia
        return! updateF newMedia items
      }
      State.execute fn p

  type RemoveOldHandler = DateTime -> IPersistence -> IPersistence
  type FileHandler = FileHelper -> DateTime -> media_directories -> FileInfo -> IPersistence -> IPersistence
  let updateMedia ((fileHandlerF:FileHandler), (removeOldF:RemoveOldHandler)) =
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
