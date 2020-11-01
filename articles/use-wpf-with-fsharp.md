---
title: "WPFでF#も使いたい！"
emoji: "🎵"
type: "tech" # tech: 技術記事 / idea: アイデア
topics: ["FSharp","WPF"]
published: true
---

# はじめに
F#には現状、WPFを扱うテンプレートが存在しておらず、デフォルトの状態ではWPFでF#を用いるのは難しいですが、今回F#のみでウィンドウの表示に成功している例を見つけることができたので共有したいと思います。
参考:https://github.com/kalugvasy3/How-to-create-WPF-application-with-F-sharp-only-with-Dot-Net-Core-3.1

# プロジェクトファイル(fsproj)

``` xml
<Project Sdk="Microsoft.NET.Sdk.WindowsDesktop">

  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>netcoreapp3.1</TargetFramework>
    <UseWPF>true</UseWPF>
  </PropertyGroup>

  <ItemGroup>
    <Page Remove="MainWindow.xaml" />
  </ItemGroup>

  <ItemGroup>
    <EmbeddedResource Include="MainWindow.xaml" />
    <Compile Include="MainWindow.fs" />
  </ItemGroup>

</Project>
```

WPFのcsprojと異なるのは、埋め込みリソースとしてMainWindow.xamlを含める必要があります。

# MainWindow.fs

``` fsharp
open System 
open System.Windows 
open System.Windows.Markup
open System.Reflection

[<STAThread>] 
[<EntryPoint>]
do Assembly.GetExecutingAssembly().GetManifestResourceNames() 
    |> Array.find(fun x -> x.Contains("MainWindow.xaml"))
    |> Assembly.GetExecutingAssembly().GetManifestResourceStream
    |> XamlReader.Load :?> Window
    |> Application().Run |> ignore
```

現在実行中のアセンブリを`GetExecutingAssembly`メソッドで取得し、さらに`GetManifestResourceNames`でリソース名称が格納された文字列配列を取得します。その中からMainWindow.xamlの文字列を含む要素を`find`関数で取得します。見つからない可能性も加味してoption型で返す`tryFind`関数を使った方がいいかもしれません。

そして、取得したリソース名を使ってリソースの`Stream`を取得し、`Stream`(つまりXamlファイルの内容)から`XamlReader.Load`メソッドを使ってオブジェクトを生成しています。`Load`メソッドからの戻り値は`Object`型(F#では`obj`)なので`:?>`演算子で`System.Windows.Window`型へダウンキャストを行っています。

# MainWindow.xaml
特に通常と変わりない記述で問題ありません。

``` XML
<Window xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" 
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" 
        MinWidth="450" MinHeight="266" Title="I want to use Windows Presentation Foundation with F#!" SizeToContent="WidthAndHeight">

    <Grid x:Name="gridAll" Margin="0,0,0,0"  RenderTransformOrigin="0.5,0.5"  >
        <Grid.RowDefinitions>
            <RowDefinition />
            <RowDefinition />
        </Grid.RowDefinitions>
        <Grid.ColumnDefinitions>
            <ColumnDefinition />
            <ColumnDefinition />
        </Grid.ColumnDefinitions>
        <TextBlock Grid.Row="0" Grid.Column="0" Text="Hello F#!" />
    </Grid>
</Window>
```

# 実行
![実行結果](https://raw.githubusercontent.com/msanou/zenn-articles/master/image/fswpf.png)

## 備考
ホットリロードはどうやら動かないみたいです。
Visual StudioのデザイナーはC#と同様に使用することができます。

# 終わりに
このままだとF#でWPFをやる！という時に逐一プロジェクトファイルなどを書き換える必要があるので、[こちらのチュートリアル](https://docs.microsoft.com/ja-jp/dotnet/core/tutorials/cli-templates-create-item-template)などを参考にテンプレートを作ると便利です。

## 備考
[GitHubのIssue](https://github.com/dotnet/wpf/issues/162)にもサポートを望む声が少しあるようです。

また、[Elm](https://guide.elm-lang.jp/)というJavaScriptにコンパイル可能な関数型言語のF#実装である、[Elmish](https://elmish.github.io/elmish/)というライブラリがF#には存在していて、ElmishでWPFを書く[Elmish.WPF](https://github.com/elmish/Elmish.WPF)があるので、こちらもいずれ試してみたいと思います。