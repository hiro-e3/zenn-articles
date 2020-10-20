---
title: "F#をはじめよう"
emoji: "ℱ"
type: "tech" # tech: 技術記事 / idea: アイデア
topics: ["FSharp","dotnet"]
published: false
---

F#は Don Syme氏とMicrosoft Researchが開発したOCamlがベースとなっている .NET の関数型がメインのマルチパラダイム言語です。オープンソースで、クロスプラットフォームで……といった詳しい説明は[Microsoft Docs](https://docs.microsoft.com/ja-jp/dotnet/fsharp/what-is-fsharp)や[F# Software Foundation](https://fsharp.org/)、Wikipedia等に譲ります。

# F#をはじめる
.NET Core をインストールしましょう。
https://dotnet.microsoft.com/download

エディタは[Visual Studio Code](https://code.visualstudio.com/)と拡張機能の[Ionide](http://ionide.io/)を使うのが良いでしょう。
使い慣れたエディタがあればそれでも構いませんし、Visual Studioを使用することももちろん可能です。

ちなみに本稿での.NETのバージョンは .NET 5.0.100-rc.1.20452.10 です。.NET Core 3.1でも同様の内容で可能なはずです。

# Hello, World from F#!
## F# Interactiveで対話的に実行する
`dotnet fsi`のコマンドを実行することで、対話的にF#のコードを実行することができる[F# Interactive](https://docs.microsoft.com/ja-jp/dotnet/fsharp/tutorials/fsharp-interactive/)が起動します。

ちょっと何か試したいという時に気軽に起動して試せますし、F#は.NETのライブラリを使用することができる為、ちょっと.NETのAPIについて動作を試したいけどC#でも書くのもなぁ……なんて時にも使えます。

また、F#スクリプト(`.fsx`)のファイルを読み込んで実行することも可能です。

``` powershell
> dotnet fsi

Microsoft (R) F# インタラクティブ バージョン 11.0.0.0 for F# 5.0
Copyright (C) Microsoft Corporation. All rights reserved.

ヘルプを表示するには次を入力してください: #help;;
```

セミコロン2つで区切ることでそれまで入力したコードがコンパイル、実行されます。

``` fsharp
> printfn "Hello, World!";;
Hello, World!
val it : unit = ()
```
`unit`はC#などで言うところの`void`に相当するものです。値が存在しないことを示す型(Type)で、`()`で表されます。`it`はインタラクティブでの動作時に、直前に実行された関数が他の関数に束縛されていない場合に表示される関数名です。

``` fsharp
> let add x y = x + y;;
val add : x:int -> y:int -> int
```
例えば、これは`x`と`y`というint型の2つの引数を取り、その和を返す`add`という名前の関数です。

``` fsharp
> let from whom =
-     sprintf "from %s" whom
- let message = from "F#"
- printfn "Hello world %s" message;;
Hello world from F#
val from : whom:string -> string
val message : string = "from F#"
val it : unit = ()
```
このように複数行にまたがったコードも記述することができます。

インタラクティブモードは`#quit;;`で終了することができます。
## ソースコードをビルドして実行する
任意のディレクトリで`dotnet new console -n HelloFsharp -lang F#`のコマンドを実行することで `HelloFsharp` という名称のコンソールアプリケーションプロジェクトを作成されます。.NET Coreのデフォルト言語はC#なので、`-lang F#`のオプションで明示的にF#を指定する必要があります。C#のプロジェクトを作ったとしても`.csproj`を`.fsproj`に書き換えて、コンパイル対象に関する追記をしてあげることでF#でも使うことが可能です。

``` fsharp
// Learn more about F# at http://docs.microsoft.com/dotnet/fsharp

open System

// Define a function to construct a message to print
let from whom =
    sprintf "from %s" whom

[<EntryPoint>]
let main argv =
    let message = from "F#" // Call the function
    printfn "Hello world %s" message
    0 // return an integer exit code

```
プロジェクト作成時に既に`Program.fs`にHello, Worldを表示するコードがあります(※上記は .NET 5.0 で作成されるコード)。 書かれている内容について簡単に説明します。

まず`//`はコード上に記述するコメントです。これについてはあまり説明の必要はないかと思います。

次に`open`はC#で言うところの`using`句に相当し、他のモジュール(module)や名前空間(namespace)に定義されたリソースを使用する際に記述します。[System名前空間](https://docs.microsoft.com/ja-jp/dotnet/api/system?view=netcore-3.1)は .NETの基底クラスが含まれている名前空間ですが、実は上記のコードではF#のライブラリ(`FSharp.Core`)のみを使っているので必要ありません。

`let`は先程もチラッと登場しましたが、値や関数を定義するのに用いるキーワードで、F#ではメインとなるキーワードです。後ほど詳しく書きたいと思います。

`from`関数では`whom`という文字列(`string`)型の引数を受け取り、`sprintf`という関数を用いて新たな文字列を形成し、その戻り値を返しています。F#では`return`のキーワードは特定のケースを除いて存在せず、関数の最後に記述された式がそのまま戻り値となります。

`[<EntryPoint>]`はプログラムの開始地点を表す属性(Attribute)です。属性についてはC#の属性に関するドキュメントを参照していただきたいと思います。この属性を付けることでプログラムの実行時に始めに呼び出される、エントリーポイントとなる関数に指定できます。いくつかのプログラミング言語に存在するmain関数(メソッド)に相当するものですが、名称がmainである必要はありません。ただし必ず`string[]`型の引数を取って、`int`型の値を返す必要があります。

`printfn`は`sprintf`に似ていますが、文字列を形成して返すのではなく、形成した文字列の末尾に改行を挿入して標準出力へ表示します。どちらも似たような関数をC言語で見たことがあるかもしれません。

先程作成されたディレクトリへ移動(`cd HelloFsharp`)し、 `dotnet run`のコマンドを実行することで、プロジェクトをビルドして実行することができます。
``` 
> dotnet run
Hello World from F#!
```

### .NETのメソッドを使ったHello, World
``` fsharp
open System

let from (whom : string) =
    String.Format("from {0}", whom)

Console.WriteLine("Hello world from {0}", from "F#")
```
このように .NETのライブラリで定義されたメソッドやクラスも使用することができます。

# let束縛(let bindings)で値や関数を定義する
``` fsharp
open System
// 値の定義
let helloWorld = "Hello, World!"
let integer = 100
let pi = 3.14
let date = DateTime.Now
// integer = 200 変数はimmutableなので再定義することはできない
let mutable i = 1 // 変更可能な変数はmutableキーワードが必要
i <- i + i　// インクリメント

// 関数の定義
let square x = x * x
let addOne x = x + 1

// 関数同士の合成。let addOneAndSquare x = square (addOne x)と同じ
let addOneAndSquare = addOne >> square

// printfn "%i" (square (addOne 10))
printfn "%i" (addOneAndSquare 10) // 121

// パイプ演算子でこのようにも書ける
10 |> addOne |> square |> printfn "%i"

// 関数の中に関数を定義できる
let f x =   
    let g x = x * 2 
    let h x = x + 10
    g(h(x))

f 5 // (5 + 10) * 2 = 30
// g 5 はコンパイルエラー(スコープ外で関数gは存在しないとされる)
// f "5" もエラー(型が異なる為)
```

このように`let`を利用して値や関数を識別子(identifier)へバインディングすることができます。F#では関数も第一級オブジェクト(first-class object)として扱われるので整数や文字列と同様に扱うことができ、型推論の機能が推測できる範囲内で型を明記する必要がありません。また、Pythonなどと同じようにスコープは基本的にインデントで表現します。タブ文字は使えません。

パイプ演算子`|>`はF#でも特にお気に入りの記法です。bashのパイプラインから来たのだとか。
とはいえ、あまりパイプ演算子をむやみに利用すると可読性が落ちることもあるので注意が必要です。

``` fsharp
(10.0 |> sqrt |> (+) <| ([1.0..10.0] |> List.sum) ) |> printfn "%f"
// let sqrtTen = sqrt 10. 
// let floatListSum = [1.0..10.0] |> List.sum 
// sqrtTen + floatListSum |> printfn "%f"
```
一行目とコメントアウトした三行でやっていることは一緒です。

``` fsharp
let f x y = x - y  
let f' = 10 |> f // 関数の部分適用 f' y = 10 - y
f' 5 //　10 - 5 = 5
```
関数はデフォルトでカリー化されているので、簡単に部分適用(複数の引数を受け取る関数に対して、必要な引数より少ない引数を適用し、途中の関数を戻り値として受け取ること)が可能です。

``` fsharp
let f x y z = x + y + z
```
関数の引数は半角スペース区切りで表現され、括弧は用いません。
これは数学の式的に書くと、`f(x,y,z) = x + y + z`となり、x, y, zの3つの値を引数として受け取り、x + y + zの結果を返す関数となります。この段階ではx,y,zは整数なのか実数なのか文字列なのかということは推定できません。

``` fsharp
let f x y z = x + y + z
f 2 3 -4 |> printfn "%d" // この段階で、fは int -> int -> int -> int の関数となる。
//f 2.1 3.14 1.414 |> printfn "%f" はできない。
```
こうして値を適用することで関数`f`の引数の型が決定されます。また、その後に別の型の引数を与えてもコンパイルエラーとなってしまいます。

# 第n項のフィボナッチ数を計算する
[フィボナッチ数](https://ja.wikipedia.org/wiki/%E3%83%95%E3%82%A3%E3%83%9C%E3%83%8A%E3%83%83%E3%83%81%E6%95%B0)の計算からF#の強力なツールであるパターンマッチングや再帰関数、Listについて触れてみます。

## if...then...else
``` fsharp
let rec fibonacci n =
    if n=0 then 0
    elif n=1 then 1
    else fibonacci (n-1) + fibonacci(n-2)    
//55
fibonacci 10 |> printfn "%i"
```

まず`rec`は**再帰関数**(recursive function:自身で自身を呼び出す関数)を表します。

`if...then...else`は**条件式**を表し、`if`以下が真であれば`then`以降の値を、偽であれば`elif`が存在する場合`elif`以下の条件を判定し、それも偽であれば`else`以降の値を返します。F#では左辺と右辺が等しいことを表す演算子も`==`や`===`ではなく`=`だけで表すことができます。

また、F#はif**式**であり、値を返します。つまり、いわゆる三項条件演算子と同じ働きをします。

値が必要ない場合は`()`を記述して`unit`型を返すことで、従来の`if`文的な使い方も可能です。

## パターンマッチング
``` fsharp
let rec fibonacci n = 
    match n with
    | 0 -> 0
    | 1 -> 1
    | _ -> fibonacci (n-1) + fibonacci (n-2) 
```
上記のコードは前述した`if...then...else`の式を用いたものとほぼ同じ動作をします。
上記では0か1かそれ以外か、という判別しかしていませんが、リスト、配列のパターンやレコード、タプルなど様々な表現方法が可能です。
アンダースコアの箇所は0、1を除く任意の数値が該当します。アンダースコア自体は変数は受け取るが使わないので、識別子を記述せず破棄することを意味しています。

``` fsharp
let rec fibonacci = 
    function
    | 0 -> 0
    | 1 -> 1
    | n -> fibonacci (n-1) + fibonacci (n-2) 
```

また、引数が1つの時に限り、`match...with`の代わりに`function`を使用して引数の記述を省略することができます。
今回は関数の引数を表す識別子が存在しないので、任意の数値に該当するパターンの箇所には`n`を記述しています。

### 0から10項までを一気に計算して表示する
``` fsharp
let rec fibonacci = 
    function
    | 0 -> 0
    | 1 -> 1
    | n -> fibonacci (n-1) + fibonacci (n-2)

[0..10] |> List.map fibonacci |> printfn "%A" 
// [0; 1; 1; 2; 3; 5; 8; 13; 21; 34; 55]
```
`[0..10]` は `[0;1;2;3;4;5;6;7;8;9;10]` の要素を持つ`List`を作成します。F#における要素の区切り記号はセミコロンとなっています。
また、F#の`List`は .NET の`System.Correction.Generic.List`とは別物で、一度定義すると内容を書き換えることはできません。

`List.map`は引数のリストのそれぞれの要素に、もう一つの引数の関数を適用した新しいリストを返す関数です。.NETだとLINQの`Select`のメソッドに相当します。ただし、F#の場合は元のリストを書き換えません。

`if...then...else`式、パターンマッチングいずれの場合においても極めてシンプルに記述できます。

ちなみにC#で書くとこんな感じになります。
``` csharp
using System;
using System.Linq;

foreach(var fib in Enumerable.Range(0,11).Select(x => Fibonacci(x)))
{
    Console.Write($"{fib} ");
}

int Fibonacci(int n) => n switch
    {
        0 => 0,
        1 => 1,
        _ => Fibonacci(n-1) + Fibonacci(n-2)
    };
```

現在、C#にもswitch式やタプルなどF#(関数型のパラダイム？)のような構文が導入されており、比較的近い書き方が可能になってきている印象です。

# F#で何ができるのか？
**だいたいなんでもできそうです。**

[DeskTop](https://fsharp.org/use/desktop-apps/)も、[Web](https://fsharp.org/use/web-apps/)も、[Mobile](https://fsharp.org/use/mobile-apps/)もあります。
.NETとF#の機能的に低レイヤを触ることもできます。

# あとがき
F#はかなりの機能がありますが、その中でも最も特徴的なのは[コンピュテーション式](https://docs.microsoft.com/ja-jp/dotnet/fsharp/language-reference/computation-expressions)のように思います。LINQ to SQLめいた構文をF#で扱うクエリ式、非同期処理、シーケンス(遅延評価の行われるコレクション、IEnumerable型に対応)などに関してもこのコンピュテーション式が用いられています。

実はF#は用いられている分野からかStack Overflowでは年収の高い言語としてランキングに上がっています。F#が仕事で使われている事例はF# Software Foundationのサイトで見ることができます。一方で、日本では.NEおそらく日本語の書籍は2013年ぐらいのものを最後に出ておらず、日本語のドキュメントは非常に少ないです。そういった学習コストの高さあいまってか極めてマイナーな印象を受けます(取り扱ってる企業はありますが)。
