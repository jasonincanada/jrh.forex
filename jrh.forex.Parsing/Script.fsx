// Learn more about F# at http://fsharp.org. See the 'F# Tutorial' project
// for more guidance on F# programming.

#load "Core.fs"
#load "Forex.fs"

open jrh.forex.Parsing.Forex
open jrh.forex.Parsing.Core

// Define your library scripting code here
let line = "|EURJPY M15| EURJPY M7 R H [{09/09/2016 08:00}] [] []"

printfn "%A" (run pChannel line)
