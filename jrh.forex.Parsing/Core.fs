namespace jrh.forex.Parsing

module Core =
 
    type Result<'a> = Success of 'a | Failure of string
    type Parser<'T> = Parser of (string -> Result<'T * string>)

    // val run : parser:Parser<'a> -> input:string -> Result<'a * string>
    let run parser input =
        let (Parser innerFn) = parser
        input |> innerFn

    // val pchar : string -> Result<char * string>
    let pchar c =
        let innerFn input =
            if System.String.IsNullOrEmpty(input) then
                Failure "No more input"
            else
                let first = input.[0]

                if first = c then
                    let remaining = input.[1..]
                    Success (first, remaining)
                else
                    Failure "No match"
        Parser innerFn
        
    // val andThen : parser1:Parser<'a> -> parser2:Parser<'b> -> Parser<'a * 'b>
    let andThen parser1 parser2 =
        let innerFn input =
            let result1 = run parser1 input

            match result1 with
            | Success (a, remaining) ->
                let result2 = run parser2 remaining
                match result2 with
                | Success (b, remaining2) -> Success ((a, b), remaining2)
                | Failure error -> Failure error
            | Failure error -> Failure error
        Parser innerFn

    let ( .>>. ) = andThen

    // val andKeepLeft : Parser<'a> -> Parser<'b> -> Parser<'a>
    let andKeepLeft parser1 parser2 =
        let innerFn input =
            let result1 = run parser1 input

            match result1 with
            | Success (a, remaining) ->
                let result2 = run parser2 remaining
                match result2 with
                | Success (b, remaining) -> Success (a, remaining)
                | Failure error -> Failure error
            | Failure error -> Failure error
        Parser innerFn

    let ( .>> ) = andKeepLeft

    // val andKeepRight : Parser<'a> -> Parser<'b> -> Parser<'b>
    let andKeepRight parser1 parser2 =
        let innerFn input =
            let result1 = run parser1 input

            match result1 with
            | Success (a, remaining) ->
                let result2 = run parser2 remaining
                match result2 with
                | Success (b, remaining) -> Success (b, remaining)
                | Failure error -> Failure error
            | Failure error -> Failure error
        Parser innerFn

    let ( >>. ) = andKeepRight

    // val orElse : Parser<'a> -> Parser<'a> -> Parser<'a>
    let orElse parser1 parser2 =
        let innerFn input =
            let result1 = run parser1 input

            match result1 with
            | Success _ -> result1
            | Failure _ -> run parser2 input
        Parser innerFn

    let ( <|> ) = orElse

    // val mapP : f:('a -> 'b) -> parser:Parser<'a> -> Parser<'b>
    let mapP f parser =
        let innerFn input =
            let result = run parser input

            match result with
            | Success (a, b) -> Success (f a, b)
            | Failure err -> Failure err
        Parser innerFn

    let ( <!> ) = mapP
    let ( |>> ) x f = mapP f x

    // val parseZeroOrMore : parser:Parser<'a> -> input:string -> (string list * string)
    let rec parseZeroOrMore parser input =
        let firstResult = run parser input

        match firstResult with
        | Failure _ -> ([], input)
        | Success (firstMatch, remainingAfterFirstParse) ->
            let (secondaries, rem) = 
                parseZeroOrMore parser remainingAfterFirstParse
            let values = firstMatch :: secondaries
            (values, rem)

    // val many : parser: Parser<'a> -> Parser<'a list>
    let many parser =
        let rec innerFn input =
            Success (parseZeroOrMore parser input)

        Parser innerFn

    // val oneOrMore :: parser:Parser<'a> -> Parser<'a list>
    let oneOrMore parser =
        let innerFn input = 
            let result = run parser input

            match result with
            | Success (a, remainingAfterFirst) ->
                let (subsequentValues, remainingInput) = 
                    parseZeroOrMore parser remainingAfterFirst
                let values = a :: subsequentValues
                Success (values, remainingInput)

            | Failure err ->
                Failure err
        Parser innerFn

    let number = 
        let resultToString digitList = 
            System.String(List.toArray digitList)
        
        let digit = [ '0' .. '9' ] |> List.map pchar |> List.reduce (<|>)
        let digits = oneOrMore digit 
        digits 
        |> mapP resultToString

    // val pWithin : opening:char -> closing:char -> Parser<string * string>
    let pWithin opening (closing:char) =
        let innerFn input =
            if System.String.IsNullOrEmpty(input) then
                Failure "No input"
            else
                if input.[0] = opening then
                    let closeIdx = input.[1..].IndexOf(closing)  // todo: reference exception bug
                    if (closeIdx = -1) then
                        Failure "No terminating ]"
                    else
                        let label = input.[1..closeIdx]
                        let remaining = input.[closeIdx+2..]
                        Success (label, remaining)
                else
                    Failure "No match"
        Parser innerFn
