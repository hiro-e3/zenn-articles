---
title: "パイプライン"
---

## パイプライン(Pipeline)とは何か
OpenGLに慣れている方は、シェーダープログラムを使った記憶があるかもしれません。パイプラインは、それをより堅牢にしたものと考えることができます。パイプラインは、一連のデータに作用するときにGPUが実行するすべてのアクションを記述します。このセクションでは、特に`RenderPipeline`を作成します。

## 待って、シェーダー(shader)って？
シェーダーは、データに対する操作を実行するために GPU に送信するミニプログラムです。 シェーダには主に、頂点(vertex)、フラグメント(fragment)、コンピュート(compute)の3つのタイプがあります。他にもジオメトリ(geometry)シェーダーやテッセレーション(tesselation)シェーダーなどがありますが、WebGLではサポートされていません。これらは一般に避けるべきです ([議論を参照](https://community.khronos.org/t/does-the-use-of-geometric-shaders-significantly-reduce-performance/106326/10))。現時点では、頂点シェーダーとフラグメントシェーダーのみを使用します。


## 頂点, フラグメント...なんですか、それ？
頂点は3次元空間の点です（2次元も同様）。この頂点を2個ずつ束ねて線にしたり、3個ずつ束ねて三角形にしたりします。

![](https://sotrh.github.io/learn-wgpu/assets/img/tutorial3-pipeline-vertices.5e39e8fc.png)

最近のレンダリングでは、立方体などの単純なものから人物などの複雑なものまで、あらゆる形状を三角形で表現することが多いです。三角形は角を構成する頂点として保存されます。

頂点シェーダーを使って頂点を操作することで、形状を思い通りに変形させることができます。

頂点はその後、フラグメントに変換されます。結果画像の各ピクセルには、少なくとも1つのフラグメントが含まれます。各フラグメントには対応するピクセルにコピーされる色があります。フラグメントシェーダは、フラグメントがどのような色になるかを決定します。

## WGSL
[WebGPU Shading Language(WGSL)](https://www.w3.org/TR/WGSL/)は、WebGPUのためのシェーダ言語です。WGSLの開発は、バックエンドに対応するシェーダ言語（例えば、VulkanはSPIR-V、MetalはMSL、DX12はHLSL、OpenGLはGLSL）に簡単に変換できるようにすることに重点を置いて行われています。この変換は内部で行われるため、通常、詳細を気にする必要はありません。wgpuの場合、[naga](https://github.com/gfx-rs/naga)というライブラリが行っています。

なお、これを書いている時点では、WebGPUの実装でもSPIR-Vをサポートしているものがありますが、これはWGSLへの移行期間中の一時的な措置で、今後削除されます（SPIR-VとWGSLの裏側のドラマが気になる方は、[こちらのブログ記事](http://kvark.github.io/spirv/2021/05/01/spirv-horrors.html)を参照してください）。

::: message
このチュートリアルを以前にご覧になった方は、私が GLSL の使用から WGSL の使用に切り替えたことにお気づきでしょう。GLSLのサポートは二の次で、WGSLはWGPUの第一級言語であることから、私はすべてのチュートリアルをWGSLを使用するように変換することを選択したのです。いくつかのショーケースの例ではまだGLSLを使用していますが、メインのチュートリアルと今後のすべての例ではWGSLを使用する予定です。
:::
::: message
WGSLの仕様とWGPUへの搭載は、まだ開発中です。WGSLの使用に問題がある場合、https://app.element.io/#/room/#wgpu:matrix.org にあなたのコードを見てもらうとよいでしょう。
:::

## シェーダーを記述する
`main.rs`と同じフォルダに、`shader.wgsl`というファイルを作成します。`shader.wgsl`に以下のコードを記述してください。
```wgsl:src/shader.wgsl
// Vertex shader

struct VertexOutput {
    @builtin(position) clip_position: vec4<f32>,
};

@vertex
fn vs_main(
    @builtin(vertex_index) in_vertex_index: u32,
) -> VertexOutput {
    var out: VertexOutput;
    let x = f32(1 - i32(in_vertex_index)) * 0.5;
    let y = f32(i32(in_vertex_index & 1u) * 2 - 1) * 0.5;
    out.clip_position = vec4<f32>(x, y, 0.0, 1.0);
    return out;
}
```
まず、頂点シェーダの出力を格納する`struct`を宣言します。これは現在、頂点の`clip_position`という1つのフィールドだけで構成されています。`@builtin(position)`は、この値を頂点の[クリップ座標(clip coordinates)](https://en.wikipedia.org/wiki/Clip_coordinates)として使用することを WGPU に伝えます。これは、GLSL の `gl_Position` 変数に類似しています。

::: message
`vec4` などのベクトル型はジェネリックです。現在では、ベクトルが含む値の型を指定する必要があります。したがって、32ビット浮動小数点を使用する3Dベクトルは `vec3<f32>` となります。
:::

シェーダーコードの次の部分は `vs_main` 関数です。この関数を頂点シェーダの有効なエントリーポイントとしてマークするため、 `@vertex`を使用しています。`in_vertex_index` という `u32` が必要で、これは `@builtin(vertex_index)`から値を取得します。

次に、`VertexOutput`構造体を使用して、`out`という変数を宣言します。その他に三角形の`x`と`y`を表す2つの変数を作成します。

::: message
`f32()`と`i32()`はキャストの一例です。
:::

::: message
`var `で定義された変数は変更可能ですが、型を指定する必要があります。`let` で作成された変数は、その型を推論することができますが、シェーダ中にその値を変更することはできません。
:::

これで、`clip_position`を`out`に保存できるようになりました。あとは `out` を返すだけで、頂点シェーダは完了です!

::: message
この例では、技術的には構造体は必要なく、次のようにすればよかったのです。
```wgsl
@vertex
fn vs_main(
    @builtin(vertex_index) in_vertex_index: u32
) -> @builtin(position) vec4<f32> {
    // Vertex shader code...
}
```
`VertexOutput`には後でもっと多くのフィールドを追加する予定なので、今のうちに使い始めておくとよいでしょう。
:::

次はフラグメントシェーダーです。さらに`shader.wgsl`に以下を追加します。
```wgsl:shader.wgsl
// Fragment shader

@fragment
fn fs_main(in: VertexOutput) -> @location(0) vec4<f32> {
    return vec4<f32>(0.3, 0.2, 0.1, 1.0);
}
```
現在のフラグメントの色を茶色に設定します。

::: message
バーテックスシェーダのエントリーポイントが vs_main で、フラグメントシェーダのエントリーポイントが fs_main であることに注目してください。wgpu の以前のバージョンでは、これらの関数は両方とも同じ名前でよかったのですが、新しいバージョンの WGSL 仕様では、これらの名前は異なる必要があります。したがって、上記の命名法（wgpuの例から採用）は、チュートリアルを通して使用されます。
:::

`@location(0)`は、この関数が返す`vec4`値を最初のカラーターゲットに格納するよう、WGPUに指示します。これが何であるかは、後で説明します。

:::message
`@builtin(position)`について注意すべき点は、フラグメントシェーダーでは、この値は[フレームバッファ空間](https://gpuweb.github.io/gpuweb/#coordinate-systems)にあります。これは、ウィンドウが800x600の場合、`clip_position`の`x`と`y`はそれぞれ0～800と0～600の間になり、y = 0が画面の上部になることを意味します。これは、特定のフラグメントのピクセル座標を知りたい場合には便利ですが、位置座標が必要な場合は、それらを個別に渡す必要があります。
```wgsl
struct VertexOutput {
    @builtin(position) clip_position: vec4<f32>,
    @location(0) vert_pos: vec3<f32>,
}

@vertex
fn vs_main(
    @builtin(vertex_index) in_vertex_index: u32,
) -> VertexOutput {
    var out: VertexOutput;
    let x = f32(1 - i32(in_vertex_index)) * 0.5;
    let y = f32(i32(in_vertex_index & 1u) * 2 - 1) * 0.5;
    out.clip_position = vec4<f32>(x, y, 0.0, 1.0);
    out.vert_pos = out.clip_position.xyz;
    return out;
}
```
:::


## シェーダーの使い方

いよいよタイトルにあるパイプラインを作る部分です。まず、`State`を以下のように修正してみましょう。
```rust:lib.rs
struct State {
    surface: wgpu::Surface,
    device: wgpu::Device,
    queue: wgpu::Queue,
    config: wgpu::SurfaceConfiguration,
    size: winit::dpi::PhysicalSize<u32>,
    // NEW!
    render_pipeline: wgpu::RenderPipeline,
}
```

では、`new()`メソッドに移動して、パイプラインの作成を開始します。先ほど作ったシェーダーをロードする必要があります。`render_pipeline`はシェーダーを必要とするからです。

```rust
let shader = device.create_shader_module(&wgpu::ShaderModuleDescriptor {
    label: Some("Shader"),
    source: wgpu::ShaderSource::Wgsl(include_str!("shader.wgsl").into()),
});
```

::: message
また、`ShaderModuleDescriptor`を作成するための小さなショートカットとして、`include_wgsl!` マクロを使用することができます。

```rust
let shader = device.create_shader_module(wgpu::include_wgsl!("shader.wgsl"));
```
:::

もう一つ、`PipelineLayout`を作成する必要があります。これについては、`Buffer`を取り上げた後で詳しく説明します。

```rust
let render_pipeline_layout =
    device.create_pipeline_layout(&wgpu::PipelineLayoutDescriptor {
        label: Some("Render Pipeline Layout"),
        bind_group_layouts: &[],
        push_constant_ranges: &[],
    });
 
```

これでようやく、`render_pipeline`を作成するのに必要なものが揃いました。

```rust
let render_pipeline = device.create_render_pipeline(&wgpu::RenderPipelineDescriptor {
    label: Some("Render Pipeline"),
    layout: Some(&render_pipeline_layout),
    vertex: wgpu::VertexState {
        module: &shader,
        entry_point: "vs_main", // 1.
        buffers: &[], // 2.
    },
    fragment: Some(wgpu::FragmentState { // 3.
        module: &shader,
        entry_point: "fs_main",
        targets: &[Some(wgpu::ColorTargetState { // 4.
            format: config.format,
            blend: Some(wgpu::BlendState::REPLACE),
            write_mask: wgpu::ColorWrites::ALL,
        })],
    }),
    // continued ...
```

ここで注意すべき点が4つあります。

1. ここでは、シェーダー内部のどの関数を `entry_point` とするかを指定します。これらは `@vertex` と `@fragment` でマークした関数です。

2. `buffers`フィールドは頂点シェーダに渡す頂点の種類をwgpuに伝えます。頂点シェーダ自体で頂点を指定するので、ここは空欄にしておきます。次のチュートリアルで、そこに何かを入れることになります。

3. `fragment`は技術的にオプションなので、`Some()`でラップする必要があります。色データをサーフェスに格納する場合は、これが必要です。

4. `targets`フィールドはwgpuにどのような色出力をセットアップすべきかを伝えます。surfaceへのコピーが簡単にできるようにsurfaceの`format`を使用し、ブレンディングは古いピクセルデータを新しいデータで置き換えるだけでよいことを指定します。また、赤、青、緑、アルファのすべての色に書き込むようにwgpuに指示します。テクスチャについて話すとき、`color_state`についてもっと話します。

```rust
    primitive: wgpu::PrimitiveState {
        topology: wgpu::PrimitiveTopology::TriangleList, // 1.
        strip_index_format: None,
        front_face: wgpu::FrontFace::Ccw, // 2.
        cull_mode: Some(wgpu::Face::Back),
        // Fill 以外に設定する場合は Features::NON_FILL_POLYGON_MODE が必要です。
        polygon_mode: wgpu::PolygonMode::Fill,
        // Features::DEPTH_CLIP_CONTROLが必要
        unclipped_depth: false,
        // Features::CONSERVATIVE_RASTERIZATION が必要
        conservative: false,
    },
    // continued ...
```

`primitive`フィールドは、頂点を三角形に変換する際に、どのように解釈するかを記述します。

1. `PrimitiveTopology::TriangleList`を使用すると、3つの頂点がそれぞれ1つの三角形に対応することになります。
2. `front_face`と`cull_mode`フィールドは与えられた三角形が正面を向いているか否かを決定する方法をwgpuに教えます。`FrontFace::Ccw`は、頂点が反時計回りに配置されている場合、その三角形は正面を向いていることを意味します。正面を向いていないとみなされた三角形は、`CullMode::Back` で指定されたようにカリング(culling) (cull: 選んで取り除く、間引く)されます（レンダリングに含まれない）。カリングについては、`Buffer`の説明のときにもう少し詳しく説明します。

```rust
    depth_stencil: None, // 1.
    multisample: wgpu::MultisampleState {
        count: 1, // 2.
        mask: !0, // 3.
        alpha_to_coverage_enabled: false, // 4.
    },
    multiview: None, // 5.
});
```

残りのメソッドは非常にシンプルです。

1. 現在、深度/ステンシルバッファを使用していないので、`depth_stencil`は`None`のままにしています。これは後で変更します。
2. `count` は、パイプラインが使用するサンプル数を決定します。マルチサンプリングは複雑なトピックなので、ここでは触れません。
3. `mask` は、どのサンプルをアクティブにするかを指定します。この場合、すべてを使用します。
4. `alpha_to_coverage_enabled` はアンチエイリアシングに関係するものです。ここでは、アンチエイリアシングについて説明しないので、false のままにしておきます。
5. `multiview` は、レンダーアタッチメントがいくつの配列レイヤーを持つことができるかを示します。今回は配列テクスチャにレンダリングしないので、これを `None` に設定します。

あとは、`State`に`render_pipeline`を追加すれば、使えるようになります!

```rust
// new()
Self {
    surface,
    device,
    queue,
    config,
    size,
    // NEW!
    render_pipeline,
}
```

## パイプラインを使う
今プログラムを実行すると、起動に少し時間がかかりますが、最後のセクションで得た青い画面が表示されたままです。これは、`render_pipeline`を作成した一方で、それを実際に使用するために`render()`のコードを変更する必要があるためです。
```rust
// render()

// ...
{
    // 1.
    let mut render_pass = encoder.begin_render_pass(&wgpu::RenderPassDescriptor {
        label: Some("Render Pass"),
        color_attachments: &[
            // This is what @location(0) in the fragment shader targets
            Some(wgpu::RenderPassColorAttachment {
                view: &view,
                resolve_target: None,
                ops: wgpu::Operations {
                    load: wgpu::LoadOp::Clear(
                        wgpu::Color {
                            r: 0.1,
                            g: 0.2,
                            b: 0.3,
                            a: 1.0,
                        }
                    ),
                    store: wgpu::StoreOp::Store,
                }
            })
        ],
        depth_stencil_attachment: None,
    });

    // NEW!
    render_pass.set_pipeline(&self.render_pipeline); // 2.
    render_pass.draw(0..3, 0..1); // 3.
}
// ...
```

あまり大きな変更はありませんでしたが、変更した点について説明します。

1. ` _render_pass` を `render_pass` に改名し、mutableにしました。
2.  `render_pass` のパイプラインを、先ほど作成したものを使って設定しました。
3.  3つの頂点と1つのインスタンスで何かを描くようにwgpuに伝えます。これは、`@builtin(vertex_index)`から来ています。

これで、きれいな茶色の三角形が表示されるはずです。
![](https://storage.googleapis.com/zenn-user-upload/48428a108404-20220117.png)

## 課題
三角形の位置データを使って色を作り、それをフラグメントシェーダーに送る2つ目のパイプラインを作成します。スペースキーを押すと、アプリがこれらの間で切り替わるようにします。ヒント：`VertexOutput `を変更する必要があります。

[コードを確認する](https://github.com/sotrh/learn-wgpu/tree/master/code/beginner/tutorial3-pipeline/)