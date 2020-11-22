---
title: ".NET Interactive Notebooks(VS Code Notebook)"
emoji: "📖"
type: "tech" # tech: 技術記事 / idea: アイデア
topics: ["FSharp","DotNet"]
published: true
---

[F# 5.0について紹介していた.NET Blogの記事](https://devblogs.microsoft.com/dotnet/announcing-f-5/)にあった"VS Code Notebook"にまつわる記事です。

# [.NET Interactive](https://github.com/dotnet/interactive)とは
.NET言語のインタラクティブなプログラミング環境を提供するもの……らしい。

[Jupyter](https://jupyter.org/)や[nteract](https://nteract.io/)で.NETの言語(C#, F#, Powershell)が使えるようになるほか、Raspberry Piにも組み込めるようなことがドキュメントに記載されています。元々、ブラウザ実行環境として[Try .NET](https://dotnet.microsoft.com/platform/try-dotnet)があり、これがリネームされたものだという話も個人ブログなどで見かけるのですが、特に信憑性のありそうな記述に出会えませんでした。

# [.NET Interactive Notebooks](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.dotnet-interactive-vscode)とは
.NET Interactive Notebooksは.NETの言語(C#, F#, PowerShell）やJavaScript、HTML、Markdownを使って、VS Code上でNotebooksを扱える拡張機能です。最新の.NET SDKが必要となります。VS CodeのInsider版が必要となっていますが、一応通常のものでも動きます。

F#のデータ可視化パッケージである[XPlot](https://fslab.org/XPlot/)は[Plotly](https://plotly.com/python/)をサポートしており、このパッケージと.NET Interactive Notebooksを組み合わせることで、F#でもこんな感じにグラフを描画できるようになります。

``` fsharp : cell.fsx
#r "nuget:FSharp.Data"
#r "nuget:XPlot.Plotly"
open FSharp.Data
open XPlot.Plotly

// CSVからデータの取得、型生成(FSharp.Data)
type data = CsvProvider<"data.csv">
let csvdata = data.GetSample()

// 日付と降雪量のTupleのListを作成
let list = [ for r in csvdata.Rows do r.Date, r.Snowfall]

// Titleの設定
let layout = Layout(title="Snowfall in Asahikawa")

// x軸へ日付,y軸へ降雪量のデータをセットして描画
Scatter(x=(list |> List.map fst), y=(list |> List.map snd)) // C# : new Scatter(){x=..., y=...}
|> Chart.Plot
|> Chart.WithLayout layout
|> Chart.WithHeight 500
|> Chart.WithWidth 800
```

![旭川市の降雪量](https://raw.githubusercontent.com/msanou/zenn-articles/master/image/snowfall.png)
このグラフは[気象庁のWebサイト](https://www.data.jma.go.jp/gmd/risk/obsdl/index.php)からダウンロードした北海道旭川市の2019年10月1日から2020年4月1日までの一日あたりの降雪量のデータを描画したものです。そのままでは文字コードがShift-JISだったり、項目名が日本語で使いにくいので少し編集しています。

F#におけるCSVの取り扱いに関してはやはり[FSharp.Data](https://fsharp.github.io/FSharp.Data/index.html)の`CsvProvider`が強力です。`Date`や`Snowfall`はCSVの1行目に記述したラベルから生成されており、ラベル行以下のデータから`Date`は`DateTime`型、`Snowfall`は`int`型のプロパティとして生成されています。ちなみに日本語で記述されていた場合でも`r.日付`というような形でプロパティを参照することができます。

# 終わりに
Visula Studio Codeの拡張機能に関する紹介がほとんどとなってしまいましたが、.NET 5.0の話題に関連して上がってくる様子が見えなかったので、簡単に紹介させていただきました。Jupyterの拡張機能もMSが作っているみたいなのでいつか .NET Interactive Notebooksと統合されたりしそうな気がしなくもない。