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
  type UpdateDb<'db> = 'db -> 'db list -> StateFunc<IPersistence,'db>
  type UpdateMedia = UpdateDb<medias>
  type UpdateDir = UpdateDb<media_directories>

  let update<'db> (mInsert:'db -> 'db) (mUpdate:'db -> 'db -> 'db) (newValue:'db) (existingValues:'db list) (per:IPersistence) =
    match existingValues with
      | [] ->
        per.Insert(mInsert newValue)
      | itm :: _ ->
        per.Update(mUpdate itm newValue)

  let itemUpdate : UpdateMedia = update (fun m -> { m with Id = -1 }) (fun m n -> { n with Id = m.Id })
  let dirUpdate : UpdateDir = update (fun m -> { m with Id = -1 }) (fun m n -> { n with Id = m.Id })

  type MatchDb<'db> = 'db -> StateFunc<IPersistence,'db list>
  type MatchMedia = MatchDb<medias>
  type MatchDir = MatchDb<media_directories>

  let matchExistingMedia : MatchMedia = fun (newMedia:medias) (per:IPersistence) ->
    per.Select(<@ fun (media:medias) -> media.Url = newMedia.Url @>)

  let matchExistingDir : MatchDir = fun (newDir:media_directories) (per:IPersistence) ->
    per.Select(<@ fun (dir:media_directories) -> dir.Path = newDir.Path @>)

  type CreateDir = FileHelper -> DateTime -> media_directories -> media_directories
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
    fun time (fh:FileHelper) (per:IPersistence) (dirs:media_directories list) ->
      let foldDir ps dir = 
        let foldFile p file = fileHandlerF fh time dir file p
        fh.fileFinder (makeMediaDirectory dir)
          |> List.fold foldFile ps
          |> removeOldF time
      List.fold foldDir per dirs

  // PARTIALS
  let removeOldC = removeOld getOldMedias
  let fileUpdateC = fileUpdate (createNewMedia,matchExistingMedia,itemUpdate)

let updateMedia : DateTime -> FileHelper -> IPersistence -> media_directories list -> IPersistence =
  Internal.updateMedia (Internal.fileUpdateC, Internal.removeOldC)
