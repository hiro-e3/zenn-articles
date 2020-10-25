// [1..10] |> List.sumBy(fun x -> x * x)
// |> stdout.WriteLine

// let f x = x * x
// stdout.WriteLine (f 10) 
// 10 |> f |> stdout.WriteLine

// ("{0}, {1}","Hello", "World") |> stdout.WriteLine
// printfn "%d %d %d" 123 45 678
// printfn "%s, %s" "Hello" "World"
// printfn "%A" [1..10]

// let a,b = 10,20

// if a * b % 2 = 0 then printfn "%s" "Even" 
// else printfn "%s" "Odd"

// let mutable count = 0
// let input = stdin.ReadLine() |> int
// if input % 2 = 0 then 
//     count <- count + 1
//     ()

// stdin.ReadLine() |> Seq.fold(fun count c -> if c = '1' then count + 1 else count) 0
// |> stdout.WriteLine

// let rec fold (folder : 'a -> 'b -> 'b) (state : 'b) (source : 'a[]) =
//     if source.Length < 1 then state
//     else fold folder (folder source.[0] state ) source.[1..]

// [|1;2;3|] |> fold(+) 0 |> stdout.WriteLine

let n = stdin.ReadLine() |> int
let a = stdin.ReadLine().Split() |> Array.map int
// let b = Array.fold(fun x y -> y ||| x) 0 a

// let solve x =
//     let rec f x ans =
//         if x &&& 1 <> 0 then ans
//         else f (x >>> 1) (ans + 1)
//     f x 0

// solve b
// |> stdout.WriteLine
let rec solve array ans = 
    if array |> Array.forall(fun x -> x % 2 = 0) |> not then ans
    else solve (array |> Array.map(fun x -> x / 2))  (ans + 1)

solve a 0
|> stdout.WriteLine