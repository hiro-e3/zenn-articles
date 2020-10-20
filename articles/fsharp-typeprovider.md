---
title: "F#の型プロバイダについて雑に紹介する"
emoji: "🎶"
type: "tech" # tech: 技術記事 / idea: アイデア
topics: ["FSharp","Excel"]
published: true
---

# 型プロバイダー(Type Provider)とは？

[型プロバイダー - F# | Microsoft Docs](https://docs.microsoft.com/ja-jp/dotnet/fsharp/tutorials/type-providers/)

>F# 型プロバイダーは、プログラムで使用する型、プロパティ、およびメソッドを指定するコンポーネントです。 型プロバイダーは、F# コンパイラによって生成され、外部データ ソースに基づく、指定型と呼ばれるものを生成します。

……らしいです。
私がわかる限りで言うと、読み込んだJSON, XML, CSV, HTMLやRDBなどのデータソースから型を生成し、さらにIntelliSenseも効くようになるものです。
おおよそデータを扱う上での主要なファイルの型プロバイダーは存在していて、自作する必要性はなさそうな気がしていますが、[SDKも存在しており](https://github.com/fsprojects/FSharp.TypeProviders.SDK/)自作も可能です。

## これがあると何が嬉しい？
F#のデータアクセス用ライブラリ [FSharp.Data](https://fsharp.github.io/FSharp.Data/ja/index.html) や [SQLProvider](https://fsprojects.github.io/SQLProvider/index.html)のドキュメントに例示されているデモにもありますが、データソースに応じてクラスを作成する必要性がありません。

# ちょっとした例
## Json
Jsonの型プロバイダーは`FSharp.Data`のライブラリにあります。他にも前述したとおり、CSVやXML,HTMLの型プロバイダーもこちらに含まれます。
ちなみに`FSharp.Data`は他にもパーサーなどの機能もあり、Webスクレイピングにも用いることが可能です。

``` json
[
    {
        "name" : "John Doe",
        "age"  : 20,
        "gender" : "M"
    },
    {
        "name" : "Taro Tanaka",
        "age"  : 33,
        "gender" : "M",
        "country" : "Japan",
        "active" : true
    }
]
```

上記のJsonファイルを例として用意します。
自分では一切型を定義していない状態で、以下のように記述することが可能です。

``` fsharp
open FSharp.Data

type ExampleJsonData = JsonProvider<"exampleData.json">
let json = ExampleJsonData.GetSamples()

json |> Array.iter(fun j -> printfn "%s %d %s" j.Name j.Age j.Gender)
// John Doe 20 M
// Taro Tanaka 33 M
```

ちなみに、片方にしか存在しない`"country"`などは[Option型](https://docs.microsoft.com/ja-jp/dotnet/fsharp/language-reference/options)として扱われます。

## Excel
[ExcelProvider](https://fsprojects.github.io/ExcelProvider/)を使用することで、Excelファイルも扱うことが可能です。

|String|Float|Boolean|Date|Time|Currency|Name|日本名|
|----|----|----|----|----|----|----|----|
|A|1.23|TRUE|2020/10/25|10:55:25 AM|1250円|Taro Tanaka|田中 太郎|
|B|2.22|FALSE|2020/10/26|12:54:12 PM|10015円|John Doe|ジョン  ドゥ|

上記のようなデータを持つExcelファイルを用意します。

``` fsharp
open FSharp.Interop.Excel

type DataTypesTest = ExcelFile<"DataTypes.xlsx">
let file = DataTypesTest()
let row = file.Data

row |> Seq.iter(fun r -> 
    printfn "%s %b %s %A %f %s %A %s" r.String r.Boolean r.Currency r.Date r.Float r.String r.Time r.日本名)
// A true 1250円 2020/10/25 0:00:00 1.230000 A 1899/12/31 10:55:25 田中 太郎
// B false 10015円 2020/10/26 0:00:00 2.220000 B 1899/12/31 12:54:12 ジョン  ドゥ
```

日本語が含まれていてもプロパティとしてアクセスできます。

# おわりに
近々、年月日毎に散逸したExcelファイルをまとめる機会がありそうなので、久しぶりにExcelの操作に関して調べていたところ、型プロバイダーについて着目する機会を得たので雑に紹介してみました。

各ライブラリはdotnet CLIを用いてプロジェクトに追加できます。
``` powershell
dotnet add package FSharp.Data
dotnet add package ExcelProvider && dotnet add package ExcelDataReader && dotnet add package ExcelDataReader.DataSet
```

また、F#は`.fsx`の拡張子で保存し、F# Interactive(`dotnet fsi`)から実行することでスクリプト言語としても使用可能です。
他にも、[Azure Storage](https://fsprojects.github.io/AzureStorageTypeProvider/)や[GraphQL](https://fsprojects.github.io/FSharp.Data.GraphQL/index.html)用のライブラリが存在しています。