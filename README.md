# Mitsuba2SeeSharp

Simple tool to convert a big subset of mitsuba scene files to the SeeSharp renderer

## Links

 - [SeeSharp](https://github.com/pgrit/SeeSharp)
 - [Mitsuba](https://github.com/mitsuba-renderer/mitsuba2)
 - [TinyParser-Mitsuba](https://github.com/PearCoding/TinyParser-Mitsuba)

## Supported

 - Mitsuba version 0.4+ and 2.0+
 - Most shapes
   - All mesh files (`.serialized`, `.obj` and `.ply`)
   - Rectangle shapes
 - Single hierarchy bsdfs (no `blendbsdf` etc)
   - Note, `twosided` is ignored and the inner bsdf is used instead
   - All except `diffuse` bsdfs are mapped to generic SeeSharp material (which is a Disney/Principled bsdf)
 - A subset of emitters
   - `area`
   - `envmap`
 - Bitmap images
 - Perspective camera

## Intention

This project is used in research which does not require most of Mitsuba supported objects,
but we might extend the list of features if SeeSharp itself is ready to support it in the future.
If you are using this tool and require a specific feature, feel free to create an issue or
drop a pull request with your own implementation.
