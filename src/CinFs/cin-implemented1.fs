namespace Program
    module CinQ = 
        open System
        open System.Collections.Generic
        open System.ComponentModel

        type CIn = 
            | ReadStream of readStream : (unit -> Queue<string>)
            | Tokens of queue : Queue<string>

        let cin = 
            ReadStream (fun () -> new Queue<string>(stdin.ReadLine().Split(' ')))

        let inline (>>) (cin : CIn) (x : byref<'U>) =
            let q = match cin with
                    | ReadStream f -> f ()
                    | Tokens t -> t
            let converter = TypeDescriptor.GetConverter(typeof<'U>)
            x <- converter.ConvertFromString(q.Dequeue()) :?> 'U
            Tokens q

    module IOStream =
        open System
        open System.IO
        open System.Text

        type CIn () =
            [<Literal>]
            let BUFFER_SIZE = 1024
            [<Literal>]
            let CHAR_ZERO = 48uy
            [<Literal>]
            let CHAR_NINE = 57uy 
            [<Literal>]
            let CHAR_MINUS = 45uy
            [<Literal>]
            let ASCII_CHAR_BEGIN = 33uy
            [<Literal>]
            let ASCII_CHAR_END = 126uy

            let mutable index = 0
            let mutable isEof = false
            let mutable bufferLength = 0
            let buf : byte[] = Array.zeroCreate BUFFER_SIZE
            member private __.Stream = Console.OpenStandardInput()

            member private __.ReadStream() =
                bufferLength <- __.Stream.Read(buf, 0, BUFFER_SIZE)
                
            member private __.ReadByte() =
                if index >= bufferLength then
                    index <- 0
                    __.ReadStream()
                let b = buf.[index]
                index <- index + 1
                b

            member __.Long() =
                let isNg = 
                    let rec getSign () =
                        match __.ReadByte() with
                        | CHAR_MINUS -> true
                        | b when CHAR_ZERO <= b && b <= CHAR_NINE -> 
                            index <- index - 1
                            false
                        | _ -> getSign ()
                    getSign ()

                let rec read ret =
                    match __.ReadByte() with
                    | b when b < CHAR_ZERO || CHAR_NINE < b -> 
                        if isNg then -ret else ret
                    | b ->
                        read (ret * 10L + ((b - CHAR_ZERO) |> int64))
                read 0L

            member __.Next() =
                let rec next (sb : StringBuilder) =
                    match __.ReadByte() with
                    | b when ASCII_CHAR_BEGIN <= b && b <= ASCII_CHAR_END -> 
                        b |> char |> sb.Append |> ignore
                        next sb
                    | _ ->
                        index <- index - 1  
                        sb.ToString()
                StringBuilder() |> next

            member inline __.Read () : 'T =
                match typeof<'T> with
                | ty when ty.Equals(typeof<int64>) -> __.Long() |> box
                | ty when ty.Equals(typeof<int32>) -> __.Long() |> int32 |> box
                | ty when ty.Equals(typeof<bigint>) -> __.Long() |> bigint |> box
                | ty when ty.Equals(typeof<float>) -> __.Next() |> float |> box
                | ty when ty.Equals(typeof<double>) -> __.Next() |> float |> box
                | ty when ty.Equals(typeof<string>) -> __.Next() |> box
                | _ -> __.Next() |> box
               :?> 'T

            interface IDisposable with
                member this.Dispose() =
                    this.Stream.Dispose()
        
        type COut () =
            do new StreamWriter(Console.OpenStandardOutput(), AutoFlush=false) |> Console.SetOut
            let sb = StringBuilder()

            member __.Write(str : obj) =
                str |> string |> sb.Append |> ignore
            
            interface IDisposable with
                member __.Dispose() =
                    sb.ToString() |> Console.Write
                    Console.Out.Flush()
                    
    module Main =  
        open System
        open System.Text
        open System.Collections.Generic
        open System.ComponentModel
        open IOStream
        
        let inline (>>) (cin : CIn) (x : outref<'U>) =
            x <- cin.Read()
            cin

        let inline (<<) (cout : COut) (x : obj)  =
            cout.Write(x)
            cout

        let endl = "\n"

        [<EntryPoint>]
        let main argv =
            let cin = new CIn()
            use cout = new COut()
            let mutable a,b,c,s = 0,0,0,""
            cin >> &a >> &b >> &c >> &s |> ignore
            cout << (a+b+c) << " " << s << endl |> ignore
            0