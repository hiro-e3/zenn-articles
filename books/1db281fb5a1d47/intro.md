---
title: "Introduction"
---

本書は[Learn Wgpu](https://sotrh.github.io/learn-wgpu/)の非公式日本語訳です。
https://sotrh.github.io/learn-wgpu/

## wgpuとは？
[wgpu](https://github.com/gfx-rs/wgpu)は[WebGPU API](https://gpuweb.github.io/gpuweb/)仕様のRust実装です。WebGPUは、GPU for the Web Community Groupによって公開された仕様です。Webコードが安全で信頼性の高い方法でGPU機能にアクセスできるようにすることを目的としています。これはVulkan APIを模倣し、ホストハードウェアが使用しているAPI（DirectX、Metal、Vulkanなど）に変換することで実現されています。

Wgpuはまだ開発中であり、このドキュメントの一部は変更される可能性があります。

## 何故Rustなのか？
wgpuはC/C++のコードを書く為のCバインディングがあり、Cのインターフェースを持つ他言語も使うことができます。それはそれとして、wgpuはRustで書かれています。つまり、何の苦労もなく扱える便利なRustバインディングを持っているということです。それに加えて、Rustで書くことを楽しんでいます。

このチュートリアルを使う前に、Rustの構文についてあまり詳しく説明しないので、ある程度慣れている必要があります。もし、Rustにあまり慣れていない場合は、[Rust チュートリアル](https://www.rust-lang.org/ja/learn)をご覧ください。また、[Cargo](https://doc.rust-lang.org/cargo/)にも慣れている必要があります。
