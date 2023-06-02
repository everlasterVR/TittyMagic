# TittyMagic Virt-A-Mate Plugin

Improve breast physics with automatic settings, dynamic adjustments and complete customization.

This plugin is superseded by Naturalis: https://hub.virtamate.com/resources/naturalis.33647

**Download**: https://hub.virtamate.com/resources/tittymagic.4067/

**Support me at**: https://patreon.com/everlaster

## Development

### Dependencies

Dotnet and DLL's: see [TittyMagic.csproj](TittyMagic.csproj).

#### vam-collider-editor

[vam-collider-editor branch "visualizer"](https://github.com/everlasterVR/vam-collider-editor/tree/visualizer) is set up as a submodule.

```
git submodule init
```

#### Morphs

Morphs used by the plugin aren't currently in any repository, but can unpacked from the latest release.

Morphs location must be `Custom/Atom/Person/Morphs/female/everlaster/TittyMagic_dev`.

### Code style

The project includes a comprehensive .editorconfig. Note that the `vam-collider-editor` dir has its own .editorconfig.

## License

[MIT](LICENSE)

Credit to

- Acidbubbles, ProjectCanyon and via5 for ColliderEditor plugin which I repurposed and integrated to TittyMagic for collider visualization
- VeeRifter for the original idea behind chest rotation based morph adjustment (BreastAutoGravity plugin)
- nyaacho for how to automatically detect any breast morph (MorphMassManager v10 plugin)
