Thank you for downloading this package! :)

This package provide a method to render volumetric laser beam.
It is extreme fast since all beams could be drawn in a single draw call. 
This solution is practical for mobile platform since we don't use complicated shader calculation to get "volumetric appearance".

All you need is class "VolumetricLaserBeam" in script "VolumetricLaserBeam.cs".
Only three member function you need call before render volumetric laser beam.
  => Call PreAlloc() at first of start. Make sure prealloc enough slot for usage.
  => Call Begin() to prepare generating beams.
  => Call GenerateBeam() with property of a single beam as parameters.
  => Call End() after all beams are generated.
Sample scene and full source code included.
"Demo.cs" serve as a reference example about how to use volumetric laser beam script.

========================================================================================================================================
Version 1.2 upgrade enhance performance by remove heavy garbage collection in deprecated "EndBatch()" call.

If you like it, please give us a 5 star review on asset store. We need your support and will keep moving !
Any suggestion or improvement you want, please contact qq_d_y@163.com.