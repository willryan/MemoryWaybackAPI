module MemoryWayback.Persistence

open System
open System.Linq
open System.Linq.Expressions
open Microsoft.FSharp.Linq.RuntimeHelpers
open Microsoft.FSharp.Quotations

type IPersistence =
  abstract Select<'dbType> : Expr<'dbType -> bool> -> 'dbType list * IPersistence
  abstract Update : 'dbType -> 'dbType * IPersistence
  abstract Insert : 'dbType -> 'dbType * IPersistence
  abstract Delete : 'dbType -> bool * IPersistence
