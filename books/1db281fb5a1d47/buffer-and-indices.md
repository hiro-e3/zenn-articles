---
title: "バッファとインデックス"
---

## ついにその話をする時が来た！
「バッファの話をするときに説明します」などと言う私に、うんざりしていたかもしれませんね。さあ、今こそバッファについて話す時です。でも、その前に...。

## バッファとは何か？
バッファとは、GPU上のデータの塊のことです。連続であることが保証されており、すべてのデータがメモリに順次格納されることを意味します。一般に構造体や配列のような単純なものを格納するのに使われますが、木構造（すべてのノードが一緒に格納され、バッファの外を参照しない場合のみ）のようなより複雑なものを格納することができます。私たちはバッファをたくさん使うので、最も重要な2つのバッファ、頂点バッファ(vertex buffer)とインデックスバッファ(index buffer)から始めましょう。


## 頂点バッファ(vertex buffer)
これまでは、頂点データを頂点シェーダに直接格納していました。しかし、これは長期的に見ると、うまくいきません。描画するオブジェクトの種類によってサイズが異なるし、モデルを更新する必要があるたびにシェーダを再コンパイルすると、プログラムの速度が大幅に低下します。その代わりに、描画したい頂点データを保存するためにバッファを使用することにします。しかし、その前に、頂点がどのようなものであるかを説明する必要があります。そのために、新しい構造体を作成します。
```rust:lib.rs
#[repr(C)]
#[derive(Copy, Clone, Debug)]
struct Vertex {
    position: [f32; 3],
    color: [f32; 3],
}
```
頂点はすべて位置と色を持っています。位置は、3次元空間における頂点のx、y、zを表します。色はその頂点の赤、緑、青の値です。バッファを作成するために`Vertex`を`Copy`にする必要があります。

次に、三角形を構成する実際のデータが必要です。`Vertex`の下に以下を追加してください。
```rust:lib.rs
const VERTICES: &[Vertex] = &[
    Vertex { position: [0.0, 0.5, 0.0], color: [1.0, 0.0, 0.0] },
    Vertex { position: [-0.5, -0.5, 0.0], color: [0.0, 1.0, 0.0] },
    Vertex { position: [0.5, -0.5, 0.0], color: [0.0, 0.0, 1.0] },
];
```
頂点を反時計回りに、上、左下、右下の順に並べます。部分的には伝統からこのようにしていますが、大部分は`render_pipeline`の`primitive`で三角形の`front_face`を`wgpu::FrontFace::Ccw`と指定し、裏面をカリングするようにしたためです。これは、こちらを向いているべき三角形の頂点が反時計回りの順序でなければならないことを意味します。

さて、頂点データができたので、それをバッファに格納する必要があります。`State`に`vertex_buffer`フィールドを追加してみましょう。

```rust::lib.rs
struct State {
    // ...
    render_pipeline: wgpu::RenderPipeline,

    // NEW!
    vertex_buffer: wgpu::Buffer,

    // ...
}
```

`new()`でバッファを作成しましょう。

