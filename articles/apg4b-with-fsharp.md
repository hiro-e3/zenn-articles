---
title: "F#でAtCoderを始める(APG4b with F#)"
emoji: "🎶"
type: "tech" # tech: 技術記事 / idea: アイデア
topics: ["FSharp","AtCoder"]
published: false
---

[AtCoder Programming Guide for beginners(APG4b)](https://atcoder.jp/contests/APG4b)に沿いつつ(私の学習も兼ねて)やっていきます。

# A - 1.00.はじめに
https://atcoder.jp/contests/apg4b/tasks/APG4b_a

## プログラミング言語F#とは？
Microsoft社の研究機関、Microsoft ResearchのDon Syme氏達によって、OCalmというプログラミング言語を元に開発された言語で、Microsoft社が提供するオープンソースの汎用開発環境、.NET Coreで用いることのできる言語のひとつです。
[Microsoft Docs(Microsoftの公式ドキュメント)](https://docs.microsoft.com/ja-jp/dotnet/fsharp/what-is-fsharp)では「適切で保守が容易なコードを簡単に記述できるようにする関数型プログラミング言語」と紹介されています。

## F#で提出練習

``` fsharp
"Hello, world!" |> stdout.WriteLine
```

F#の場合は「言語」に「F# (.NET Core 3.1.201)」を選択し、上記のコードをコピー＆ペーストできたら「提出」ボタンをクリックしてください。
上記のコードと、APG4bのC++で記述されたコードを比べるとわかるように、F#のコードはかなり簡潔に記述することが可能です。

# B - 1.01.出力とコメント
https://atcoder.jp/contests/apg4b/tasks/APG4b_b

## キーポイント
- F#では、現時点の内容で `#include` や `using namespace` に該当する機能を使う必要はない
- F#のプログラムは1行目から直接書くことが可能
- F#における**main関数**は`[<EntryPoint>]` という **属性(attribute)** を使用する
- `//` や `(* *)` で**コメント**が書ける(C++の`/* */`に対応するのが`(* *)`)

## 出力
F#で「Hello, world!」という文字列を出力するプログラムは以下の通りです。

``` fsharp
"Hello, world!" |> stdout.WriteLine
```

```powershell:実行結果
Hello, world!
```

また、以下のように書くことも可能です。

``` fsharp
stdout.WriteLine("Hello, world!")
```

[コードテスト](https://atcoder.jp/contests/apg4b/custom_test)で実行してみるとわかりますが、F#ではこれだけで文字列を出力することが可能です。

### stdout.WriteLine
F#で文字列を出力するには`stdout.WriteLine`(えすてぃーでぃーあうと らいとらいん)という関数(正しくはメソッド)を使用します。
`stdout`については必ず半角の小文字のみを使用し、`WriteLine`についてはWとLは半角大文字、それ以外は半角小文字である必要があります。
また、`stdout`と`WriteLine`の間には`.`(半角ピリオド)が必要です

F#もC++と同様に文字列は`" "`で囲う必要があります。`|>`は`"Hello, world!"`というデータを`stdout.WriteLine`という関数に送っていくイメージで考えると良いでしょう。
縦棒`|`はBackspaceキー(Macではdelete)の左隣のキーをShiftキーを押しながら押すと入力できます。

### 別の文字列の出力
F#でも同様に`" "`の中身を書き換えれば出力される文字列も変わります。
``` fsharp
"こんにちは世界" |> stdout.WriteLine
```

```powershell:実行結果
こんにちは世界
```

### 複数の出力
F#で複数の出力を行う場合は少し注意が必要です。

``` fsharp
"a" |> stdout.Write
"b" |> stdout.WriteLine
stdout.WriteLine ("{0}{1}","c","d") // ("{0}{1}","c","d") |> stdout.WriteLine
```

`WriteLine`では最後に改行が追加されますが、`Write`では改行はされません。
また、今回のように文字列同士を繋げる(連結する)場合は以下のようにも書けます。

```fsharp
"a" + "b" |> stdout.WriteLine
"c" + "d" |> stdout.WriteLine
```

```powershell:実行結果
ab
cd
```

### 数値の出力
数値の場合は`" "`で囲う必要はありません
``` fsharp
184 |> stdout.WriteLine
184.997 |> stdout.WriteLine
```
## コメント
F#でのコメントは、C++と用いる記号こそ少し違いますが機能としてはほとんど同じです。

```fsharp
// "a" + "b" |> stdout.WriteLine
"c" + "d" |> stdout.WriteLine // cとdを連結して出力
(* 
ここもコメント
*)
```

## F#のmain関数
**※この項については読まなくても問題ありません。**

F#ではC++のようなプログラムの基本形はなく、main関数というものは必ずしも存在しません。
ただし、F#でもmain関数に相当するものを記述することは可能で、以下のようになります。

``` fsharp
[<EntryPoint>]
let main argv =
    0
```

**エントリーポイント**とは、**プログラムが実行される際に一番最初に実行される箇所**のことを指します。関数については後ほど説明しますが、F#では文字列を引数に取り、整数を返す関数の上に`[<EntryPoint>]`を書くことで、他の言語で言うところのmain関数に指定することが可能です。ただし、AtCoderをやる上でエントリーポイントおよび属性についてはほとんど知る必要はありません。

F#では特に指定のない場合は**プログラムの一行目から順に実行**していきます。

# EX1 - コードテストと出力の練習
F#でのサンプルプログラムは以下となります。
``` fsharp
"Hello, world!" |> stdout.WriteLine
"Hello, AtCoder!" |> stdout.WriteLine
"Hello, C++!" |> stdout.WriteLine
```

# C - 1.02.プログラムの書き方とエラー
## キーポイント
- F#ではスペース・改行・インデントが重要
- その他についてはC++と同様

###