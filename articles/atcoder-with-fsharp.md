---
title: "F#でAtCoderをやる"
emoji: "🎼"
type: "tech" # tech: 技術記事 / idea: アイデア
topics: ["FSharp","AtCoder"]
published: true
---

# 環境構築
もちろん、F#もAtCoderのコードテストから実行可能ですが、Visual Studio Codeの拡張機能であるIonide-fsharpが極めて強力ですので、利用した方が断然良いでしょう。Visual StudioもIntelliSenceが使えますが流石に重いのでVS Codeを個人的にはオススメしたいです。
[.NET Core](https://dotnet.microsoft.com/download)
[Visual Studio Code](https://code.visualstudio.com/)
[Ionide-fsharp](https://marketplace.visualstudio.com/items?itemName=Ionide.Ionide-fsharp)

# 標準入出力
やはり、まずAtCoderの問題に取り組む上で標準入力からの入力を受け取ったり、標準出力から答えを出力することは大切ですので、紹介したいと思います。

## 標準入力

```fsharp
// 整数Nが与えられる場合
let n = stdin.ReadLine() |> int 
// 整数A,B,Cが半角スペース区切りで与えられる場合(警告が出ます)
let [|a;b;c|] = stdin.ReadLine().Split() |> Array.map int 
// N個の整数が半角スペース区切りで与えられる場合
let d = stdin.ReadLine().Split() |> Array.map int
// N個の整数が改行されて与えられる場合
let e = [| for i=0 to N-1 do stdin.ReadLine() |> int |]
```
`let`は値や関数を識別子に束縛します。端的に言えば変数の定義ですが、F#の変数は基本Immutable(不変)です。
`stdin`は[System.Console.Inプロパティ](https://docs.microsoft.com/ja-jp/dotnet/api/system.console.in?view=netcore-3.1)をラップしているだけですが、`open System`を記述する必要がないのが楽です。.NETのライブラリを直接参照する機会はあまりありません。

先程から度々登場している`|>`はパイプライン演算子と呼ばれるものでbashに登場する`|`のように、関数の返り値や値を、他の関数へ送ることができます。

### パイプライン演算子
一切、パイプライン演算子を使わない表記だとこうなるのも
```fsharp 
let f x = x * x
stdout.WriteLine (f 10) 
```

次のように書けます。

```fsharp
let f x = x * x
10 |> f |> stdout.WriteLine
```

かなり直観的に使える演算子ですが、場合によっては可読性がかえって落ちることもあるのでケースバイケースで使っていきましょう。

```fsharp
let f x = stdin.ReadLine() |> int |> (+) <| (stdin.ReadLine() |> int)
        |> (/) <| ((pown 10 9) + 7)
```

## 標準出力
標準出力に関しては.NETライブラリの`System.IO.TextWriter.WriteLine`メソッドを使うのと、F#のライブラリにある`printfn`関数を使う二通りの手段が主に考えられます。

### WriteLine
こちらはC#と同様に.NETのライブラリを使用していく場合です。その場合でも、[Console.Outプロパティ](https://docs.microsoft.com/ja-jp/dotnet/api/system.console.out?view=netcore-3.1)をラップした`stdout`オペレータがあるので`open System`は不要です

```fsharp
10 |> stdout.WriteLine
10.123 |> stdout.WriteLine

stdout.WriteLine("{0}, {1}","Hello", "World") // Hello, World
```

### printfn
こちらはConsole.OutをF#での型推論や厳密な型指定がはたらくようにラップした関数です。C言語の`printf`のような形で扱うことができます。

```fsharp
printfn "%d %d %f" 123 45 678.9 // 123 45 678.9
printfn "%s, %s" "Hello" "World" // Hello, World
printfn "%A" [1..10] // [1; 2; 3; 4; 5; 6; 7; 8; 9; 10]
```

リストや配列の要素についても限りはありますが表示することが可能です。こちらに関しては`stdout.WriteLine`ではできません。

## 複数行にまたがる出力に関する注意点

```fsharp
[1..100000] |> List.iter(fun x -> printfn "%d" x)
```

これはC#でも同様の問題がありますが、`printfn`関数や`WriteLine`メソッドを複数回呼び出す場合、呼び出す都度標準出力に出力する(AutoFlushがtrue)ので非常に遅くなります。
その為、文字列の連結や`System.Text.StringBuilder`を利用して関数orメソッドを一度呼び出すだけで答えの出力を完了させるようにしたり、標準出力を行う独自のクラスや関数などを用意してあげる必要があります。

```fsharp
let sb = System.Text.StringBuilder()

for i=0 to 100 do
    i |> sb.AppendLine

sb.ToString() |> stdout.Write
```

# [AtCoder Beginners Selection](https://atcoder.jp/contests/abs)に挑む
## [PracticeA - Welcome to AtCoder](https://atcoder.jp/contests/abs/tasks/practice_1)
$a,b,c$は`int`の範囲内に収まるので、入力は`int`にパースして問題ありません。
ただし、$a$と$b,c$は改行が挟まるのと、$b,c$の間に半角スペースがあることについて留意する必要があります。
$s$は文字列なので特に何かをする必要はありません。

```fsharp
let a = stdin.ReadLine() |> int
let b,c = stdin.ReadLine().Split() |> Array.map int |> fun i -> i.[0], i.[1]
// let [|b;c|] = stdin.ReadLine().Split() |> Array.map int
let s = stdin.ReadLine()

printfn "%d %s" (a + b + c) s
```

## [ABC086A - Product](https://atcoder.jp/contests/abs/tasks/abc086_a)
入力の$a,b$間には空白が挟まるのでここでも`Split`メソッドを利用する必要があります。
偶奇の判別には$a$と$b$の積について、2で割った余りが0かどうか、つまり $a * b mod 2$が0かどうか判定し、その結果に応じて`Even`か`Odd`を出力すればよいです。

```fsharp
#nowarn "0025"
let [|a;b|] = stdin.ReadLine().Split() |> Array.map int

if a * b % 2 = 0 then "Even" else "Odd"
|> stdout.WriteLine
```

`#nowarn "0025"`はプリプロセッサディレクティブと呼ばれるもので、この場合は0025のコードを持つ警告を表示しないようにします。`let [|a;b|] = ...`の記述はパターンマッチングの機能を利用しているのですが、パターンを網羅していない為に警告が出てしまうのが煩わしいので、これを利用して表示しないようにすることができます。もちろん、通常は利用しない方が良いでしょう。

**F#のif式は値を返すことができます**。この場合、`if`以下の条件式が`true`であれば`"Even"`の文字列を、`false`であれば`"Odd"`の文字列を返します。
いわゆる三項条件演算子`a * b % 2 == 0 ? "Even" : "Odd"`に近いです。
ちなみに、文字列を返さずそのまま標準出力へ文字列を表示したい場合は以下のような記述になります。

```fsharp
#nowarn "0025"
let [|a;b|] = stdin.ReadLine().Split() |> Array.map int

if a * b % 2 = 0 then printfn "%s" "Even" 
else printfn "%s" "Odd"
```

ちなみに、`printfn`が返すのはunit型という`void`に相当する型になります。
unit型を返す場合、`()`という記述を用います。
また、F#では左辺と右辺が等しいかを判別したいときに`==`や`===`ではなく`=`を使用できます。

```fsharp
let mutable count = 0
let input = stdin.ReadLine() |> int

if input % 2 = 0 then 
    count <- count + 1
    ()
```

これは入力された数値が偶数なら`count`を1加算するというコードです。
`else`はunit型を返す場合のみ省略できます。

```csharp
using System;

var count = 0;
var input = int.Parse(Console.ReadLine());
if(input % 2 == 0)
{
    count++;
}
```

C#で書くとこのような形のコードです(C#9.0の機能を使っているのでAtCoderで上記のコードは動きません)。

## [ABC081A - Placing Marbles](https://atcoder.jp/contests/abs/tasks/abc081_a)
ビー玉が置かれるマス目は`'1'`の文字が書かれたマスなので、ビー玉の置かれるマスの個数を知るためには与えられた入力文字列の中から`'1'`の文字をカウントすれば良さそうです。

```fsharp
stdin.ReadLine() |> Seq.fold(fun count c -> if c = '1' then count + 1 else count) 0
|> stdout.WriteLine
```

F#において、文字列はchar型のシーケンスとして扱うことができます。シーケンスは、C#で言うところのIEnumerable型に相当します。

`fold`関数はシーケンス、リスト、配列といったF#のコレクション型いずれにも存在する関数で、コレクションの各要素に対して与えられた関数を適用していき、最終的には単一の値を返します。
例えば、`List.fold f state [i1;i2;i3]` は `(f (f (f state i1) i2) i3)` というような計算になります。

```fsharp
let mutable count = 0

for c in stdin.ReadLine() do
    if c = '1' then count <- count + 1

count |> stdout.WriteLine
```

for文とmutableなカウンタ変数を用いた場合は上記のような書き方になります。

## [ABC081B - Shift only](https://atcoder.jp/contests/abs/tasks/abc081_b)
問題文に記載のある通り、$A_1,...,A_N$に対して、全てが偶数である(2で割り切れる)時、全ての要素を2で割る操作を何回できるかカウントしていきます。

```fsharp
let n = stdin.ReadLine() |> int
let a = stdin.ReadLine().Split() |> Array.map int

let rec solve array ans = 
    if array |> Array.forall(fun x -> x % 2 = 0) |> not then ans
    else solve (array |> Array.map(fun x -> x / 2))  (ans + 1)

solve a 0
|> stdout.WriteLine
```

`forall`関数は引数に与えた関数をコレクションのすべての要素に適用し、いずれも`true`の結果であれば`true`を、`false`が含まれれば`false`を返します。
`not`は文字通り否定で、`true`であれば`false`を、`false`であれば`true`を返します。C#などのCライクな言語の`!`に相当します。
`map`関数は与えられたコレクションのすべての要素に、与えられた関数を適用したコレクションを返す関数です。

F#では再帰関数である場合、必ず`rec`(recursive)キーワードを付けて関数が自身を呼び出す再帰関数であることを明示しなければいけません。

### 別解
参考：[AtCoderの「ABC081B - Shift only」をShift Onlyで解こうとしたらビット演算で計算量を削減していた話](https://qiita.com/malbare932/items/cb942a12d175157134de#2-%E3%83%93%E3%83%83%E3%83%88%E6%BC%94%E7%AE%97%E3%81%A8%E3%82%B7%E3%83%95%E3%83%88%E6%BC%94%E7%AE%97%E3%81%A7%E8%80%83%E3%81%88%E3%82%8B%E8%A7%A3%E6%B3%95)

他により良い(愚直にやらないで計算量を減らせる)方法があるだろうと思い、記事を書くにあたって探していたらビット演算を利用した解き方をしていた方がいました。解法の詳細については元の記事を参照いただくとして、F#のコードだけ提示します。

```fsharp
let n = stdin.ReadLine() |> int
let a = stdin.ReadLine().Split() |> Array.map int
let b = Array.fold(fun x y -> y ||| x) 0 a
 
let solve x =
    let rec f x ans =
        if x &&& 1 <> 0 then ans
        else f (x >>> 1) (ans + 1)
    f x 0
 
solve b
|> stdout.WriteLine
```

F#のビット演算子は他の言語と違い、記号を三度繰り返すものが割り当てられています。
`&&&`はANDを、`|||`はORを、`>>>`は右シフトを表します。

## [ABC087B - Coins](https://atcoder.jp/contests/abs/tasks/abc087_b)
これも問題文の通りに三重のループを駆使して全ての組み合わせを愚直に試します。
最大でも$50^3 = 125000$回なので(あってる？)余裕をもって計算可能です。

```fsharp
let A = stdin.ReadLine() |> int
let B = stdin.ReadLine() |> int
let C = stdin.ReadLine() |> int
let X = stdin.ReadLine() |> int
 
[0..A] |> List.sumBy (fun a -> 
    [0..B] |> List.sumBy (fun b -> 
        [0..C] |> List.filter (fun c -> 
            (500*a+100*b+50*c)=X
            ) 
        |> List.length)
) 
|> stdout.WriteLine
```

`sumBy`関数は`map`関数と、要素を全て足し合わせた合計値を返す`sum`関数を組み合わせたような関数です。
例えば、1から10の数を二乗した和を求めたい場合、`[1..10] |> List.sumBy(fun x -> x * x)`で求めることが可能です。
`filter`関数は`bool`を返す関数をそれぞれの要素に対して適用し、`false`を返す要素を除いたコレクションを返します。
`length`関数はコレクションの要素数を返します。

0からCまでのリストから$500*A + 100*B + 50*C = X$の条件を満たす要素だけを抽出し、その要素数を足し合わせています。やっていることは以下とほぼ同じ。

```fsharp
let A = stdin.ReadLine() |> int
let B = stdin.ReadLine() |> int
let C = stdin.ReadLine() |> int
let X = stdin.ReadLine() |> int

let mutable ans = 0

for a=0 to A do
    for b=0 to B do
        for c=0 to C do
            if 500*a + 100*b + 50*c = X then ans <- ans + 1

ans |> stdout.WriteLine
```

## [ABC083B - Some Sums](https://atcoder.jp/contests/abs/tasks/abc083_b)
この問題において悩ましいのが「各桁の和」を求めるというところですが、ある10進数の1桁目を取り出したい場合、10で割った余りを求めれば良いです。もしくは一度stringに変換し、char型の配列として各桁を見ていく方法もあります。

```fsharp
let [|N;A;B|] = stdin.ReadLine().Split() |> Array.map int

let sumDigit number =
    let rec inner num result =
        match num with
        | 0 -> result
        | _ -> inner (num / 10) (result + num % 10) 
    inner number 0

[1..N] |> List.filter(fun a -> 
    let sumDigitResult = sumDigit a
    B >= sumDigitResult && sumDigitResult >= A )
    |> List.sum |> stdout.WriteLine
```

`match ... with`はいわゆるパターンマッチングというもので、`num`が`0`であれば`result`を返し、それ以外なら再び`inner`関数を呼び出すというものです。
今回はパターンの数が少ない為、あまりそうする意義はありませんが条件が増えて`if..then..elif..then....`と`elif`がネストしていくような状況だと有用になります。
また、コンパイラがパターンを網羅出来ているかどうかチェックしてくれます。

## [ABC088B - Card Game for Two](https://atcoder.jp/contests/abs/tasks/abc088_b)

得点が最大化される時、AliceとBobは$a_1,a_2,a_3,...,a_N$の中から得点が大きい順にカードを取っていくことになるので、与えられた得点の配列を降順にソートし、偶数番目の要素の和をAliceの得点として、奇数番目の要素の和をBobの得点とすると良いです。AliceはBobより何点多く取るかというのは差を求めろということなので、それぞれの得点の和の差を求めます。

```fsharp
let N = stdin.ReadLine() |> int
stdin.ReadLine().Split() 
    |> Array.map int 
    |> Array.sortDescending 
    |> Array.mapi(fun index elm ->
        if index % 2 = 0 then elm else -elm) 
    |> Array.sum |> stdout.WriteLine
```

`mapi`関数は与えられたコレクションにインデックスを付与し、与えられた関数を適用するものです。単純にインデックスを付与するだけなら`indexed`関数というのもあります。
`sortDescending`関数はコレクションを降順にソートします。

## [ABC085B - Kagami Mochi](https://atcoder.jp/contests/abs/tasks/abc085_b)

入力の中に異なる$d_i$がいくつあるかを数えればOKです。

```fsharp
[for _ in 0..(stdin.ReadLine() |> int)-1 -> stdin.ReadLine() |> int] 
|> List.distinct 
|> List.length 
|> stdout.WriteLine
```

`distinct`はコレクションの中から重複する要素を削除して一意にする関数です。F#のコレクションの中にはバイナリツリーに基づく一意な要素を持つ`Set`というものがあり、これを利用して以下のようにも記述できます。

```fsharp
[for _ in 0..(stdin.ReadLine() |> int)-1 do stdin.ReadLine()] 
|> Set |> Set.count |> stdout.WriteLine
```

`_`は変数を使用しない場合、破棄する名目で使うことができます。

## [ABC085C - Otoshidama](https://atcoder.jp/contests/abs/tasks/abc085_c)
10000円札の数を$A$、5000円札の数を$B$、1000円札の数を$C$とすると以下の式を満たす$A,B,C$が出力する答えとなります。
$10000A + 5000B + 1000C = Y$
また、お札の総和が$N$であることから、
$A + B + C = N$
となります。

愚直に3重ループを回そうとするとTLEになるので、二つ目の式を利用して$A,B,C$のいずれか一つは固定し、2重ループを回すことになります。

```fsharp
let n,y = stdin.ReadLine().Split() |> Array.map int |> fun a -> a.[0], a.[1]

let solve n y =
    let rec inner a b =
        match 9*a+4*b+n=y/1000, a>n, b+a>n with
        |_,true,_ -> sprintf "-1 -1 -1"
        |_,_,true -> inner (a+1) 0
        |true,_,_ -> sprintf "%d %d %d" a b (n-a-b)
        |_ -> inner a (b+1)
    inner 0 0

solve n y |> stdout.WriteLine    
```

ちなみに、F#にはfor文を脱出するための`break`がありません。その為、再帰関数でそれを実現することとなります。`while...do`構文もあるのでそちらを使うのも可。

## [ABC049C - 白昼夢](https://atcoder.jp/contests/abs/tasks/arc065_a)
与えられた文字列の先頭および末尾が`dream,dreamer,erase,eraser`のいずれかで、さらにそれを1回以上繰り返す文字列かどうかを確かめれば良いので、正規表現を使って以下のように書けます。
```fsharp
open System.Text.RegularExpressions
 
if "^(dream|dreamer|erase|eraser)+$" |> Regex 
|> (fun reg -> stdin.ReadLine() |> reg.IsMatch) then "YES" else "NO"
|> stdout.WriteLine
```

## [ABC086C - Traveling](https://atcoder.jp/contests/abs/tasks/arc089_a)
AtCoDeerくんが移動した量と経過した時刻の偶奇は常に一致するので、和・差は常に偶数となることと、移動量は常に時刻の増加量を超えないことを利用します。

```fsharp
let n = stdin.ReadLine() |> int
let rec canArrive t x y i =
    if i=n then "Yes"
    else
        let [|T;X;Y|] = stdin.ReadLine().Split() |> Array.map int
        let z =  abs (X - x)  + abs (Y - y) - T + t
        if z <= 0 && z % 2 = 0 then canArrive T X Y (i+1)
        else "No"
 
canArrive 0 0 0 0 |> stdout.WriteLine 
```
入力された$t_i, x_i, y_i$について、前述の条件を全ての入力において満たせば`"Yes"`、さもなくば`"No"`です。
ちなみに、`abs`は文字通り絶対値を返す演算子です。

# おわりに
まだまだ灰色のよわよわF#erですがF#の紹介も兼ねて書いてみました。
F#でやっている方は数えるほどしかいなさそうですが、F#自体競技プログラミングの文脈外でもコンピュテーション式や型プロバイダを始めとした面白い機能があるので、もうちょっと広まってほしいという思いがあります。
コンピュテーション式を用いると大きい数の際に$10^9 + 7$の余りを使う時の処理を上手く書けそうな気がしますので、試してみて書けたらと思います。
