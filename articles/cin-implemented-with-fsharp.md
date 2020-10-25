---
title: "C++のcinをF#で実装する"
emoji: "🎸"
type: "tech" # tech: 技術記事 / idea: アイデア
topics: ["FSharp"]
published: true
---

C++の`cin`から標準入力を受け取って`>>`の演算子を使って変数へと代入するというのをF#で実装する、というものです。
ネタ的な要素と、演算子のオーバーロードを利用してこんな風なことができるという紹介が主となります。

``` cpp
#include <iostream>

int main() {
    int a,b;
    std::cin >> a >> b;
    std::cout << a << b;
}
```

``` powershell : 入力
123 456
```

``` powershell : 出力 
123456
```

上記のようなものをF#で記述できるようにします。

# 実装
``` fsharp
module IOStream =
    type CIn () =
        // 省略
        member __.Read () : 'T =
            match typeof<'T> with
            | ty when ty.Equals(typeof<int64>)  -> __.Long() |> box
            | ty when ty.Equals(typeof<int32>)  -> __.Long() |> int32  |> box
            | ty when ty.Equals(typeof<bigint>) -> __.Long() |> bigint |> box
            | ty when ty.Equals(typeof<float>)  -> __.Next() |> float  |> box
            | ty when ty.Equals(typeof<double>) -> __.Next() |> float  |> box
            | ty when ty.Equals(typeof<string>) -> __.Next() |> box
            | _ -> __.Next() |> box
            :?> 'T　// downcast

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
        // #nowarn "0020" を記述しない場合、警告が出ます。
        cin >> &a >> &b >> &c >> &s
        cout << (a+b+c) << " " << s << endl
        0
```

``` powershell : 出力
> 123 456 100000 hello,world!
100579 hello,world!
```

`IOStream`モジュールはC#で競プロやっている方のソースでよく見る、高速な標準入出力のライブラリをF#に書き換えたものです。
今回、cinっぽい書き方をするために書き換えていますが、長いので省略しています。

### outref<'T>
いわゆる参照渡しですが少し注意が必要で、C#の`out`パラメータ修飾子がF#での`byref<'T>`に相当するもので、`ref`が`outref`、`in`が`inref`となります。
C#での`out int x`はF#では`x : byref<int>`となります。引数として渡す変数は`mutable`である必要があります。また、変数名の頭に`&`を記述する必要があります。

ちなみに、F#では`out`パラメータ修飾子のついた引数に代入される値を受け取る時に、タプルを利用してこのように書けたりします。

``` fsharp
let returns, result = Int32.TryParse("123");;
// val returns : bool = true
// val result : int = 123
```

### 演算子のオーバーロード
既存の演算子をオーバーロードする他にも、`!` `%` `&` `*` `+` `-` `.` `/` `<` `=` `>` `?` `@` `^` `|` および `~` を使って演算子を自作することもできます。
`>>`, `<<`は元々関数の合成の機能を持つ演算子で、以下のように使用します。

``` fsharp
let f x = // 'T1 -> 'T2
    // 'T1の型を持つ値を引数として受け取り'T2の型を持つ値を返す

let g x = // 'T2 -> 'T3
    // 同上

let h = // 'T1 -> 'T3
    f >> g
```

これを左辺に`CIn`型の値を、右辺に`outref<'T>`型の値を取り、`CIn`型の値を返す演算子にオーバーロードしました。
ちなみに、演算子を関数として使用することもできます。

``` fsharp
let main argv =
    let cin = new CIn()
    let mutable a = 0
    (>>) cin &a
    0
```

# 終わりに
関数型言語の文脈としてはあまり良くないように思いますが、それらしく書けたのではないでしょうか。
FSharp.Coreの実装を今一度よく読んで`>>`演算子の実装や、入力の受取・変換をもう少しスマートに書きたいところです。

## 参考
[C#でC++のcinっぽいものを作ってみる - Qiita](https://qiita.com/yuchiki1000yen/items/53061e3937a7f38e2f74)