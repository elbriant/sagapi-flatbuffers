# Sagapi FlatBuffers - Generated Schemas

**[WARNING] Do not edit files in this branch manually.**
This is an orphan branch managed automatically by GitHub Actions. All files here are generated from the `main` branch extraction pipeline.

## Directory Structure

This branch contains the generated FlatBuffers schemas for Arknights data decoding.

* `/rawfbs/`: Contains the raw, unbundled `.fbs` files directly extracted from the DummyDlls by the C# parser. Categorized by server (`global/`, `cn/`, `tw/`).
* `/[server_name]/`: Contains the final, bundled monolithic `.fbs` schemas ready to be used with the `flatc` compiler to decode game `.bytes` into JSON.

## Supported Servers
* `global` (com.YoStarJP.Arknights)
* ~~`cn` (com.hypergryph.arknights)~~ not yet implemented
* `tw` (tw.txwy.and.arknights)

To update these files, trigger the `Update FlatBuffers` workflow in the Actions tab of the `main` branch.
