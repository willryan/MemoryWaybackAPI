module MemoryWayback.OrmlitePersistence

open System
open ServiceStack.OrmLite
open System.Linq
open System.Linq.Expressions
open Microsoft.FSharp.Linq.RuntimeHelpers
open Microsoft.FSharp.Quotations
open MemoryWayback.Persistence
open System.Reflection

let dbConnection server db uid =
  let connString = sprintf "Server=%s;Database=%s;UID=%s" server db uid
  let fact = new OrmLiteConnectionFactory(connString,MySqlDialect.Provider)
  fact.Open()

let connection = dbConnection "localhost" "memory_wayback" "root"

let rec translateExpr (linq:Expression) =
  match linq with
  | :? MethodCallExpression as mc ->
      let le = mc.Arguments.[0] :?> LambdaExpression
      let args, body = translateExpr le.Body
      le.Parameters.[0] :: args, body
  | _ -> [], linq

let DbSelect<'dbType> (expr:Expr<'dbType -> bool>) =
  let e1 = LeafExpressionConverter.QuotationToExpression(expr)
  let args, body = translateExpr e1
  let csExpr = Expression.Lambda<Func<'dbType, bool>>(body, args |> Array.ofSeq)
  let res = connection.Select<'dbType>(csExpr) |> List.ofSeq
  res

let DbUpdate record =
  ignore <| connection.Update<'dbType> [|record|]
  record
let DbInsert record =
  ignore <| connection.Insert<'dbType> [|record|]
  record
let DbDelete record =
  connection.Delete<'dbType> [|record|] > 0

type OrmlitePersistence() =
  member x.Change (updFun:('dbType -> 'ig)) record =
    (updFun record, x :> IPersistence)
  interface IPersistence with
    member x.Select<'dbType> e =
      DbSelect<'dbType> e, x :> IPersistence
    member x.Update<'dbType> (r:'dbType) = x.Change DbUpdate r
    member x.Insert<'dbType> (r:'dbType) = x.Change DbInsert r
    member x.Delete<'dbType> (r:'dbType) = x.Change DbDelete r