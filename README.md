# Sven Coop Hand Remover

![image](https://github.com/user-attachments/assets/ee91a629-e626-48ce-b050-07d27f261d59)

Just a small tool I made for myself, inspired by my friend Floober, in order to remove all hands from viewmodels in sven coop!

The idea is to improve immersion for custom player models by simply not having hands.

Thank you to [Toodles2You](https://github.com/Toodles2You/halflife-tools) for the awesome decomp application.

## How it works

Using the vast repository of existing view models from [wootguy's map db download](https://wootguy.github.io/scmapdb/), I can directly edit every viewmodel to make the hands invisbile with texture overrides.

It essentially scans for textures and smds that contain certain strings and explodes them, here's some example output.

```
Decompiling D:\SteamLibrary\steamapps\common\Sven Co-op\svencoop_downloads\models\ragemap2023\kezaeiv\knife\v_knife.mdl...
        Exploded glove_glow.bmp
        Exploded rubberglovechrome.bmp
        SMD Removal: hev_hands
                Exploded rubberglovechrome.bmp
                Exploded remap1_000_000_255.bmp
                Exploded remap3_000_000_255.bmp
                Exploded remap0_000_255_255.bmp
                Exploded remap2_000_000_255.bmp
                Exploded chrome2.bmp
                Exploded glove_glow.bmp
Decompiling D:\SteamLibrary\steamapps\common\Sven Co-op\svencoop_downloads\models\tb_disco\triguncross\v_rpg.mdl...
        Exploded gordon_glove.bmp
        Exploded rubberglovechrome.bmp
        SMD Removal: v_gordon_hands_reference
                Exploded rubberglovechrome.bmp
                Exploded gordon_glove.bmp
                Exploded gordon_sleeve.bmp
```

It doesn't need to know the chrome names or special extra textures added to the hands. Since the original smd had the string "hands", it nuked all of them.

## Limitations

- Non-english models are mostly ignored.
- Textures and meshs that are shared between guns and hands get nuked. It's overzealous because otherwise you'd see hovering ghost hands.
- Several models fail to decompile, this is due to me using an external decompiling app, I'm too lazy to write my own decompiler, sorry! These are ignored during compilation.
- This is a build-time hand removal for Sven Coop. It needs to be re-ran anytime a new map with new viewmodels are created to create a new "mega pack" of removed hands.
- Many animations make assumptions with the hands covering some tweening. Now magazines happily do jank in broad view. Hurting the artist's intention! (Sorry!!)
- It has incorrect casing from the original resources, due to studiomdl naturally stripping all casing. Shouldn't matter unless you're doing map development and accidentally include it into your map. Otherwise you might cause a problem with conflicting casing of similar assets.

## Usage

It's a console app with included help, I do not intend for people to use it directly. This repository is mainly me writing notes about my jank tool I made. Read the top of Program.cs if you feel like you really need to run this.

## Installation

To install the hand removal pack, download the large releases and extract it to your svencoop_addon directory.