```rust:lib.rs
// new()
let vertex_buffer = device.create_buffer_init(
    &wgpu::util::BufferInitDescriptor {
        label: Some("Vertex Buffer"),
        contents: bytemuck::cast_slice(VERTICES),
        usage: wgpu::BufferUsages::VERTEX,
    }
);
```
`wgpu::Device`の`create_buffer_init`メソッドにアクセスするために、[`DeviceExt`](https://docs.rs/wgpu/latest/wgpu/util/trait.DeviceExt.html#tymethod.create_buffer_init)拡張トレイトをインポートする必要があります。拡張トレイトの詳細については、[こちらの記事](http://xion.io/post/code/rust-extension-traits.html)を参照してください。

拡張トレイトをインポートするには、`lib.rs`の先頭付近のこの行をインポートしてください。

```rust:lib.rs
use wgpu::util::DeviceExt;
```

[bytemuck](https://docs.rs/bytemuck/latest/bytemuck/index.html)を使って、`VERTICES`を`&[u8]`にキャストしていることに注意してください。`create_buffer_init()`メソッドは`&[u8]`を期待しますが、`bytemuck::cast_slice` がそれをやってくれます。`Cargo.toml`に以下を追加してください。

```toml:Cargo.toml
bytemuck = { version = "1.12", features = [ "derive" ] }
```

また、bytemuckを動作させるために、[bytemuck::Pod](https://docs.rs/bytemuck/latest/bytemuck/trait.Pod.html) と [bytemuck::Zeroable](https://docs.rs/bytemuck/latest/bytemuck/trait.Zeroable.html)、2つのtraitを実装する必要があります。`Pod`は`Vertex`が「Plain Old Data」であることを示し、したがって`&[u8]`として解釈することができる。`Zeroable`は`std::mem::zeroed()`が使えることを表しています。これらのメソッドを派生させるために、`Vertex`構造体を修正することができます。

```rust
#[repr(C)]
#[derive(Copy, Clone, Debug, bytemuck::Pod, bytemuck::Zeroable)]
struct Vertex {
    position: [f32; 3],
    color: [f32; 3],
}
```

::: message
`Pod`と`Zeroable`を実装していない型が構造体に含まれている場合、これらのtraitを手動で実装する必要があります。これらのtraitは、どのようなメソッドも実装する必要はありません。

```rust
unsafe impl bytemuck::Pod for Vertex {}
unsafe impl bytemuck::Zeroable for Vertex {}
```
:::

最後に`State`構造体に`vertex_buffer`を追加します。

```rust
Self {
    surface,
    device,
    queue,
    config,
    size,
    render_pipeline,
    vertex_buffer,
}
```

## で、どうするの？
描画時にこのバッファを使うように`render_pipeline`に指示する必要がありますが、まずバッファの読み方を`render_pipeline`に指示する必要があります。これは`VertexBufferLayouts`と`vertex_buffers`フィールドを使用します。

`VertexBufferLayout` は、バッファがメモリ上でどのように表現されるかを定義します。これがないと、`render_pipeline` はシェーダでバッファをどのようにマッピングすればよいのかわかりません。以下は、`Vertex` でいっぱいのバッファの記述子がどのように見えるかです。

```rust
wgpu::VertexBufferLayout {
    array_stride: std::mem::size_of::<Vertex>() as wgpu::BufferAddress, // 1.
    step_mode: wgpu::VertexStepMode::Vertex, // 2.
    attributes: &[ // 3.
        wgpu::VertexAttribute {
            offset: 0, // 4.
            shader_location: 0, // 5.
            format: wgpu::VertexFormat::Float32x3, // 6.
        },
        wgpu::VertexAttribute {
            offset: std::mem::size_of::<[f32; 3]>() as wgpu::BufferAddress,
            shader_location: 1,
            format: wgpu::VertexFormat::Float32x3,
        }
    ]
}
```

1. `array_stride`は、頂点の幅を定義します。シェーダが次の頂点を読みに行くとき、`array_stride` のバイト数だけ読み飛ばします。この例では、`array_stride` はおそらく24 バイトです。
2. `step_mode` はパイプラインが次の頂点に移動する頻度を指定します。このケースでは冗長に見えますが、新しいインスタンスを描き始めるときに頂点を変更したいだけなら、`wgpu::VertexStepMode::Instance`を指定することができます。インスタンス化については、後のチュートリアルで説明します。
3. `attributes` は、頂点の個々のパーツを記述します。一般的に、この属性は構造体のフィールドと1対1に対応します。
4. アトリビュートの開始位置までの`offset`をバイト単位で定義します。最初のアトリビュートでは、オフセットは通常ゼロです。それ以降のアトリビュートでは、オフセットは前のアトリビュートのデータの`size_of` を超える合計となります。
5. `shader_location`はシェーダに、このアトリビュートをどの位置に格納するかを指示します。たとえば、頂点シェーダの`@location(0) x: vec3<f32>`は `Vertex` 構造体の`position`フィールドに対応し、 `@location(1) x: vec3<f32>` は`color`フィールドに対応します。
6. `format` はシェーダに属性の形状を伝えます。`Float32x3` はシェーダーコードでは `vec3<f32>` に相当します。アトリビュートに格納できる最大値は `Float32x4` です（`Uint32x4`、`Sint32x4` も同様に機能します）。`Float32x4`より大きな値を格納しなければならないときのために、このことを覚えておこう。

視覚的に学習する人のために、頂点バッファは次のようになります。
![](https://sotrh.github.io/learn-wgpu/assets/img/vb_desc.63afb652.png)
この記述子を返す`Vertex`の静的なメソッドを作ってみましょう。

```rust:lib.rs
impl Vertex {
    fn desc() -> wgpu::VertexBufferLayout<'static> {
        wgpu::VertexBufferLayout {
            array_stride: std::mem::size_of::<Vertex>() as wgpu::BufferAddress,
            step_mode: wgpu::VertexStepMode::Vertex,
            attributes: &[
                wgpu::VertexAttribute {
                    offset: 0,
                    shader_location: 0,
                    format: wgpu::VertexFormat::Float32x3,
                },
                wgpu::VertexAttribute {
                    offset: std::mem::size_of::<[f32; 3]>() as wgpu::BufferAddress,
                    shader_location: 1,
                    format: wgpu::VertexFormat::Float32x3,
                }
            ]
        }
    }
}
```

::: message
今までのような属性の指定はかなり冗長です。wgpuが提供する`vertex_attr_array`マクロを使用すれば、少しはすっきりします。それを使って、私たちの`VertexBufferLayout`は次のようになります。

```rust
wgpu::VertexBufferLayout {
    array_stride: std::mem::size_of::<Vertex>() as wgpu::BufferAddress,
    step_mode: wgpu::VertexStepMode::Vertex,
    attributes: &wgpu::vertex_attr_array![0 => Float32x3, 1 => Float32x3],
}
```

これは確かに良いことですが、Rustは`vertex_attr_array`の結果を一時的な値だと見なすので、関数からそれを返すには微調整が必要です。`wgpu::VertexBufferLayout`のライフタイムを`'static`に変更するか、それを[`const`にすることができ](https://github.com/gfx-rs/wgpu/discussions/1790#discussioncomment-1160378)ます。以下に例を示します。

```rust
impl Vertex {
    const ATTRIBS: [wgpu::VertexAttribute; 2] =
        wgpu::vertex_attr_array![0 => Float32x3, 1 => Float32x3];

    fn desc() -> wgpu::VertexBufferLayout<'static> {
        use std::mem;

        wgpu::VertexBufferLayout {
            array_stride: mem::size_of::<Self>() as wgpu::BufferAddress,
            step_mode: wgpu::VertexStepMode::Vertex,
            attributes: &Self::ATTRIBS,
        }
    }
}
```

とはいえ、データがどのようにマッピングされるかを示すのは良いことだと思うので、とりあえずこのマクロの使用は見送ることにします。
:::

これで、`render_pipeline`を作成するときに使用できるようになりました。
```rust
let render_pipeline = device.create_render_pipeline(&wgpu::RenderPipelineDescriptor {
    // ...
    vertex: wgpu::VertexState {
        // ...
        buffers: &[
            Vertex::desc(),
        ],
    },
    // ...
});
```
もうひとつ、`render()`メソッドで実際に頂点バッファを設定しないと、プログラムがクラッシュしてしまいます。

```rust
// render()
render_pass.set_pipeline(&self.render_pipeline);
// NEW!
render_pass.set_vertex_buffer(0, self.vertex_buffer.slice(..));
render_pass.draw(0..3, 0..1);
```

`set_vertex_buffer` は2つのパラメータを取ります。1つ目は、この頂点バッファに使用するバッファスロットを指定します。一度に複数の頂点バッファを設定することができます。

2つ目のパラメータは、使用するバッファのスライスです。バッファにはハードウェアが許す限りいくつでもオブジェクトを格納できるので、`slice`によってバッファのどの部分を使用するかを指定することができます。バッファ全体を指定する場合は`..`を使用します。

続行する前に、`VERTICES` で指定された頂点の数を使用するように `render_pass.draw()` 呼び出しを変更する必要があります。`State`に`num_vertices`を追加し、`VERTICES.len()`と等しくなるように設定します。
```rust::lib.rs
struct State {
    // ...
    num_vertices: u32,
}

impl State {
    // ...
    fn new(...) -> Self {
        // ...
        let num_vertices = VERTICES.len() as u32;

        Self {
            surface,
            device,
            queue,
            config,
            render_pipeline,
            vertex_buffer,
            num_vertices,
        }
    }
}
```
そして`draw()`の呼び出しで使用します。

```rust
// render
render_pass.draw(0..self.num_vertices, 0..1);
```

この変更が効果を発揮する前に、頂点バッファからデータを取得するために、頂点シェーダを更新する必要があります。また、同様に頂点カラーも含めるようにします

```wgsl:shader.wgsl
// Vertex shader

struct VertexInput {
    @location(0) position: vec3<f32>,
    @location(1) color: vec3<f32>,
};

struct VertexOutput {
    @builtin(position) clip_position: vec4<f32>,
    @location(0) color: vec3<f32>,
};

@vertex
fn vs_main(
    model: VertexInput,
) -> VertexOutput {
    var out: VertexOutput;
    out.color = model.color;
    out.clip_position = vec4<f32>(model.position, 1.0);
    return out;
}

// Fragment shader

@fragment
fn fs_main(in: VertexOutput) -> @location(0) vec4<f32> {
    return vec4<f32>(in.color, 1.0);
}
```

正しく操作できていれば、次のような三角形が表示されるはずです。
![](/images/winit_window2.png)

## インデックスバッファ
厳密に言えば、インデックスバッファは必要ないのですが、それでも十分便利です。インデックスバッファは、三角形がたくさんあるモデルを使い始めると、その威力を発揮します。この五角形について考えてみましょう。
![](/images/index_buffer.png)

全部で5つの頂点と3つの三角形があります。さて、このようなものを頂点だけで表示しようとすると、次のようなものが必要になります。
```rust
const VERTICES: &[Vertex] = &[
    Vertex { position: [-0.0868241, 0.49240386, 0.0], color: [0.5, 0.0, 0.5] }, // A
    Vertex { position: [-0.49513406, 0.06958647, 0.0], color: [0.5, 0.0, 0.5] }, // B
    Vertex { position: [0.44147372, 0.2347359, 0.0], color: [0.5, 0.0, 0.5] }, // E

    Vertex { position: [-0.49513406, 0.06958647, 0.0], color: [0.5, 0.0, 0.5] }, // B
    Vertex { position: [-0.21918549, -0.44939706, 0.0], color: [0.5, 0.0, 0.5] }, // C
    Vertex { position: [0.44147372, 0.2347359, 0.0], color: [0.5, 0.0, 0.5] }, // E

    Vertex { position: [-0.21918549, -0.44939706, 0.0], color: [0.5, 0.0, 0.5] }, // C
    Vertex { position: [0.35966998, -0.3473291, 0.0], color: [0.5, 0.0, 0.5] }, // D
    Vertex { position: [0.44147372, 0.2347359, 0.0], color: [0.5, 0.0, 0.5] }, // E
];
```
しかし、いくつかの頂点は2回以上使われていることにお気づきでしょうか。CとBは2回使われ、Eは3回繰り返されています。仮に1つの`float`が4バイトだとすると、`VERTICES`に使う216バイトのうち96バイトは重複したデータということになります。これらの頂点を1回でリストアップできたらいいと思いませんか？　それが可能なのです！　そこで、インデックスバッファの出番です。

基本的には、`VERTICES`にユニークな頂点をすべて保存し、三角形を作るために`VERTICES`の要素へのインデックスを保存する別のバッファを作成します。以下は、五角形の例です。

```rust:main.rs
const VERTICES: &[Vertex] = &[
    Vertex { position: [-0.0868241, 0.49240386, 0.0], color: [0.5, 0.0, 0.5] }, // A
    Vertex { position: [-0.49513406, 0.06958647, 0.0], color: [0.5, 0.0, 0.5] }, // B
    Vertex { position: [-0.21918549, -0.44939706, 0.0], color: [0.5, 0.0, 0.5] }, // C
    Vertex { position: [0.35966998, -0.3473291, 0.0], color: [0.5, 0.0, 0.5] }, // D
    Vertex { position: [0.44147372, 0.2347359, 0.0], color: [0.5, 0.0, 0.5] }, // E
];

const INDICES: &[u16] = &[
    0, 1, 4,
    1, 2, 4,
    2, 3, 4,
];
```
この設定では、`VERTICES`は約120バイト、`INDICES`はu16が2バイト幅であることから、わずか18バイトを占めます。この場合、wgpuはバッファが4バイトに整列するように自動的に2バイトのパディングを追加しますが、それでもちょうど20バイトです。全部で五角形は140バイトです。つまり、82バイトの節約になります。しかし、何十万もの三角形を扱う場合、インデックスの作成は多くのメモリを節約することになるのです。

インデックスを使用するためには、いくつか変更しなければならない点があります。まず、インデックスを格納するためのバッファを作成する必要があります。`State`の`new()`メソッドで`vertex_buffer`を作成した後に`index_buffer`を作成します。また、`num_vertices` を `num_indices` に変更し、`INDICES.len()` と等しくなるように設定してください。

```rust
let vertex_buffer = device.create_buffer_init(
    &wgpu::util::BufferInitDescriptor {
        label: Some("Vertex Buffer"),
        contents: bytemuck::cast_slice(VERTICES),
        usage: wgpu::BufferUsages::VERTEX,
    }
);
// NEW!
let index_buffer = device.create_buffer_init(
    &wgpu::util::BufferInitDescriptor {
        label: Some("Index Buffer"),
        contents: bytemuck::cast_slice(INDICES),
        usage: wgpu::BufferUsages::INDEX,
    }
);
let num_indices = INDICES.len() as u32;
```
indicesに`Pod`と`Zeroable`を実装する必要はありません。
なぜなら、`bytemuck`はすでに`u16`のような基本的な型に対して`Pod`と`Zeroable`を実装しているからです。つまり、ただ`State`構造体に`index_buffer`と`num_indices`を追加すればいいのです。

```rust
struct State {
    surface: wgpu::Surface,
    device: wgpu::Device,
    queue: wgpu::Queue,
    config: wgpu::SurfaceConfiguration,
    size: winit::dpi::PhysicalSize<u32>,
    render_pipeline: wgpu::RenderPipeline,
    vertex_buffer: wgpu::Buffer,
    // NEW!
    index_buffer: wgpu::Buffer, 
    num_indices: u32,
}
```

そして、コンストラクタのフィールドにも追加します。

```rust
Self {
    surface,
    device,
    queue,
    config,
    size,
    render_pipeline,
    vertex_buffer,
    // NEW!
    index_buffer,
    num_indices,
}
```
あとは、`index_buffer`を使用するように`render()`メソッドを更新するだけです。

```rust
// render()
render_pass.set_pipeline(&self.render_pipeline);
render_pass.set_vertex_buffer(0, self.vertex_buffer.slice(..));
render_pass.set_index_buffer(self.index_buffer.slice(..), wgpu::IndexFormat::Uint16); // 1.
render_pass.draw_indexed(0..self.num_indices, 0, 0..1); // 2.
```

いくつか注意点があります。

1. メソッド名は、`set_index_buffers` ではなく、`set_index_buffer` です。一度に設定できるインデックスバッファは1つだけです。
2. インデックスバッファを使用する場合、`draw_indexed` を使用する必要があります。`draw` メソッドはインデックスバッファを無視します。また、頂点ではなくインデックスの数 (`num_indices`) を使っていることを確認してください。モデルが誤って描画されていたり、メソッドが`panic`したりする場合、インデックスの数が十分ではありません。

これで、ウィンドウに派手なマゼンタ色の五角形が表示されるはずです。

![](/images/magenta_pentagon.png)

## 色調補正
マゼンタの五角形にカラーピッカーを使うと、16進数で#BC00BCという値が得られます。これをRGB値に変換すると、(188, 0, 188)となります。この値を255で割って[0, 1]の範囲にすると、おおよそ(0.737254902, 0, 0.737254902)になります。これは、頂点カラーに使用している (0.5, 0.0, 0.5) と同じではありません。この理由は、色空間と関係があります。

ほとんどのモニターはsRGBと呼ばれる色空間を使用しています。私たちのサーフェスは（`surface.get_preferred_format()`から何が返されるかによりますが）sRGBテクスチャフォーマットを使用している可能性が高いです。sRGBフォーマットは、実際の明るさではなく、相対的な明るさに従って色を保存します。この理由は、私たちの目が光を直線的に認識できないからです。私たちは、明るい色よりも暗い色の方が、より多くの違いを感じるのです。

次の式で正しい色の近似値を得ることができます：`srgb_color = (rgb_color / 255) ^ 2.2`. これをRGB値(188, 0, 188)で実行すると、(0.511397819, 0.0, 0.511397819)となります。(0.5, 0.0, 0.5)と少しずれていますね。テクスチャはデフォルトでsRGBとして保存されるため、頂点カラーと同じように色の不正確さに悩まされることがないからです。テクスチャについては、次のレッスンで説明します。

## 課題
頂点バッファとインデックスバッファを使って、先ほど作ったものよりも複雑な形状（3つ以上の三角形）を作ってみてください。スペースキーで2つのバッファを切り替えてください。
[コードを確認する](https://github.com/sotrh/learn-wgpu/tree/master/code/beginner/tutorial4-buffer/)