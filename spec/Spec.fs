open canopy
open runner
open System
open OpenQA.Selenium

canopy.configuration.phantomJSDir <- @"../webdrivers"
canopy.configuration.chromeDir <- @"../webdrivers"
start chrome

"taking canopy for a spin" &&& (fun _ ->
    //this is an F# function body, it's whitespace enforced

    //go to url
    url "http://localhost:8083/hello"

    //assert that the element with an id of 'welcome' has
    //the text 'Welcome'
    "#welcome" == "Welcome"
)

//run all tests
run()

printfn "press [enter] to exit"
System.Console.ReadLine() |> ignore

quit()
