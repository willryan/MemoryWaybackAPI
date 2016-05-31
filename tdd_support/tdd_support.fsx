#I "../tests/MemoryWayback.Tests/bin/Debug"

#r "ExtCore.dll"
#r "FSharp.Quotations.Evaluator.dll"
#r "FsPickler.dll"
#r "FsPickler.Json.dll"
#r "MySql.Data.dll"
#r "Newtonsoft.Json.dll"
#r "ServiceStack.Common.dll"
#r "ServiceStack.Interfaces.dll"
#r "ServiceStack.OrmLite.dll"
#r "ServiceStack.OrmLite.MySql.dll"
#r "ServiceStack.Text.dll"
#r "Suave.dll"
#r "Zlib.Portable.dll"
#r "MemoryWayback.exe"
#r "MemoryWayback.Tests.dll"
#r "xUnit.dll"
#r "FsUnit.Xunit.dll"
#r "FsUnit.CustomMatchers.dll"
#r "NHamcrest.dll"

open System
open System.Reflection

let rec exceptionToHtml (ex:Exception) : string =
  if not (isNull ex.InnerException) then
    exceptionToHtml ex.InnerException
  else
    let lines =
      seq {
        yield sprintf "<div>"
        yield! ex.Message.Split('\n')
          |> Array.map (sprintf "<div>%s</div>")
        yield sprintf "</div>"
        yield sprintf "<div>Stack trace:</div><div style='font-size: 9pt'>"
        yield! ex.StackTrace.Split('\n')
          |> Array.map (sprintf "<div style='margin-left: 20px'>%s</div>")
        yield "</div>"
      }
    String.Join("", lines)

[<StructuredFormatDisplay("{Format}")>]
type TestResult =
  | Pass
  | Fail of Exception
  member x.Format =
    match x with
      | Pass -> "_____PASS_____"
      | Fail ex -> sprintf "!!!!!FAIL!!!!\n%A" ex
  member x.ResultHtml =
    match x with
      | Pass -> "<span class='success'>✔</span>"
      | Fail ex -> "<span class='failure'>✘</span>"
  member x.DetailHtml =
    match x with
      | Pass -> ""
      | Fail ex -> exceptionToHtml ex

let htmlWrapper body =
  let str = """<html>
       <header>
          <style type='text/css'>
            body { font-family: Calibri, Verdana, Arial, sans-serif; background-color: White; color: Black; }
            h2,h3,h4,h5 { margin: 0; padding: 0; }
            h3 { font-weight: normal; }
            h4 { margin: 0.5em 0; }
            h5 { font-weight: normal; font-style: italic; margin-bottom: 0.75em; }
            h6 { font-size: 0.9em; font-weight: bold; margin: 0.5em 0 0 0.75em; padding: 0; }
            pre,table { font-family: Consolas; font-size: 0.8em; margin: 0 0 0 1em; padding: 0; }
            table { padding-bottom: 0.25em; }
            th { padding: 0 0.5em; border-right: 1px solid #bbb; text-align: left; }
            td { padding-left: 0.5em; }
            .divided { border-top: solid 1px #f0f5fa; padding-top: 0.5em; }
            .row, .altrow { padding: 0.1em 0.3em; }
            .row { background-color: #f0f5fa; }
            .altrow { background-color: #e1ebf4; }
            .success, .failure, .skipped { font-family: Arial Unicode MS; font-weight: normal; float: left; width: 1em; display: block; }
            .success { color: #0c0; }
            .failure { color: #c00; }
            .skipped { color: #cc0; }
            .timing { float: right; }
            .indent { margin: 0.25em 0 0.5em 2em; }
            .clickable { cursor: pointer; }
            .testcount { font-size: 85%; }
          </style>
        </header>
        <body>"""

  let str2 = """
        </body>
      </html>
  """
  str + body + str2

[<StructuredFormatDisplay("{Format}")>]
type TestResultInfo =
  {
    Name : string
    Result : TestResult
  }
  member x.Format =
    sprintf "<<<<<TEST>>>> %s\n%s" x.Name (x.Result.Format)
  member x.Html =
    htmlWrapper <| sprintf "<div class='row'>%s<span style='font-weight: bold'>&nbsp;%s</span>%s</div></div>" x.Result.ResultHtml x.Name x.Result.DetailHtml

let printer (h:TestResultInfo[]) : string =
  let strings = h |> Array.map (fun r -> r.Html)
  String.Join("", strings)

#if HAS_FSI_ADDHTMLPRINTER
fsi.AddHtmlPrinter(printer)
#endif

let runTest (fixture: obj) (mi:MethodInfo) : TestResultInfo =
  let getResult () =
    try
      ignore <| mi.Invoke(fixture, [||])
      Pass
    with
      | _ as ex -> Fail ex
  { Name  = mi.Name ; Result = getResult() }

let runTests (fixture: obj) (tests: string list) =
  let typ = fixture.GetType()
  let useMethods =
    match tests with
    | [] -> typ.GetMethods(System.Reflection.BindingFlags.Instance)
    | _ ->
      tests
      |> List.map (typ.GetMethod)
      |> List.toArray
  useMethods
  |> Array.map (runTest fixture)
