namespace jrh.forex.Parsing

module Forex =
    open Core

    type StructurePoint = { 
        Label: string; 
        Symbol: string; 
        Date: string; 
        Price: string
    }

    type Channel = {
        Label: string; 
        Symbol: string; 
        Timeframe: string; 
        Start: string; 
        Cast: string; 
        Support: string; 
        LeftRight: string; 
        OHLC: string;
     }

    let space = pchar ' '
    let slash = pchar '/'
    let colon = pchar ':'

    let anyDigit = ['0' .. '9'] |> List.map pchar |> List.reduce (<|>)

    // For date/time matching
    let pTwoDigits = (anyDigit .>>. anyDigit) |>> fun (a, b) -> string a + string b
    let pFourDigits = pTwoDigits .>>. pTwoDigits |>> fun (a, b) -> a + b

    let pSPLabel = pWithin '[' ']'
    let pChannelLabel = pWithin '|' '|'    
    let priceDigits = "-0123456789.".ToCharArray() |> Array.map pchar |> Array.reduce (<|>)

    let pPrice = 
        (oneOrMore anyDigit) .>> pchar '.' .>>. (oneOrMore anyDigit)
        |>> fun (left, right) -> System.String(List.toArray(left)) + "." + System.String(List.toArray(right))

    let pDate =
        (pTwoDigits .>>. slash .>>. pTwoDigits .>>. slash .>>. pFourDigits)
        |>> fun ((((month, _), day), _), year) -> month + "/" + day + "/" + year

    let pTime =
        (pTwoDigits .>> colon .>>. pTwoDigits)
        |>> fun (hour, min) -> hour + ":" + min

    let pDateTime = 
        (pDate .>> space .>>. pTime)
        |>> fun (date, time) -> date + " " + time

    let symbols = [| 
        "AUDUSD"; "AUDJPY"; "AUDNZD"; 
        "EURUSD"; "EURJPY"; "EURGBP"; "EURAUD"; "EURNZD";
        "GBPUSD"; "GBPJPY"; "GBPAUD"; "GBPCHF"
        "USDJPY"; "USDCAD"; "USDCHF";
        "CHFJPY";
        "NZDUSD";
    |]

    // val pSymbol : -> Parser<string>
    let pSymbol =
        let innerFn (input:string) =
            if System.String.IsNullOrEmpty(input) then Failure "No input"
            else
                let known x = Seq.exists((=) x) symbols
                let firstSix = input.Substring(0, 6)

                if known firstSix then 
                    Success (firstSix, input.[6..])
                else
                    Failure "Unknown symbol"                             
        Parser innerFn

    let pTimeframe =
        let innerFn (input:string) =
            if System.String.IsNullOrEmpty(input) then Failure "No input"
            else               
                let left2 = input.Substring(0, 2)
                match left2 with
                | "M1" -> Success (left2, input.[2..])
                | "M5" -> Success (left2, input.[2..])
                | "M7" -> Success ("M15", input.[2..])
                | "H1" -> Success (left2, input.[2..])
                | "H4" -> Success (left2, input.[2..])
                | "D1" -> Success (left2, input.[2..])
                | _ -> Failure (sprintf "Not a timeframe: %s" input)
        Parser innerFn
                   
    // val pStructurePoint : Parser<StructurePoint>
    let pStructurePoint = 
        (pSPLabel .>> space .>>. pSymbol .>> space .>>. pDateTime .>> space .>>. pPrice)
        |>> fun (((label, s), date), price) -> {Label=label; Symbol=s; Date=date; Price=price}

    let pLeftOrRight = (pchar 'L') <|> (pchar 'R') |> mapP (fun (x) -> x.ToString())
    let pOHLC = ((pchar 'O') <|> (pchar 'H') <|> (pchar 'L') <|> (pchar 'C')) |> mapP (fun (x) -> x.ToString())

    // val pChannel : Parser<Channel>
    let pChannel =
        (pChannelLabel .>> space .>>. pSymbol .>> space .>>. pTimeframe .>> space .>>. pLeftOrRight .>> space .>>. pOHLC .>> space .>>.   pSPLabel .>> space .>>. pSPLabel .>> space .>>. pSPLabel)
        |>> fun (((((((label, symbol), timeframe), leftRight), ohlc), start), cast), support) -> { Label = label; Symbol = symbol; Timeframe = timeframe; Start = start; Cast = cast; Support = support; LeftRight = leftRight; OHLC = ohlc; }

    
    (* Methods for use in C# code *)

    // val IsChannel : string -> bool
    let IsChannel line = 
        let result = run pChannel line
        match result with
        | Success _ -> true
        | Failure _ -> false
        
    // val IsStructurePoint : string -> bool
    let IsStructurePoint line =
        let result = run pStructurePoint line
        match result with
        | Success _ -> true
        | Failure _ -> false

    // val ParseChannel : string -> Channel
    let ParseChannel input =
        let result = run pChannel input
        match result with
        | Success (a, _) -> a
        | Failure err -> { Label = ""; Symbol = ""; Timeframe = ""; Start = ""; Cast = ""; Support = ""; LeftRight = ""; OHLC = ""; }
        
    let ParseStructurePoint input =
        let result = run pStructurePoint input
        match result with
        | Success (a, _) -> a
        | Failure err -> { Label = ""; Symbol = ""; Date = ""; Price = "" }