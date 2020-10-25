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

[<EntryPoint>]
let main argv =
    let mutable a,b,c = 0,0.0,""
    cin >> &a >> &b
    printfn "%d %f %s" a b c
    cin >> &c
    printfn "%s" c
    0