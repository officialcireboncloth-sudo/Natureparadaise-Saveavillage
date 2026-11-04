# Generated cow sign decal

The user requested an image-generated replacement for the manually modeled cow silhouette. Created with built-in image_gen (not a CLI/API fallback); exact generation and refinement prompts are retained in Generation.json. Cow_Generated_Master.png is the selected original image, including its original alpha. Do not redraw or trace its anatomy into the prior procedural silhouette.

Native GIMP 3 pack_emblems_gimp.py only resizes and packs the selected RGBA image at 384 x 256, centered in a transparent 512 x 256 shared emblem page. It preserves the image shape and alpha. NP_CozyEmblems_01_BaseColor.xcf is editable. Runtime output: Textures/Shared/Cozy/NP_CozyEmblems_01_BaseColor.png; slot metadata: NP_CozyEmblemSlots_01.json. Existing building atlas pages 01/02 are unchanged.

The BarnExteriorCozy builder uses a two-triangle plane, correct image orientation and a slight offset in front of the sign, retained in every LOD. Its material NP_CozyEmblems_01 uses the existing Cozy shader with alpha cutout, no normal/mask lookup, mipmaps and ASTC 6x6 on Android/iPhone. This adds one shared material slot per static barn LOD. The sign and wheat remain 3D. Unity setup validates both transparent exterior and opaque cow pixels, then tests building restoration and access normally.

Rebuild the image page in native GIMP via python-fu-eval, setting __file__ to the absolute pack_emblems_gimp.py path before exec. Then rebuild BarnExterior_Cozy_Master.blend and FBXs using its builder; run Nature Paradise > Art > Apply Cozy Player Barn Exterior in Unity Edit Mode.
